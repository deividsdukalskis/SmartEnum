namespace SmartEnum.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

public sealed partial class SmartEnumGenerator
{
	private static void EmitSubhierarchyViews(GenerationContext output, TransferPlan outer)
	{
		foreach (INamedTypeSymbol mappedGroup in outer.Keys.Select(key => key.Group).Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default).Where(group => !SameDefinition(group, outer.Root)).ToArray())
		{
			INamedTypeSymbol group = mappedGroup.OriginalDefinition;
			INamedTypeSymbol localRoot = GetPath(group, outer.Root)[0];
			if (!SymbolEqualityComparer.Default.Equals(localRoot, outer.Root))
			{
				TransferNode localModel = new(localRoot.Name + "Flattened", FlattenedTypeName(localRoot), string.Empty);
				TransferPlan local = new(localRoot, outer.Members.Select(member => MapToRoot(member.OriginalDefinition, localRoot)!).ToArray(), localModel, localModel, null, outer.Separate);
				PopulateTransferPlan(local, outer.Keys[0].Attribute.AttributeClass!.OriginalDefinition);
				outer = local;
			}

			TransferNode model = new(group.Name + "Flattened", FlattenedTypeName(group), string.Empty);
			TransferPlan view = new(group, outer.Members.Where(type => GetPath(type, group).Count > 0).ToArray(), model, model, outer.Identity, outer.Separate);
			bool hidesFlatten = GetPath(group, outer.Root).Skip(1).Any(node => !SameDefinition(node, group) && outer.Keys.Any(key => SameDefinition(key.Group, node)));
			if (outer.Separate)
			{
				INamedTypeSymbol parent = GetPath(group, outer.Root).TakeWhile(node => !SameDefinition(node, group)).Last(node => outer.Keys.Any(key => SameDefinition(key.Group, node)));
				output.AddSource("Hierarchy.Flattened." + GetHintName(group), WrapDeclaration(group, StrategyDtoDeclaration(group, true, FlattenedTypeName(parent)), string.Empty));
				string parameters = string.Join(", ", CompositeConstructionCallbacks(outer).Select(TransferCallbackDeclaration));
				string arguments = string.Join(", ", CompositeConstructionCallbacks(outer).Select(TransferCallback));
				string body = $$"""
					/// <summary>Warning: Bypasses domain validation. Use only for data transfer. Returns null for another hierarchy or unknown discriminator.</summary>
					{{string.Join("\n", CompositeConstructionCallbacks(outer).Select(CompositeCallbackDoc))}}
					public static {{TypeName(group)}}? ConstructUnvalidated({{model.Type}} flattened{{(parameters.Length == 0 ? "" : ", " + parameters)}})
						=> {{TypeName(outer.Root)}}.ConstructUnvalidated(flattened{{(arguments.Length == 0 ? "" : ", " + arguments)}}) as {{TypeName(group)}};
					/// <summary>Copies this value into the flattened model for this subclass hierarchy.</summary>
					{{string.Join("\n", outer.FlattenCallbacks.Select(CompositeFlattenCallbackDoc))}}
					{{string.Join("\n", outer.KeyCallbacks.Select(KeyCallbackDoc))}}
					public {{(hidesFlatten ? "new " : "")}}{{model.Type}} FlattenHierarchy({{string.Join(", ", CompositeFlattenParameters(outer))}})
						=> ({{model.Type}})this.Flatten({{string.Join(", ", (outer.Identity is { } identity ? new[] { Identifier(ForeignKeyName(identity)) } : Array.Empty<string>()).Concat(CompositeFlattenArguments(outer)))}});
					""";
				output.AddSource("Hierarchy." + GetHintName(group), WrapType(group, body));
				continue;
			}
			// Reuse the already resolved attribute symbol, including restarted groups.
			INamedTypeSymbol attribute = outer.Keys[0].Attribute.AttributeClass!.OriginalDefinition;
			PopulateTransferPlan(view, attribute);
			IEnumerable<TransferSlot> inherited = outer.Slots.Where(slot => slot.Data is { } member && GetPath(group, outer.Root).TakeWhile(node => !SameDefinition(node, group)).Any(node => SameDefinition(node, member.Owner)));
			foreach (TransferSlot slot in inherited.Concat(outer.Model.Slots.Where(slot => !outer.Slots.Contains(slot) && slot.Data is not null)))
			{
				TransferSlot copy = new(slot.Name, slot.Type, model, slot.Data);
				view.Slots.Add(copy);
				model.Slots.Add(copy);
			}

			if (!ValidateTransferLayout(output, group, model, new[] { view })) continue;
			output.AddSource("Hierarchy.Flattened." + GetHintName(group), WrapDeclaration(group, CompositeDtoDeclaration(group), RenderTransferBody(model)));
			TransferSlot[] callbacks = outer.Members.Where(type => !type.IsAbstract && GetPath(type, group).Count > 0).SelectMany(outer.ConstructionCallbacks).GroupBy(slot => slot.Name).Select(slots => slots.First()).ToArray();
			string mappedArguments = string.Concat(CompositeConstructionCallbacks(outer).Select(slot => ", " + (callbacks.Any(candidate => candidate.Name == slot.Name) ? TransferCallback(slot) : "() => default!")));
			string flattenArguments = string.Join(", ", (outer.Identity is { } owner ? new[] { Identifier(ForeignKeyName(owner)) } : Array.Empty<string>())
				.Concat(outer.FlattenCallbacks.Select(slot => view.FlattenCallbacks.Any(candidate => candidate.Name == slot.Name) ? TransferCallback(slot) : "() => default!"))
				.Concat(outer.KeyCallbacks.Select(key => view.KeyCallbacks.Any(candidate => SameDefinition(candidate.Group, key.Group) && candidate.Index == key.Index) ? SeparateKeyCallback(key.Attribute) : "() => default!")));
			string api = $$"""
				/// <summary>Warning: Bypasses domain validation. Use only for plain data transfer. Maps this subclass hierarchy and retains inherited data.</summary>
				{{string.Join("\n", callbacks.Select(CompositeCallbackDoc))}}
				public static {{TypeName(group)}}? ConstructUnvalidated({{model.Type}} flattened{{string.Concat(callbacks.Select(slot => ", " + TransferCallbackDeclaration(slot)))}})
				{
					if (flattened is null) throw new global::System.ArgumentNullException(nameof(flattened));
					{{outer.Model.Type}} outer = {{ProjectHierarchyNode(outer.Model, outer, view, "flattened", group)}};
					return {{TypeName(outer.Root)}}.ConstructUnvalidated(outer{{mappedArguments}}) as {{TypeName(group)}};
				}
				/// <summary>Copies inherited data and this hierarchy's branches into its own flattened model.</summary>
				{{string.Join("\n", view.FlattenCallbacks.Select(CompositeFlattenCallbackDoc))}}
				{{string.Join("\n", view.KeyCallbacks.Select(KeyCallbackDoc))}}
				public {{(hidesFlatten ? "new " : "")}}{{model.Type}} FlattenHierarchy({{string.Join(", ", CompositeFlattenParameters(view))}})
				{
					{{outer.Model.Type}} outer = this.Flatten({{flattenArguments}});
					return {{ProjectHierarchyNode(model, view, outer, "outer", group)}};
				}
				""";
			output.AddSource("Hierarchy." + GetHintName(group), WrapType(group, api));
		}
	}

	private static bool MatchingSlot(TransferSlot left, TransferSlot right) => left.Name == right.Name && ((left.Key is null && right.Key is null)
		|| (left.Key is { } first && right.Key is { } second && SameDefinition(first.Group, second.Group) && first.Index == second.Index));

	private static string ProjectHierarchyNode(TransferNode target, TransferPlan targetPlan, TransferPlan sourcePlan, string variable, INamedTypeSymbol group)
	{
		List<string> values = new();
		foreach (TransferSlot slot in target.Slots)
		{
			TransferSlot? source = sourcePlan.Slots.FirstOrDefault(candidate => MatchingSlot(candidate, slot));
			string value = "default!";
			if (source is not null)
			{
				value = TransferAccess(variable, source.Path, source.Node.Path.Length > 0);
				if (source.Node.Path.Length > 0 && !slot.Type.EndsWith("?", StringComparison.Ordinal)) value += " ?? default!";
			}
			else if (slot.Key is { } key && KeyOwner(group, key) is { } keyOwner)
			{
				value = TypeName(keyOwner) + "." + Identifier(key.Name);
			}

			values.Add(Identifier(slot.Name) + " = " + value);
		}

		foreach (TransferNode child in target.Children)
		{
			string value = ProjectHierarchyNode(child, targetPlan, sourcePlan, variable, group);
			if (child.Branch is { } branch && !GetPath(group, targetPlan.Root).Any(node => SameDefinition(node, branch)))
			{
				TransferNode? source = sourcePlan.Nodes.FirstOrDefault(pair => SameDefinition(pair.Key, branch)).Value;
				value = source is null ? "null" : TransferAccess(variable, source.Path, true) + " is null ? null : " + value;
			}

			values.Add(Identifier(child.Name) + " = " + value);
		}

		return "new " + target.Type + " { " + string.Join(", ", values) + " }";
	}
}