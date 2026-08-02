namespace SmartEnum.Analyzer;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public static partial class SubclassFieldAnalyzerExtensions
{
	public static 
		(SymbolAnalysisContext Context,
		IFieldSymbol FieldInfo,
		ITypeSymbol IntendedFieldType,
		string IntendedFieldName)?
			EnforceDefinitionExists(this (SymbolAnalysisContext Context, IFieldSymbol? FieldInfo, ITypeSymbol IntendedFieldType, string IntendedFieldName)? data, DiagnosticDescriptor error)
	{
		if (!data.HasValue)
		{
			return null;
		}

		if (data.Value.FieldInfo is null)
		{
			data.Value.Context.ReportDiagnostic(Diagnostic.Create(
				error,
				data.Value.Context.Symbol.Locations.FirstOrDefault(),
				data.Value.IntendedFieldType.ToDisplayString(),
				data.Value.IntendedFieldName));

			return null;
		}

		return data!;
	}
}
