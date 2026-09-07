namespace SmartEnum.Analyzer;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using SmartEnum.Shared;

public sealed partial class SmartEnumAnalyzer
{
	private void AnalyzeKeyDefinitionPublic(HierarchyError error, CompilationAnalysisContext context)
	{
		if (error is not HierarchyError.KeyFieldNotPublic notPublicError)
		{
			return;
		}

		Func<Location> getFieldAccessibilityModifierLocation = () =>
		{
			SyntaxNode? syntax = notPublicError.Field.DeclaringSyntaxReferences
				.FirstOrDefault()?
				.GetSyntax(context.CancellationToken);

			if (syntax is not VariableDeclaratorSyntax variable ||
				variable.Parent?.Parent is not FieldDeclarationSyntax declaration)
			{
				return notPublicError.Field.Locations.FirstOrDefault() ?? Location.None;
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

		context.ReportDiagnostic(Diagnostic.Create(KeyIsNotPublic, getFieldAccessibilityModifierLocation()));
	}
}