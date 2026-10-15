namespace SmartEnum.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

public sealed partial class SmartEnumGenerator
{
	private sealed class TransferNode
	{
		public string Name { get; }
		public string Type { get; }
		public string Path { get; }
		public INamedTypeSymbol? Branch { get; }
		public List<TransferNode> Children { get; } = new();
		public List<TransferSlot> Slots { get; } = new();
		public TransferNode(string name, string type, string path, INamedTypeSymbol? branch = null)
			=> (this.Name, this.Type, this.Path, this.Branch) = (name, type, path, branch);
	}

	private sealed class TransferSlot
	{
		public string Name { get; }
		public string Type { get; }
		public TransferNode Node { get; }
		public TransferMember? Data { get; }
		public TransferKey? Key { get; }
		public bool ForeignKey { get; }
		public string Path => this.Node.Path + "." + Identifier(this.Name);
		public TransferSlot(string name, string type, TransferNode node, TransferMember? data = null, TransferKey? key = null, bool foreignKey = false)
			=> (this.Name, this.Type, this.Node, this.Data, this.Key, this.ForeignKey) = (name, type, node, data, key, foreignKey);
	}

	private sealed class TransferKey
	{
		public INamedTypeSymbol Group { get; }
		public AttributeData Attribute { get; }
		public int Index { get; }
		public string Name => (string)this.Attribute.ConstructorArguments[0].Value!;
		public int Strategy => (int)this.Attribute.ConstructorArguments[1].Value!;
		public int Policy => (int)this.Attribute.ConstructorArguments[2].Value!;
		public TransferKey(INamedTypeSymbol group, AttributeData attribute, int index) => (this.Group, this.Attribute, this.Index) = (group, attribute, index);
	}

	private sealed class TransferPlan
	{
		public INamedTypeSymbol Root { get; }
		public INamedTypeSymbol[] Members { get; }
		public TransferNode Model { get; }
		public TransferNode DataNode { get; }
		public List<TransferKey> Keys { get; } = new();
		public List<TransferSlot> Slots { get; } = new();
		public Dictionary<INamedTypeSymbol, TransferNode> Nodes { get; } = new(SymbolEqualityComparer.Default);
		public TransferMember? Identity { get; }
		public bool Separate { get; }
		public TransferPlan(INamedTypeSymbol root, INamedTypeSymbol[] members, TransferNode model, TransferNode dataNode, TransferMember? identity, bool separate)
			=> (this.Root, this.Members, this.Model, this.DataNode, this.Identity, this.Separate) = (root, members, model, dataNode, identity, separate);
		public IEnumerable<TransferSlot> ConstructionCallbacks(INamedTypeSymbol type) => this.Slots.Where(slot => slot.Data is { } member && GetPath(type, this.Root).Any(node => SameDefinition(node, member.Owner))
			&& (member.NeedsFallback || (!member.Nullable && slot.Node.Path.Length > 0)));
		public IEnumerable<TransferSlot> FlattenCallbacks => this.Separate ? Enumerable.Empty<TransferSlot>() : this.Slots.Where(slot => slot.Data is { Nullable: false }
			&& slot.Node == this.DataNode && GetPath(this.Root, slot.Data.Owner).Count == 0
			&& this.Members.Any(type => !type.IsAbstract && !GetPath(type, this.Root).Any(node => SameDefinition(node, slot.Data.Owner))));
		public IEnumerable<TransferKey> KeyCallbacks => this.Keys.Where(key => key.Policy == 1 && this.Members.Any(type => !type.IsAbstract && KeyOwner(type, key) is null));
	}

	private sealed class ParentLink
	{
		public TransferMember Member { get; }
		public TransferPlan Plan { get; }
		public bool Wrapped { get; }
		public bool Independent { get; }
		public ParentLink(TransferMember member, TransferPlan plan, bool wrapped, bool independent)
			=> (this.Member, this.Plan, this.Wrapped, this.Independent) = (member, plan, wrapped, independent);
	}

