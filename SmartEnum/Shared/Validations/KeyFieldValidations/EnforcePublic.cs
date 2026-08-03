namespace SmartEnum.Shared.Validations.KeyFieldValidations;

using Microsoft.CodeAnalysis;

public static partial class KeyFieldValidationFunctions
{
	public static ValidationStatus EnforcePublic(IFieldSymbol fieldInfo)
	{
		if (fieldInfo.DeclaredAccessibility == Accessibility.Public)
		{
			return ValidationStatus.Valid;
		}

		return ValidationStatus.Invalid;
	}
}