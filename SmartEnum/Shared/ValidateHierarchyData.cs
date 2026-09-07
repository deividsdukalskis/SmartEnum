namespace SmartEnum.Shared;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

public static partial class EnumHierarchy
{
	public static Result<ValidatedHierarchyData, ImmutableArray<HierarchyError>> ValidateHierarchyData(Result<HierarchyData, HierarchyError.DuplicateAttributesFound> data)
	{
		return data switch
		{
			Failure<HierarchyData, HierarchyError.DuplicateAttributesFound> failure => new Failure<ValidatedHierarchyData, ImmutableArray<HierarchyError>>(ImmutableArray.Create<HierarchyError>(failure.Error)),
			Success<HierarchyData, HierarchyError.DuplicateAttributesFound> success => HandleSuccess(success),
			_ => throw new NotImplementedException()
		};

		Result<ValidatedHierarchyData, ImmutableArray<HierarchyError>> HandleSuccess(Success<HierarchyData, HierarchyError.DuplicateAttributesFound> data)
		{
			List<HierarchyError> errors = new();
			Result<HierarchyError.DuplicateKeyNamesFound> duplicateKeyCheck = EnforceNoDuplicateKeyNames(data.Value);
			Result<HierarchyError.InvalidKeyName> keyNameCheck = EnforceProperKeyName(data.Value);
			if (duplicateKeyCheck is Failure<HierarchyError.DuplicateKeyNamesFound> failure1) errors.Add(failure1.Error);
			if (keyNameCheck is Failure<HierarchyError.InvalidKeyName> failure2) errors.Add(failure2.Error);
			if (errors.Any())
			{
				return new Failure<ValidatedHierarchyData, ImmutableArray<HierarchyError>>(errors.ToImmutableArray());
			}

			Result<HierarchyError.ClassNotAbstract> abstractCheck = EnforceAbstractClass(data.Value);
			Result<HierarchyError.ClassNotPartial> partialCheck = EnforcePartialClass(data.Value);
			Result<HierarchyError.ClassIsAbstract> notAbstractCheck = EnforceClassNotAbstract(data.Value);
			Result<HierarchyError.DuplicatePropertyDefinitionsFound> duplicatePropertiesCheck = EnforceNoDuplicateProperties(data.Value);

			IFieldSymbol? keyField = null;
			if (data.Value is HierarchyData.DerivedTypeData derived)
			{
				Result<(IFieldSymbol, ITypeSymbol), HierarchyError.KeyFieldNotDefined> keyFieldExistsCheck = EnforceKeyFieldDefined(derived);
				switch (keyFieldExistsCheck)
				{
					case Success<(IFieldSymbol Field, ITypeSymbol IntendedType), HierarchyError.KeyFieldNotDefined> success:
						Result<HierarchyError.KeyFieldNotPublic> keyFieldPublicCheck = EnforceKeyFieldPublic(derived, success.Value.Field);
						Result<HierarchyError.KeyFieldNotConst> keyFieldConstCheck = EnforceKeyFieldConst(derived, success.Value.Field);
						Result<HierarchyError.KeyFieldInvalidType> keyFieldTypeCheck = EnforceKeyFieldType(derived, success.Value);
						if (keyFieldPublicCheck is Failure<HierarchyError.KeyFieldNotPublic> keyCheckFailure1) errors.Add(keyCheckFailure1.Error);
						if (keyFieldConstCheck is Failure<HierarchyError.KeyFieldNotConst> keyCheckFailure2) errors.Add(keyCheckFailure2.Error);
						if (keyFieldTypeCheck is Failure<HierarchyError.KeyFieldInvalidType> keyCheckFailure3) errors.Add(keyCheckFailure3.Error);
						keyField = success.Value.Field;

						break;

					case Failure<(IFieldSymbol, ITypeSymbol), HierarchyError.KeyFieldNotDefined> failure:
						errors.Add(failure.Error);
						break;
				}
			}

			if (abstractCheck is Failure<HierarchyError.ClassNotAbstract> failure3) errors.Add(failure3.Error);
			if (partialCheck is Failure<HierarchyError.ClassNotPartial> failure4) errors.Add(failure4.Error);
			if (notAbstractCheck is Failure<HierarchyError.ClassIsAbstract> failure5) errors.Add(failure5.Error);
			if (duplicatePropertiesCheck is Failure<HierarchyError.DuplicatePropertyDefinitionsFound> failure6) errors.Add(failure6.Error);

			if (errors.Any())
			{
				return new Failure<ValidatedHierarchyData, ImmutableArray<HierarchyError>>(errors.ToImmutableArray());
			}

			return data.Value switch
			{
				HierarchyData.BaseTypeData baseTypeData => new Success<ValidatedHierarchyData, ImmutableArray<HierarchyError>>(new ValidatedHierarchyData.BaseTypeData(baseTypeData.Type)),
				HierarchyData.DerivedTypeData derivedTypeData => derivedTypeData.LastAttributeIndex == derivedTypeData.HierarchyLevel
					? new Success<ValidatedHierarchyData, ImmutableArray<HierarchyError>>(new ValidatedHierarchyData.DerivedConcreteTypeData(derivedTypeData.Type, derivedTypeData.BaseType, keyField!))
					: new Success<ValidatedHierarchyData, ImmutableArray<HierarchyError>>(new ValidatedHierarchyData.DerivedAbstractTypeData(derivedTypeData.Type, derivedTypeData.BaseType, keyField!)),
				_ => throw new NotImplementedException()
			};
		}
	}
}