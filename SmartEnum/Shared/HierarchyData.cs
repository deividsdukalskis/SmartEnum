namespace SmartEnum.Shared;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

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
		public ImmutableArray<string> ParentTypeNames { get; }
		public AttributeData RelevantAttribute { get; }
		public int LastAttributeIndex { get; }
		public int HierarchyLevel { get; }

		public DerivedTypeData(
			INamedTypeSymbol type,
			INamedTypeSymbol baseType,
			ImmutableArray<string> parentTypeNames,
			AttributeData relevantAttribute,
			int hierarchyLevel,
			int lastAttributeIndex) : base(type)
		{
			this.BaseType = baseType;
			this.ParentTypeNames = parentTypeNames;
			this.RelevantAttribute = relevantAttribute;
			this.HierarchyLevel = hierarchyLevel;
			this.LastAttributeIndex = lastAttributeIndex;
		}
	}
}