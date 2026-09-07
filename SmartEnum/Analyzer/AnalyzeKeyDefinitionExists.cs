namespace SmartEnum.Analyzer;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public partial class SmartEnumAnalyzer
{
	private void AnalyzeKeyDefinitionExists(HierarchyError error, CompilationAnalysisContext context)
	{
		if (error is HierarchyError.KeyFieldNotDefined notDefinedError)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				KeyFieldDoesNotExist,
				notDefinedError.Type.Locations.FirstOrDefault(),
				notDefinedError.KeyType,
				notDefinedError.KeyName));
		}
	}
}