namespace SmartEnum.Analyzer;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public sealed partial class SmartEnumAnalyzer
{
	private void AnalyzeKeyName(HierarchyError error, CompilationAnalysisContext context)
	{
		if (error is not HierarchyError.InvalidKeyName invalidKeyError)
		{
			return;
		}

		Func<AttributeData, Location> getAttribLocation = (attrib) =>
		{
			SyntaxNode? syntax = attrib.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken);
			if (syntax is AttributeSyntax attributeSyntax)
			{
				return attributeSyntax.ArgumentList?.GetLocation() ?? Location.None;
			}

			return Location.None;
		};

		foreach ((AttributeData attrib, string name) in invalidKeyError.AttributesWithInvalidKeyNames)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				InvalidKeyName,
				getAttribLocation(attrib),
				name));
		}
	}
}