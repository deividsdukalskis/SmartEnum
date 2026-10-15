namespace SmartEnum.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

public sealed partial class SmartEnumGenerator
{
	private static HashSet<INamedTypeSymbol> GenerateReusableParents(GenerationContext output, Compilation compilation, INamedTypeSymbol[] all, INamedTypeSymbol[] roots, INamedTypeSymbol[] parents, INamedTypeSymbol attribute)
	{
		bool Separate(INamedTypeSymbol parent) => parent.GetAttributes().Any(item => item.AttributeClass?.ToDisplayString() == "SmartEnum.SmartEnumParentAttribute"
			&& item.ConstructorArguments.Length == 1 && item.ConstructorArguments[0].Value is 2);
		INamedTypeSymbol[] References(INamedTypeSymbol parent) => roots.Where(root => StrategyData(parent).Any(member => ContainsTransferRoot(member.Type, root))).ToArray();
		INamedTypeSymbol[] Owners(INamedTypeSymbol root) => parents.Where(parent => References(parent).Contains(root, SymbolEqualityComparer.Default)).ToArray();
		HashSet<INamedTypeSymbol> selected = new(SymbolEqualityComparer.Default);
		foreach (INamedTypeSymbol root in roots)
		{
			INamedTypeSymbol[] owners = Owners(root);
			if (owners.All(Separate) && (root.IsGenericType || owners.Any(parent => parent.IsGenericType)
				|| owners.Sum(parent => StrategyData(parent).Count(member => ContainsTransferRoot(member.Type, root))) > 1))
			{
				foreach (INamedTypeSymbol owner in owners) _ = selected.Add(owner);
			}
		}
		// A reusable root has one standalone model, so all its owners use the same boundary.
		bool changed;
		do
		{
			changed = false;
			foreach (INamedTypeSymbol root in selected.SelectMany(References).Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default).ToArray())
				foreach (INamedTypeSymbol owner in Owners(root)) changed |= selected.Add(owner);
		} while (changed);

		foreach (INamedTypeSymbol parent in selected)
		{
			if (!Separate(parent))
			{
				CompositeError(output, parent, "A reusable hierarchy requires Separate on every parent that references it.", id: "SMARTENUM046");
				continue;
			}

			if (!ValidateCompositeContract(output, compilation, parent, new[] { parent }, attribute, true)) continue;
			INamedTypeSymbol[] referenced = References(parent);
			GenerationContext validation = new(output.CancellationToken, (_, _) => { }, _ => { });
			if (referenced.Any(root => !ValidateCompositeContract(validation, compilation, root, all.Where(type => GetPath(type, root).Count > 0).ToArray(), attribute, false))) continue;
			EmitReusableParent(output, parent, referenced, all, attribute);
		}

