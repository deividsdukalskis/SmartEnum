namespace SmartEnum.Shared;

using System.Collections.Generic;
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
		INamedTypeSymbol? root = null;
		ImmutableArray<AttributeData> attributes = ImmutableArray<AttributeData>.Empty;
		int distance = 0;
		int rootDistance = 0;
		HashSet<ISymbol> visited = new(SymbolEqualityComparer.Default);
		for (INamedTypeSymbol? current = type; current is not null && current.SpecialType != SpecialType.System_Object && visited.Add(current);
			current = current.BaseType, distance++)
		{
			ImmutableArray<AttributeData> currentAttributes = current.GetAttributes()
				.Where(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass?.OriginalDefinition, attributeDefinition))
				.ToImmutableArray();
			if (currentAttributes.IsEmpty) continue;
			if (root is not null)
				return new Failure<HierarchyData, HierarchyError>(new DuplicateAttributesFound(root, current));
			root = current;
			attributes = currentAttributes;
			rootDistance = distance;
		}

		if (root is null) return new Failure<HierarchyData, HierarchyError>(new NotInEnumHierarchy());
		if (rootDistance == 0) return new Success<HierarchyData, HierarchyError>(new BaseTypeData(type, attributes));
		int level = rootDistance - 1;
		return level >= attributes.Length
			? new Failure<HierarchyData, HierarchyError>(new RelevantAttributeNotFound(type))
			: new Success<HierarchyData, HierarchyError>(new DerivedTypeData(type, root, attributes[level], level, attributes.Length - 1));
	}
}