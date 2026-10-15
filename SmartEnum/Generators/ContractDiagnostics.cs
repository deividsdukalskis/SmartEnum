namespace SmartEnum.Generators;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

public sealed partial class SmartEnumGenerator
{
	internal static ImmutableArray<DiagnosticDescriptor> ContractRules => ImmutableArray.Create(
		RuleSMARTENUM001,
		RuleSMARTENUM002,
		RuleSMARTENUM003,
		RuleSMARTENUM004,
		RuleSMARTENUM005,
		RuleSMARTENUM006,
		RuleSMARTENUM007,
		RuleSMARTENUM009,
		RuleSMARTENUM010,
		RuleSMARTENUM011,
		RuleSMARTENUM012,
		RuleSMARTENUM013,
		RuleSMARTENUM014, StrategyError, IdentityWarning, RuleSMARTENUM017, RuleSMARTENUM018, RuleSMARTENUM019, RuleSMARTENUM020, RuleSMARTENUM021, RuleSMARTENUM022, RuleSMARTENUM023, RuleSMARTENUM024, RuleSMARTENUM025, RuleSMARTENUM026, RuleSMARTENUM027, RuleSMARTENUM028, RuleSMARTENUM029, RuleSMARTENUM030, RuleSMARTENUM031, RuleSMARTENUM032, RuleSMARTENUM033, RuleSMARTENUM034, RuleSMARTENUM035, RuleSMARTENUM036, RuleSMARTENUM037, RuleSMARTENUM038, RuleSMARTENUM039, RuleSMARTENUM040, RuleSMARTENUM041, RuleSMARTENUM042, RuleSMARTENUM043, RuleSMARTENUM044, RuleSMARTENUM045, RuleSMARTENUM046, RuleSMARTENUM047);

	internal static readonly DiagnosticDescriptor RuleSMARTENUM001 = new("SMARTENUM001", "Hierarchy class must be abstract", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM002 = new("SMARTENUM002", "Hierarchy class must be partial", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM003 = new("SMARTENUM003", "Invalid discriminator name", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM004 = new("SMARTENUM004", "Missing discriminator field", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM005 = new("SMARTENUM005", "Discriminator field must be public", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM006 = new("SMARTENUM006", "Discriminator field must be constant", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM007 = new("SMARTENUM007", "Discriminator type mismatch", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM009 = new("SMARTENUM009", "Duplicate discriminator names", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM010 = new("SMARTENUM010", "Abstract branch has no concrete descendant", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM011 = new("SMARTENUM011", "Duplicate transfer property names", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM012 = new("SMARTENUM012", "Unsupported declaration", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM013 = new("SMARTENUM013", "Duplicate discriminator combination", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM014 = new("SMARTENUM014", "Invalid hierarchy depth", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM017 = new("SMARTENUM017", "Parent class must be sealed", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM018 = new("SMARTENUM018", "Primary constructor requires explicit integration", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM019 = new("SMARTENUM019", "Ordinary base state cannot be reconstructed", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM020 = new("SMARTENUM020", "Invalid parent strategy", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM021 = new("SMARTENUM021", "Generic variant cannot be inferred", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM022 = new("SMARTENUM022", "Inaccessible hierarchy variant", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM023 = new("SMARTENUM023", "Invalid FirstBranch position", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM024 = new("SMARTENUM024", "Required member cannot be ignored", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM025 = new("SMARTENUM025", "Multiple identity properties", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM026 = new("SMARTENUM026", "Member conflicts with flattened class name", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM027 = new("SMARTENUM027", "Property requires readable storage", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM028 = new("SMARTENUM028", "Invalid private accessor", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM029 = new("SMARTENUM029", "Unsupported transfer member", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM030 = new("SMARTENUM030", "Inaccessible transfer member type", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM031 = new("SMARTENUM031", "Shared private accessor field", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM032 = new("SMARTENUM032", "Reserved generated member name", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM033 = new("SMARTENUM033", "Flattened type already exists", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM034 = new("SMARTENUM034", "Incompatible shared data constructor", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM035 = new("SMARTENUM035", "Conflicting transfer constructor", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM036 = new("SMARTENUM036", "Incompatible flattening strategies", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM037 = new("SMARTENUM037", "Discriminator conflicts with data member", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM038 = new("SMARTENUM038", "Discriminator conflicts with model name", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM039 = new("SMARTENUM039", "Duplicate nested model type", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM040 = new("SMARTENUM040", "Invalid generated property name", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM041 = new("SMARTENUM041", "Inaccessible owner model type", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM042 = new("SMARTENUM042", "Flattened layout name collision", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM043 = new("SMARTENUM043", "Construction callback name collision", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM044 = new("SMARTENUM044", "Flatten callback name collision", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM045 = new("SMARTENUM045", "Generic ownership requires Separate parent", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM046 = new("SMARTENUM046", "Shared hierarchy requires Separate parents", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor RuleSMARTENUM047 = new("SMARTENUM047", "Parent has no hierarchy property", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	private static DiagnosticDescriptor ContractRule(string id) => ContractRules.First(rule => rule.Id == id);
}