namespace SmartEnum.Shared.Validations;

using System.Linq;
using Microsoft.CodeAnalysis;
using static KeyFieldValidations.KeyFieldValidationFunctions;

public static partial class SharedValidationFunctions
{
	public static ValidationResult.KeyFieldValidationResult ValidateKeyField(RecursionResult recursionResult)
	{
		if (!recursionResult.ShouldOriginalClassBeProcessedFurther || recursionResult.EnumHierarchyLevel == -1)
		{
			return new(null, null, null);
		}

		AttributeData? relevantAttribute = recursionResult.Attributes.ElementAtOrDefault(recursionResult.EnumHierarchyLevel);
		object? keyName = relevantAttribute?.ConstructorArguments.FirstOrDefault().Value;
		if (keyName is not string name)
		{
			return new(null, null, null);
		}

		ITypeSymbol? intendedKeyType = relevantAttribute?.AttributeClass?.TypeArguments.FirstOrDefault();
		if (intendedKeyType is null)
		{
			return new(null, null, null);
		}

		if (recursionResult.Type.GetMembers(name).FirstOrDefault(member => member.Kind is SymbolKind.Field) is not IFieldSymbol fieldInfo)
		{
			return new(null, intendedKeyType, name, ValidationStatus.Invalid);
		}

		ValidationStatus publicCheckStatus = EnforcePublic(fieldInfo);
		ValidationStatus constCheckStatus = EnforceConst(fieldInfo);
		ValidationStatus typeCheckStatus = EnforceType(fieldInfo, intendedKeyType);

		return new(fieldInfo, intendedKeyType, name, ValidationStatus.Valid, publicCheckStatus, constCheckStatus, typeCheckStatus);
	}
}