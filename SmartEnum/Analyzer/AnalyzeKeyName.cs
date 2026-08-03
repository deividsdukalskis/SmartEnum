namespace SmartEnum.Analyzer;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public sealed partial class SmartEnumAnalyzer
{
	private void AnalyzeKeyName(ValidationResult validationResult, SymbolAnalysisContext context)
	{
		Func<AttributeData, Location> getAttribLocation = (attrib) =>
		{
			SyntaxNode? syntax = attrib.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken);
			if (syntax is AttributeSyntax attributeSyntax)
			{
				return attributeSyntax.ArgumentList?.GetLocation() ?? Location.None;
			}

			return Location.None;
		};

		foreach (AttributeData attrib in validationResult.AttributesWithInvalidKeyName)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				InvalidKeyName,
				getAttribLocation(attrib),
				attrib.ConstructorArguments.FirstOrDefault().Value));
		}
	}
}