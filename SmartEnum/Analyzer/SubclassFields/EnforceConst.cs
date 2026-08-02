namespace SmartEnum.Analyzer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public static partial class SubclassFieldAnalyzerExtensions
{
	public static
		(SymbolAnalysisContext Context,
		IFieldSymbol FieldInfo,
		ITypeSymbol IntendedFieldType,
		string IntendedFieldName)?
			EnforceConst(this (SymbolAnalysisContext Context, IFieldSymbol FieldInfo, ITypeSymbol IntendedFieldType, string IntendedFieldName)? data, DiagnosticDescriptor error)
	{
		if (!data.HasValue)
		{
			return null;
		}

		if (!data.Value.FieldInfo.IsConst)
		{
			data.Value.Context.ReportDiagnostic(Diagnostic.Create(
				error,
				data.Value.FieldInfo.Locations.FirstOrDefault()));
		}

		return data;
	}
}