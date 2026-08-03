namespace SmartEnum.Analyzer;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public static partial class KeyFieldAnalyzerFunctions
{
	public static void AnalyzeConst(ValidationResult.KeyFieldValidationResult keyFieldResult, SymbolAnalysisContext context, DiagnosticDescriptor error)
	{
		if (keyFieldResult.ConstCheckStatus is ValidationStatus.Invalid)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				error,
				keyFieldResult.FieldInfo?.Locations.FirstOrDefault()));
		}
	}
}