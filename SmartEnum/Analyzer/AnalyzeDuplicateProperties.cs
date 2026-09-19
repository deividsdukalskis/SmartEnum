namespace SmartEnum.Analyzer;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public sealed partial class SmartEnumAnalyzer
{
	private void AnalyzeDuplicateProperties(HierarchyError error, SymbolAnalysisContext context)
	{
		if (error is not HierarchyError.DuplicatePropertyDefinitionsFound duplicateError)
		{
			return;
		}

		foreach (HierarchyError.DuplicatePropertyDefinitionsFound.DuplicateProperties propertyWithDuplicateName in duplicateError.PropertiesWithDuplicateName)
		{
			string typesWithSameProperties = string.Empty;
			foreach (string propertyContainingType in propertyWithDuplicateName.OtherTypeNamesWithDuplicatePropertyNames)
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
				propertyWithDuplicateName.Property.Locations.FirstOrDefault(),
				propertyWithDuplicateName.Property.Name,
				typesWithSameProperties,
				propertyWithDuplicateName.CommonTypeName));
		}
	}
}