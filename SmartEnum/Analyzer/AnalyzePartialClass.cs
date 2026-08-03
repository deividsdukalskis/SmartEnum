namespace SmartEnum.Analyzer;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public sealed partial class SmartEnumAnalyzer
{
	private void AnalyzePartialClass(ValidationResult validationResult, SymbolAnalysisContext context)
	{
		if (validationResult.PartialClassCheckStatus is ValidationStatus.Invalid)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				MustBePartialClass,
				validationResult.OriginalClass.Locations.FirstOrDefault()));
		}
	}
}