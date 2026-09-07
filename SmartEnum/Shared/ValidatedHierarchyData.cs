namespace SmartEnum.Shared;

using Microsoft.CodeAnalysis;

public abstract class ValidatedHierarchyData
{
	public INamedTypeSymbol Type { get; }

	protected ValidatedHierarchyData(INamedTypeSymbol type) => this.Type = type;

	public class BaseTypeData : ValidatedHierarchyData
	{
		public BaseTypeData(INamedTypeSymbol type) : base(type) { }
	}

	public class DerivedConcreteTypeData : ValidatedHierarchyData
	{
		public INamedTypeSymbol BaseType { get; }
		public IFieldSymbol KeyField { get; }

		public DerivedConcreteTypeData(INamedTypeSymbol type, INamedTypeSymbol baseType, IFieldSymbol keyField) : base(type)
		{
			this.BaseType = baseType;
			this.KeyField = keyField;
		}
	}

	public class DerivedAbstractTypeData : ValidatedHierarchyData
	{
		public INamedTypeSymbol BaseType { get; }
		public IFieldSymbol KeyField { get; }

		public DerivedAbstractTypeData(INamedTypeSymbol type, INamedTypeSymbol baseType, IFieldSymbol keyField) : base(type)
		{
			this.BaseType = baseType;
			this.KeyField = keyField;
		}
	}
}