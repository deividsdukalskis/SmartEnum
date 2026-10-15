namespace SmartEnum.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

public sealed partial class SmartEnumGenerator
{
	private static string CompositeValue(TransferSlot slot, string source = "flattened")
	{
		TransferMember member = slot.Data!;
		string value = TransferAccess(source, slot.Path, slot.Node.Path.Length > 0);
		return value + (member.NeedsFallback || (!member.Nullable && slot.Node.Path.Length > 0) ? " ?? " + TransferCallback(slot) + "()" : string.Empty);
	}

	private static string CompositeConstructor(INamedTypeSymbol type, TransferPlan plan)
	{
		TransferSlot[] slots = plan.Slots.Where(slot => slot.Data is { } data && GetPath(type, plan.Root).Any(node => SameDefinition(node, data.Owner))).ToArray();
		string value(TransferMember member) => CompositeValue(slots.Single(slot => slot.Data!.Name == member.Name));
		string parent = " : base(" + string.Join(", ", StrategyData(type.BaseType!, plan.Root).Select(value)) + ")";
		string required = slots.Any(slot => slot.Data!.Property is IPropertySymbol { IsRequired: true } || slot.Data.Storage is IFieldSymbol { IsRequired: true })
			? "[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]" : string.Empty;
		return $$"""
			{{required}}
			private {{Identifier(type.Name)}}({{TransferModelType(plan, type)}} flattened{{string.Concat(plan.ConstructionCallbacks(type).Select(slot => ", " + TransferCallbackDeclaration(slot)))}}){{parent}}
			{
				this.FlattenedData = flattened;
				{{Indent(string.Join("\n", StrategyData(type).OrderBy(member => member.Storage is IFieldSymbol).Select(member => $"this.{Identifier(member.Storage.Name)} = {value(member)};")))}}
			}
			""";
	}

	private static IEnumerable<TransferSlot> CompositeConstructionCallbacks(TransferPlan plan) => plan.Members.Where(type => !type.IsAbstract).SelectMany(plan.ConstructionCallbacks).GroupBy(slot => slot.Name).Select(group => group.First());
	private static IEnumerable<string> CompositeFlattenParameters(TransferPlan plan, bool identity = true) => (identity && plan.Identity is { } member ? new[] { TypeName(member.Type) + " " + Identifier(ForeignKeyName(member)) } : Enumerable.Empty<string>())
		.Concat(plan.FlattenCallbacks.Select(TransferCallbackDeclaration)).Concat(plan.KeyCallbacks.Select(TransferKeyDeclaration));
	private static IEnumerable<string> CompositeFlattenArguments(TransferPlan plan) => plan.FlattenCallbacks.Select(TransferCallback).Concat(plan.KeyCallbacks.Select(key => SeparateKeyCallback(key.Attribute)));
	private static string CompositeCallbackDoc(TransferSlot slot) => $"/// <param name=\"{TransferCallback(slot)}\">Restores {slot.Data!.Property.ContainingType.Name}.{slot.Data.Property.Name} from missing flattened {slot.Path.Replace("@", string.Empty).TrimStart('.')}. Called only if the transfer value or its containing object is null; the domain property requires a value.</param>";
	private static string CompositeFlattenCallbackDoc(TransferSlot slot) => $"/// <param name=\"{TransferCallback(slot)}\">Supplies flattened {slot.Path.Replace("@", string.Empty).TrimStart('.')} for {slot.Data!.Property.ContainingType.Name}.{slot.Data.Property.Name}. Called only when the active branch does not contain this required property and no retained transfer value exists. Active branch values and retained values take precedence.</param>";
	private static string KeyCallbackDoc(TransferKey key) => $"/// <param name=\"{SeparateKeyCallback(key.Attribute)}\">Supplies flattened discriminator {FlattenedName(key.Name)} for hierarchy {key.Group.Name}. Called only for a branch without this discriminator level because its policy is PromptSubstitute. Existing branch constants take precedence.</param>";

	private static string CompositeRootApi(TransferPlan plan)
	{
		TransferSlot[] callbacks = CompositeConstructionCallbacks(plan).ToArray();
		IEnumerable<string> branches = plan.Members.Where(type => !type.IsAbstract).OrderByDescending(type => GetPath(type, plan.Root).Count).Select((type, index) =>
		{
			string variable = plan.Separate ? "variant" + index : "flattened";
			string test = plan.Separate ? $"flattened is {FlattenedTypeName(type)} {variable} && " : string.Empty;
			test += string.Join(" && ", plan.Slots.Where(slot => slot.Key is { } key && KeyOwner(type, key) is not null).Select(slot =>
			{
				TransferKey key = slot.Key!;
				string access = TransferAccess("flattened", slot.Path, false);
				string guard = slot.Node.Path.Length == 0 ? string.Empty : TransferAccess("flattened", slot.Node.Path, true) + " is not null && ";
				return $"({guard}global::System.Collections.Generic.EqualityComparer<{slot.Type}>.Default.Equals({access}, {TypeName(KeyOwner(type, key)!)}.{Identifier(key.Name)}))";
			}));
			return $"if ({test}) return {TypeName(type)}.ConstructUnvalidated({variable}{string.Concat(plan.ConstructionCallbacks(type).Select(slot => ", " + TransferCallback(slot)))});";
		});
		return $$"""
			/// <summary>Retains the supplied transfer data, including values belonging to other branches.</summary>
			protected {{plan.Model.Type}}? FlattenedData { get; init; }
			/// <summary>Warning: Bypasses domain validation. Use only for plain data transfer, such as ORM materialization. Returns null for an unknown discriminator.</summary>
			{{string.Join("\n", callbacks.Select(CompositeCallbackDoc))}}
			public static {{TypeName(plan.Root)}}? ConstructUnvalidated({{plan.Model.Type}} flattened{{string.Concat(callbacks.Select(slot => ", " + TransferCallbackDeclaration(slot)))}})
			{
				if (flattened is null) throw new global::System.ArgumentNullException(nameof(flattened));
				{{Indent(string.Join("\n", branches))}}
				return null;
			}
			/// <summary>Copies current domain values and preserves unused transfer data. Fallback delegates supply required values only when no retained value exists.</summary>
			{{string.Join("\n", plan.FlattenCallbacks.Select(CompositeFlattenCallbackDoc))}}
			{{string.Join("\n", plan.KeyCallbacks.Select(key => $"/// <param name=\"{SeparateKeyCallback(key.Attribute)}\">Supplies discriminator {FlattenedName(key.Name)} only when the concrete branch has no value at that discriminator level.</param>"))}}
			public abstract {{plan.Model.Type}} Flatten({{string.Join(", ", CompositeFlattenParameters(plan))}});
			""";
	}

