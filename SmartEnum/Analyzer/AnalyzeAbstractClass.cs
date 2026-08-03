namespace SmartEnum.Analyzer;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public sealed partial class SmartEnumAnalyzer
{
	private void AnalyzeAbstractClass(ValidationResult validationResult, SymbolAnalysisContext context)
	{
		if (validationResult.AbstractClassCheckStatus is ValidationStatus.Invalid)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				MustBeAbstractClass,
				validationResult.OriginalClass.Locations.FirstOrDefault()));
		}
	}
}