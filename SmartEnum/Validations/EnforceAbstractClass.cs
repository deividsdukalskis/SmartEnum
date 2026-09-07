namespace SmartEnum.Shared;

public static partial class EnumHierarchy
{
	private static Result<HierarchyError.ClassNotAbstract> EnforceAbstractClass(HierarchyData data)
	{
		if (data is HierarchyData.DerivedTypeData derived && derived.LastAttributeIndex == derived.HierarchyLevel)
		{
			return new Success<HierarchyError.ClassNotAbstract>();
		}

		if (data.Type.IsAbstract || data.Type.IsStatic)
		{
			return new Success<HierarchyError.ClassNotAbstract>();
		}

		return new Failure<HierarchyError.ClassNotAbstract>(new(data.Type));
	}
}