namespace SmartEnum.Analyzer;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public sealed partial class SmartEnumAnalyzer
{
	private void AnalyzeAbstractClass(HierarchyError error, CompilationAnalysisContext context)
	{
		if (error is HierarchyError.ClassNotAbstract abstractError)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				MustBeAbstractClass,
				abstractError.Type.Locations.FirstOrDefault()));
		}
	}
}