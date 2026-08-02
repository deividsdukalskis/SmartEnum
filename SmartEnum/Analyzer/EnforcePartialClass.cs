namespace SmartEnum.Analyzer;

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

public sealed partial class SmartEnumAnalyzer
{
	private DiagnosticDescriptor MustBePartialClass = new(
		id: "SMARTENUM002",
		title: "Class must be partial",
		messageFormat:
			"Class must be defined partial",
		category: "SmartEnum",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	private void EnforcePartialClass(RecursionResult recursionResult, SymbolAnalysisContext context)
	{
		bool isPartial = recursionResult.Type.DeclaringSyntaxReferences
			.Select(r => r.GetSyntax())
			.OfType<ClassDeclarationSyntax>()
			.Any(c => c.Modifiers.Any(SyntaxKind.PartialKeyword));

		int lastAttributeIndex = recursionResult.Attributes.Count - 1;
		if (!recursionResult.ShouldOriginalClassBeValidated
			|| (recursionResult.EnumHierarchyLevel != lastAttributeIndex
			&& recursionResult.EnumHierarchyLevel != -1))
		{
			return;
		}

		if (isPartial)
		{
			return;
		}

		context.ReportDiagnostic(Diagnostic.Create(
			MustBePartialClass,
			this.GetClassNameLocation(recursionResult.Type, context.CancellationToken)));
	}
}