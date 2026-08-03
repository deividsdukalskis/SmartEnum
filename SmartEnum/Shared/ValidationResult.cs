namespace SmartEnum.Shared;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

public class ValidationResult
{
	public class KeyFieldValidationResult
	{
		public ValidationStatus DefinitionExistsCheckStatus { get; }
		public ValidationStatus PublicCheckStatus { get; }
		public ValidationStatus ConstCheckStatus { get; }
		public ValidationStatus TypeCheckStatus { get; }
		public IFieldSymbol? FieldInfo { get; }
		public ITypeSymbol? IntendedKeyType { get; }
		public string? IntendedKeyName { get; }
		public bool HasAllChecksPassed
			=> this.DefinitionExistsCheckStatus is ValidationStatus.Valid &&
			this.PublicCheckStatus is ValidationStatus.Valid &&
			this.ConstCheckStatus is ValidationStatus.Valid &&
			this.TypeCheckStatus is ValidationStatus.Valid;

		public KeyFieldValidationResult(
			IFieldSymbol? fieldInfo,
			ITypeSymbol? intendedKeyType,
			string? intendedKeyName,
			ValidationStatus definitionExistsCheckStatus = ValidationStatus.NotChecked,
			ValidationStatus publicCheckStatus = ValidationStatus.NotChecked,
			ValidationStatus constCheckStatus = ValidationStatus.NotChecked,
			ValidationStatus typeCheckStatus = ValidationStatus.NotChecked)
		{
			this.FieldInfo = fieldInfo;
			this.IntendedKeyType = intendedKeyType;
			this.IntendedKeyName = intendedKeyName;
			this.DefinitionExistsCheckStatus = definitionExistsCheckStatus;
			this.PublicCheckStatus = publicCheckStatus;
			this.ConstCheckStatus = constCheckStatus;
			this.TypeCheckStatus = typeCheckStatus;
		}
	}

	public bool IsBaseClass { get; }
	public bool IsInEnumHierarchy { get; }
	public bool HasDuplicateAttributes { get; }
	public ValidationStatus AbstractClassCheckStatus { get; }
	public ValidationStatus PartialClassCheckStatus { get; }
	public ImmutableList<AttributeData> AttributesWithInvalidKeyName { get; }
	public ImmutableList<AttributeData> Attributes { get; }
	public KeyFieldValidationResult FieldValidationResult { get; }
	public INamedTypeSymbol OriginalClass { get; }
	public bool HasAllChecksPassed
		=> this.AbstractClassCheckStatus is ValidationStatus.Valid &&
		this.PartialClassCheckStatus is ValidationStatus.Valid &&
		this.AttributesWithInvalidKeyName.IsEmpty &&
		this.FieldValidationResult.HasAllChecksPassed;

	public ValidationResult(INamedTypeSymbol originalClass)
	{
		this.IsBaseClass = false;
		this.IsInEnumHierarchy = false;
		this.HasDuplicateAttributes = false;
		this.AbstractClassCheckStatus = ValidationStatus.NotChecked;
		this.PartialClassCheckStatus = ValidationStatus.NotChecked;
		this.Attributes = ImmutableList.Create<AttributeData>();
		this.AttributesWithInvalidKeyName = ImmutableList.Create<AttributeData>();
		this.FieldValidationResult = new(null, null, null);
		this.OriginalClass = originalClass;
	}

	public ValidationResult(
		bool isBaseClass,
		ValidationStatus abstractClassCheckStatus,
		ValidationStatus partialClassCheckStatus,
		ImmutableList<AttributeData> attributesWithInvalidKeyName,
		KeyFieldValidationResult fieldValidationResult,
		INamedTypeSymbol originalClass,
		bool hasDuplicateAttributes,
		ImmutableList<AttributeData> attributes)
	{
		this.IsBaseClass = isBaseClass;
		this.IsInEnumHierarchy = true;
		this.Attributes = attributes;
		this.HasDuplicateAttributes = hasDuplicateAttributes;
		this.AbstractClassCheckStatus = abstractClassCheckStatus;
		this.PartialClassCheckStatus = partialClassCheckStatus;
		this.AttributesWithInvalidKeyName = attributesWithInvalidKeyName;
		this.FieldValidationResult = fieldValidationResult;
		this.OriginalClass = originalClass;
	}
}