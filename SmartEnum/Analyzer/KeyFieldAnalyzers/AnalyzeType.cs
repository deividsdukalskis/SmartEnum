namespace SmartEnum.Analyzer;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public static partial class KeyFieldAnalyzerFunctions
{
	public static void AnalyzeType(ValidationResult.KeyFieldValidationResult keyFieldResult, SymbolAnalysisContext context, DiagnosticDescriptor error)
	{
		Func<Location> getFieldTypeLocation = () =>
		{
			SyntaxNode? syntax = keyFieldResult.FieldInfo?.DeclaringSyntaxReferences
				.FirstOrDefault()?
				.GetSyntax(context.CancellationToken);

			if (syntax is VariableDeclaratorSyntax variable &&
				variable.Parent?.Parent is FieldDeclarationSyntax declaration)
			{
				return declaration.Declaration.Type.GetLocation();
			}

			return keyFieldResult.FieldInfo?.Locations.FirstOrDefault() ?? Location.None;
		};

		if (keyFieldResult.TypeCheckStatus is ValidationStatus.Invalid)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				error,
				getFieldTypeLocation(),
				keyFieldResult.IntendedKeyType?.ToDisplayString(),
				keyFieldResult.FieldInfo?.Type.ToDisplayString()));
		}
	}
}
