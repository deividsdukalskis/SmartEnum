namespace SmartEnum.Analyzer;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public sealed partial class SmartEnumAnalyzer
{
	private void AnalyzePartialClass(HierarchyError error, SymbolAnalysisContext context)
	{
		if (error is HierarchyError.ClassNotPartial partialError)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				MustBePartialClass,
				partialError.Type.Locations.FirstOrDefault()));
		}
	}
}