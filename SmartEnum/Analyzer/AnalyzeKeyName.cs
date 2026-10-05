namespace SmartEnum.Analyzer;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public sealed partial class SmartEnumAnalyzer
{
	private void AnalyzeKeyName(HierarchyError error, SymbolAnalysisContext context)
	{
		if (error is not HierarchyError.InvalidKeyName invalidKeyError)
		{
			return;
		}

		Location getAttribLocation(AttributeData attrib)
		{
			SyntaxNode? syntax = attrib.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken);
			return syntax is AttributeSyntax attributeSyntax ? attributeSyntax.ArgumentList?.GetLocation() ?? Location.None : Location.None;
		}

		foreach ((AttributeData attrib, string name) in invalidKeyError.AttributesWithInvalidKeyNames)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				InvalidKeyName,
				getAttribLocation(attrib),
				name));
		}
	}
}