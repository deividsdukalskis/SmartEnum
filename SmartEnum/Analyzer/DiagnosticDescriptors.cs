namespace SmartEnum.Analyzer;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using SmartEnum.Generators;

public sealed partial class SmartEnumAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SmartEnumGenerator.ContractRules;
}