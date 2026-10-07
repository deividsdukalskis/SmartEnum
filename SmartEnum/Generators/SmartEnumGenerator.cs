namespace SmartEnum.Generators;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SmartEnum.Shared;

[Generator(LanguageNames.CSharp)]
public sealed partial class SmartEnumGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		IncrementalValuesProvider<INamedTypeSymbol> types = context.SyntaxProvider.CreateSyntaxProvider(
			static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
			static (ctx, ct) => (INamedTypeSymbol)ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, ct)!);

		context.RegisterSourceOutput(context.CompilationProvider.Combine(types.Collect()), static (output, input) =>
		{
			(Compilation? compilation, ImmutableArray<INamedTypeSymbol> symbols) = input;
			INamedTypeSymbol? attribute = compilation.GetTypeByMetadataName("SmartEnum.SmartEnumAttribute`1");
			if (attribute is null) return;

			Lazy<ImmutableArray<DuplicatePropertyData>> duplicates = new(() => DuplicatePropertyData.Collect(compilation, attribute, output.CancellationToken));
			HashSet<ISymbol> seen = new(SymbolEqualityComparer.Default);
			Dictionary<INamedTypeSymbol, ValidatedHierarchyData> valid = new(SymbolEqualityComparer.Default);
			foreach (INamedTypeSymbol? type in symbols)
			{
				output.CancellationToken.ThrowIfCancellationRequested();
				if (seen.Add(type) && ValidatedHierarchyData.Validate(HierarchyData.Collect(type, attribute), duplicates)
					is Success<ValidatedHierarchyData, ImmutableArray<HierarchyError>> success)
				{
					valid.Add(type, success.Value);
				}
			}

			foreach (ValidatedHierarchyData.BaseTypeData root in valid.Values.OfType<ValidatedHierarchyData.BaseTypeData>())
			{
				output.CancellationToken.ThrowIfCancellationRequested();
				INamedTypeSymbol[] members = valid.Keys.Where(type => GetPath(type, root.Type).Count > 0)
					.OrderBy(type => type.ToDisplayString(), StringComparer.Ordinal).ToArray();
				// A descendant cannot be generated unless its entire constructor chain is valid.
				members = members.Where(type => GetPath(type, root.Type).All(ancestor => valid.ContainsKey(ancestor.OriginalDefinition))).ToArray();
				if (!ValidateGeneration(output, compilation, root.Type, members, attribute)) continue;
				INamedTypeSymbol[] mappedMembers = members.Select(type => MapToRoot(type, root.Type)!).ToArray();
				output.AddSource("Flattened." + GetHintName(root.Type), GenerateFlattenedModel(root.Type, mappedMembers, attribute));
				foreach (INamedTypeSymbol? type in members)
				{
					output.CancellationToken.ThrowIfCancellationRequested();
					string body = valid[type] switch
					{
						ValidatedHierarchyData.BaseTypeData => GenerateBaseClass(type, mappedMembers, attribute),
						ValidatedHierarchyData.DerivedConcreteTypeData => GenerateDerivedConcreteClass(type, root.Type),
						_ => GenerateDerivedAbstractClass(type, root.Type)
					};
					output.AddSource(GetHintName(type), WrapType(type, body + "\n\n" + GenerateFlattenWriter(type, root.Type, mappedMembers, attribute, compilation)));
				}
			}
		});
	}
}