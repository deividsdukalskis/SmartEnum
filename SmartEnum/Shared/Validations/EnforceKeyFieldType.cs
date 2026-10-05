namespace SmartEnum.Shared;

using Microsoft.CodeAnalysis;

public abstract partial class ValidatedHierarchyData
{
	private static Result<HierarchyError.KeyFieldInvalidType> EnforceKeyFieldType(HierarchyData.DerivedTypeData data, (IFieldSymbol Field, ITypeSymbol IntendedType) tuple) => SymbolEqualityComparer.Default.Equals(tuple.IntendedType, tuple.Field.Type)
			? new Success<HierarchyError.KeyFieldInvalidType>()
			: new Failure<HierarchyError.KeyFieldInvalidType>(new(data.Type, tuple.Field, tuple.IntendedType, tuple.Field.Type));
}