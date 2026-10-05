namespace SmartEnum.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public sealed partial class SmartEnumGenerator
{
	private static readonly DiagnosticDescriptor UnsupportedGeneration = new(
		"SMARTENUM012", "Unsupported SmartEnum declaration", "Cannot generate SmartEnum for '{0}': {1}",
		"SmartEnum", DiagnosticSeverity.Error, true);
	private static readonly DiagnosticDescriptor AmbiguousKeys = new(
		"SMARTENUM013", "Ambiguous SmartEnum keys", "Types '{0}' and '{1}' have the same complete key combination",
		"SmartEnum", DiagnosticSeverity.Error, true);

	private static bool ValidateGeneration(SourceProductionContext output, Compilation compilation,
		INamedTypeSymbol root, INamedTypeSymbol[] members, INamedTypeSymbol attribute)
	{
		bool valid = true;
		void Reject(ISymbol symbol, string reason)
		{
			output.ReportDiagnostic(Diagnostic.Create(UnsupportedGeneration, symbol.Locations.FirstOrDefault(), symbol.Name, reason));
			valid = false;
		}

		string[] keyNames = GetAttributes(root, attribute).Select(key => (string)key.ConstructorArguments[0].Value!).ToArray();
		List<INamedTypeSymbol> mappedMembers = new();
		foreach (INamedTypeSymbol type in members)
		{
			output.CancellationToken.ThrowIfCancellationRequested();
			for (INamedTypeSymbol? container = type; container is not null; container = container.ContainingType)
			{
				if (container.IsFileLocal || !container.DeclaringSyntaxReferences.All(reference => reference.GetSyntax(output.CancellationToken) is TypeDeclarationSyntax declaration
					&& declaration.Modifiers.Any(SyntaxKind.PartialKeyword) && declaration.ParameterList is null))
				{
					Reject(type, "the type and its containers must be partial and cannot be file-local or have a primary constructor");
					break;
				}
			}

			INamedTypeSymbol? mapped = MapToRoot(type, root);
			if (mapped is null || !SatisfiesConstraints(mapped, compilation))
				Reject(type, "descendant type arguments must be inferable from the root, and its generic constraints cannot be stronger than the root's");
			else mappedMembers.Add(mapped);
			if (type.IsStatic || type.TypeKind != TypeKind.Class) Reject(type, "hierarchy types must be instance classes or record classes");
			if (!compilation.IsSymbolAccessibleWithin(type, root)) Reject(type, "the type must be accessible from the hierarchy root");
			DataMember[] data = GetData(type, root);
			foreach (DataMember member in GetOwnData(type))
			{
				if (member.Type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer || member.Type.IsRefLikeType
					|| member.Symbol is IPropertySymbol { ReturnsByRef: true } or IPropertySymbol { ReturnsByRefReadonly: true } or IFieldSymbol { IsFixedSizeBuffer: true })
				{
					Reject(member.Symbol, "constructor data cannot contain pointers, ref-like values, ref returns, or fixed buffers");
				}

				if (!compilation.IsSymbolAccessibleWithin(member.Type, root)) Reject(member.Symbol, "the data type must be accessible from the hierarchy root");
				if (!IsVisibleInPublicApi(member.Type, type)) Reject(member.Symbol, "the data type is less accessible than the generated public factory");
			}

			foreach (IGrouping<string, DataMember> group in GetExternalData(GetPath(type, root)[0]).Concat(GetPath(type, root).SelectMany(GetOwnData)).GroupBy(member => member.Name))
			{
				if (group.Count() > 1 && !group.All(member => member.Symbol is IPropertySymbol { IsOverride: true } or IPropertySymbol { IsVirtual: true }))
					Reject(type, $"multiple constructor members are named '{group.Key}'");
			}

			IMethodSymbol? constructor = FindConstructor(type, data);
			if (constructor is not null)
			{
				Accessibility expected = type.IsAbstract ? Accessibility.Protected : Accessibility.Private;
				if (constructor.DeclaredAccessibility != expected) Reject(type, $"the existing data constructor must be {expected.ToString().ToLowerInvariant()}");
				if (data.Any(member => member.IsRequired) && !HasSetsRequiredMembers(constructor)) Reject(type, "the existing data constructor must declare SetsRequiredMembers");
			}

			string factoryName = SameDefinition(type, root) ? "MapDataToTypeUnvalidated" : "ConstructUnvalidated";
			if (type.Name == factoryName) Reject(type, "the class name conflicts with the generated factory name");
			if (type.GetMembers(factoryName).Any(member => member is not IMethodSymbol)) Reject(type, $"'{factoryName}' conflicts with a generated factory");
			if (!type.IsAbstract && type.GetMembers(factoryName).OfType<IMethodSymbol>().Any(method => method.Arity == 0
				&& method.Parameters.Select(parameter => parameter.Type).SequenceEqual(data.Select(member => member.Type), SymbolEqualityComparer.Default)))
			{
				Reject(type, $"an existing '{factoryName}' method has the generated factory signature");
			}
		}

		if (root.BaseType is { SpecialType: not SpecialType.System_Object } parent)
		{
			IMethodSymbol? externalConstructor = GetExternalConstructor(root);
			if (externalConstructor is null) Reject(root, "the non-SmartEnum parent needs an accessible parameterless constructor or one unambiguous accessible constructor");
			else if (externalConstructor.Parameters.Any(parameter => parameter.RefKind != RefKind.None)) Reject(root, "the parent constructor cannot require ref, in, or out arguments");
			if (parent.GetMembers().Any(member => member is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true })
				&& (externalConstructor is null || !HasSetsRequiredMembers(externalConstructor)))
			{
				Reject(root, "the external parent constructor must initialize its required members and declare SetsRequiredMembers");
			}
		}

		DataMember[] mapperData = mappedMembers.SelectMany(type => GetData(type, root)).ToArray();
		foreach (DataMember member in mapperData)
		{
			if (!IsVisibleInPublicApi(member.Type, root)) Reject(member.Symbol, "the data type is less accessible than the root's public mapper");
		}

		foreach (IGrouping<string, DataMember> group in mapperData.GroupBy(member => member.Name))
		{
			if (group.Select(member => member.Type).Distinct(SymbolEqualityComparer.Default).Count() > 1) Reject(root, $"mapper data '{group.Key}' has incompatible types across branches");
		}

		ITypeSymbol[] mapperTypes = GetAttributes(root, attribute).Select(key => key.AttributeClass!.TypeArguments[0])
			.Concat(mapperData.GroupBy(member => member.Name).Select(group => group.First().Type)).ToArray();
		if (root.GetMembers("MapDataToTypeUnvalidated").OfType<IMethodSymbol>().Any(method => method.Arity == 0
			&& method.Parameters.Select(parameter => parameter.Type).SequenceEqual(mapperTypes, SymbolEqualityComparer.Default)))
		{
			Reject(root, "an existing mapper has the generated signature");
		}

		Dictionary<KeyValues, INamedTypeSymbol> combinations = new();
		foreach (INamedTypeSymbol leaf in members.Where(type => !type.IsAbstract))
		{
			output.CancellationToken.ThrowIfCancellationRequested();
			object?[] values = GetPath(leaf, root).Skip(1).Select((type, index) => type.GetMembers(keyNames[index]).OfType<IFieldSymbol>().First().ConstantValue).ToArray();
			KeyValues key = new(values);
			if (combinations.TryGetValue(key, out INamedTypeSymbol? previous))
			{
				output.ReportDiagnostic(Diagnostic.Create(AmbiguousKeys, leaf.Locations.FirstOrDefault(), previous.ToDisplayString(), leaf.ToDisplayString()));
				valid = false;
			}
			else
			{
				combinations.Add(key, leaf);
			}
		}

		return valid;
	}

	private static bool HasSetsRequiredMembers(IMethodSymbol constructor) => constructor.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute");

	private static bool IsVisibleInPublicApi(ITypeSymbol type, INamedTypeSymbol owner)
	{
		if (type is IArrayTypeSymbol array) return IsVisibleInPublicApi(array.ElementType, owner);
		if (type is not INamedTypeSymbol named) return true;
		int ownerVisibility = 3;
		for (INamedTypeSymbol? current = owner; current is not null; current = current.ContainingType)
			ownerVisibility = Math.Min(ownerVisibility, Visibility(current.DeclaredAccessibility));
		return Visibility(named.DeclaredAccessibility) >= ownerVisibility
			&& (named.ContainingType is null || IsVisibleInPublicApi(named.ContainingType, owner))
			&& named.TypeArguments.All(argument => IsVisibleInPublicApi(argument, owner));
	}

	private static int Visibility(Accessibility accessibility) => accessibility switch
	{
		Accessibility.Public => 3,
		Accessibility.Internal or Accessibility.ProtectedOrInternal => 2,
		_ => 1
	};

	private sealed class KeyValues : IEquatable<KeyValues>
	{
		private readonly object?[] values;
		public KeyValues(object?[] values) => this.values = values;
		public bool Equals(KeyValues? other) => other is not null && this.values.SequenceEqual(other.values);
		public override bool Equals(object? other) => other is KeyValues keys && this.Equals(keys);
		public override int GetHashCode()
		{
			int hash = 17;
			foreach (object? value in this.values) hash = unchecked((hash * 31) + (value?.GetHashCode() ?? 0));
			return hash;
		}
	}
}