namespace SmartEnum.Shared;

using Microsoft.CodeAnalysis;

public static partial class EnumHierarchy
{
	private static Result<HierarchyError.KeyFieldInvalidType> EnforceKeyFieldType(HierarchyData.DerivedTypeData data, (IFieldSymbol Field, ITypeSymbol IntendedType) tuple)
	{
		if (SymbolEqualityComparer.Default.Equals(tuple.IntendedType, tuple.Field.Type))
		{
			return new Success<HierarchyError.KeyFieldInvalidType>();
		}

		return new Failure<HierarchyError.KeyFieldInvalidType>(new(data.Type, tuple.Field, tuple.IntendedType, tuple.Field.Type));
	}
}