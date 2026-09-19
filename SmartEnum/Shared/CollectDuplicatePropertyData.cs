namespace SmartEnum.Shared;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

public class DuplicatePropertyData
{
	public string DuplicatePropertyName { get; }
	public INamedTypeSymbol CommonParentType { get; }
	public ImmutableArray<IPropertySymbol> DuplicateProperties { get; }

	public DuplicatePropertyData(string duplicatePropertyName, INamedTypeSymbol commonParentType, ImmutableArray<IPropertySymbol> duplicateProperties)
	{
		this.DuplicatePropertyName = duplicatePropertyName;
		this.CommonParentType = commonParentType;
		this.DuplicateProperties = duplicateProperties;
	}

	private class TypeHierarchyData
	{
		public INamedTypeSymbol Type { get; }
		public ImmutableArray<(INamedTypeSymbol Type, int HierarchyLevel)> HierarchyInfo { get; }

		public TypeHierarchyData(INamedTypeSymbol type, ImmutableArray<(INamedTypeSymbol Type, int HierarchyLevel)> hierarchyInfo)
		{
			this.Type = type;
			this.HierarchyInfo = hierarchyInfo;
		}
	}

	public static ImmutableArray<DuplicatePropertyData> Collect(Compilation compilation, INamedTypeSymbol attributeDefinition)
	{
		ImmutableArray<DuplicatePropertyData>.Builder result = ImmutableArray.CreateBuilder<DuplicatePropertyData>();
		ImmutableArray<IGrouping<INamedTypeSymbol, TypeHierarchyData>> multipleHierarchyInfos = CollectTypeHierarchyData(compilation, attributeDefinition);
		foreach (IGrouping<INamedTypeSymbol, TypeHierarchyData> singleHierarchyInfo in multipleHierarchyInfos)
		{
			List<(IPropertySymbol Property, ImmutableArray<INamedTypeSymbol> HierarchyData)> duplicateProperties = new();
			foreach (TypeHierarchyData firstType in singleHierarchyInfo)
			{
				duplicateProperties.AddRange(firstType.Type
					.GetMembers()
					.OfType<IPropertySymbol>()
					.Select(property => (Property: property, firstType.HierarchyInfo.Select(hierarchy => hierarchy.Type).ToImmutableArray()))
					.Where(firstTypePropertyTuple =>
						!duplicateProperties.Contains(firstTypePropertyTuple)
						&& singleHierarchyInfo.Any(secondType =>
							!SymbolEqualityComparer.Default.Equals(firstType.Type, secondType.Type)
							&& secondType.Type.GetMembers().OfType<IPropertySymbol>().Any(secondTypeProperty => firstTypePropertyTuple.Property.Name == secondTypeProperty.Name))));
			}

			Func<IEnumerable<(IPropertySymbol Property, ImmutableArray<INamedTypeSymbol> HierarchyData)>, INamedTypeSymbol> findCommonParentType =
				(IEnumerable<(IPropertySymbol Property, ImmutableArray<INamedTypeSymbol> HierarchyData)> hierarchyInfo) =>
				{
					int maxLength = hierarchyInfo.Select(list => list.HierarchyData.Count()).DefaultIfEmpty(0).Max();

					IEnumerable<ImmutableArray<INamedTypeSymbol>> set = Enumerable.Range(0, maxLength)
						.Select(index => hierarchyInfo
							.Where(list => index < list.HierarchyData.Count())
							.Select(list => list.HierarchyData[index]).ToImmutableArray());

					IEqualityComparer<INamedTypeSymbol> symbolComparer = SymbolEqualityComparer.Default;

					return set.Last(x => x.Distinct(symbolComparer).Count() == 1 && x.Count() == hierarchyInfo.Count()).First();
				};

			IEnumerable<DuplicatePropertyData> groupedDuplicateProperties = duplicateProperties
				.GroupBy(x => x.Property.Name)
				.Select(x => new DuplicatePropertyData(x.Key, findCommonParentType(x.AsEnumerable()), x.Select(y => y.Property).ToImmutableArray()));

			result.AddRange(groupedDuplicateProperties);
		}

		return result.ToImmutable();
	}

	private static ImmutableArray<IGrouping<INamedTypeSymbol, TypeHierarchyData>> CollectTypeHierarchyData(Compilation compilation, INamedTypeSymbol attributeDefinition)
	{
		List<TypeHierarchyData> hierarchyData = new();
		ImmutableArray<INamespaceOrTypeSymbol> symbols = compilation.Assembly.GlobalNamespace.GetMembers().ToImmutableArray();
		foreach (INamespaceOrTypeSymbol symbol in symbols)
		{
			Stack<INamespaceOrTypeSymbol> symbolsToCheck = new();
			symbolsToCheck.Push(symbol);

			while (symbolsToCheck.Any())
			{
				INamespaceOrTypeSymbol currentSymbol = symbolsToCheck.Pop();
				if (currentSymbol is INamedTypeSymbol namedTypeSymbol)
				{
					bool previousClassHasAttributesApplied = false;
					List<INamedTypeSymbol> parentTypes = new();
					INamedTypeSymbol? baseType = null;
					INamedTypeSymbol? parentTypeWithDuplicateAttributes = null;

					for (INamedTypeSymbol? currentType = namedTypeSymbol;
						currentType is not null && currentType.SpecialType != SpecialType.System_Object;
						currentType = currentType.BaseType)
					{
						if (!previousClassHasAttributesApplied)
						{
							parentTypes.Add(currentType);
						}

						if (currentType.GetAttributes()
							.Any(attrib => SymbolEqualityComparer.Default.Equals(attrib.AttributeClass?.OriginalDefinition, attributeDefinition)))
						{
							if (previousClassHasAttributesApplied)
							{
								parentTypeWithDuplicateAttributes = currentType;
								break;
							}

							baseType = currentType;
							previousClassHasAttributesApplied = true;
						}
					}

					if (baseType is not null
						&& previousClassHasAttributesApplied
						&& parentTypeWithDuplicateAttributes is null)
					{
						parentTypes.Reverse();
						hierarchyData.Add(new(namedTypeSymbol, parentTypes.Select((x, index) => (x, index)).ToImmutableArray()));
					}
				}

				ImmutableArray<INamespaceOrTypeSymbol> childSymbols = currentSymbol.GetMembers().OfType<INamespaceOrTypeSymbol>().ToImmutableArray();
				foreach (INamespaceOrTypeSymbol childSymbol in childSymbols)
				{
					symbolsToCheck.Push(childSymbol);
				}
			}
		}

		IEqualityComparer<INamedTypeSymbol> symbolComparer = SymbolEqualityComparer.Default;

		return hierarchyData.GroupBy(x => x.HierarchyInfo.First().Type, symbolComparer).ToImmutableArray();
	}
}