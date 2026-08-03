namespace SmartEnum.Analyzer;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public static partial class KeyFieldAnalyzerFunctions
{
	public static void AnalyzeDefinitionExists(ValidationResult.KeyFieldValidationResult keyFieldResult, SymbolAnalysisContext context, DiagnosticDescriptor error)
	{
		if (keyFieldResult.DefinitionExistsCheckStatus is ValidationStatus.Invalid)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				error,
				context.Symbol.Locations.FirstOrDefault(),
				keyFieldResult.IntendedKeyType?.ToDisplayString(),
				keyFieldResult.IntendedKeyName));
		}
	}
}