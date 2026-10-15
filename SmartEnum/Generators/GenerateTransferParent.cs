namespace SmartEnum.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

public sealed partial class SmartEnumGenerator
{
	private static string ParentMappingName(ParentLink link) => "map" + link.Member.Name;
	private static bool NeedsParentCallback(ParentLink link) => link.Wrapped || link.Member.Type.NullableAnnotation != NullableAnnotation.Annotated;
	private static string ParentTransferType(ParentLink link) => RewriteTransferType(link.Member.Type, link.Plan.Root, link.Plan.Model.Type)
		+ (link.Member.Nullable && link.Member.Type.NullableAnnotation != NullableAnnotation.Annotated && !IsNullableValue(link.Member.Type) ? "?" : string.Empty);
	private static string ParentMappingDeclaration(ParentLink link, bool flatten)
	{
		if (!link.Wrapped) return $"global::System.Func<{TypeName(link.Member.Type)}> {ParentMappingName(link)}";
		List<string> input = new();
		if (flatten)
		{
			if (link.Plan.Identity is { } identity) input.Add(TypeName(identity.Type) + " " + Identifier(ForeignKeyName(identity)));
			input.Add(TypeName(link.Member.Type) + " " + Identifier(link.Member.Name));
			input.AddRange(link.Plan.FlattenCallbacks.Select(slot => TransferCallbackDeclaration(slot).Replace(" map", " Map")));
			input.AddRange(link.Plan.KeyCallbacks.Select(key => TransferKeyDeclaration(key).Replace(" map", " Map")));
		}
		else
		{
			input.Add(ParentTransferType(link) + " Flattened");
			input.AddRange(CompositeConstructionCallbacks(link.Plan).Select(slot => TransferCallbackDeclaration(slot).Replace(" map", " Map")));
		}

		string inputType = input.Count == 1 ? (flatten ? TypeName(link.Member.Type) : ParentTransferType(link)) : "(" + string.Join(", ", input) + ")";
		string result = flatten ? ParentTransferType(link) : TypeName(link.Member.Type);
		return $"global::System.Func<{inputType}, {result}> {ParentMappingName(link)}";
	}

