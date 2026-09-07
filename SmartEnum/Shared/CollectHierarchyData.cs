namespace SmartEnum.Shared;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

public static partial class EnumHierarchy
{
	public static ConcurrentBag<Result<HierarchyData, HierarchyError.DuplicateAttributesFound>> HierarchyDatas = new();

	public static void CollectHierarchyData(INamedTypeSymbol type, INamedTypeSymbol attributeDefinition)
	{
		bool previousClassHasAttributesApplied = false;
		bool originalClassHasAttributesApplied = false;
		int hierarchyLevel = -1;
		List<string> parentNames = new();
		INamedTypeSymbol? baseType = null;
		INamedTypeSymbol? parentTypeWithDuplicateAttributes = null;
		ImmutableArray<AttributeData> baseTypeEnumAttributes = new();

		for (INamedTypeSymbol? currentType = type;
			currentType is not null && currentType.SpecialType != SpecialType.System_Object;
			currentType = currentType.BaseType)
		{
			ImmutableArray<AttributeData> enumAttributes = currentType
							.GetAttributes()
							.Where(attrib => SymbolEqualityComparer.Default.Equals(attrib.AttributeClass?.OriginalDefinition, attributeDefinition))
							.ToImmutableArray();

			if (hierarchyLevel >= 0 && !previousClassHasAttributesApplied)
			{
				parentNames.Add(currentType.ToDisplayString());
			}

			if (enumAttributes.Any())
			{
				if (previousClassHasAttributesApplied)
				{
					parentTypeWithDuplicateAttributes = currentType;
					break;
				}

				baseType = currentType;
				baseTypeEnumAttributes = enumAttributes;
				if (!previousClassHasAttributesApplied && !originalClassHasAttributesApplied && hierarchyLevel >= 0)
				{
					previousClassHasAttributesApplied = true;
					continue;
				}

				previousClassHasAttributesApplied = true;
				originalClassHasAttributesApplied = true;
			}

			hierarchyLevel++;
		}

		if (baseType is null)
		{
			return;
		}

		if (previousClassHasAttributesApplied)
		{
			if (originalClassHasAttributesApplied)
			{
				if (parentTypeWithDuplicateAttributes is null)
				{
					HierarchyDatas.Add(new Success<HierarchyData, HierarchyError.DuplicateAttributesFound>(new HierarchyData.BaseTypeData(baseType, baseTypeEnumAttributes)));
				}
				else
				{
					HierarchyDatas.Add(new Failure<HierarchyData, HierarchyError.DuplicateAttributesFound>(new(baseType, parentTypeWithDuplicateAttributes)));
				}
			}
			else
			{
				if (parentTypeWithDuplicateAttributes is null)
				{
					AttributeData? relevantAttribute = baseTypeEnumAttributes.ElementAtOrDefault(hierarchyLevel);
					if (relevantAttribute is null)
					{
						return;
					}

					HierarchyDatas.Add(new Success<HierarchyData, HierarchyError.DuplicateAttributesFound>(new HierarchyData.DerivedTypeData(
						type,
						baseType,
						parentNames.ToImmutableArray(),
						relevantAttribute,
						hierarchyLevel,
						baseTypeEnumAttributes.Count() - 1)));
				}
			}
		}
	}
}