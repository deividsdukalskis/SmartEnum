namespace SmartEnum.Generators;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SmartEnum.Generators.Models;
using SmartEnum.Shared;

[Generator(LanguageNames.CSharp)]
public sealed partial class SmartEnumGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		IncrementalValueProvider<ImmutableArray<GenerationModel>> values = context.SyntaxProvider.ForAttributeWithMetadataName(
			"SmartEnum.SmartEnumAttribute`1",
			(node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
			(attributeContext, cancellationToken) =>
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (attributeContext.TargetSymbol is not INamedTypeSymbol type)
				{
					return null;
				}

				ValidationResult result = SharedFunctions.Validate(type, attributeContext.Attributes[0].AttributeClass!.OriginalDefinition);
				if (!result.IsInEnumHierarchy || !result.HasAllChecksPassed || result.BaseClass is null)
				{
					return null;
				}

				IEnumerable<IPropertySymbol> properties = result.BaseClass.GetMembers()
					.Where(member => member.Kind is SymbolKind.Property)
					.Select(prop => (IPropertySymbol)prop);
				GenerationModel generationModel = new(result.BaseClass, result.IsBaseClass ? null : result.OriginalClass, result.FieldValidationResult.FieldInfo, properties.ToImmutableArray());
				return generationModel;
			})
			.Where((result) => result is not null)
			.Select((result, cancellationToken) =>
			{
				cancellationToken.ThrowIfCancellationRequested();
				return result!;
			})
			.Collect();

		context.RegisterSourceOutput(
			values,
			(productionContext, models) =>
			{
				ParallelLoopResult result = Parallel.ForEach(models, (model) =>
				{
					this.GenerateDerivedAbstractClassConstructor(productionContext, model);
					this.GenerateDerivedClassConstructorAndFactoryMethod(productionContext, model);
				});

				if (!result.IsCompleted)
				{
					return;
				}

				GenerationModel? baseModel = models.FirstOrDefault(model => model.DerivedClass is null);
				if (baseModel is null)
				{
					return;
				}

				this.GenerateBaseClassConstructorAndFactoryMethod(productionContext, baseModel, models.Where(model => model.DerivedClass is not null).ToImmutableArray());
			});
	}
}