namespace SmartEnum.Generators;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using SmartEnum.Shared;

public sealed partial class SmartEnumGenerator
{
	internal static readonly DiagnosticDescriptor StrategyError = new("SMARTENUM015", "Invalid SmartEnum attribute arguments", "{0}", "SmartEnum", DiagnosticSeverity.Error, true);
	internal static readonly DiagnosticDescriptor IdentityWarning = new("SMARTENUM016", "Ambiguous foreign key name", "Foreign key name '{0}' may conflict with another flattened identity; specify a descriptive foreignKeyName", "SmartEnum", DiagnosticSeverity.Warning, true);

	private static void GenerateStrategyHierarchies(GenerationContext output, Compilation compilation, ImmutableArray<INamedTypeSymbol> symbols, INamedTypeSymbol attribute)
	{
		INamedTypeSymbol[] all = symbols.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default).ToArray();
		HashSet<INamedTypeSymbol> composed = GenerateCompositeHierarchies(output, compilation, all, attribute);
		foreach (INamedTypeSymbol root in all.Where(type => GetAttributes(type, attribute).Any(item => item.ConstructorArguments.Length == 3)
			&& (type.BaseType is null || !StrategyContract.IsExplicit(type.BaseType))))
		{
			output.CancellationToken.ThrowIfCancellationRequested();
			if (composed.Contains(root)) continue;
			INamedTypeSymbol[] members = all.Where(type => GetPath(type, root).Count > 0).OrderBy(type => type.ToDisplayString(), StringComparer.Ordinal).ToArray();
			if (!ValidateCompositeContract(output, compilation, root, members, attribute, false)) continue;
			AttributeData[] keys = GetAttributes(root, attribute);
			string keyProperties = string.Join("\n", keys.Select(key => $"public {SeparateKeyType(key)} {FlattenedIdentifier((string)key.ConstructorArguments[0].Value!)} {{ get; set; }} = default!;"));
			string commonProperties = string.Join("\n", StrategyData(root).Select(SeparateProperty));
			output.AddSource("Separate.Flattened." + GetHintName(root), WrapDeclaration(root, StrategyDtoDeclaration(root, true), commonProperties + "\n" + keyProperties));
			foreach (INamedTypeSymbol type in members)
			{
				output.CancellationToken.ThrowIfCancellationRequested();
				bool isRoot = SameDefinition(type, root);
				string body = StrategyConstructor(type, root, !type.IsAbstract) + "\n" + StrategyReaders(type);
				if (isRoot) body += "\n" + SeparateRootApi(root, members, keys);
				if (!type.IsAbstract)
				{
					List<INamedTypeSymbol> path = GetPath(type, root);
					body += "\n" + SeparateConcreteApi(type, root, members, keys);
					string properties = string.Join("\n", path.Skip(1).SelectMany(StrategyData).Select(SeparateProperty));
					output.AddSource("Separate.Flattened." + GetHintName(type), WrapDeclaration(type, StrategyDtoDeclaration(type, false, FlattenedTypeName(path[0])), properties));
				}

				output.AddSource(GetHintName(type), WrapType(type, body));
			}
		}
	}

	private static string SeparateProperty(TransferMember member) => $"public {StrategyNullableType(member)} {Identifier(member.Name)} {{ get; set; }} = default!;";
	private static string SeparateKeyType(AttributeData key) => TypeName(key.AttributeClass!.TypeArguments[0]) + ((int)key.ConstructorArguments[2].Value! == 0 ? "?" : string.Empty);
	private static int[] SeparateMissingKeyCallbacks(INamedTypeSymbol root, INamedTypeSymbol[] members, AttributeData[] keys) => Enumerable.Range(0, keys.Length)
		.Where(index => (int)keys[index].ConstructorArguments[2].Value! == 1 && members.Any(type => !type.IsAbstract && GetPath(type, root).Count <= index + 1)).ToArray();
	private static string SeparateKeyCallback(AttributeData key) => "map" + FlattenedName((string)key.ConstructorArguments[0].Value!);
	private static string SeparateFlattenParameters(INamedTypeSymbol root, INamedTypeSymbol[] members, AttributeData[] keys) => string.Join(", ", SeparateMissingKeyCallbacks(root, members, keys)
		.Select(index => $"global::System.Func<{TypeName(keys[index].AttributeClass!.TypeArguments[0])}> {SeparateKeyCallback(keys[index])}"));

	private static string SeparateRootApi(INamedTypeSymbol root, INamedTypeSymbol[] members, AttributeData[] keys)
	{
		INamedTypeSymbol[] mapped = members.Select(member => MapToRoot(member, root)!).ToArray();
		TransferMember[] callbacks = mapped.Where(type => !type.IsAbstract).SelectMany(type => StrategyData(type, root)).Where(member => member.NeedsFallback)
			.GroupBy(member => member.Name).Select(group => group.First()).ToArray();
		string parameters = FlattenedTypeName(root) + " flattened" + string.Concat(callbacks.Select(member => ", " + StrategyFallback(member)));
		IEnumerable<string> branches = mapped.Where(type => !type.IsAbstract).Select((type, number) =>
		{
			List<INamedTypeSymbol> path = GetPath(type, root);
			string variable = "variant" + number;
			IEnumerable<string> matches = path.Skip(1).Select((node, index) =>
				$"global::System.Collections.Generic.EqualityComparer<{SeparateKeyType(keys[index])}>.Default.Equals(flattened.{FlattenedIdentifier((string)keys[index].ConstructorArguments[0].Value!)}, {TypeName(node)}.{Identifier((string)keys[index].ConstructorArguments[0].Value!)})");
			return $"if (flattened is {FlattenedTypeName(type)} {variable} && {string.Join(" && ", matches)}) return {TypeName(type)}.ConstructUnvalidated({variable}{string.Concat(StrategyData(type, root).Where(member => member.NeedsFallback).Select(member => ", " + member.Parameter))});";
		});
		return $$"""
			/// <summary>Retains the transfer object supplied during unvalidated construction.</summary>
			protected {{FlattenedTypeName(root)}}? FlattenedData { get; init; }

			/// <summary>Warning: Reconstructs domain data without domain validation. Use only for plain data transfer, such as ORM materialization.</summary>
			/// <remarks>Returns null when the concrete transfer type and discriminator values do not match a known variant.</remarks>
			{{string.Join("\n", callbacks.Select(SeparateCallbackDoc))}}
			public static {{TypeName(root)}}? ConstructUnvalidated({{parameters}})
			{
				if (flattened is null) throw new global::System.ArgumentNullException(nameof(flattened));
				{{Indent(string.Join("\n", branches))}}
				return null;
			}

			/// <summary>Copies the concrete variant's complete data into its separate persistence model.</summary>
			{{string.Join("\n", SeparateMissingKeyCallbacks(root, members, keys).Select(index => $"/// <param name=\"{SeparateKeyCallback(keys[index])}\">Supplies discriminator {FlattenedName((string)keys[index].ConstructorArguments[0].Value!)} only when this variant has no such discriminator level.</param>"))}}
			public abstract {{FlattenedTypeName(root)}} Flatten({{SeparateFlattenParameters(root, members, keys)}});
			""";
	}

	private static string SeparateCallbackDoc(TransferMember member) => $"/// <param name=\"{member.Parameter}\">Supplies {member.Property.ContainingType.Name}.{member.Property.Name} only when flattened {member.Name} is null; the domain member requires a non-null value.</param>";

	private static string SeparateConcreteApi(INamedTypeSymbol type, INamedTypeSymbol root, INamedTypeSymbol[] members, AttributeData[] keys)
	{
		TransferMember[] data = StrategyData(type, root);
		TransferMember[] callbacks = data.Where(member => member.NeedsFallback).ToArray();
		List<INamedTypeSymbol> path = GetPath(type, root);
		IEnumerable<string> assignments = keys.Select((key, index) =>
		{
			string value = path.Count > index + 1 ? TypeName(path[index + 1]) + "." + Identifier((string)key.ConstructorArguments[0].Value!)
				: (int)key.ConstructorArguments[2].Value! == 0 ? "null" : SeparateKeyCallback(key) + "()";
			return FlattenedIdentifier((string)key.ConstructorArguments[0].Value!) + " = " + value;
		}).Concat(data.Select(member => Identifier(member.Name) + " = " + StrategyRead(member)));
		return $$"""
			/// <summary>Warning: Bypasses domain validation. Use only for plain data transfer, such as materializing ORM data.</summary>
			{{string.Join("\n", callbacks.Select(SeparateCallbackDoc))}}
			public static {{TypeName(type)}} ConstructUnvalidated({{FlattenedTypeName(type)}} flattened{{string.Concat(callbacks.Select(member => ", " + StrategyFallback(member)))}})
			{
				if (flattened is null) throw new global::System.ArgumentNullException(nameof(flattened));
				return new {{TypeName(type)}}(flattened{{string.Concat(callbacks.Select(member => ", " + member.Parameter))}});
			}

			/// <summary>Copies this variant into its flattened persistence model.</summary>
			{{string.Join("\n", SeparateMissingKeyCallbacks(root, members, keys).Select(index => $"/// <param name=\"{SeparateKeyCallback(keys[index])}\">Supplies flattened discriminator {FlattenedName((string)keys[index].ConstructorArguments[0].Value!)} for hierarchy {root.Name}. Called only when this variant has no such discriminator level and PromptSubstitute requires a value.</param>"))}}
			public override {{FlattenedTypeName(type)}} Flatten({{SeparateFlattenParameters(root, members, keys)}})
				=> new {{FlattenedTypeName(type)}} { {{string.Join(", ", assignments)}} };
			""";
	}
}