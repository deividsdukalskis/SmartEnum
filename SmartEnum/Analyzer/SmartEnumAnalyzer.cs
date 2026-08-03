namespace SmartEnum.Analyzer;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class SmartEnumAnalyzer : DiagnosticAnalyzer
{
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(
			GeneratedCodeAnalysisFlags.None);

		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(compilationContext =>
		{
			INamedTypeSymbol? attributeDefinition =
				compilationContext.Compilation.GetTypeByMetadataName(
					"SmartEnum.SmartEnumAttribute`1");

			// The project being analyzed does not reference the attribute.
			if (attributeDefinition is null)
			{
				return;
			}

			compilationContext.RegisterSymbolAction((context) =>
			{
				INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
				ValidationResult result = SharedFunctions.Validate(type, attributeDefinition);
				if (!result.IsInEnumHierarchy)
				{
					return;
				}

				this.AnalyzeDuplicateAttributes(result, context);
				this.AnalyzeAbstractClass(result, context);
				this.AnalyzePartialClass(result, context);
				this.AnalyzeKeyName(result, context);
				this.AnalyzeKeyField(result, context);
			},
			SymbolKind.NamedType);
		});
	}
}