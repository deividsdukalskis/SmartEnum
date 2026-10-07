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
		using MemoryStream assemblyBytes = new();
		Microsoft.CodeAnalysis.Emit.EmitResult emit = output.Emit(assemblyBytes);
		if (!emit.Success) throw new Exception(name + ": emit failed: " + string.Join("\n", emit.Diagnostics));
		Diagnostic[] generatedWarnings = [.. output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning
			&& diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true)];
		if (generatedWarnings.Length > 0) throw new Exception(name + ": generated warnings: " + string.Join("\n", generatedWarnings.AsEnumerable()));
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

const string simple = """
using SmartEnum;
[SmartEnum<int>("Id")] public abstract partial class Root { public string Name { get; } }
public partial class Leaf : Root { public const int Id = 1; public int Number { get; private set; } }
""";
await Check("single key, global namespace, get-only and private setters", simple, generatedTypes: 3);
await Check("partial declarations are deduplicated", simple + "\npublic partial class Leaf { }", generatedTypes: 3);
await Check("nullable, init, required, escaped identifiers and computed properties", """
using SmartEnum;
[SmartEnum<string>("Kind")] public abstract partial class Root { public required string @event { get; init; } }
public partial class Leaf : Root {
 public const string Kind = "value";
 public string? Optional { get; private set; }
 public string Computed => "computed";
 public static int Static { get; set; }
 public int this[int index] => index;
}
""", generatedTypes: 3);
await Check("nested partial classes", """
using SmartEnum;
namespace Example;
public static partial class Container {
 [SmartEnum<int>("Id")] public abstract partial class Root { }
 public partial class Leaf : Root { public const int Id = 1; }
}
""", generatedTypes: 3);
await Check("non-SmartEnum ancestor does not consume a key level", """
using SmartEnum;
public abstract class Entity { protected Entity() { } }
[SmartEnum<int>("Id")] public abstract partial class Root : Entity { }
public partial class Leaf : Root { public const int Id = 1; }
""", generatedTypes: 3);
await Check("three key levels", """
using SmartEnum;
[SmartEnum<int>("A")][SmartEnum<string>("B")][SmartEnum<char>("C")]
public abstract partial class Root { }
public abstract partial class Middle : Root { public const int A = 1; }
public abstract partial class Middle2 : Middle { public const string B = "two"; }
public partial class Leaf : Middle2 { public const char C = '3'; }
""", generatedTypes: 5);
await Check("no hierarchy", "public class Ordinary { }", generatedTypes: 0);
await Check("non-abstract root", simple.Replace("abstract partial class Root", "partial class Root"), "SMARTENUM001");
await Check("non-partial leaf", simple.Replace("partial class Leaf", "class Leaf"), "SMARTENUM002");
await Check("null key name", simple.Replace("\"Id\"", "null"), "SMARTENUM003");
await Check("empty key name", simple.Replace("\"Id\"", "\"\""), "SMARTENUM003");
await Check("invalid key name", simple.Replace("\"Id\"", "\"bad name\""), "SMARTENUM003");
await Check("missing key", simple.Replace("public const int Id = 1;", ""), "SMARTENUM004");
await Check("private key", simple.Replace("public const int Id", "private const int Id"), "SMARTENUM005");
await Check("nonconstant key", simple.Replace("public const int Id", "public static int Id"), "SMARTENUM006");
await Check("wrong key type", simple.Replace("const int Id", "const long Id"), "SMARTENUM007");
await Check("attributes repeated down hierarchy", simple.Replace("public partial class Leaf", "[SmartEnum<int>(\"Other\")] public partial class Leaf"), "SMARTENUM008");
await Check("duplicate key names", simple.Replace("[SmartEnum<int>(\"Id\")]", "[SmartEnum<int>(\"Id\")][SmartEnum<int>(\"Id\")]"), "SMARTENUM009");
await Check("abstract leaf", simple.Replace("public partial class Leaf", "public abstract partial class Leaf"), "SMARTENUM010");
await Check("duplicate ancestor property", simple.Replace("public int Number", "public string Name"), "SMARTENUM011");
await Check("duplicate sibling property", simple + "\npublic partial class Other : Root { public const int Id = 2; public int Number { get; set; } }", "SMARTENUM011");
await Check("generic root", """
using SmartEnum;
[SmartEnum<int>("Id")] public abstract partial class Root<T> { }
public partial class Leaf<T> : Root<T> { public const int Id = 1; }
""", generatedTypes: 3);
await Check("existing constructor conflict", simple.Replace("public const int Id", "public Leaf(string Name, int Number) : base(Name) { this.Number = Number; } public const int Id"), "SMARTENUM012");
await Check("ambiguous full keys", simple + "\npublic partial class Other : Root { public const int Id = 1; }", "SMARTENUM013");
await Check("too many inheritance levels", simple + "\npublic partial class TooDeep : Leaf { }", "SMARTENUM014");
await Check("static root", "using SmartEnum; [SmartEnum<int>(\"Id\")] public static partial class Root { }", "SMARTENUM001");
await Check("non-partial enclosing class", """
using SmartEnum;
public class Container {
 [SmartEnum<int>("Id")] public abstract partial class Root { }
 public partial class Leaf : Root { public const int Id = 1; }
}
""", "SMARTENUM012");
await Check("enum keys and null constant string keys", """
using SmartEnum;
public enum Code { One }
[SmartEnum<Code>("A")][SmartEnum<string>("B")] public abstract partial class Root { }
public abstract partial class Middle : Root { public const Code A = Code.One; }
public partial class Leaf : Middle { public const string B = null!; }
""", generatedTypes: 4);
await Check("same class name in separate namespaces", """
using SmartEnum;
namespace One { [SmartEnum<int>("Id")] public abstract partial class Root { }
public partial class Leaf : Root { public const int Id = 1; } }
namespace Two { [SmartEnum<int>("Id")] public abstract partial class Root { }
public partial class Leaf : Root { public const int Id = 2; } }
""", generatedTypes: 6);
foreach (RegressionCase test in RegressionCases.All.Concat(EdgeCases.All).Concat(PrivateFieldCases.All).Concat(FlattenedCases.All).Concat(TransferSafetyCases.All)) await Check(test.Name, test.Source, test.Diagnostic);
await LifecycleChecks.RunAsync(references);
Console.WriteLine($"All {passed} generator/analyzer regression cases and lifecycle checks passed.");