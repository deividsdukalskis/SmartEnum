namespace SmartEnum.Shared.Validations.KeyFieldValidations;

using Microsoft.CodeAnalysis;

public static partial class KeyFieldValidationFunctions
{
	public static ValidationStatus EnforceType(IFieldSymbol fieldInfo, ITypeSymbol intendedType)
	{
		if (SymbolEqualityComparer.Default.Equals(intendedType, fieldInfo.Type))
		{
			return ValidationStatus.Valid;
		}

		return ValidationStatus.Invalid;
	}
}