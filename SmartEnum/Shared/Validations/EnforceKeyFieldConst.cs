namespace SmartEnum.Shared;

using Microsoft.CodeAnalysis;

public abstract partial class ValidatedHierarchyData
{
	private static Result<HierarchyError.KeyFieldNotConst> EnforceKeyFieldConst(HierarchyData.DerivedTypeData data, IFieldSymbol keyField)
	{
		if (keyField.IsConst is false)
		{
			return new Failure<HierarchyError.KeyFieldNotConst>(new(data.Type, keyField));
		}

		return new Success<HierarchyError.KeyFieldNotConst>();
	}
}