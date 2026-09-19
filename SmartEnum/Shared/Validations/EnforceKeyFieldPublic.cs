namespace SmartEnum.Shared;

using Microsoft.CodeAnalysis;

public abstract partial class ValidatedHierarchyData
{
	private static Result<HierarchyError.KeyFieldNotPublic> EnforceKeyFieldPublic(HierarchyData.DerivedTypeData data, IFieldSymbol keyField)
	{
		if (keyField.DeclaredAccessibility is not Accessibility.Public)
		{
			return new Failure<HierarchyError.KeyFieldNotPublic>(new(data.Type, keyField));
		}

		return new Success<HierarchyError.KeyFieldNotPublic>();
	}
}