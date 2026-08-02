namespace SmartEnum.Analyzer;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

public static partial class SubclassFieldAnalyzerExtensions
{
	public static
		(SymbolAnalysisContext Context,
		IFieldSymbol FieldInfo,
		ITypeSymbol IntendedFieldType,
		string IntendedFieldName)?
			EnforceType(this (SymbolAnalysisContext Context, IFieldSymbol FieldInfo, ITypeSymbol IntendedFieldType, string IntendedFieldName)? data, DiagnosticDescriptor error)
	{
		if (!data.HasValue)
		{
			return null;
		}

		Func<Location> getFieldTypeLocation = () =>
		{
			SyntaxNode? syntax = data.Value.FieldInfo.DeclaringSyntaxReferences
				.FirstOrDefault()?
				.GetSyntax(data.Value.Context.CancellationToken);

			if (syntax is VariableDeclaratorSyntax variable &&
				variable.Parent?.Parent is FieldDeclarationSyntax declaration)
			{
				return declaration.Declaration.Type.GetLocation();
			}

			return data.Value.FieldInfo.Locations.FirstOrDefault() ?? Location.None;
		};

		if (!SymbolEqualityComparer.Default.Equals(data.Value.IntendedFieldType, data.Value.FieldInfo.Type))
		{
			data.Value.Context.ReportDiagnostic(Diagnostic.Create(
				error,
				getFieldTypeLocation(),
				data.Value.IntendedFieldType.ToDisplayString(),
				data.Value.FieldInfo.Type.ToDisplayString()));
		}

		return data;
	}
}
