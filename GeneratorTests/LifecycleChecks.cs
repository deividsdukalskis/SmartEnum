using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Analyzer;
using SmartEnum.Generators;

internal static class LifecycleChecks
{
	public static async Task RunAsync(MetadataReference[] references)
	{
		CSharpParseOptions options = new(LanguageVersion.CSharp11);
		const string original = """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root { }
			public partial class Leaf : Root { public const int Id = 1; }
			""";
		CSharpCompilation compilation = CSharpCompilation.Create("Lifecycle", [CSharpSyntaxTree.ParseText(original, options)], references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		SmartEnumGenerator generator = new();
		SmartEnumAttributeGenerator attributeGenerator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create([attributeGenerator.AsSourceGenerator(), generator.AsSourceGenerator()], parseOptions: options);
		driver = driver.RunGenerators(compilation);
		CSharpCompilation edited = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.Single(),
			CSharpSyntaxTree.ParseText(original.Replace("public const int Id = 1;", "public const int Id = 2; public string Added { get; }"), options));
		driver = driver.RunGeneratorsAndUpdateCompilation(edited, out Compilation output, out ImmutableArray<Diagnostic> diagnostics);
		if (diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) || output.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
			throw new Exception("Incremental edit produced compilation errors.");
		if (!driver.GetRunResult().Results[1].GeneratedSources.Any(source => source.SourceText.ToString().Contains("@Added")))
			throw new Exception("Incremental edit did not update factory parameters.");
		GeneratorDriverRunResult reverted = driver.RunGenerators(compilation).GetRunResult();
		if (reverted.Results[1].GeneratedSources.Any(source => source.SourceText.ToString().Contains("@Added")))
			throw new Exception("Reverted source retained stale parameters.");

		SmartEnumAnalyzer analyzer = new();
		await Task.WhenAll(Enumerable.Range(0, 8).Select(index => Task.Run(async () =>
		{
			CSharpCompilation concurrent = compilation.WithAssemblyName("Concurrent" + index);
			GeneratorDriver concurrentDriver = CSharpGeneratorDriver.Create([attributeGenerator.AsSourceGenerator(), generator.AsSourceGenerator()], parseOptions: options);
			concurrentDriver = concurrentDriver.RunGeneratorsAndUpdateCompilation(concurrent, out Compilation result, out ImmutableArray<Diagnostic> errors);
			ImmutableArray<Diagnostic> analysis = await result.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync();
			if (errors.Concat(analysis).Concat(result.GetDiagnostics()).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
				throw new Exception("Concurrent generator/analyzer use failed.");
			if (concurrentDriver.GetRunResult().Results[1].GeneratedSources.Length != 3)
				throw new Exception("Concurrent generation lost output.");
		})));
		Console.WriteLine("PASS incremental edits, reverts, and eight concurrent compilations");
	}
}