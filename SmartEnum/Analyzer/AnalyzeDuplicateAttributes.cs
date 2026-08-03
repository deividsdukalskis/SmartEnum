namespace SmartEnum.Analyzer;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public sealed partial class SmartEnumAnalyzer
{
	private void AnalyzeDuplicateAttributes(ValidationResult validationResult, SymbolAnalysisContext context)
	{
		if (validationResult.HasDuplicateAttributes)
		{
			context.ReportDiagnostic(Diagnostic.Create(DuplicateAttributes, validationResult.OriginalClass.Locations.FirstOrDefault()));
		}
	}
}