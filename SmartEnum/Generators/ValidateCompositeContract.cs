namespace SmartEnum.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public sealed partial class SmartEnumGenerator
{
	private static void CompositeError(GenerationContext output, ISymbol symbol, string message, Location? location = null, string id = "SMARTENUM015")
		=> output.ReportDiagnostic(Diagnostic.Create(ContractRule(id), location ?? symbol.Locations.FirstOrDefault(), message));

	private static bool ValidateCompositeContract(GenerationContext output, Compilation compilation, INamedTypeSymbol root, INamedTypeSymbol[] members, INamedTypeSymbol attribute, bool parent)
	{
		bool valid = true;
		void Reject(ISymbol symbol, string message, Location? location = null, string id = "SMARTENUM015") { CompositeError(output, symbol, message, location, id); valid = false; }

		if (parent && !root.IsSealed) Reject(root, "SmartEnumParent must be sealed.", id: "SMARTENUM017");
		if (!parent && !root.IsAbstract) Reject(root, "The SmartEnum root must be abstract.", id: "SMARTENUM001");
		if (HasExternalBase(root) && !HasSharedDataConstructor(root, StrategyData(root)) && ExternalBaseConstructor(root, StrategyData(root)) is null)
			Reject(root, "The ordinary base requires an unambiguous accessible constructor whose parameters match flattened properties by normalized name and type. Read-only inherited properties must be restored by that constructor, or by a protected root data constructor.", id: "SMARTENUM019");
		if (parent)
		{
			AttributeData parentAttribute = root.GetAttributes().First(item => item.AttributeClass?.ToDisplayString() == "SmartEnum.SmartEnumParentAttribute");
			if (parentAttribute.ConstructorArguments.Length != 1 || parentAttribute.ConstructorArguments[0].Value is not (0 or 1 or 2)) Reject(root, "Unknown parent flattening strategy.", id: "SMARTENUM020");
		}

		List<TransferKey> allKeys = new();
		foreach (INamedTypeSymbol type in members.OrderBy(type => GetPath(type, root).Count))
		{
			output.CancellationToken.ThrowIfCancellationRequested();
			if (type.IsStatic || type.IsFileLocal) Reject(type, "Transfer contracts require non-file-local instance classes.", id: "SMARTENUM012");
			if (!type.DeclaringSyntaxReferences.All(reference => reference.GetSyntax() is TypeDeclarationSyntax declaration && declaration.Modifiers.Any(SyntaxKind.PartialKeyword)))
				Reject(type, "Generated transfer classes must be partial.", id: "SMARTENUM002");
			if (type.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is TypeDeclarationSyntax { ParameterList: not null }))
				Reject(type, "Primary constructors require explicit construction integration.", id: "SMARTENUM018");
			for (INamedTypeSymbol? enclosing = type.ContainingType; enclosing is not null; enclosing = enclosing.ContainingType)
				if (!enclosing.DeclaringSyntaxReferences.All(reference => reference.GetSyntax() is TypeDeclarationSyntax declaration && declaration.Modifiers.Any(SyntaxKind.PartialKeyword))) Reject(enclosing, "Enclosing declarations must be partial.", id: "SMARTENUM002");
			if (!parent && (MapToRoot(type, root) is not { } mapped || !SatisfiesConstraints(mapped, compilation))) Reject(type, "Variant type arguments must be inferable from the root and satisfy its constraints.", id: "SMARTENUM021");
			if (!compilation.IsSymbolAccessibleWithin(type, root)) Reject(type, "A variant must be accessible from its root mapper.", id: "SMARTENUM022");
			AttributeData[] attributes = GetAttributes(type, attribute);
			for (int index = 0; index < attributes.Length; index++)
			{
				AttributeData key = attributes[index];
				Location? location = key.ApplicationSyntaxReference?.GetSyntax().GetLocation();
				if (key.ConstructorArguments.Length == 3 && (key.ConstructorArguments[0].Value is not string keyName || !SyntaxFacts.IsValidIdentifier(keyName)))
				{
					Reject(type, "A discriminator requires a valid field name.", location, "SMARTENUM003");
					continue;
				}

				if (key.ConstructorArguments.Length != 3 || key.ConstructorArguments[0].Value is not string name || !SyntaxFacts.IsValidIdentifier(name)
					|| key.ConstructorArguments[1].Value is not (0 or 1 or 2) || key.ConstructorArguments[2].Value is not (0 or 1 or 2))
				{
					Reject(type, "Each discriminator requires a valid name, flattening strategy and missing-branch policy.", location);
					continue;
				}

				if (!SyntaxFacts.IsValidIdentifier(FlattenedName(name))) Reject(type, "A discriminator name must remain a valid identifier after normalization.", location, "SMARTENUM003");

				if (key.ConstructorArguments[2].Value is 2 && index != 0) Reject(type, "FirstBranch is only valid on the first attribute in a discriminator group.", location, id: "SMARTENUM023");
				allKeys.Add(new(type, key, index));
			}

			if (attributes.Where(item => item.ConstructorArguments.Length == 3).GroupBy(item => item.ConstructorArguments[0].Value as string).Any(group => group.Count() > 1)) Reject(type, "Discriminator names must be unique within a hierarchy group.", id: "SMARTENUM009");

			if (!parent && !SameDefinition(type, root))
			{
				TransferKey[] applicable = allKeys.Where(key => KeyOwner(type, key) is { } owner && SameDefinition(owner, type)).ToArray();
				if (applicable.Length != 1)
				{
					Reject(type, "Each derived class must have exactly one discriminator level.", id: "SMARTENUM014");
				}
				else
				{
					TransferKey key = applicable[0];
					IFieldSymbol? field = type.GetMembers(key.Name).OfType<IFieldSymbol>().FirstOrDefault();
					if (field is null) Reject(type, $"Declare public const {TypeName(key.Attribute.AttributeClass!.TypeArguments[0])} {key.Name} for this discriminator level.", id: "SMARTENUM004");
					else if (!field.IsConst) Reject(field, "Discriminator fields must be constant.", id: "SMARTENUM006");
					else if (field.DeclaredAccessibility != Accessibility.Public) Reject(field, "Discriminator fields must be public.", id: "SMARTENUM005");
					else if (!SymbolEqualityComparer.Default.Equals(field.Type, key.Attribute.AttributeClass!.TypeArguments[0])) Reject(field, $"Discriminator type must be {TypeName(key.Attribute.AttributeClass!.TypeArguments[0])}.", id: "SMARTENUM007");
				}
			}

			if (!parent && type.IsAbstract && !members.Any(candidate => candidate.BaseType is not null && SameDefinition(candidate.BaseType, type))) Reject(type, "An abstract branch requires a concrete descendant.", id: "SMARTENUM010");
			if (!parent && !type.IsAbstract && members.Any(candidate => candidate.BaseType is not null && SameDefinition(candidate.BaseType, type))) Reject(type, "Intermediate transfer hierarchy classes must be abstract.", id: "SMARTENUM001");
			if (type.GetMembers().Concat(ExternalBaseMembers(type)).Any(member => HasDataAttribute(member, "SmartEnumIgnoreAttribute") && member is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true })) Reject(type, "Required members cannot be excluded from transfer construction.", id: "SMARTENUM024");
			TransferMember[] data = StrategyData(type);
			if (data.Count(member => HasDataAttribute(member.Property, "SmartEnumIdentityAttribute")) > 1) Reject(type, "Only one identity property may be declared on each transfer level.", id: "SMARTENUM025");
			foreach (TransferMember member in data)
			{
				if (member.Name == root.Name + "Flattened" || member.Name == type.Name + "Flattened") Reject(member.Property, "A transfer member conflicts with its flattened class name.", id: "SMARTENUM026");
				AttributeData? accessor = member.Property.GetAttributes().FirstOrDefault(item => item.AttributeClass?.ToDisplayString() == "SmartEnum.SmartEnumPrivateAccessorAttribute");
				if (!member.Inherited && member.Property is IPropertySymbol property && (property.GetMethod is null || (HasImplementedGetter(property) && property.SetMethod is null && accessor is null)))
					Reject(member.Property, "Transfer properties require a readable getter; an implemented getter without a setter requires SmartEnumPrivateAccessor.", id: "SMARTENUM027");
				if (member.Inherited && member.Property is IPropertySymbol inherited && (inherited.GetMethod is null || !BaseAccessible(inherited.GetMethod, type)))
					Reject(type, $"Inherited property '{inherited.Name}' requires a getter accessible from the hierarchy root.", id: "SMARTENUM027");
				if (accessor is not null && (member.Storage is not IFieldSymbol storage || storage.IsStatic || storage.IsConst)) Reject(member.Property, "SmartEnumPrivateAccessor must name an instance field in the same class.", accessor.ApplicationSyntaxReference?.GetSyntax().GetLocation(), id: "SMARTENUM028");
				if (!SyntaxFacts.IsValidIdentifier(member.Name) || member.Type.IsRefLikeType || member.Type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer
					|| member.Property is IPropertySymbol { ReturnsByRef: true } or IPropertySymbol { ReturnsByRefReadonly: true })
				{
					Reject(member.Property, "Transfer members require valid normalized names and ordinary value or reference types.", id: "SMARTENUM029");
				}

				if (!IsVisibleInPublicApi(member.Type, root) || !compilation.IsSymbolAccessibleWithin(member.Type, root)) Reject(member.Property, "The transfer member type must be accessible to the generated public model and mapper.", id: "SMARTENUM030");
				if (HasDataAttribute(member.Property, "SmartEnumIdentityAttribute") && new[] { "id", "guid", "uuid", "key" }.Contains(ForeignKeyName(member).ToLowerInvariant()))
					output.ReportDiagnostic(Diagnostic.Create(IdentityWarning, member.Property.Locations.FirstOrDefault(), ForeignKeyName(member)));
			}

			if (data.GroupBy(member => member.Storage, SymbolEqualityComparer.Default).Any(group => group.Count() > 1)) Reject(type, "Multiple properties cannot share a private accessor field.", id: "SMARTENUM031");
			foreach (string name in new[] { "Flatten", "FlattenHierarchy", "FlattenedData", "ConstructUnvalidated" }.Concat(data.Select(member => "SmartEnumRead" + member.Name)))
				if (type.GetMembers(name).Any() || ExternalBaseMembers(type).Any(member => member.Name == name && BaseAccessible(member, type))) Reject(type, $"Member '{name}' conflicts with the generated transfer API.", id: "SMARTENUM032");
			if ((type.ContainingType?.GetTypeMembers(type.Name + "Flattened", type.Arity) ?? type.ContainingNamespace.GetTypeMembers(type.Name + "Flattened", type.Arity)).Any()) Reject(type, "The generated flattened type name is already in use.", id: "SMARTENUM033");
			foreach (IMethodSymbol constructor in type.InstanceConstructors.Where(item => !item.IsImplicitlyDeclared))
			{
				TransferMember[] pathData = StrategyData(type, root);
				if (type.IsAbstract && constructor.Parameters.Select(parameter => parameter.Type).SequenceEqual(pathData.Select(member => member.Type), SymbolEqualityComparer.Default)
					&& (constructor.DeclaredAccessibility != Accessibility.Protected || constructor.Parameters.Any(parameter => parameter.RefKind != RefKind.None)))
				{
					Reject(type, "An existing shared data constructor must be protected and take values without ref, in or out.", id: "SMARTENUM034");
				}

				if (!type.IsAbstract && constructor.Parameters.Any(parameter => parameter.Type.Name.EndsWith("Flattened", StringComparison.Ordinal))) Reject(type, "An explicit transfer constructor conflicts with generated construction.", id: "SMARTENUM035");
			}
		}

		if (allKeys.Any(key => key.Strategy == 2) && allKeys.Any(key => key.Strategy != 2)) Reject(root, "Separate must be selected for every level of a hierarchy; Flat and Nested can be mixed with each other.", id: "SMARTENUM036");
		if (allKeys.Count > 0 && allKeys.All(key => key.Strategy == 2))
		{
			string[] keyNames = allKeys.Select(key => FlattenedName(key.Name)).ToArray();
			if (keyNames.Distinct().Count() != keyNames.Length) Reject(root, "Separate discriminator names must be unique after normalization.", id: "SMARTENUM009");
			if (members.SelectMany(StrategyData).Any(member => keyNames.Contains(member.Name))) Reject(root, "A transfer property conflicts with a discriminator after normalization.", id: "SMARTENUM037");
			if (members.Any(type => keyNames.Contains(type.Name + "Flattened"))) Reject(root, "A discriminator conflicts with a flattened class name.", id: "SMARTENUM038");
		}

		foreach (IGrouping<string, TransferMember> group in members.SelectMany(StrategyData).GroupBy(member => member.Name))
			if (group.Count() > 1) Reject(root, $"Transfer property '{group.Key}' is declared more than once after PascalCase normalization.", id: "SMARTENUM011");
		if (!valid) return false;
		foreach (INamedTypeSymbol branch in members.Where(type => type.IsAbstract))
		{
			INamedTypeSymbol[] children = members.Where(type => type.BaseType is not null && SameDefinition(type.BaseType, branch)).ToArray();
			if (children.Length == 0) continue;
			TransferKey key = allKeys.Single(item => KeyOwner(children[0], item) is { } owner && SameDefinition(owner, children[0]));
			foreach (IGrouping<object?, INamedTypeSymbol> duplicates in children.GroupBy(type => type.GetMembers(key.Name).OfType<IFieldSymbol>().First().ConstantValue))
			{
				if (duplicates.Count() > 1) Reject(root, "Sibling branches must use distinct discriminator constants.", id: "SMARTENUM013");
			}
		}

		return valid;
	}

	private static bool ValidateTransferLayout(GenerationContext output, INamedTypeSymbol owner, TransferNode model, IEnumerable<TransferPlan> plans)
	{
		bool valid = true;
		TransferPlan[] items = plans.ToArray();
		if (items.Select(plan => plan.Model).Distinct().GroupBy(node => node.Type).Any(group => group.Count() > 1))
		{
			CompositeError(output, owner, "Referenced hierarchies produce the same nested flattened type name.", id: "SMARTENUM039");
			valid = false;
		}

		foreach (TransferNode node in TransferNodes(model).Concat(items.SelectMany(plan => TransferNodes(plan.Model))).Distinct())
		{
			foreach (TransferSlot slot in node.Slots)
			{
				if (!SyntaxFacts.IsValidIdentifier(slot.Name))
				{
					CompositeError(output, owner, "A discriminator, data member or foreign key has an invalid normalized property name.", id: "SMARTENUM040");
					valid = false;
				}

				ITypeSymbol? type = slot.Data?.Type ?? slot.Key?.Attribute.AttributeClass?.TypeArguments[0];
				if (type is not null && !IsVisibleInPublicApi(type, owner))
				{
					CompositeError(output, owner, $"Transfer property '{slot.Name}' has a type that is not visible in the owner's public flattened model.", id: "SMARTENUM041");
					valid = false;
				}
			}

			IEnumerable<string> names = node.Slots.Select(slot => slot.Name).Concat(node.Children.Select(child => child.Name)).Concat(node.Children.Select(child => child.Type.Substring(child.Type.LastIndexOf('.') + 1).TrimStart('@')));
			string typeName = node.Type.Substring(node.Type.LastIndexOf('.') + 1).TrimStart('@');
			if (names.GroupBy(name => name).Any(group => group.Count() > 1) || names.Contains(typeName))
			{
				CompositeError(output, owner, $"Flattened members, nested type names or foreign keys collide in '{node.Type}'. Rename the conflicting member or specify a distinct foreignKeyName.", id: "SMARTENUM042");
				valid = false;
			}
		}

		IEnumerable<string> callbacks = items.SelectMany(plan => CompositeConstructionCallbacks(plan)).Select(TransferCallback);
		if (callbacks.GroupBy(name => name).Any(group => group.Count() > 1))
		{
			CompositeError(output, owner, "Referenced hierarchies have colliding mapping callback names.", id: "SMARTENUM043");
			valid = false;
		}

		foreach (TransferPlan plan in items)
		{
			IEnumerable<string> flattenNames = plan.FlattenCallbacks.Select(TransferCallback).Concat(plan.KeyCallbacks.Select(key => SeparateKeyCallback(key.Attribute)));
			if (flattenNames.GroupBy(name => name).Any(group => group.Count() > 1))
			{
				CompositeError(output, owner, "Data and discriminator fallback parameter names collide after normalization.", id: "SMARTENUM044");
				valid = false;
			}
		}

		return valid;
	}
}