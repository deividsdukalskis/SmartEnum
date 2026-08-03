namespace SmartEnum.Analyzer;

using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public sealed partial class SmartEnumAnalyzer
{
	private void AnalyzeKeyField(ValidationResult validationResult, SymbolAnalysisContext context)
	{
		KeyFieldAnalyzerFunctions.AnalyzeDefinitionExists(validationResult.FieldValidationResult, context, KeyFieldDoesNotExist);
		KeyFieldAnalyzerFunctions.AnalyzePublic(validationResult.FieldValidationResult, context, KeyIsNotPublic);
		KeyFieldAnalyzerFunctions.AnalyzeConst(validationResult.FieldValidationResult, context, KeyIsNotConst);
		KeyFieldAnalyzerFunctions.AnalyzeType(validationResult.FieldValidationResult, context, InvalidKeyType);
	}
}