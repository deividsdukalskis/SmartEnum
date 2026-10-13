namespace SmartEnum.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public sealed partial class SmartEnumGenerator
{
	private static bool ValidateSeparateContract(SourceProductionContext output, Compilation compilation, INamedTypeSymbol root, INamedTypeSymbol[] members, INamedTypeSymbol attribute)
	{
		bool valid = true;
		void Reject(ISymbol symbol, string reason, Location? location = null)
		{
			output.ReportDiagnostic(Diagnostic.Create(StrategyError, location ?? symbol.Locations.FirstOrDefault(), reason));
			valid = false;
		}

		AttributeData[] keys = GetAttributes(root, attribute);
		if (!root.IsAbstract) Reject(root, "The SmartEnum root must be abstract.");
		if (root.BaseType?.SpecialType != SpecialType.System_Object) Reject(root, "A Separate hierarchy root must derive directly from object.");
		for (int index = 0; index < keys.Length; index++)
		{
			AttributeData key = keys[index];
			Location? location = key.ApplicationSyntaxReference?.GetSyntax().GetLocation();
			if (key.ConstructorArguments.Length != 3 || key.ConstructorArguments[1].Value is not int strategy || strategy != 2)
			{
				Reject(root, "A Separate hierarchy must select Separate on every discriminator level.", location);
				continue;
			}

			if (key.ConstructorArguments[0].Value is not string name || !SyntaxFacts.IsValidIdentifier(name)) Reject(root, "A discriminator must have a valid field name.", location);
			if (key.ConstructorArguments[2].Value is not int policy || policy < 0 || policy > 2) Reject(root, "Unknown missing-branch key policy.", location);
			else if (policy == 2 && index != 0) Reject(root, "FirstBranch is only valid on the first SmartEnum attribute.", location);
		}

		if (!valid) return false;
		string[] keyNames = keys.Select(key => FlattenedName((string)key.ConstructorArguments[0].Value!)).ToArray();
		if (keyNames.Distinct(StringComparer.Ordinal).Count() != keys.Length) Reject(root, "Discriminator names must remain unique after PascalCase normalization.");
		foreach (IGrouping<string, TransferMember> group in members.SelectMany(StrategyData).GroupBy(member => member.Name))
			if (group.Count() > 1) Reject(root, $"Transfer property '{group.Key}' is declared more than once in the hierarchy.");
		foreach (INamedTypeSymbol type in members)
		{
			output.CancellationToken.ThrowIfCancellationRequested();
			if (type.IsStatic || type.IsRecord || type.TypeKind != TypeKind.Class) Reject(type, "Separate domain declarations must be non-static classes.");
			if (type.ContainingType is not null) Reject(type, "Separate persistence types must currently be declared at namespace scope.");
			if (!type.DeclaringSyntaxReferences.All(reference => reference.GetSyntax() is ClassDeclarationSyntax declaration
				&& declaration.Modifiers.Any(SyntaxKind.PartialKeyword) && declaration.ParameterList is null))
			{
				Reject(type, "SmartEnum domain classes must be partial and cannot declare primary constructors.");
			}

			if (MapToRoot(type, root) is not INamedTypeSymbol mapped || !SatisfiesConstraints(mapped, compilation)) Reject(type, "Variant type arguments and constraints must be inferable from the root.");
			if (!compilation.IsSymbolAccessibleWithin(type, root)) Reject(type, "Concrete variants must be accessible from the root mapper.");
			if (!SameDefinition(type, root) && GetAttributes(type, attribute).Length > 0) Reject(type, "A Separate hierarchy uses discriminator attributes declared on its root.");
			List<INamedTypeSymbol> path = GetPath(type, root);
			int level = path.Count - 2;
			if (level >= keys.Length)
			{
				Reject(type, "The hierarchy exceeds its declared discriminator levels.");
			}
			else if (level >= 0)
			{
				string name = (string)keys[level].ConstructorArguments[0].Value!;
				IFieldSymbol? field = type.GetMembers(name).OfType<IFieldSymbol>().FirstOrDefault();
				if (field is null || !field.IsConst || field.DeclaredAccessibility != Accessibility.Public || !SymbolEqualityComparer.Default.Equals(field.Type, keys[level].AttributeClass!.TypeArguments[0]))
					Reject(type, $"Declare public const {TypeName(keys[level].AttributeClass!.TypeArguments[0])} {name} for this discriminator level.");
			}

			if (type.IsAbstract && !members.Any(candidate => candidate.BaseType is not null && SameDefinition(candidate.BaseType, type))) Reject(type, "An abstract hierarchy branch needs a concrete descendant.");
			foreach (TransferMember member in StrategyData(type))
			{
				AttributeData? accessor = member.Property.GetAttributes().FirstOrDefault(item => item.AttributeClass?.ToDisplayString() == "SmartEnum.SmartEnumPrivateAccessorAttribute");
				if (member.Property is IPropertySymbol property && HasImplementedGetter(property) && property.SetMethod is null && accessor is null)
					Reject(member.Property, "An implemented getter without a setter requires SmartEnumPrivateAccessor.", member.Property.DeclaringSyntaxReferences.First().GetSyntax().GetLocation());
				if (accessor is not null && (member.Storage is not IFieldSymbol storage || storage.IsStatic || storage.IsConst))
					Reject(member.Property, "SmartEnumPrivateAccessor must name an instance field declared in the same class.", accessor.ApplicationSyntaxReference?.GetSyntax().GetLocation());
				if (member.Type.IsRefLikeType || member.Type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer || member.Property is IPropertySymbol { ReturnsByRef: true } or IPropertySymbol { ReturnsByRefReadonly: true })
					Reject(member.Property, "Transfer data cannot use ref-like types, pointers, or by-ref properties.");
				if (!SyntaxFacts.IsValidIdentifier(member.Name) || keyNames.Contains(member.Name) || member.Name == root.Name + "Flattened" || member.Name == type.Name + "Flattened")
					Reject(member.Property, "The normalized property name conflicts with a discriminator or flattened type.");
				if (!IsVisibleInPublicApi(member.Type, root) || !compilation.IsSymbolAccessibleWithin(member.Type, root)) Reject(member.Property, "The transfer member type must be accessible to the root mapper.");
				AttributeData? identity = member.Property.GetAttributes().FirstOrDefault(item => item.AttributeClass?.ToDisplayString() == "SmartEnum.SmartEnumIdentityAttribute");
				if (identity is not null)
				{
					string name = identity.ConstructorArguments[0].Value as string ?? member.Property.Name;
					if (new[] { "id", "guid", "uuid", "key" }.Contains(name.ToLowerInvariant()))
						output.ReportDiagnostic(Diagnostic.Create(IdentityWarning, identity.ApplicationSyntaxReference?.GetSyntax().GetLocation(), name));
				}
			}

			TransferMember[] data = StrategyData(type, root);
			if (data.GroupBy(member => member.Name).Any(group => group.Count() > 1)) Reject(type, "Property names on a variant path must remain unique after normalization.");
			if (StrategyData(type).Where(member => member.Storage is IFieldSymbol).GroupBy(member => member.Storage, SymbolEqualityComparer.Default).Any(group => group.Count() > 1))
				Reject(type, "Multiple properties cannot claim the same private accessor field.");
			foreach (string name in new[] { "Flatten", "FlattenedData", "ConstructUnvalidated", "MapFromFlattenedDataUnvalidated" }.Concat(StrategyData(type).Select(member => "SmartEnumRead" + member.Name)))
				if (type.GetMembers(name).Any()) Reject(type, $"Member '{name}' conflicts with the generated Separate API.");
			foreach (IMethodSymbol constructor in type.InstanceConstructors.Where(constructor => !constructor.IsImplicitlyDeclared))
			{
				if (type.IsAbstract && constructor.Parameters.Select(parameter => parameter.Type).SequenceEqual(data.Select(member => member.Type), SymbolEqualityComparer.Default)
					&& (constructor.DeclaredAccessibility != Accessibility.Protected || constructor.Parameters.Any(parameter => parameter.RefKind != RefKind.None)))
				{
					Reject(type, "An existing shared data constructor must be protected and accept values without ref, in or out.");
				}

				if (!type.IsAbstract && constructor.Parameters.Length > 0 && constructor.Parameters[0].Type.Name == type.Name + "Flattened")
					Reject(type, "An explicit flattened-data constructor conflicts with the generated constructor.");
			}

			if ((SameDefinition(type, root) || !type.IsAbstract) && type.ContainingNamespace.GetTypeMembers(type.Name + "Flattened", type.Arity).Any())
				Reject(type, "The flattened type name is already in use.");
		}

		if (!valid) return false;
		Dictionary<KeyValues, INamedTypeSymbol> combinations = new();
		foreach (INamedTypeSymbol leaf in members.Where(type => !type.IsAbstract))
		{
			List<INamedTypeSymbol> path = GetPath(leaf, root);
			KeyValues combination = new(path.Skip(1).Select((type, index) => type.GetMembers((string)keys[index].ConstructorArguments[0].Value!).OfType<IFieldSymbol>().First().ConstantValue).ToArray());
			if (combinations.TryGetValue(combination, out INamedTypeSymbol? previous)) Reject(leaf, $"The discriminator combination duplicates '{previous.Name}'.");
			else combinations.Add(combination, leaf);
		}

		return valid;
	}
}