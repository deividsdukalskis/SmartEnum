namespace SmartEnum.Shared;

using Microsoft.CodeAnalysis;

public abstract partial class ValidatedHierarchyData
{
	private static Result<HierarchyError.KeyFieldNotConst> EnforceKeyFieldConst(HierarchyData.DerivedTypeData data, IFieldSymbol keyField) => keyField.IsConst is false
			? new Failure<HierarchyError.KeyFieldNotConst>(new(data.Type, keyField))
			: new Success<HierarchyError.KeyFieldNotConst>();
}