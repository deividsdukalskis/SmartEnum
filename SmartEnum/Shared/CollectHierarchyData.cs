namespace SmartEnum.Shared;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using static SmartEnum.Shared.HierarchyError;

public abstract class HierarchyData
{
	public INamedTypeSymbol Type { get; }

	protected HierarchyData(INamedTypeSymbol type) => this.Type = type;

	public class BaseTypeData : HierarchyData
	{
		public ImmutableArray<AttributeData> EnumAttributes { get; }

		public BaseTypeData(INamedTypeSymbol type, ImmutableArray<AttributeData> enumAttributes) : base(type) => this.EnumAttributes = enumAttributes;
	}

	public class DerivedTypeData : HierarchyData
	{
		public INamedTypeSymbol BaseType { get; }
		public AttributeData RelevantAttribute { get; }
		public int LastAttributeIndex { get; }
		public int HierarchyLevel { get; }

		public DerivedTypeData(
			INamedTypeSymbol type,
			INamedTypeSymbol baseType,
			AttributeData relevantAttribute,
			int hierarchyLevel,
			int lastAttributeIndex) : base(type)
		{
			this.BaseType = baseType;
			this.RelevantAttribute = relevantAttribute;
			this.HierarchyLevel = hierarchyLevel;
			this.LastAttributeIndex = lastAttributeIndex;
		}
	}

	public static Result<HierarchyData, HierarchyError> Collect(INamedTypeSymbol type, INamedTypeSymbol attributeDefinition)
	{
		bool previousClassHasAttributesApplied = false;
		bool originalClassHasAttributesApplied = false;
		int hierarchyLevel = -1;
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
			return new Failure<HierarchyData, HierarchyError>(new NotInEnumHierarchy());
		}

		if (parentTypeWithDuplicateAttributes is null)
		{
			if (originalClassHasAttributesApplied)
			{
				return new Success<HierarchyData, HierarchyError>(new BaseTypeData(baseType, baseTypeEnumAttributes));
			}
			else
			{
				AttributeData? relevantAttribute = baseTypeEnumAttributes.ElementAtOrDefault(hierarchyLevel);
				if (relevantAttribute is null)
				{
					return new Failure<HierarchyData, HierarchyError>(new RelevantAttributeNotFound());
				}

				return new Success<HierarchyData, HierarchyError>(new DerivedTypeData(
					type,
					baseType,
					relevantAttribute,
					hierarchyLevel,
					baseTypeEnumAttributes.Count() - 1));
			}
		}

		return new Failure<HierarchyData, HierarchyError>(new DuplicateAttributesFound(baseType, parentTypeWithDuplicateAttributes));
	}
}