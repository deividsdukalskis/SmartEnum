namespace SmartEnum.Analyzer;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public sealed partial class SmartEnumAnalyzer
{
	private void AnalyzeDuplicateProperties(HierarchyError error, CompilationAnalysisContext context)
	{
		if (error is not HierarchyError.DuplicatePropertyDefinitionsFound duplicateError)
		{
			return;
		}

		foreach (IGrouping<(string CommonParentTypeName, IPropertySymbol CurrentTypeProperty), string> commonParentTypeGroup in duplicateError.PropertiesWithSameName)
		{
			string typesWithSameProperties = string.Empty;
			foreach (string propertyContainingType in commonParentTypeGroup)
			{
				if (string.IsNullOrEmpty(typesWithSameProperties))
				{
					typesWithSameProperties += $"'{propertyContainingType}'";
				}
				else
				{
					typesWithSameProperties += $", '{propertyContainingType}'";
				}
			}

			context.ReportDiagnostic(Diagnostic.Create(
				DuplicateProperties,
				commonParentTypeGroup.Key.CurrentTypeProperty.Locations.FirstOrDefault(),
				commonParentTypeGroup.Key.CurrentTypeProperty.Name,
				typesWithSameProperties,
				commonParentTypeGroup.Key.CommonParentTypeName));
		}
	}
}