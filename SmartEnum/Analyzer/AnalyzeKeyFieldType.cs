namespace SmartEnum.Analyzer;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public sealed partial class SmartEnumAnalyzer
{
	private void AnalyzeKeyFieldType(HierarchyError error, CompilationAnalysisContext context)
	{
		if (error is not HierarchyError.KeyFieldInvalidType invalidTypeError)
		{
			return;
		}

		Func<Location> getFieldTypeLocation = () =>
		{
			SyntaxNode? syntax = invalidTypeError.Field.DeclaringSyntaxReferences
				.FirstOrDefault()?
				.GetSyntax(context.CancellationToken);

			if (syntax is VariableDeclaratorSyntax variable &&
				variable.Parent?.Parent is FieldDeclarationSyntax declaration)
			{
				return declaration.Declaration.Type.GetLocation();
			}

			return invalidTypeError.Field.Locations.FirstOrDefault() ?? Location.None;
		};

		context.ReportDiagnostic(Diagnostic.Create(
			InvalidKeyType,
			getFieldTypeLocation(),
			invalidTypeError.IntendedType.ToDisplayString(),
			invalidTypeError.ProvidedType.ToDisplayString()));
	}
}
