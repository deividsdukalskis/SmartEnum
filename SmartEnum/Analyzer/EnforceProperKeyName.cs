namespace SmartEnum.Analyzer;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

public sealed partial class SmartEnumAnalyzer
{
	private DiagnosticDescriptor InvalidKeyName = new(
		id: "SMARTENUM003",
		title: "Invalid key name",
		messageFormat:
			"'{0}' may not resolve to a proper field name",
		category: "SmartEnum",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	private void EnforceProperKeyName(RecursionResult recursionResult, SymbolAnalysisContext context)
	{
		if (!recursionResult.ShouldOriginalClassBeValidated)
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

		IEnumerable<(AttributeData, object?)> keyNames = recursionResult.Attributes.Select(arg => (arg, arg.ConstructorArguments.FirstOrDefault().Value));
		foreach ((AttributeData attrib, object? keyName) tuple in keyNames)
		{
			if (tuple.keyName is string name && !SyntaxFacts.IsValidIdentifier(name))
			{
				context.ReportDiagnostic(Diagnostic.Create(
				InvalidKeyName,
				getAttribLocation(tuple.attrib),
				name));
			}
		}
	}
}