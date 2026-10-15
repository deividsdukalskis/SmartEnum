namespace SmartEnum.Generators;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator(LanguageNames.CSharp)]
public sealed partial class SmartEnumGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		IncrementalValuesProvider<INamedTypeSymbol> types = context.SyntaxProvider.CreateSyntaxProvider(
			static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
			static (ctx, ct) => (INamedTypeSymbol)ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, ct)!);
		context.RegisterSourceOutput(context.CompilationProvider.Combine(types.Collect()), static (output, input) =>
			Generate(new GenerationContext(output.CancellationToken, (name, source) => output.AddSource(name, source), _ => { }), input.Left, input.Right));
	}

	internal static void Generate(GenerationContext output, Compilation compilation, ImmutableArray<INamedTypeSymbol> symbols)
	{
		INamedTypeSymbol? attribute = compilation.GetTypeByMetadataName("SmartEnum.SmartEnumAttribute`1");
		if (attribute is null) return;
		foreach (INamedTypeSymbol type in symbols.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default))
		{
			foreach (AttributeData applied in GetAttributes(type, attribute).Where(item => item.ConstructorArguments.Length != 3))
				output.ReportDiagnostic(Diagnostic.Create(StrategyError, applied.ApplicationSyntaxReference?.GetSyntax().GetLocation(), "SmartEnum requires a key name, flattening strategy and missing-branch policy. The one-argument API has been removed."));
		}

		GenerateStrategyHierarchies(output, compilation, symbols, attribute);
	}
}