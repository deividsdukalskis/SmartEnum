namespace SmartEnum.Analyzer;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

public sealed partial class SmartEnumAnalyzer
{
	private void AnalyzeKeyFieldType(HierarchyError error, SymbolAnalysisContext context)
	{
		if (error is not HierarchyError.KeyFieldInvalidType invalidTypeError)
		{
			return;
		}

		Location getFieldTypeLocation()
		{
			SyntaxNode? syntax = invalidTypeError.Field.DeclaringSyntaxReferences
				.FirstOrDefault()?
				.GetSyntax(context.CancellationToken);

			return syntax is VariableDeclaratorSyntax variable &&
				variable.Parent?.Parent is FieldDeclarationSyntax declaration
				? declaration.Declaration.Type.GetLocation()
				: invalidTypeError.Field.Locations.FirstOrDefault() ?? Location.None;
		}

		context.ReportDiagnostic(Diagnostic.Create(
			InvalidKeyType,
			getFieldTypeLocation(),
			invalidTypeError.IntendedType.ToDisplayString(),
			invalidTypeError.ProvidedType.ToDisplayString()));
	}
}