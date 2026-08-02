namespace SmartEnum.Analyzer;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

public static partial class SubclassFieldAnalyzerExtensions
{
	public static
		(SymbolAnalysisContext Context,
		IFieldSymbol FieldInfo,
		ITypeSymbol IntendedFieldType,
		string IntendedFieldName)?
			EnforcePublic(this (SymbolAnalysisContext Context, IFieldSymbol FieldInfo, ITypeSymbol IntendedFieldType, string IntendedFieldName)? data, DiagnosticDescriptor error)
	{
		if (!data.HasValue)
		{
			return null;
		}

		Func<Location> getFieldAccessibilityModifierLocation = () =>
		{
			SyntaxNode? syntax = data.Value.FieldInfo.DeclaringSyntaxReferences
				.FirstOrDefault()?
				.GetSyntax(data.Value.Context.CancellationToken);

			if (syntax is not VariableDeclaratorSyntax variable ||
				variable.Parent?.Parent is not FieldDeclarationSyntax declaration)
			{
				return data.Value.FieldInfo.Locations.FirstOrDefault() ?? Location.None;
			}

			SyntaxToken[] accessibilityModifiers = declaration.Modifiers
				.Where((modifier) => modifier.IsKind(SyntaxKind.PublicKeyword) ||
					modifier.IsKind(SyntaxKind.PrivateKeyword) ||
					modifier.IsKind(SyntaxKind.ProtectedKeyword) ||
					modifier.IsKind(SyntaxKind.InternalKeyword))
				.ToArray();

			// No explicit accessibility modifier exists, so underline
			// the field name as the most useful fallback.
			if (accessibilityModifiers.Length == 0)
			{
				return variable.Identifier.GetLocation();
			}

			// Handles compound accessibility such as:
			// protected internal / private protected
			TextSpan span = TextSpan.FromBounds(
				accessibilityModifiers[0].SpanStart,
				accessibilityModifiers[accessibilityModifiers.Length - 1].Span.End);

			return Location.Create(
				declaration.SyntaxTree,
				span);
		};

		if (data.Value.FieldInfo.DeclaredAccessibility != Accessibility.Public)
		{
			data.Value.Context.ReportDiagnostic(Diagnostic.Create(error, getFieldAccessibilityModifierLocation()));
		}

		return data;
	}
}
