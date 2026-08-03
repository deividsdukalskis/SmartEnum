namespace SmartEnum.Shared;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using static SmartEnum.Shared.Validations.SharedValidationFunctions;

public static partial class SharedFunctions
{
	public static ValidationResult Validate(INamedTypeSymbol type, INamedTypeSymbol attributeDefinition)
	{
		RecursionResult recursionResult = RecurseEnumHierarchy(type, attributeDefinition, true);
		if (!recursionResult.EnumBaseClassFound)
		{
			return new(recursionResult.Type);
		}

		ValidationStatus abstractCheckStatus = EnforceAbstractClass(recursionResult);
		ValidationStatus partialCheckStatus = EnforcePartialClass(recursionResult);
		ImmutableList<AttributeData> attributesWithInvalidKeyNames = EnforceProperKeyName(recursionResult);
		ValidationResult.KeyFieldValidationResult keyFieldValidationResult;
		if (attributesWithInvalidKeyNames.Any())
		{
			keyFieldValidationResult = new(null, null, null);
		}
		else
		{
			keyFieldValidationResult = ValidateKeyField(recursionResult);
		}

		return new(recursionResult.EnumHierarchyLevel == -1,
			abstractCheckStatus,
			partialCheckStatus,
			attributesWithInvalidKeyNames,
			keyFieldValidationResult,
			type,
			recursionResult.DuplicateAttributesFound,
			recursionResult.Attributes);
	}
}
