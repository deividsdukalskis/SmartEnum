namespace SmartEnum.Analyzer;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SmartEnum.Shared;
using System.Linq;

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
				EnumHierarchy.CollectHierarchyData(type, attributeDefinition);
			},
			SymbolKind.NamedType);

			compilationContext.RegisterCompilationEndAction((context) =>
			{
				foreach (Result<HierarchyData, HierarchyError.DuplicateAttributesFound> hierarchyData in EnumHierarchy.HierarchyDatas)
				{
					Result<ValidatedHierarchyData, ImmutableArray<HierarchyError>> validationResult = EnumHierarchy.ValidateHierarchyData(hierarchyData);
					if (validationResult is Failure<ValidatedHierarchyData, ImmutableArray<HierarchyError>> failure)
					{
						foreach (HierarchyError error in failure.Error)
						{
							this.AnalyzeDuplicateAttributes(error, context);
							this.AnalyzeDuplicateKeyNames(error, context);
							this.AnalyzeDuplicateProperties(error, context);
							this.AnalyzeAbstractClass(error, context);
							this.AnalyzeNonAbstractClass(error, context);
							this.AnalyzePartialClass(error, context);
							this.AnalyzeKeyName(error, context);
							this.AnalyzeKeyDefinitionExists(error, context);
							this.AnalyzeKeyDefinitionPublic(error, context);
							this.AnalyzeKeyIsConst(error, context);
							this.AnalyzeKeyFieldType(error, context);
						}
					}
				}
			});
		});
	}
}