	private static string CompositeConcreteApi(INamedTypeSymbol type, TransferPlan plan)
	{
		TransferSlot[] callbacks = plan.ConstructionCallbacks(type).ToArray();
		string modelType = TransferModelType(plan, type);
		return $$"""
			/// <summary>Warning: Bypasses domain validation. Use only for plain data transfer, such as ORM materialization.</summary>
			{{string.Join("\n", callbacks.Select(CompositeCallbackDoc))}}
			public {{(!plan.Separate && callbacks.Select(slot => TypeName(slot.Data!.Type)).SequenceEqual(CompositeConstructionCallbacks(plan).Select(slot => TypeName(slot.Data!.Type))) ? "new " : "")}}static {{TypeName(type)}} ConstructUnvalidated({{modelType}} flattened{{string.Concat(callbacks.Select(slot => ", " + TransferCallbackDeclaration(slot)))}})
			{
				if (flattened is null) throw new global::System.ArgumentNullException(nameof(flattened));
				return new {{TypeName(type)}}(flattened{{string.Concat(callbacks.Select(slot => ", " + TransferCallback(slot)))}});
			}
			/// <summary>Flattens this variant, retaining inactive branch data and requesting missing required values only when necessary.</summary>
			{{string.Join("\n", plan.FlattenCallbacks.Select(CompositeFlattenCallbackDoc))}}
			{{string.Join("\n", plan.KeyCallbacks.Select(KeyCallbackDoc))}}
			public override {{modelType}} Flatten({{string.Join(", ", CompositeFlattenParameters(plan))}})
			{
				if (this.GetType() != typeof({{TypeName(type)}})) throw new global::System.InvalidOperationException("An unregistered runtime subclass cannot be flattened without losing its state.");
				return {{CompositeObject(plan.Model, type, plan, modelType)}};
			}
			""";
	}

	private static string CompositeObject(TransferNode node, INamedTypeSymbol type, TransferPlan plan, string? modelType = null)
	{
		List<INamedTypeSymbol> path = GetPath(type, plan.Root);
		List<string> assignments = new();
		foreach (TransferSlot slot in node.Slots)
		{
			string value;
			if (!plan.Slots.Contains(slot))
			{
				value = TransferAccess("this.FlattenedData", slot.Path, true) + " ?? default!";
			}
			else if (slot.ForeignKey)
			{
				TransferMember? identity = plan.Identity ?? path.SelectMany(StrategyData).FirstOrDefault(member => HasDataAttribute(member.Property, "SmartEnumIdentityAttribute") && ForeignKeyName(member) == slot.Name);
				value = plan.Identity is not null ? Identifier(slot.Name) : identity is not null ? StrategyRead(identity) : "default!";
			}
			else if (slot.Key is { } key)
			{
				INamedTypeSymbol? owner = KeyOwner(type, key);
				value = owner is not null ? TypeName(owner) + "." + Identifier(key.Name) : key.Policy == 0 ? "null" : key.Policy == 1 ? SeparateKeyCallback(key.Attribute) + "()" : "default!";
			}
			else if (slot.Data is { } member && path.Any(item => SameDefinition(item, member.Owner)))
			{
				value = StrategyRead(member);
			}
			else if (plan.Separate)
			{
				continue;
			}
			else
			{
				value = TransferAccess("this.FlattenedData", slot.Path, true);
				if (plan.FlattenCallbacks.Contains(slot)) value += " ?? " + TransferCallback(slot) + "()";
				else if (slot.Data is { Nullable: false }) value += " ?? default!";
			}

			assignments.Add(Identifier(slot.Name) + " = " + value);
		}

		foreach (TransferNode child in node.Children)
		{
			bool belongs = plan.Nodes.Values.Contains(child);
			bool active = belongs && (child.Branch is null || path.Any(item => SameDefinition(item, child.Branch)));
			string value = active ? CompositeObject(child, type, plan) : TransferAccess("this.FlattenedData", child.Path, true);
			assignments.Add(Identifier(child.Name) + " = " + value);
		}

		return $"new {modelType ?? node.Type} {{ {string.Join(", ", assignments)} }}";
	}
}