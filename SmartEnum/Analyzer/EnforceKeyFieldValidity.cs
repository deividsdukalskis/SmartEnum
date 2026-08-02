namespace SmartEnum.Analyzer;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public sealed partial class SmartEnumAnalyzer
{
	private DiagnosticDescriptor KeyFieldDoesNotExist = new(
		id: "SMARTENUM004",
		title: "Key does not exist",
		messageFormat:
			"Class must define key 'public const {0} {1}'",
		category: "SmartEnum",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	private DiagnosticDescriptor KeyIsNotPublic = new(
		id: "SMARTENUM005",
		title: "Key is not public",
		messageFormat:
			"Field must be declared public",
		category: "SmartEnum",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	private DiagnosticDescriptor KeyIsNotConst = new(
		id: "SMARTENUM006",
		title: "Key is not const",
		messageFormat:
			"Field must be declared const",
		category: "SmartEnum",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	private DiagnosticDescriptor InvalidKeyType = new(
		id: "SMARTENUM007",
		title: "Invalid key type",
		messageFormat:
			"Field must have type '{0}', but is declared as '{1}'",
		category: "SmartEnum",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	private void EnforceKeyFieldValidity(SymbolAnalysisContext context, INamedTypeSymbol smartEnumAttribute) => (context, smartEnumAttribute)
		.GetFieldInformation()
		.EnforceDefinitionExists(KeyFieldDoesNotExist)
		.EnforcePublic(KeyIsNotPublic)
		.EnforceConst(KeyIsNotConst)
		.EnforceType(InvalidKeyType);
}