	private static string GenerateTransferParent(INamedTypeSymbol parent, TransferNode model, List<ParentLink> links)
	{
		TransferMember[] own = StrategyData(parent);
		TransferMember[] plain = own.Where(member => !links.Any(link => link.Member.Name == member.Name)).ToArray();
		TransferSlot[] constructionCallbacks = links.SelectMany(link => CompositeConstructionCallbacks(link.Plan)).GroupBy(slot => slot.Name).Select(group => group.First()).ToArray();
		List<string> constructParameters = new() { model.Type + " flattened" };
		constructParameters.AddRange(plain.Where(member => member.NeedsFallback).Select(StrategyFallback));
		constructParameters.AddRange(constructionCallbacks.Select(TransferCallbackDeclaration));
		constructParameters.AddRange(links.Where(NeedsParentCallback).Select(link => ParentMappingDeclaration(link, false)));
		List<string> values = new();
		foreach (TransferMember member in own)
		{
			ParentLink? link = links.FirstOrDefault(item => item.Member.Name == member.Name);
			if (link is null)
			{
				values.Add(StrategyValue(member));
			}
			else
			{
				string source = link.Independent ? "flattened." + Identifier(member.Name) : "flattened";
				List<string> arguments = new() { source };
				arguments.AddRange(CompositeConstructionCallbacks(link.Plan).Select(TransferCallback));
				string expression = link.Wrapped ? $"{ParentMappingName(link)}({TupleArguments(arguments)})"
					: $"({TypeName(link.Plan.Root)}.ConstructUnvalidated({string.Join(", ", arguments)}) as {TypeName(link.Member.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated))})";
				if (!link.Wrapped && link.Independent) expression = "(" + source + " is null ? null : " + expression + ")";
				if (!link.Wrapped && NeedsParentCallback(link)) expression += $" ?? {ParentMappingName(link)}()";
				values.Add(expression);
			}
		}

		List<string> flattenParameters = links.SelectMany(link => CompositeFlattenParameters(link.Plan, false)).Distinct(StringComparer.Ordinal).ToList();
		flattenParameters.AddRange(links.Where(link => link.Wrapped).Select(link => ParentMappingDeclaration(link, true)));
		List<string> statements = new() { $"{model.Type} result = new {model.Type}();" };
		foreach (ParentLink link in links)
		{
			string domain = "domain" + link.Member.Name;
			statements.Add($"{TypeName(link.Member.Type)} {domain} = {StrategyRead(link.Member)};");
			List<string> args = new();
			if (link.Plan.Identity is { } identity) args.Add(StrategyRead(identity));
			if (link.Wrapped) args.Add(domain);
			args.AddRange(CompositeFlattenArguments(link.Plan));
			string value = link.Wrapped ? $"{ParentMappingName(link)}({TupleArguments(args)})" : $"{domain}.Flatten({string.Join(", ", args)})";
			if (link.Member.Nullable && !link.Wrapped) value = domain + " is null ? " + (link.Independent ? "null" : "new " + model.Type + "()") + " : " + value;
			if (link.Independent)
			{
				statements.Add($"result.{Identifier(link.Member.Name)} = {value};");
			}
			else
			{
				string variable = "mapped" + link.Member.Name;
				statements.Add($"{model.Type} {variable} = {value};");
				foreach (TransferSlot slot in link.Plan.Slots.Where(slot => slot.Node == model)) statements.Add($"result.{Identifier(slot.Name)} = {variable}.{Identifier(slot.Name)};");
				foreach (TransferNode child in model.Children.Where(child => link.Plan.Nodes.Values.Contains(child))) statements.Add($"result.{Identifier(child.Name)} = {variable}.{Identifier(child.Name)};");
			}
		}

		statements.AddRange(plain.Select(member => $"result.{Identifier(member.Name)} = {StrategyRead(member)};"));
		statements.Add("return result;");
		string required = own.Any(member => member.Property is IPropertySymbol { IsRequired: true } || member.Storage is IFieldSymbol { IsRequired: true }) ? "[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]" : string.Empty;
		return $$"""
			{{StrategyReaders(parent)}}
			/// <summary>Retains the transfer model supplied during unvalidated construction.</summary>
			private {{model.Type}}? FlattenedData { get; init; }
			{{required}}
			private {{Identifier(parent.Name)}}({{model.Type}} flattened{{string.Concat(own.Select(member => ", " + TypeName(member.Type) + " " + Identifier(member.Name)))}}){{ExternalBaseInitializer(parent, member => Identifier(member.Name))}}
			{
				this.FlattenedData = flattened;
				{{Indent(string.Join("\n", TransferAssignments(parent, own, member => Identifier(member.Name))))}}
			}
			/// <summary>Warning: Bypasses domain validation. Use only for plain data transfer, such as ORM materialization.</summary>
			{{string.Join("\n", plain.Where(member => member.NeedsFallback).Select(SeparateCallbackDoc))}}
			{{string.Join("\n", constructionCallbacks.Select(CompositeCallbackDoc))}}
			{{string.Join("\n", links.Where(link => link.Wrapped).Select(link => $"/// <param name=\"{ParentMappingName(link)}\">Converts the flattened container to {parent.Name}.{link.Member.Property.Name}. Called once during construction, including for null containers. The caller controls element conversion, collection shape and unknown discriminator handling; supplied fallback delegates restore missing element properties.</param>"))}}
			{{string.Join("\n", links.Where(link => !link.Wrapped && NeedsParentCallback(link)).Select(link => $"/// <param name=\"{ParentMappingName(link)}\">Supplies required property {parent.Name}.{link.Member.Property.Name} only when automatic ConstructUnvalidated cannot restore it: the transfer object is missing, the discriminator is unknown, or the selected variant does not match the property type. Valid data is constructed directly without calling this fallback; the caller decides how to handle missing values.</param>"))}}
			public static {{TypeName(parent)}} ConstructUnvalidated({{string.Join(", ", constructParameters)}})
			{
				if (flattened is null) throw new global::System.ArgumentNullException(nameof(flattened));
				return new {{TypeName(parent)}}(flattened, {{string.Join(", ", values)}});
			}
			/// <summary>Copies owner data and maps each hierarchy to its configured transfer representation. Container delegates control element mapping and unknown-variant handling.</summary>
			{{string.Join("\n", links.SelectMany(link => link.Plan.FlattenCallbacks).Select(CompositeFlattenCallbackDoc).Distinct(StringComparer.Ordinal))}}
			{{string.Join("\n", links.SelectMany(link => link.Plan.KeyCallbacks).Select(key => $"/// <param name=\"{SeparateKeyCallback(key.Attribute)}\">Supplies discriminator {FlattenedName(key.Name)} only for a branch without that discriminator level.</param>").Distinct(StringComparer.Ordinal))}}
			{{string.Join("\n", links.Where(link => link.Wrapped).Select(link => $"/// <param name=\"{ParentMappingName(link)}\">Maps {parent.Name}.{link.Member.Property.Name} using the owner identity and supplied missing-value delegates.</param>"))}}
			public {{model.Type}} Flatten({{string.Join(", ", flattenParameters)}})
			{
				{{Indent(string.Join("\n", statements))}}
			}
			""";
	}

	private static string TupleArguments(List<string> arguments) => arguments.Count == 1 ? arguments[0] : "(" + string.Join(", ", arguments) + ")";
}