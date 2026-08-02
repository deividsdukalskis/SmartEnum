namespace SmartEnum.Analyzer;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public sealed partial class SmartEnumAnalyzer
{
	private DiagnosticDescriptor MustBeAbstractClass = new(
		id: "SMARTENUM001",
		title: "Class must be abstract",
		messageFormat:
			"Class must be defined abstract",
		category: "SmartEnum",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	private void EnforceAbstractClass(RecursionResult recursionResult, SymbolAnalysisContext context)
	{
		int lastAttributeIndex = recursionResult.Attributes.Count - 1;
		if (recursionResult.ShouldOriginalClassBeValidated is false || recursionResult.EnumHierarchyLevel >= lastAttributeIndex)
		{
			return;
		}

		if (recursionResult.Type.IsAbstract || recursionResult.Type.IsStatic)
		{
			return;
		}

		context.ReportDiagnostic(Diagnostic.Create(
			MustBeAbstractClass,
			this.GetClassNameLocation(recursionResult.Type, context.CancellationToken)));
	}
}