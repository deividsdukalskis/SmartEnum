using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Analyzer;
using SmartEnum.Generators;

internal static class DiagnosticChecks
{
	public static async Task RunAsync(PortableExecutableReference[] references)
	{
		SmartEnumAnalyzer analyzer = new();
		string[] expected = [.. Enumerable.Range(1, 47).Where(id => id != 8).Select(id => $"SMARTENUM{id:000}")];
		if (!analyzer.SupportedDiagnostics.Select(rule => rule.Id).Order().SequenceEqual(expected)) throw new Exception("Analyzer rule registration is incomplete.");
		const string source = """
            using SmartEnum;
            [SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)]
            public abstract partial class Root { }
            public sealed partial class Leaf : Root { public const string Kind = "wrong type"; }
            """;
		CSharpParseOptions parse = new(LanguageVersion.CSharp11);
		CSharpCompilation compilation = CSharpCompilation.Create("Diagnostics", [CSharpSyntaxTree.ParseText(source, parse, "ClosedFile.cs")], references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new SmartEnumAttributeGenerator().AsSourceGenerator(), new SmartEnumGenerator().AsSourceGenerator()], parseOptions: parse);
		_ = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out ImmutableArray<Diagnostic> generatorDiagnostics);
		ImmutableArray<Diagnostic> diagnostics = await output.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync();
		if (generatorDiagnostics.Any() || diagnostics.Length != 1 || diagnostics[0].Id != "SMARTENUM007" || diagnostics[0].Location.GetLineSpan().Path != "ClosedFile.cs")
			throw new Exception("Expected one analyzer error located in the user's source: " + string.Join("\n", diagnostics));
		Compilation suppressed = output.WithOptions(output.Options.WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic> { ["SMARTENUM007"] = ReportDiagnostic.Suppress }));
		if ((await suppressed.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync()).Any()) throw new Exception("Analyzer ignores diagnostic suppression.");
		Compilation warning = output.WithOptions(output.Options.WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic> { ["SMARTENUM007"] = ReportDiagnostic.Warn }));
		if ((await warning.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync()).Single().Severity != DiagnosticSeverity.Warning) throw new Exception("Analyzer ignores configured severity.");
		const string distinctSource = """
            using SmartEnum;
            [SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { }
            public sealed partial class Leaf : Root {
                public const int Kind = 1;
                [SmartEnumPrivateAccessor("missing")] public int Value => 1;
                [SmartEnumIgnore] public required int Required { get; init; }
            }
            [SmartEnumParent(FlatteningStrategy.Separate)] public partial class Owner { public Root First { get; } public Root Second { get; } }
            """;
		compilation = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(CSharpSyntaxTree.ParseText(distinctSource, parse, "IndependentErrors.cs"));
		_ = driver.RunGeneratorsAndUpdateCompilation(compilation, out output, out generatorDiagnostics);
		diagnostics = await output.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync();
		if (!diagnostics.Select(item => item.Id).Order().SequenceEqual(["SMARTENUM017", "SMARTENUM024", "SMARTENUM028"]))
			throw new Exception("Unrelated failures must have distinct codes without duplicate reports: " + string.Join("\n", diagnostics));
		Console.WriteLine("PASS all active rules registered, source locations, no duplicate reports, suppression and severity configuration");
	}
}