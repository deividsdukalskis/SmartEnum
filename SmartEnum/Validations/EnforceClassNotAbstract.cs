namespace SmartEnum.Shared;

public static partial class EnumHierarchy
{
	private static Result<HierarchyError.ClassIsAbstract> EnforceClassNotAbstract(HierarchyData data)
	{
		if (data is not HierarchyData.DerivedTypeData derivedType)
		{
			return new Success<HierarchyError.ClassIsAbstract>();
		}

		if (derivedType.LastAttributeIndex != derivedType.HierarchyLevel)
		{
			return new Success<HierarchyError.ClassIsAbstract>();
		}

		if (!derivedType.Type.IsAbstract && !derivedType.Type.IsStatic)
		{
			return new Success<HierarchyError.ClassIsAbstract>();
		}

		return new Failure<HierarchyError.ClassIsAbstract>(new(derivedType.Type));
	}
}