		return selected;
	}

	private static string ReusableTransferType(ITypeSymbol type, INamedTypeSymbol[] roots)
	{
		if (type is INamedTypeSymbol direct)
		{
			foreach (INamedTypeSymbol root in roots)
			{
				if (GetPath(direct, root) is { Count: > 0 } path)
					return FlattenedTypeName(path[0]) + (type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : string.Empty);
			}
		}

		if (type is IArrayTypeSymbol array)
			return ReusableTransferType(array.ElementType, roots) + "[" + new string(',', array.Rank - 1) + "]" + (type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : string.Empty);
		if (type is not INamedTypeSymbol named || named.Arity == 0) return TypeName(type);
		string prefix = named.ContainingType is not null ? TypeName(named.ContainingType) + "." : named.ContainingNamespace.IsGlobalNamespace ? "global::" : "global::" + named.ContainingNamespace.ToDisplayString() + ".";
		return prefix + Identifier(named.Name) + "<" + string.Join(", ", named.TypeArguments.Select(argument => ReusableTransferType(argument, roots))) + ">" + (type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : string.Empty);
	}

	private static void EmitReusableParent(GenerationContext output, INamedTypeSymbol parent, INamedTypeSymbol[] roots, INamedTypeSymbol[] all, INamedTypeSymbol attribute)
	{
		TransferMember[] members = StrategyData(parent);
		bool Linked(TransferMember member) => roots.Any(root => ContainsTransferRoot(member.Type, root));
		bool Direct(TransferMember member) => roots.Any(root => IsDirectTransferType(member.Type, root));
		bool RequiredDirect(TransferMember member) => Direct(member) && member.Type.NullableAnnotation != NullableAnnotation.Annotated;
		Dictionary<string, TransferPlan> directPlans = new();
		foreach (TransferMember member in members.Where(Direct))
		{
			INamedTypeSymbol definition = roots.First(root => IsDirectTransferType(member.Type, root));
			INamedTypeSymbol root = GetPath((INamedTypeSymbol)member.Type, definition)[0].WithNullableAnnotation(NullableAnnotation.NotAnnotated) as INamedTypeSymbol ?? definition;
			TransferNode node = new(root.Name + "Flattened", FlattenedTypeName(root), string.Empty);
			TransferPlan plan = new(root, all.Where(type => GetPath(type, definition).Count > 0).Select(type => MapToRoot(type, root)!).ToArray(), node, node, null, GetAttributes(root, attribute)[0].ConstructorArguments[1].Value is 2);
			PopulateTransferPlan(plan, attribute);
			directPlans.Add(member.Name, plan);
		}

		string DirectCallback(TransferMember member, TransferSlot slot) => "map" + member.Name + slot.Name;
		IEnumerable<string> callbackNames = members.Where(member => (!Linked(member) && member.NeedsFallback) || (Linked(member) && !Direct(member)) || RequiredDirect(member)).Select(member => "map" + member.Name)
			.Concat(members.Where(Direct).SelectMany(member => CompositeConstructionCallbacks(directPlans[member.Name]).Select(slot => DirectCallback(member, slot))));
		if (callbackNames.GroupBy(name => name).Any(group => group.Count() > 1))
		{
			CompositeError(output, parent, "Parent property fallbacks and nested property callbacks have colliding parameter names. Rename the conflicting property.", id: "SMARTENUM043");
			return;
		}

		string ModelType(TransferMember member) => Linked(member) ? ReusableTransferType(member.Type, roots)
			+ (member.NeedsFallback ? "?" : string.Empty) : StrategyNullableType(member);
		string Callback(TransferMember member, bool flatten) => $"global::System.Func<{(flatten ? TypeName(member.Type) : ModelType(member))}, {(flatten ? ModelType(member) : TypeName(member.Type))}> map{member.Name}";
		string model = FlattenedTypeName(parent);
		string properties = string.Join("\n", members.Select(member => $"public {ModelType(member)} {Identifier(member.Name)} {{ get; set; }} = default!;"));
		output.AddSource("Reusable.Flattened." + GetHintName(parent), WrapDeclaration(parent, CompositeDtoDeclaration(parent), properties));
		string constructParameters = string.Concat(members.Where(member => !Linked(member) && member.NeedsFallback).Select(member => ", " + StrategyFallback(member)))
			+ string.Concat(members.Where(member => Linked(member) && !Direct(member)).Select(member => ", " + Callback(member, false)))
			+ string.Concat(members.Where(RequiredDirect).Select(member => $", global::System.Func<{TypeName(member.Type)}> map{member.Name}"))
			+ string.Concat(members.Where(Direct).SelectMany(member => CompositeConstructionCallbacks(directPlans[member.Name]).Select(slot => $", global::System.Func<{TypeName(slot.Data!.Type)}> {DirectCallback(member, slot)}")));
		string ConstructValue(TransferMember member)
		{
			if (!Linked(member)) return StrategyValue(member);
			if (!Direct(member)) return $"map{member.Name}(flattened.{Identifier(member.Name)})";
			TransferPlan plan = directPlans[member.Name];
			string source = "flattened." + Identifier(member.Name);
			string result = $"({source} is null ? null : {TypeName(plan.Root)}.ConstructUnvalidated({source}{string.Concat(CompositeConstructionCallbacks(plan).Select(slot => ", " + DirectCallback(member, slot)))}) as {TypeName(member.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated))})";
			return member.Type.NullableAnnotation == NullableAnnotation.Annotated ? result : result + $" ?? map{member.Name}()";
		}

		string values = string.Join(", ", members.Select(ConstructValue));
		string required = members.Any(member => member.Property is IPropertySymbol { IsRequired: true } || member.Storage is IFieldSymbol { IsRequired: true })
			? "[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]" : string.Empty;
		string body = $$"""
            {{StrategyReaders(parent)}}
            {{required}}
            private {{Identifier(parent.Name)}}({{model}} flattened{{string.Concat(members.Select(member => ", " + TypeName(member.Type) + " " + Identifier(member.Name)))}}){{ExternalBaseInitializer(parent, member => Identifier(member.Name))}}
            {
                {{Indent(string.Join("\n", TransferAssignments(parent, members, member => Identifier(member.Name))))}}
            }
            /// <summary>Warning: Bypasses domain validation. Use only for plain data transfer, such as ORM materialization. Each mapping delegate controls one property's conversion and missing values.</summary>
            {{string.Join("\n", members.Where(member => !Linked(member) && member.NeedsFallback).Select(SeparateCallbackDoc))}}
            {{string.Join("\n", members.Where(member => Linked(member) && !Direct(member)).Select(member => $"/// <param name=\"map{member.Name}\">Converts flattened {member.Name} into {parent.Name}.{member.Property.Name}. Called once per construction, including for null containers. Supply this delegate to choose element conversion, container shape and unknown discriminator handling.</param>"))}}
            {{string.Join("\n", members.Where(RequiredDirect).Select(member => $"/// <param name=\"map{member.Name}\">Supplies required property {parent.Name}.{member.Property.Name} only when automatic ConstructUnvalidated returns no compatible value because transfer data is missing or its discriminator is unknown. Valid data is constructed directly; the caller chooses the fallback behavior.</param>"))}}
            {{string.Join("\n", members.Where(Direct).SelectMany(member => CompositeConstructionCallbacks(directPlans[member.Name]).Select(slot => CompositeCallbackDoc(slot).Replace(TransferCallback(slot), DirectCallback(member, slot)).Replace("Restores ", $"Restores {parent.Name}.{member.Property.Name}'s "))))}}
            public static {{TypeName(parent)}} ConstructUnvalidated({{model}} flattened{{constructParameters}})
            {
                if (flattened is null) throw new global::System.ArgumentNullException(nameof(flattened));
                return new {{TypeName(parent)}}(flattened{{(values.Length == 0 ? "" : ", " + values)}});
            }
            /// <summary>Copies parent data and converts each independent hierarchy property with its own mapping delegate.</summary>
            {{string.Join("\n", members.Where(Linked).Select(member => $"/// <param name=\"map{member.Name}\">Converts {parent.Name}.{member.Property.Name} to flattened {member.Name}. Called once per flatten operation, including for null values. Invoke the hierarchy's Flatten method for direct values or map each container element; this delegate controls missing branch data and copy policy.</param>"))}}
            public {{model}} Flatten({{string.Join(", ", members.Where(Linked).Select(member => Callback(member, true)))}})
                => new {{model}} { {{string.Join(", ", members.Select(member => Identifier(member.Name) + " = " + (Linked(member) ? $"map{member.Name}({StrategyRead(member)})" : StrategyRead(member))))}} };
            """;
		output.AddSource("Reusable." + GetHintName(parent), WrapType(parent, body));
	}
}