namespace SmartEnum.Analyzer;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public sealed partial class SmartEnumAnalyzer
{
	private void AnalyzeDuplicateAttributes(HierarchyError error, CompilationAnalysisContext context)
	{
		if (error is HierarchyError.DuplicateAttributesFound duplicate)
		{
			context.ReportDiagnostic(Diagnostic.Create(DuplicateAttributes, duplicate.Type.Locations.FirstOrDefault(), duplicate.ParentType.ToDisplayString()));
		}
	}
}