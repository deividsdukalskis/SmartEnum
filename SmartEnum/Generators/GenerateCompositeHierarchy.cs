namespace SmartEnum.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using SmartEnum.Shared;

public sealed partial class SmartEnumGenerator
{
	private static HashSet<INamedTypeSymbol> GenerateCompositeHierarchies(SourceProductionContext output, Compilation compilation, INamedTypeSymbol[] all, INamedTypeSymbol attribute)
	{
		INamedTypeSymbol[] roots = all.Where(type => GetAttributes(type, attribute).Any(item => item.ConstructorArguments.Length == 3)
			&& (type.BaseType is null || !StrategyContract.IsExplicit(type.BaseType))).ToArray();
		INamedTypeSymbol[] parents = all.Where(type => HasDataAttribute(type, "SmartEnumParentAttribute")).ToArray();
		HashSet<INamedTypeSymbol> handled = new(SymbolEqualityComparer.Default);
		HashSet<INamedTypeSymbol> invalid = new(SymbolEqualityComparer.Default);
		Dictionary<INamedTypeSymbol, INamedTypeSymbol[]> members = new(SymbolEqualityComparer.Default);
		Dictionary<INamedTypeSymbol, List<(INamedTypeSymbol Parent, TransferMember Member)>> owners = new(SymbolEqualityComparer.Default);
		foreach (INamedTypeSymbol root in roots)
		{
			members[root] = all.Where(type => GetPath(type, root).Count > 0).OrderBy(type => type.ToDisplayString(), StringComparer.Ordinal).ToArray();
			owners[root] = parents.SelectMany(parent => StrategyData(parent).Where(member => ContainsTransferRoot(member.Type, root)).Select(member => (parent, member))).ToList();
			bool separate = members[root].SelectMany(type => GetAttributes(type, attribute)).All(item => item.ConstructorArguments.Length == 3 && item.ConstructorArguments[1].Value is 2);
			if (separate && owners[root].Count == 0) continue;
			_ = handled.Add(root);
			if (!ValidateCompositeContract(output, compilation, root, members[root], attribute, false)) _ = invalid.Add(root);
			if (owners[root].Count > 1)
			{
				CompositeError(output, root, "A hierarchy used in multiple parent properties has an ambiguous flattened owner. Use distinct hierarchy roots for distinct owned transfer contracts.");
				_ = invalid.Add(root);
			}
		}

		foreach (INamedTypeSymbol parent in parents)
		{
			output.CancellationToken.ThrowIfCancellationRequested();
			INamedTypeSymbol[] referenced = roots.Where(root => owners[root].Any(owner => SameDefinition(owner.Parent, parent))).ToArray();
			if (!ValidateCompositeContract(output, compilation, parent, new[] { parent }, attribute, true) || referenced.Any(invalid.Contains)) continue;
			if (referenced.Length == 0)
			{
				CompositeError(output, parent, "SmartEnumParent requires a property containing an explicit-strategy SmartEnum hierarchy.");
				continue;
			}

			TransferNode model = new(parent.Name + "Flattened", FlattenedTypeName(parent), string.Empty);
			List<ParentLink> links = new();
			int strategy = (int)parent.GetAttributes().First(item => item.AttributeClass?.ToDisplayString() == "SmartEnum.SmartEnumParentAttribute").ConstructorArguments[0].Value!;
			TransferMember? identity = StrategyData(parent).FirstOrDefault(member => HasDataAttribute(member.Property, "SmartEnumIdentityAttribute"));
			foreach (INamedTypeSymbol root in referenced)
			{
				TransferMember member = owners[root].First(owner => SameDefinition(owner.Parent, parent)).Member;
				bool wrapped = !IsDirectTransferType(member.Type, root);
				bool separate = GetAttributes(root, attribute)[0].ConstructorArguments[1].Value is 2;
				bool independent = wrapped || strategy == 2 || separate;
				TransferNode transferModel = independent
					? new(root.Name + "Flattened", separate || strategy == 2 ? FlattenedTypeName(root) : model.Type + "." + Identifier(root.Name + "Flattened"), string.Empty) : model;
				TransferNode data = transferModel;
				if (!independent && strategy == 1)
				{
					data = new(member.Name, model.Type + "." + Identifier(root.Name + "Flattened"), "." + Identifier(member.Name));
					model.Children.Add(data);
				}

				TransferPlan plan = new(root, members[root], transferModel, data, identity, separate);
				PopulateTransferPlan(plan, attribute);
				links.Add(new(member, plan, wrapped, independent));
			}

			foreach (TransferMember member in StrategyData(parent))
			{
				ParentLink? link = links.FirstOrDefault(item => item.Member.Name == member.Name);
				if (link is null) model.Slots.Add(new(member.Name, StrategyNullableType(member), model, member));
				else if (link.Independent) model.Slots.Add(new(member.Name, ParentTransferType(link), model));
			}

			if (!ValidateTransferLayout(output, parent, model, links.Select(link => link.Plan))) continue;
			foreach (ParentLink link in links)
			{
				EmitTransferHierarchy(output, link.Plan, link.Independent && (strategy == 2 || link.Plan.Separate));
			}

			string nested = string.Join("\n", links.Where(link => link.Independent && strategy != 2 && !link.Plan.Separate).Select(link => RenderTransferNode(link.Plan.Model)));
			output.AddSource("Composite.Flattened." + GetHintName(parent), WrapDeclaration(parent, CompositeDtoDeclaration(parent), RenderTransferBody(model) + "\n" + nested));
			output.AddSource(GetHintName(parent), WrapType(parent, GenerateTransferParent(parent, model, links)));
		}

		foreach (INamedTypeSymbol root in handled.Where(root => owners[root].Count == 0 && !invalid.Contains(root)))
		{
			TransferNode model = new(root.Name + "Flattened", FlattenedTypeName(root), string.Empty);
			TransferPlan plan = new(root, members[root], model, model, null, false);
			PopulateTransferPlan(plan, attribute);
			if (ValidateTransferLayout(output, root, model, new[] { plan })) EmitTransferHierarchy(output, plan, true);
		}

		return handled;
	}

