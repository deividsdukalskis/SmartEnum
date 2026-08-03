namespace SmartEnum.Shared.Validations;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static partial class SharedValidationFunctions
{
	public static ValidationStatus EnforcePartialClass(RecursionResult recursionResult)
	{
		bool isPartial = recursionResult.Type.DeclaringSyntaxReferences
			.Select(r => r.GetSyntax())
			.OfType<ClassDeclarationSyntax>()
			.Any(c => c.Modifiers.Any(SyntaxKind.PartialKeyword));

		int lastAttributeIndex = recursionResult.Attributes.Count - 1;
		if (!recursionResult.ShouldOriginalClassBeProcessedFurther)
		{
			return ValidationStatus.NotChecked;
		}

		if ((recursionResult.EnumHierarchyLevel != lastAttributeIndex && recursionResult.EnumHierarchyLevel != -1) || isPartial)
		{
			return ValidationStatus.Valid;
		}

		return ValidationStatus.Invalid;
	}
}