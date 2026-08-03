namespace SmartEnum.Shared;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

public class RecursionResult
{
	public ImmutableList<AttributeData> Attributes { get; }
	public INamedTypeSymbol Type { get; }
	public bool DuplicateAttributesFound { get; }
	public int EnumHierarchyLevel { get; set; } = -1;
	public bool EnumBaseClassFound { get; }
	public bool ShouldOriginalClassBeProcessedFurther { get; }

	public RecursionResult(
		ImmutableList<AttributeData> attributes,
		INamedTypeSymbol type,
		bool duplicateAttributesFound,
		bool enumBaseClassFound,
		bool shouldOriginalClassBeProcessedFurther)
	{
		this.Attributes = attributes;
		this.Type = type;
		this.DuplicateAttributesFound = duplicateAttributesFound;
		this.EnumBaseClassFound = enumBaseClassFound;
		this.ShouldOriginalClassBeProcessedFurther = shouldOriginalClassBeProcessedFurther;
	}
}