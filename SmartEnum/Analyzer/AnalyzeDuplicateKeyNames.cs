namespace SmartEnum.Analyzer;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public sealed partial class SmartEnumAnalyzer
{
	private void AnalyzeDuplicateKeyNames(HierarchyError error, CompilationAnalysisContext context)
	{
		if (error is not HierarchyError.DuplicateKeyNamesFound duplicate)
		{
			return;
		}

		string result = string.Empty;
		foreach (string item in duplicate.DuplicateKeyNames)
		{
			if (string.IsNullOrEmpty(result))
			{
				result += $"'{item}'";
			}
			else
			{
				result += $", '{item}'";
			}
		}

		context.ReportDiagnostic(Diagnostic.Create(DuplicateKeyNames, duplicate.Type.Locations.FirstOrDefault(), result));
	}
}