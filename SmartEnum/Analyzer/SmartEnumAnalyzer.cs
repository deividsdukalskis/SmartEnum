namespace SmartEnum.Analyzer;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
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

			Lazy<ImmutableArray<DuplicatePropertyData>> duplicateProperties = new(() => DuplicatePropertyData.Collect(compilationContext.Compilation, attributeDefinition), LazyThreadSafetyMode.ExecutionAndPublication);

			compilationContext.RegisterSymbolAction((context) =>
			{
				INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
				Result<HierarchyData, HierarchyError> hierarchyData = HierarchyData.Collect(type, attributeDefinition);
				Result<ValidatedHierarchyData, ImmutableArray<HierarchyError>> validationResult = ValidatedHierarchyData.Validate(hierarchyData, duplicateProperties);
				if (validationResult is Failure<ValidatedHierarchyData, ImmutableArray<HierarchyError>> failure)
				{
					foreach (HierarchyError error in failure.Error)
					{
						if (error is HierarchyError.RelevantAttributeNotFound depth)
							context.ReportDiagnostic(Diagnostic.Create(InvalidHierarchyDepth, depth.Type.Locations[0], depth.Type.Name));
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
			},
			SymbolKind.NamedType);
		});
	}
}