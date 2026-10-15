namespace SmartEnum.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using SmartEnum.Shared;

public sealed partial class SmartEnumGenerator
{
	private static bool HasExternalBase(INamedTypeSymbol type) => type.BaseType is { SpecialType: not SpecialType.System_Object } parent
		&& !StrategyContract.IsExplicit(parent)
		&& type.GetAttributes().Any(attribute => attribute.AttributeClass?.OriginalDefinition.ToDisplayString() is "SmartEnum.SmartEnumAttribute<TKey>" or "SmartEnum.SmartEnumParentAttribute");

	private static bool BaseAccessible(ISymbol symbol, INamedTypeSymbol root) => symbol.DeclaredAccessibility switch
	{
		Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal => true,
		Accessibility.Internal or Accessibility.ProtectedAndInternal => SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, root.ContainingAssembly)
			|| symbol.ContainingAssembly.GivesAccessTo(root.ContainingAssembly),
		_ => false
	};

	private static IEnumerable<TransferMember> ExternalBaseData(INamedTypeSymbol root)
	{
		if (!HasExternalBase(root)) yield break;
		HashSet<string> names = new(root.GetMembers().Select(member => member.Name), StringComparer.Ordinal);
		HashSet<ISymbol> visited = new(SymbolEqualityComparer.Default);
		for (INamedTypeSymbol? parent = root.BaseType; parent is not null && parent.SpecialType != SpecialType.System_Object && visited.Add(parent); parent = parent.BaseType)
		{
			foreach (IPropertySymbol property in parent.GetMembers().OfType<IPropertySymbol>())
			{
				if (property.IsStatic || property.IsIndexer || property.IsImplicitlyDeclared || !property.ExplicitInterfaceImplementations.IsEmpty || !names.Add(property.Name)) continue;
				if (!HasDataAttribute(property, "SmartEnumIgnoreAttribute") && BaseAccessible(property, root))
					yield return new(property, property, root);
			}

			foreach (IFieldSymbol field in parent.GetMembers().OfType<IFieldSymbol>())
			{
				if (field.DeclaredAccessibility == Accessibility.Public && !field.IsStatic && !field.IsConst && !field.IsImplicitlyDeclared && names.Add(field.Name)
					&& !HasDataAttribute(field, "SmartEnumIgnoreAttribute"))
				{
					yield return new(field, field, root);
				}
			}
		}
	}

	private static bool ExternalWritable(TransferMember member) => member.Storage is IFieldSymbol { IsReadOnly: false }
		|| (member.Property is IPropertySymbol { SetMethod: { } setter } && BaseAccessible(setter, member.Owner));

	private static TransferMember? ConstructorMember(IParameterSymbol parameter, TransferMember[] data) => data.FirstOrDefault(member => member.Name == FlattenedName(parameter.Name)
		&& SymbolEqualityComparer.Default.Equals(member.Type, parameter.Type));

	private static IMethodSymbol? ExternalBaseConstructor(INamedTypeSymbol root, TransferMember[] data)
	{
		if (!HasExternalBase(root)) return null;
		IMethodSymbol[] candidates = root.BaseType!.InstanceConstructors.Where(constructor => BaseAccessible(constructor, root)
			&& constructor.Parameters.All(parameter => parameter.RefKind == RefKind.None && (ConstructorMember(parameter, data) is not null || parameter.IsOptional))
			&& data.Where(member => member.Inherited && !ExternalWritable(member)).All(member => constructor.Parameters.Any(parameter => ConstructorMember(parameter, data) == member)))
			.OrderByDescending(constructor => constructor.Parameters.Count(parameter => ConstructorMember(parameter, data) is not null))
			.ThenBy(constructor => constructor.Parameters.Length).ToArray();
		if (candidates.Length == 0) return null;
		IMethodSymbol best = candidates[0];
		return candidates.Skip(1).Any(candidate => candidate.Parameters.Length == best.Parameters.Length
			&& candidate.Parameters.Count(parameter => ConstructorMember(parameter, data) is not null) == best.Parameters.Count(parameter => ConstructorMember(parameter, data) is not null))
			? null
			: best;
	}

	private static string ExternalBaseInitializer(INamedTypeSymbol root, Func<TransferMember, string> value)
	{
		IMethodSymbol? constructor = ExternalBaseConstructor(root, StrategyData(root));
		return constructor is null ? string.Empty : " : base(" + string.Join(", ", constructor.Parameters.Select(parameter => (Parameter: parameter, Member: ConstructorMember(parameter, StrategyData(root))))
			.Where(pair => pair.Member is not null).Select(pair => Identifier(pair.Parameter.Name) + ": " + value(pair.Member!))) + ")";
	}

	private static IEnumerable<string> TransferAssignments(INamedTypeSymbol type, TransferMember[] members, Func<TransferMember, string> value)
	{
		IMethodSymbol? constructor = ExternalBaseConstructor(type, members);
		return members.Where(member => !member.Inherited || (ExternalWritable(member) && constructor?.Parameters.Any(parameter => ConstructorMember(parameter, members) == member) != true))
			.OrderBy(member => member.Storage is IFieldSymbol).Select(member => $"this.{Identifier(member.Storage.Name)} = {value(member)};");
	}

	private static bool HasSharedDataConstructor(INamedTypeSymbol type, TransferMember[] data) => type.IsAbstract && type.InstanceConstructors.Any(constructor => !constructor.IsImplicitlyDeclared
		&& constructor.DeclaredAccessibility == Accessibility.Protected && constructor.Parameters.All(parameter => parameter.RefKind == RefKind.None)
		&& constructor.Parameters.Select(parameter => parameter.Type).SequenceEqual(data.Select(member => member.Type), SymbolEqualityComparer.Default));

	private static IEnumerable<ISymbol> ExternalBaseMembers(INamedTypeSymbol type)
	{
		if (!HasExternalBase(type)) yield break;
		HashSet<ISymbol> visited = new(SymbolEqualityComparer.Default);
		for (INamedTypeSymbol? parent = type.BaseType; parent is not null && parent.SpecialType != SpecialType.System_Object && visited.Add(parent); parent = parent.BaseType)
			foreach (ISymbol member in parent.GetMembers()) yield return member;
	}
}