namespace SmartEnum.Analyzer;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public sealed partial class SmartEnumAnalyzer
{
	private void AnalyzeKeyIsConst(HierarchyError error, SymbolAnalysisContext context)
	{
		if (error is HierarchyError.KeyFieldNotConst notConstError)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				KeyIsNotConst,
				notConstError.Field.Locations.FirstOrDefault()));
		}
	}
}