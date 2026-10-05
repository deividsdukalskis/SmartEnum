namespace SmartEnum.Shared;

using Microsoft.CodeAnalysis;

public abstract partial class ValidatedHierarchyData
{
	private static Result<HierarchyError.KeyFieldNotPublic> EnforceKeyFieldPublic(HierarchyData.DerivedTypeData data, IFieldSymbol keyField) => keyField.DeclaredAccessibility is not Accessibility.Public
			? new Failure<HierarchyError.KeyFieldNotPublic>(new(data.Type, keyField))
			: new Success<HierarchyError.KeyFieldNotPublic>();
}