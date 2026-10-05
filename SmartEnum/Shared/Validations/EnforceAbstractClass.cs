namespace SmartEnum.Shared;

public abstract partial class ValidatedHierarchyData
{
	private static Result<HierarchyError.ClassNotAbstract> EnforceAbstractClass(HierarchyData data) => data is HierarchyData.DerivedTypeData derived && derived.LastAttributeIndex == derived.HierarchyLevel
			? new Success<HierarchyError.ClassNotAbstract>()
			: data.Type.IsAbstract && !data.Type.IsStatic
			? new Success<HierarchyError.ClassNotAbstract>()
			: new Failure<HierarchyError.ClassNotAbstract>(new(data.Type));
}