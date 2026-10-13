namespace SmartEnum.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public sealed partial class SmartEnumGenerator
{
	private static void CompositeError(SourceProductionContext output, ISymbol symbol, string message, Location? location = null)
		=> output.ReportDiagnostic(Diagnostic.Create(StrategyError, location ?? symbol.Locations.FirstOrDefault(), message));

	private static bool ValidateCompositeContract(SourceProductionContext output, Compilation compilation, INamedTypeSymbol root, INamedTypeSymbol[] members, INamedTypeSymbol attribute, bool parent)
	{
		bool valid = true;
		void Reject(ISymbol symbol, string message, Location? location = null) { CompositeError(output, symbol, message, location); valid = false; }

		if (parent ? !root.IsSealed : !root.IsAbstract) Reject(root, parent ? "SmartEnumParent must be sealed." : "The SmartEnum root must be abstract.");
		if (root.BaseType?.SpecialType != SpecialType.System_Object) Reject(root, "Composite transfer roots must derive directly from object.");
		if (parent)
		{
			AttributeData parentAttribute = root.GetAttributes().First(item => item.AttributeClass?.ToDisplayString() == "SmartEnum.SmartEnumParentAttribute");
			if (parentAttribute.ConstructorArguments.Length != 1 || parentAttribute.ConstructorArguments[0].Value is not (0 or 1 or 2)) Reject(root, "Unknown parent flattening strategy.");
		}

		List<TransferKey> allKeys = new();
		foreach (INamedTypeSymbol type in members.OrderBy(type => GetPath(type, root).Count))
		{
			output.CancellationToken.ThrowIfCancellationRequested();
			if (type.IsStatic || type.IsRecord || type.ContainingType is not null || type.Arity > 0) Reject(type, "Composite transfer contracts require namespace-level, non-generic ordinary classes.");
			if (!type.DeclaringSyntaxReferences.All(reference => reference.GetSyntax() is ClassDeclarationSyntax declaration && declaration.Modifiers.Any(SyntaxKind.PartialKeyword) && declaration.ParameterList is null))
				Reject(type, "Generated transfer classes must be partial and cannot declare primary constructors.");
			if (!compilation.IsSymbolAccessibleWithin(type, root)) Reject(type, "A variant must be accessible from its root mapper.");
			AttributeData[] attributes = GetAttributes(type, attribute);
			for (int index = 0; index < attributes.Length; index++)
			{
				AttributeData key = attributes[index];
				Location? location = key.ApplicationSyntaxReference?.GetSyntax().GetLocation();
				if (key.ConstructorArguments.Length != 3 || key.ConstructorArguments[0].Value is not string name || !SyntaxFacts.IsValidIdentifier(name)
					|| key.ConstructorArguments[1].Value is not (0 or 1 or 2) || key.ConstructorArguments[2].Value is not (0 or 1 or 2))
				{
					Reject(type, "Each discriminator requires a valid name, flattening strategy and missing-branch policy.", location);
					continue;
				}

				if (key.ConstructorArguments[2].Value is 2 && index != 0) Reject(type, "FirstBranch is only valid on the first attribute in a discriminator group.", location);
				allKeys.Add(new(type, key, index));
			}

			if (!parent && !SameDefinition(type, root))
			{
				TransferKey[] applicable = allKeys.Where(key => KeyOwner(type, key) is { } owner && SameDefinition(owner, type)).ToArray();
				if (applicable.Length != 1)
				{
					Reject(type, "Each derived class must have exactly one discriminator level; restarted groups must not overlap an ancestor's remaining levels.");
				}
				else
				{
					TransferKey key = applicable[0];
					IFieldSymbol? field = type.GetMembers(key.Name).OfType<IFieldSymbol>().FirstOrDefault();
					if (field is null || !field.IsConst || field.DeclaredAccessibility != Accessibility.Public || !SymbolEqualityComparer.Default.Equals(field.Type, key.Attribute.AttributeClass!.TypeArguments[0]))
						Reject(type, $"Declare public const {TypeName(key.Attribute.AttributeClass!.TypeArguments[0])} {key.Name} for this discriminator level.");
				}
			}

			if (!parent && type.IsAbstract && !members.Any(candidate => candidate.BaseType is not null && SameDefinition(candidate.BaseType, type))) Reject(type, "An abstract branch requires a concrete descendant.");
			if (!parent && !type.IsAbstract && members.Any(candidate => candidate.BaseType is not null && SameDefinition(candidate.BaseType, type))) Reject(type, "Intermediate transfer hierarchy classes must be abstract.");
			if (type.GetMembers().Any(member => HasDataAttribute(member, "SmartEnumIgnoreAttribute") && member is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true })) Reject(type, "Required members cannot be excluded from transfer construction.");
			TransferMember[] data = StrategyData(type);
			if (data.Count(member => HasDataAttribute(member.Property, "SmartEnumIdentityAttribute")) > 1) Reject(type, "Only one identity property may be declared on each transfer level.");
			foreach (TransferMember member in data)
			{
				AttributeData? accessor = member.Property.GetAttributes().FirstOrDefault(item => item.AttributeClass?.ToDisplayString() == "SmartEnum.SmartEnumPrivateAccessorAttribute");
				if (member.Property is IPropertySymbol property && (property.GetMethod is null || (HasImplementedGetter(property) && property.SetMethod is null && accessor is null)))
					Reject(member.Property, "Transfer properties require a readable getter; an implemented getter without a setter requires SmartEnumPrivateAccessor.");
				if (accessor is not null && (member.Storage is not IFieldSymbol storage || storage.IsStatic || storage.IsConst)) Reject(member.Property, "SmartEnumPrivateAccessor must name an instance field in the same class.", accessor.ApplicationSyntaxReference?.GetSyntax().GetLocation());
				if (!SyntaxFacts.IsValidIdentifier(member.Name) || member.Type.IsRefLikeType || member.Type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer
					|| member.Property is IPropertySymbol { ReturnsByRef: true } or IPropertySymbol { ReturnsByRefReadonly: true })
				{
					Reject(member.Property, "Transfer members require valid normalized names and ordinary value or reference types.");
				}

				if (!IsVisibleInPublicApi(member.Type, root) || !compilation.IsSymbolAccessibleWithin(member.Type, root)) Reject(member.Property, "The transfer member type must be accessible to the generated public model and mapper.");
				if (HasDataAttribute(member.Property, "SmartEnumIdentityAttribute") && new[] { "id", "guid", "uuid", "key" }.Contains(ForeignKeyName(member).ToLowerInvariant()))
					output.ReportDiagnostic(Diagnostic.Create(IdentityWarning, member.Property.Locations.FirstOrDefault(), ForeignKeyName(member)));
			}

			if (data.GroupBy(member => member.Storage, SymbolEqualityComparer.Default).Any(group => group.Count() > 1)) Reject(type, "Multiple properties cannot share a private accessor field.");
			foreach (string name in new[] { "Flatten", "FlattenedData", "ConstructUnvalidated", "MapFromFlattenedDataUnvalidated" }.Concat(data.Select(member => "SmartEnumRead" + member.Name)))
				if (type.GetMembers(name).Any()) Reject(type, $"Member '{name}' conflicts with the generated transfer API.");
			if (type.ContainingNamespace.GetTypeMembers(type.Name + "Flattened", type.Arity).Any()) Reject(type, "The generated flattened type name is already in use.");
			foreach (IMethodSymbol constructor in type.InstanceConstructors.Where(item => !item.IsImplicitlyDeclared))
			{
				TransferMember[] pathData = StrategyData(type, root);
				if (type.IsAbstract && constructor.Parameters.Select(parameter => parameter.Type).SequenceEqual(pathData.Select(member => member.Type), SymbolEqualityComparer.Default)
					&& (constructor.DeclaredAccessibility != Accessibility.Protected || constructor.Parameters.Any(parameter => parameter.RefKind != RefKind.None)))
				{
					Reject(type, "An existing shared data constructor must be protected and take values without ref, in or out.");
				}

				if (!type.IsAbstract && constructor.Parameters.Any(parameter => parameter.Type.Name.EndsWith("Flattened", StringComparison.Ordinal))) Reject(type, "An explicit transfer constructor conflicts with generated construction.");
			}
		}

		if (allKeys.Any(key => key.Strategy == 2) && allKeys.Any(key => key.Strategy != 2)) Reject(root, "Separate must be selected for every level of a hierarchy; Flat and Nested can be mixed with each other.");
		if (allKeys.Any(key => key.Strategy == 2) && allKeys.Any(key => !SameDefinition(key.Group, root))) Reject(root, "Separate hierarchies declare all discriminator levels on their root.");
		foreach (IGrouping<string, TransferMember> group in members.SelectMany(StrategyData).GroupBy(member => member.Name))
			if (group.Count() > 1) Reject(root, $"Transfer property '{group.Key}' is declared more than once after PascalCase normalization.");
		if (!valid) return false;
		foreach (INamedTypeSymbol branch in members.Where(type => type.IsAbstract))
		{
			INamedTypeSymbol[] children = members.Where(type => type.BaseType is not null && SameDefinition(type.BaseType, branch)).ToArray();
			if (children.Length == 0) continue;
			TransferKey key = allKeys.Single(item => KeyOwner(children[0], item) is { } owner && SameDefinition(owner, children[0]));
			foreach (IGrouping<object?, INamedTypeSymbol> duplicates in children.GroupBy(type => type.GetMembers(key.Name).OfType<IFieldSymbol>().First().ConstantValue))
			{
				if (duplicates.Count() > 1) Reject(root, "Sibling branches must use distinct discriminator constants.");
			}
		}

		return valid;
	}

	private static bool ValidateTransferLayout(SourceProductionContext output, INamedTypeSymbol owner, TransferNode model, IEnumerable<TransferPlan> plans)
	{
		bool valid = true;
		TransferPlan[] items = plans.ToArray();
		if (items.Select(plan => plan.Model).Distinct().GroupBy(node => node.Type).Any(group => group.Count() > 1))
		{
			CompositeError(output, owner, "Referenced hierarchies produce the same nested flattened type name.");
			valid = false;
		}

		foreach (TransferNode node in TransferNodes(model).Concat(items.SelectMany(plan => TransferNodes(plan.Model))).Distinct())
		{
			foreach (TransferSlot slot in node.Slots)
			{
				if (!SyntaxFacts.IsValidIdentifier(slot.Name))
				{
					CompositeError(output, owner, "A discriminator, data member or foreign key has an invalid normalized property name.");
					valid = false;
				}

				ITypeSymbol? type = slot.Data?.Type ?? slot.Key?.Attribute.AttributeClass?.TypeArguments[0];
				if (type is not null && !IsVisibleInPublicApi(type, owner))
				{
					CompositeError(output, owner, $"Transfer property '{slot.Name}' has a type that is not visible in the owner's public flattened model.");
					valid = false;
				}
			}

			IEnumerable<string> names = node.Slots.Select(slot => slot.Name).Concat(node.Children.Select(child => child.Name)).Concat(node.Children.Select(child => child.Type.Substring(child.Type.LastIndexOf('.') + 1).TrimStart('@')));
			string typeName = node.Type.Substring(node.Type.LastIndexOf('.') + 1).TrimStart('@');
			if (names.GroupBy(name => name).Any(group => group.Count() > 1) || names.Contains(typeName))
			{
				CompositeError(output, owner, $"Flattened members, nested type names or foreign keys collide in '{node.Type}'. Rename the conflicting member or specify a distinct foreignKeyName.");
				valid = false;
			}
		}

		IEnumerable<string> callbacks = items.SelectMany(plan => CompositeConstructionCallbacks(plan)).Select(TransferCallback);
		if (callbacks.GroupBy(name => name).Any(group => group.Count() > 1))
		{
			CompositeError(output, owner, "Referenced hierarchies have colliding mapping callback names.");
			valid = false;
		}

		foreach (TransferPlan plan in items)
		{
			IEnumerable<string> flattenNames = plan.FlattenCallbacks.Select(TransferCallback).Concat(plan.KeyCallbacks.Select(key => SeparateKeyCallback(key.Attribute)));
			if (flattenNames.GroupBy(name => name).Any(group => group.Count() > 1))
			{
				CompositeError(output, owner, "Data and discriminator fallback parameter names collide after normalization.");
				valid = false;
			}
		}

		return valid;
	}
}