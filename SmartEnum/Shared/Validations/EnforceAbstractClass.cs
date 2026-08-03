namespace SmartEnum.Shared.Validations;

using SmartEnum.Shared;

public static partial class SharedValidationFunctions
{
	public static ValidationStatus EnforceAbstractClass(RecursionResult recursionResult)
	{
		int lastAttributeIndex = recursionResult.Attributes.Count - 1;
		if (!recursionResult.ShouldOriginalClassBeProcessedFurther)
		{
			return ValidationStatus.NotChecked;
		}

		if (recursionResult.EnumHierarchyLevel >= lastAttributeIndex
			|| recursionResult.Type.IsAbstract
			|| recursionResult.Type.IsStatic)
		{
			return ValidationStatus.Valid;
		}

		return ValidationStatus.Invalid;
	}
}