namespace SmartEnum.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using SmartEnum.Shared;

public sealed partial class SmartEnumGenerator
{
	private static HashSet<INamedTypeSymbol> GenerateCompositeHierarchies(GenerationContext output, Compilation compilation, INamedTypeSymbol[] all, INamedTypeSymbol attribute)
	{
		INamedTypeSymbol[] roots = all.Where(type => GetAttributes(type, attribute).Any(item => item.ConstructorArguments.Length == 3)
			&& (type.BaseType is null || !StrategyContract.IsExplicit(type.BaseType))).ToArray();
		INamedTypeSymbol[] parents = all.Where(type => HasDataAttribute(type, "SmartEnumParentAttribute")).ToArray();
		HashSet<INamedTypeSymbol> reusableParents = GenerateReusableParents(output, compilation, all, roots, parents, attribute);
		parents = parents.Where(parent => !reusableParents.Contains(parent)).ToArray();
		HashSet<INamedTypeSymbol> handled = new(SymbolEqualityComparer.Default);
		HashSet<INamedTypeSymbol> invalid = new(SymbolEqualityComparer.Default);
		Dictionary<INamedTypeSymbol, INamedTypeSymbol[]> members = new(SymbolEqualityComparer.Default);
		Dictionary<INamedTypeSymbol, List<(INamedTypeSymbol Parent, TransferMember Member)>> owners = new(SymbolEqualityComparer.Default);
		foreach (INamedTypeSymbol root in roots)
		{
			members[root] = all.Where(type => GetPath(type, root).Count > 0).OrderBy(type => type.ToDisplayString(), StringComparer.Ordinal).ToArray();
			owners[root] = parents.SelectMany(parent => StrategyData(parent).Where(member => ContainsTransferRoot(member.Type, root)).Select(member => (parent, member))).ToList();
			bool separate = members[root].SelectMany(type => GetAttributes(type, attribute)).All(item => item.ConstructorArguments.Length == 3 && item.ConstructorArguments[1].Value is 2);
			if (separate && owners[root].Count == 0 && !members[root].Any(type => !SameDefinition(type, root) && GetAttributes(type, attribute).Length > 0)) continue;
			_ = handled.Add(root);
			if (!ValidateCompositeContract(output, compilation, root, members[root], attribute, false)) _ = invalid.Add(root);
			if (owners[root].Count > 0 && (root.IsGenericType || owners[root].Any(owner => owner.Parent.IsGenericType)))
			{
				CompositeError(output, root, "Use SmartEnumParent(Separate) for generic parents or generic hierarchy properties.", id: "SMARTENUM045");
				_ = invalid.Add(root);
			}

			if (owners[root].Count > 1)
			{
				CompositeError(output, root, "Use SmartEnumParent(Separate) on every owner when a hierarchy appears in multiple parent properties.", id: "SMARTENUM046");
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
				CompositeError(output, parent, "SmartEnumParent requires a property containing an explicit-strategy SmartEnum hierarchy.", id: "SMARTENUM047");
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
			IEnumerable<string> callbackNames = StrategyData(parent).Where(member => member.NeedsFallback && !links.Any(link => link.Member.Name == member.Name)).Select(member => member.Parameter)
				.Concat(links.SelectMany(link => CompositeConstructionCallbacks(link.Plan)).Select(TransferCallback).Distinct())
				.Concat(links.Where(NeedsParentCallback).Select(ParentMappingName));
			if (callbackNames.GroupBy(name => name).Any(group => group.Count() > 1))
			{
				CompositeError(output, parent, "Parent property fallbacks and hierarchy callbacks have colliding parameter names. Rename the conflicting property.", id: "SMARTENUM043");
				continue;
			}

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
			TransferPlan plan = new(root, members[root].Select(type => MapToRoot(type, root)!).ToArray(), model, model, null, GetAttributes(root, attribute)[0].ConstructorArguments[1].Value is 2);
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

	private static string CompositeDtoDeclaration(INamedTypeSymbol type) => StrategyDtoDeclaration(type, false).Replace("sealed partial", "partial");

	private static void EmitTransferHierarchy(GenerationContext output, TransferPlan plan, bool emitModel)
	{
		if (plan.Separate)
		{
			string common = string.Join("\n", plan.Slots.Where(slot => slot.Data is null || SameDefinition(slot.Data.Owner, plan.Root)).Select(slot => $"public {slot.Type} {Identifier(slot.Name)} {{ get; set; }} = default!;"));
			output.AddSource("Composite.Flattened." + GetHintName(plan.Root), WrapDeclaration(plan.Root, StrategyDtoDeclaration(plan.Root, true), common));
			foreach (INamedTypeSymbol mappedLeaf in plan.Members.Where(type => !type.IsAbstract))
			{
				INamedTypeSymbol leaf = mappedLeaf.OriginalDefinition;
				string properties = string.Join("\n", StrategyData(leaf, plan.Root).Where(member => !SameDefinition(member.Owner, plan.Root)).Select(SeparateProperty));
				INamedTypeSymbol? group = GetPath(leaf, plan.Root).Skip(1).Reverse().FirstOrDefault(node => plan.Keys.Any(key => SameDefinition(key.Group, node)));
				output.AddSource("Composite.Flattened." + GetHintName(leaf), WrapDeclaration(leaf, StrategyDtoDeclaration(leaf, false, FlattenedTypeName(group ?? GetPath(leaf, plan.Root)[0])), properties));
			}
		}
		else if (emitModel)
		{
			output.AddSource("Composite.Flattened." + GetHintName(plan.Root), WrapDeclaration(plan.Root, CompositeDtoDeclaration(plan.Root), RenderTransferBody(plan.Model)));
		}

		foreach (INamedTypeSymbol mappedType in plan.Members)
		{
			INamedTypeSymbol type = mappedType.OriginalDefinition;
			TransferPlan local = plan;
			INamedTypeSymbol localRoot = GetPath(type, plan.Root)[0];
			if (!SymbolEqualityComparer.Default.Equals(localRoot, plan.Root))
			{
				TransferNode model = new(localRoot.Name + "Flattened", FlattenedTypeName(localRoot), string.Empty);
				local = new(localRoot, plan.Members.Select(member => MapToRoot(member.OriginalDefinition, localRoot)!).ToArray(), model, model, null, plan.Separate);
				PopulateTransferPlan(local, plan.Keys[0].Attribute.AttributeClass!.OriginalDefinition);
			}

			output.CancellationToken.ThrowIfCancellationRequested();
			string body = StrategyReaders(type);
			body += "\n" + (type.IsAbstract ? StrategyConstructor(type, local.Root, false) : CompositeConstructor(type, local));
			if (SameDefinition(type, local.Root)) body += "\n" + CompositeRootApi(local);
			if (!type.IsAbstract) body += "\n" + CompositeConcreteApi(type, local);
			output.AddSource(GetHintName(type), WrapType(type, body));
		}

		EmitSubhierarchyViews(output, plan);
	}
}