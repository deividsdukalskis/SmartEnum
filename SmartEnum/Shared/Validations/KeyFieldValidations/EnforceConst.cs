namespace SmartEnum.Shared.Validations.KeyFieldValidations;

using Microsoft.CodeAnalysis;

public static partial class KeyFieldValidationFunctions
{
	public static ValidationStatus EnforceConst(IFieldSymbol fieldInfo)
	{
		if (fieldInfo.IsConst)
		{
			return ValidationStatus.Valid;
		}

		return ValidationStatus.Invalid;
	}
}