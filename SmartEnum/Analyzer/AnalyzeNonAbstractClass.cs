namespace SmartEnum.Analyzer;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public sealed partial class SmartEnumAnalyzer
{
	private void AnalyzeNonAbstractClass(HierarchyError error, CompilationAnalysisContext context)
	{
		if (error is HierarchyError.ClassIsAbstract abstractError)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				MustNotBeAbstractClass,
				abstractError.Type.Locations.FirstOrDefault()));
		}
	}
}