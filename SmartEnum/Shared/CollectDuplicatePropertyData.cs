namespace SmartEnum.Shared;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

public class DuplicatePropertyData
{
	public string DuplicatePropertyName { get; }
	public INamedTypeSymbol CommonParentType { get; }
	public ImmutableArray<IPropertySymbol> DuplicateProperties { get; }

	public DuplicatePropertyData(string duplicatePropertyName, INamedTypeSymbol commonParentType, ImmutableArray<IPropertySymbol> duplicateProperties)
		=> (this.DuplicatePropertyName, this.CommonParentType, this.DuplicateProperties) = (duplicatePropertyName, commonParentType, duplicateProperties);

	public static ImmutableArray<DuplicatePropertyData> Collect(Compilation compilation, INamedTypeSymbol attributeDefinition, CancellationToken cancellationToken = default)
	{
		Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>> hierarchies = new(SymbolEqualityComparer.Default);
		Stack<INamespaceOrTypeSymbol> pending = new();
		pending.Push(compilation.Assembly.GlobalNamespace);
		while (pending.Count > 0)
		{
			cancellationToken.ThrowIfCancellationRequested();
			INamespaceOrTypeSymbol symbol = pending.Pop();
			foreach (INamespaceOrTypeSymbol child in symbol.GetMembers().OfType<INamespaceOrTypeSymbol>()) pending.Push(child);
			if (symbol is not INamedTypeSymbol type || HierarchyData.Collect(type, attributeDefinition) is not Success<HierarchyData, HierarchyError> collected) continue;
			INamedTypeSymbol root = (collected.Value is HierarchyData.DerivedTypeData derived ? derived.BaseType : type).OriginalDefinition;
			if (!hierarchies.TryGetValue(root, out List<INamedTypeSymbol>? types)) hierarchies.Add(root, types = new());
			types.Add(type);
		}

		ImmutableArray<DuplicatePropertyData>.Builder result = ImmutableArray.CreateBuilder<DuplicatePropertyData>();
		foreach (KeyValuePair<INamedTypeSymbol, List<INamedTypeSymbol>> hierarchy in hierarchies)
		{
			IEnumerable<IGrouping<string, IPropertySymbol>> groups = hierarchy.Value.SelectMany(type => type.GetMembers().OfType<IPropertySymbol>())
				.Where(property => !property.IsStatic && !property.IsIndexer && !property.IsImplicitlyDeclared && property.ExplicitInterfaceImplementations.IsEmpty)
				.GroupBy(property => property.Name);
			foreach (IGrouping<string, IPropertySymbol> group in groups)
			{
				cancellationToken.ThrowIfCancellationRequested();
				ImmutableArray<IPropertySymbol> properties = group.ToImmutableArray();
				if (properties.Select(GetOriginalProperty).Distinct(SymbolEqualityComparer.Default).Count() < 2) continue;
				INamedTypeSymbol common = hierarchy.Key;
				for (INamedTypeSymbol? ancestor = properties[0].ContainingType; ancestor is not null; ancestor = ancestor.BaseType)
				{
					if (!properties.All(property => HasAncestor(property.ContainingType, ancestor))) continue;
					common = ancestor.OriginalDefinition;
					break;
				}

				result.Add(new(group.Key, common, properties));
			}
		}

		return result.ToImmutable();
	}

	private static IPropertySymbol GetOriginalProperty(IPropertySymbol property)
	{
		while (property.OverriddenProperty is not null) property = property.OverriddenProperty;
		return property.OriginalDefinition;
	}

	private static bool HasAncestor(INamedTypeSymbol type, INamedTypeSymbol ancestor)
	{
		HashSet<ISymbol> visited = new(SymbolEqualityComparer.Default);
		for (INamedTypeSymbol? current = type; current is not null && visited.Add(current); current = current.BaseType)
			if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, ancestor.OriginalDefinition)) return true;
		return false;
	}
}