	private static bool IsDirectTransferType(ITypeSymbol type, INamedTypeSymbol root) => type is INamedTypeSymbol named && GetPath(named, root).Count > 0;
	private static bool ContainsTransferRoot(ITypeSymbol type, INamedTypeSymbol root) => IsDirectTransferType(type, root)
		|| (type is IArrayTypeSymbol array && ContainsTransferRoot(array.ElementType, root))
		|| (type is INamedTypeSymbol named && named.TypeArguments.Any(argument => ContainsTransferRoot(argument, root)));

	private static string RewriteTransferType(ITypeSymbol type, INamedTypeSymbol root, string replacement)
	{
		if (IsDirectTransferType(type, root)) return replacement + (type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : string.Empty);
		if (type is IArrayTypeSymbol array) return RewriteTransferType(array.ElementType, root, replacement) + "[" + new string(',', array.Rank - 1) + "]" + (type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : string.Empty);
		if (type is not INamedTypeSymbol named || named.Arity == 0) return TypeName(type);
		string prefix = named.ContainingType is not null ? TypeName(named.ContainingType) + "." : named.ContainingNamespace.IsGlobalNamespace ? "global::" : "global::" + named.ContainingNamespace.ToDisplayString() + ".";
		return prefix + Identifier(named.Name) + "<" + string.Join(", ", named.TypeArguments.Select(argument => RewriteTransferType(argument, root, replacement))) + ">" + (type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : string.Empty);
	}

	private static string RenderTransferBody(TransferNode node) => string.Join("\n", node.Slots.Select(slot => $"public {slot.Type} {Identifier(slot.Name)} {{ get; set; }} = default!;"))
		+ "\n" + string.Join("\n", node.Children.Select(child => RenderTransferNode(child) + $"\npublic {child.Type}? {Identifier(child.Name)} {{ get; set; }}"));
	private static string RenderTransferNode(TransferNode node) => $$"""
		public partial class {{node.Type.Substring(node.Type.LastIndexOf('.') + 1)}}
		{
			{{Indent(RenderTransferBody(node))}}
		}
		""";

	private static string CompositeDtoDeclaration(INamedTypeSymbol type) => (type.DeclaredAccessibility == Accessibility.Public ? "public" : "internal") + " partial class " + Identifier(type.Name + "Flattened");

	private static void EmitTransferHierarchy(SourceProductionContext output, TransferPlan plan, bool emitModel)
	{
		if (plan.Separate)
		{
			string common = string.Join("\n", plan.Slots.Where(slot => slot.Data is null || SameDefinition(slot.Data.Property.ContainingType, plan.Root)).Select(slot => $"public {slot.Type} {Identifier(slot.Name)} {{ get; set; }} = default!;"));
			output.AddSource("Composite.Flattened." + GetHintName(plan.Root), WrapDeclaration(plan.Root, StrategyDtoDeclaration(plan.Root, true), common));
			foreach (INamedTypeSymbol leaf in plan.Members.Where(type => !type.IsAbstract))
			{
				string properties = string.Join("\n", StrategyData(leaf, plan.Root).Where(member => !SameDefinition(member.Property.ContainingType, plan.Root)).Select(SeparateProperty));
				output.AddSource("Composite.Flattened." + GetHintName(leaf), WrapDeclaration(leaf, StrategyDtoDeclaration(leaf, false, plan.Model.Type), properties));
			}
		}
		else if (emitModel)
		{
			output.AddSource("Composite.Flattened." + GetHintName(plan.Root), WrapDeclaration(plan.Root, CompositeDtoDeclaration(plan.Root), RenderTransferBody(plan.Model)));
		}

		foreach (INamedTypeSymbol type in plan.Members)
		{
			output.CancellationToken.ThrowIfCancellationRequested();
			string body = StrategyReaders(type);
			body += "\n" + (type.IsAbstract ? StrategyConstructor(type, plan.Root, false) : CompositeConstructor(type, plan));
			if (SameDefinition(type, plan.Root)) body += "\n" + CompositeRootApi(plan);
			if (!type.IsAbstract) body += "\n" + CompositeConcreteApi(type, plan);
			output.AddSource(GetHintName(type), WrapType(type, body));
		}
	}
}