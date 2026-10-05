namespace SmartEnum.Shared;

public abstract partial class ValidatedHierarchyData
{
	private static Result<HierarchyError.ClassIsAbstract> EnforceClassNotAbstract(HierarchyData data) => data is not HierarchyData.DerivedTypeData derivedType
			? new Success<HierarchyError.ClassIsAbstract>()
			: derivedType.LastAttributeIndex != derivedType.HierarchyLevel
			? new Success<HierarchyError.ClassIsAbstract>()
			: !derivedType.Type.IsAbstract && !derivedType.Type.IsStatic
			? new Success<HierarchyError.ClassIsAbstract>()
			: new Failure<HierarchyError.ClassIsAbstract>(new(derivedType.Type));
}