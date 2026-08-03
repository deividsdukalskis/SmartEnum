namespace SmartEnum.Analyzer;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using SmartEnum.Shared;

public static partial class KeyFieldAnalyzerFunctions
{
	public static void AnalyzePublic(ValidationResult.KeyFieldValidationResult keyFieldResult, SymbolAnalysisContext context, DiagnosticDescriptor error)
	{
		Func<Location> getFieldAccessibilityModifierLocation = () =>
		{
			SyntaxNode? syntax = keyFieldResult.FieldInfo?.DeclaringSyntaxReferences
				.FirstOrDefault()?
				.GetSyntax(context.CancellationToken);

			if (syntax is not VariableDeclaratorSyntax variable ||
				variable.Parent?.Parent is not FieldDeclarationSyntax declaration)
			{
				return keyFieldResult.FieldInfo?.Locations.FirstOrDefault() ?? Location.None;
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

		if (keyFieldResult.PublicCheckStatus is ValidationStatus.Invalid)
		{
			context.ReportDiagnostic(Diagnostic.Create(error, getFieldAccessibilityModifierLocation()));
		}
	}
}