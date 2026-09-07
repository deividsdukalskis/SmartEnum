namespace SmartEnum.Analyzer;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

public sealed partial class SmartEnumAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
		MustBePartialClass,
		MustBeAbstractClass,
		InvalidKeyName,
		KeyFieldDoesNotExist,
		KeyIsNotPublic,
		KeyIsNotConst,
		InvalidKeyType,
		DuplicateAttributes,
		DuplicateKeyNames,
		MustNotBeAbstractClass,
		DuplicateProperties);

	private DiagnosticDescriptor MustBeAbstractClass = new(
		id: "SMARTENUM001",
		title: "Class must be abstract",
		messageFormat:
			"Class must be defined abstract",
		category: "SmartEnum",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

	private DiagnosticDescriptor MustBePartialClass = new(
		id: "SMARTENUM002",
		title: "Class must be partial",
		messageFormat:
			"Class must be defined partial",
		category: "SmartEnum",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

	private DiagnosticDescriptor InvalidKeyName = new(
		id: "SMARTENUM003",
		title: "Invalid key name",
		messageFormat:
			"'{0}' may not resolve to a proper field name",
		category: "SmartEnum",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

	private DiagnosticDescriptor KeyFieldDoesNotExist = new(
		id: "SMARTENUM004",
		title: "Key does not exist",
		messageFormat:
			"Class must define key 'public const {0} {1}'",
		category: "SmartEnum",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

	private DiagnosticDescriptor KeyIsNotPublic = new(
		id: "SMARTENUM005",
		title: "Key is not public",
		messageFormat:
			"Field must be declared public",
		category: "SmartEnum",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

	private DiagnosticDescriptor KeyIsNotConst = new(
		id: "SMARTENUM006",
		title: "Key is not const",
		messageFormat:
			"Field must be declared const",
		category: "SmartEnum",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

	private DiagnosticDescriptor InvalidKeyType = new(
		id: "SMARTENUM007",
		title: "Invalid key type",
		messageFormat:
			"Field must have type '{0}', but is declared as '{1}'",
		category: "SmartEnum",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

	private DiagnosticDescriptor DuplicateAttributes = new(
		id: "SMARTENUM008",
		title: "Duplicate attributes",
		messageFormat:
			"Parent class '{0}' already applies SmartEnum attributes",
		category: "SmartEnum",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

	private DiagnosticDescriptor DuplicateKeyNames = new(
		id: "SMARTENUM009",
		title: "Duplicate key names",
		messageFormat:
			"Duplicate key names '{0}' found",
		category: "SmartEnum",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

	private DiagnosticDescriptor MustNotBeAbstractClass = new(
		id: "SMARTENUM010",
		title: "Class must not be abstract",
		messageFormat:
			"Class must not be defined abstract",
		category: "SmartEnum",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

	private DiagnosticDescriptor DuplicateProperties = new(
		id: "SMARTENUM011",
		title: "Property must have unique name",
		messageFormat:
			"'{0}' is already defined in {1}. Consider moving that property to shared parent class '{2}' or higher up the inheritance chain.",
		category: "SmartEnum",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });
}