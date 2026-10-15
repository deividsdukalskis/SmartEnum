using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Analyzer;
using SmartEnum.Generators;

PortableExecutableReference[] references = [.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path))];
CSharpParseOptions parseOptions = new(LanguageVersion.CSharp11);
int passed = 0;

async Task Check(string name, string source, string? expectedDiagnostic = null, int? generatedTypes = null)
{
	CSharpCompilation compilation = CSharpCompilation.Create("Case_" + passed,
		source.Split("// ---file---", StringSplitOptions.None).Select((part, index) => CSharpSyntaxTree.ParseText(part, parseOptions, "Case" + index + ".cs")), references,
		new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
	GeneratorDriver driver = CSharpGeneratorDriver.Create(
		[new SmartEnumAttributeGenerator().AsSourceGenerator(), new SmartEnumGenerator().AsSourceGenerator()], parseOptions: parseOptions);
	driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation? output, out ImmutableArray<Diagnostic> generatorDiagnostics);
	ImmutableArray<Diagnostic> analyzerDiagnostics = await output.WithAnalyzers([new SmartEnumAnalyzer()]).GetAnalyzerDiagnosticsAsync();
	Diagnostic[] diagnostics = [.. generatorDiagnostics, .. analyzerDiagnostics];
	if (diagnostics.Any(diagnostic => diagnostic.Id is "CS8785" or "AD0001"))
		throw new Exception(name + ": generator/analyzer crashed: " + string.Join("\n", diagnostics.AsEnumerable()));
	if (expectedDiagnostic is not null)
	{
		if (!diagnostics.Any(diagnostic => diagnostic.Id == expectedDiagnostic))
			throw new Exception(name + ": missing " + expectedDiagnostic + "\n" + string.Join("\n", diagnostics.AsEnumerable()));
	}
	else
	{
		Diagnostic[] errors = [.. output.GetDiagnostics().Concat(diagnostics).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
		if (errors.Length != 0) throw new Exception(name + ": " + string.Join("\n", errors.AsEnumerable()));
		foreach (SyntaxTree tree in output.SyntaxTrees.Where(tree => tree.FilePath.EndsWith(".SmartEnum.g.cs", StringComparison.Ordinal)))
		{
			SemanticModel model = output.GetSemanticModel(tree);
			foreach (Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax method in tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>())
			{
				IMethodSymbol symbol = (IMethodSymbol)model.GetDeclaredSymbol(method)!;
				if (symbol.DeclaredAccessibility != Accessibility.Public) continue;
				foreach (IParameterSymbol parameter in symbol.Parameters.Where(parameter => parameter.Name.StartsWith("map", StringComparison.Ordinal)))
				{
					string xml = symbol.GetDocumentationCommentXml() ?? "";
					if (!xml.Contains("name=\"" + parameter.Name + "\"", StringComparison.Ordinal)) throw new Exception(name + ": missing parameter documentation for " + symbol + "." + parameter.Name);
				}
			}
		}

		using MemoryStream assemblyBytes = new();
		Microsoft.CodeAnalysis.Emit.EmitResult emit = output.Emit(assemblyBytes);
		if (!emit.Success) throw new Exception(name + ": emit failed: " + string.Join("\n", emit.Diagnostics));
		Diagnostic[] generatedWarnings = [.. output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning
			&& diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true)];
		if (generatedWarnings.Length > 0) throw new Exception(name + ": generated warnings: " + string.Join("\n", generatedWarnings.Select(warning => warning + "\n" + warning.Location.SourceTree!.GetText().Lines[warning.Location.GetLineSpan().StartLinePosition.Line].ToString())));
		AssemblyLoadContext loadContext = new(name, isCollectible: true);
		try
		{
			assemblyBytes.Position = 0;
			Assembly assembly = loadContext.LoadFromStream(assemblyBytes);
			_ = (assembly.GetType("RegressionProbe")?.GetMethod("Verify")?.Invoke(null, null));
		}
		finally { loadContext.Unload(); }
	}

	if (generatedTypes is { } expected && driver.GetRunResult().Results[1].GeneratedSources.Length != expected)
		throw new Exception(name + ": unexpected generated source count");
	// Reusing the same driver must not retain mutable state from a previous compilation.
	GeneratorDriverRunResult rerun = driver.RunGenerators(compilation).GetRunResult();
	if (!driver.GetRunResult().Results.SelectMany(result => result.GeneratedSources).Select(result => result.SourceText.ToString())
		.SequenceEqual(rerun.Results.SelectMany(result => result.GeneratedSources).Select(result => result.SourceText.ToString())))
	{
		throw new Exception(name + ": repeated generation is not deterministic");
	}

	GeneratorDriver removed = driver.RunGenerators(compilation.RemoveAllSyntaxTrees());
	if (removed.GetRunResult().Results[1].GeneratedSources.Length != 0) throw new Exception(name + ": stale generation after source removal");
	using CancellationTokenSource cancelled = new();
	cancelled.Cancel();
	try
	{
		_ = driver.RunGenerators(compilation, cancelled.Token);
		throw new Exception(name + ": cancellation was ignored");
	}
	catch (OperationCanceledException) { }

	passed++;
	Console.WriteLine("PASS " + name);
}

foreach (RegressionCase test in SeparateCases.All.Concat(CompositeCases.All).Concat(CurrentContractCases.All).Concat(ReusableParentCases.All).Concat(ExternalBaseCases.All)) await Check(test.Name, test.Source, test.Diagnostic);
await LifecycleChecks.RunAsync(references);
await DiagnosticChecks.RunAsync(references);
Console.WriteLine($"All {passed} current-contract generator/analyzer regression cases and lifecycle checks passed.");