	private static INamedTypeSymbol? KeyOwner(INamedTypeSymbol leaf, TransferKey key)
	{
		List<INamedTypeSymbol> path = GetPath(leaf, key.Group);
		return path.Skip(1).Take(key.Index).Any(type => type.GetAttributes().Any(attribute => attribute.AttributeClass?.OriginalDefinition.ToDisplayString() == "SmartEnum.SmartEnumAttribute<TKey>"))
			? null
			: path.Count > key.Index + 1 ? path[key.Index + 1] : null;
	}

	private static string ForeignKeyName(TransferMember identity)
	{
		AttributeData attribute = identity.Property.GetAttributes().First(item => item.AttributeClass?.ToDisplayString() == "SmartEnum.SmartEnumIdentityAttribute");
		return FlattenedName((attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as string : null) ?? identity.Property.Name);
	}

	private static IEnumerable<TransferNode> TransferNodes(TransferNode node) => new[] { node }.Concat(node.Children.SelectMany(TransferNodes));
	private static string TransferAccess(string variable, string path, bool optional) => variable + (optional
		? variable != "this.FlattenedData" && path.Length > 0 ? "." + path.Substring(1).Replace(".", "?.") : path.Replace(".", "?.") : path);
	private static string TransferCallback(TransferSlot slot) => "map" + slot.Name;
	private static string TransferCallbackDeclaration(TransferSlot slot) => $"global::System.Func<{TypeName(slot.Data!.Type)}> {TransferCallback(slot)}";
	private static string TransferKeyDeclaration(TransferKey key) => $"global::System.Func<{TypeName(key.Attribute.AttributeClass!.TypeArguments[0])}> {SeparateKeyCallback(key.Attribute)}";
	private static string TransferModelType(TransferPlan plan, INamedTypeSymbol type) => plan.Separate ? FlattenedTypeName(type) : plan.Model.Type;

	private static void PopulateTransferPlan(TransferPlan plan, INamedTypeSymbol attribute)
	{
		plan.Nodes.Add(plan.Root, plan.DataNode);
		foreach (INamedTypeSymbol type in plan.Members.OrderBy(type => GetPath(type, plan.Root).Count))
		{
			if (!SameDefinition(type, plan.Root))
			{
				INamedTypeSymbol parent = type.BaseType!;
				TransferNode node = plan.Nodes[parent];
				TransferKey key = plan.Keys.Last(item => KeyOwner(type, item) is { } owner && SameDefinition(owner, type));
				if (!plan.Separate && (key.Strategy == 1 || GetAttributes(type, attribute).Length > 0))
				{
					TransferNode child = new(FlattenedName(type.Name), node.Type + "." + Identifier(type.Name + "Flattened"), node.Path + "." + FlattenedIdentifier(type.Name), type);
					node.Children.Add(child);
					node = child;
				}

				plan.Nodes.Add(type, node);
			}

			TransferNode dataNode = plan.Nodes[type];
			AttributeData[] keys = GetAttributes(type, attribute);
			for (int index = 0; index < keys.Length; index++)
			{
				TransferKey key = new(type, keys[index], index);
				plan.Keys.Add(key);
				TransferNode keyNode = SameDefinition(type, plan.Root) ? plan.Model : dataNode;
				TransferSlot slot = new(FlattenedName(key.Name), SeparateKeyType(key.Attribute), keyNode, key: key);
				keyNode.Slots.Add(slot);
				plan.Slots.Add(slot);
			}

			foreach (TransferMember member in StrategyData(type))
			{
				TransferSlot slot = new(member.Name, StrategyNullableType(member), dataNode, member);
				dataNode.Slots.Add(slot);
				plan.Slots.Add(slot);
			}
		}

		foreach (TransferNode node in plan.Nodes.Values.Distinct())
		{
			TransferMember? identity = plan.Identity;
			if (identity is null && node.Branch is { } branch)
				identity = GetPath(branch, plan.Root).TakeWhile(type => !SameDefinition(type, branch)).Reverse().SelectMany(StrategyData).FirstOrDefault(member => HasDataAttribute(member.Property, "SmartEnumIdentityAttribute"));
			if (identity is not null && (node.Path.Length > 0 || (plan.Model == plan.DataNode && plan.Identity is not null)))
			{
				TransferSlot slot = new(ForeignKeyName(identity), TypeName(identity.Type), node, foreignKey: true);
				node.Slots.Add(slot);
				plan.Slots.Add(slot);
			}
		}
	}
}