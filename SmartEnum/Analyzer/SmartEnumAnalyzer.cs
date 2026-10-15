namespace SmartEnum.Analyzer;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class SmartEnumAnalyzer : DiagnosticAnalyzer
{
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationAction(context =>
		{
			// Validate user declarations, not members emitted into their partial classes.
			Compilation source = context.Compilation.RemoveSyntaxTrees(context.Compilation.SyntaxTrees
				.Where(tree => tree.FilePath.EndsWith(".SmartEnum.g.cs", System.StringComparison.Ordinal)).ToArray());
			ImmutableArray<INamedTypeSymbol> types = source.SyntaxTrees.SelectMany(tree =>
			{
				context.CancellationToken.ThrowIfCancellationRequested();
				// A new compilation is required to exclude generated partial members.
#pragma warning disable RS1030
				SemanticModel model = source.GetSemanticModel(tree);
#pragma warning restore RS1030
				return tree.GetRoot(context.CancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>()
					.Where(node => node is ClassDeclarationSyntax or RecordDeclarationSyntax)
					.Select(node => model.GetDeclaredSymbol(node, context.CancellationToken)).OfType<INamedTypeSymbol>();
			}).ToImmutableArray();
			SmartEnumGenerator.Generate(new GenerationContext(context.CancellationToken, (_, _) => { }, context.ReportDiagnostic), source, types);
		});
	}
}