namespace SmartEnum.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public sealed partial class SmartEnumGenerator
{
	private sealed class TransferMember
	{
		public ISymbol Property { get; }
		public ISymbol Storage { get; }
		public ITypeSymbol Type => this.Storage is IFieldSymbol field ? field.Type : ((IPropertySymbol)this.Property).Type;
		public string Name => FlattenedName(this.Property.Name);
		public bool Nullable => HasDataAttribute(this.Property, "SmartEnumNullablePropertyAttribute") || this.Type.NullableAnnotation == NullableAnnotation.Annotated || IsNullableValue(this.Type);
		public bool NeedsFallback => this.Nullable && this.Type.NullableAnnotation != NullableAnnotation.Annotated && !IsNullableValue(this.Type);
		public string Parameter => "map" + this.Name;
		public TransferMember(ISymbol property, ISymbol storage) => (this.Property, this.Storage) = (property, storage);
	}

	private static TransferMember[] StrategyData(INamedTypeSymbol type)
	{
		TransferMember[] properties = type.GetMembers().OfType<IPropertySymbol>()
		.Where(property => !property.IsStatic && !property.IsIndexer && !property.IsAbstract && !property.IsImplicitlyDeclared
			&& property.ExplicitInterfaceImplementations.IsEmpty && !HasDataAttribute(property, "SmartEnumIgnoreAttribute"))
		.Select(property =>
		{
			AttributeData? accessor = property.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == "SmartEnum.SmartEnumPrivateAccessorAttribute");
			ISymbol storage = accessor is { ConstructorArguments.Length: > 0 } && accessor.ConstructorArguments[0].Value is string name
				? type.GetMembers(name).OfType<IFieldSymbol>().FirstOrDefault() ?? (ISymbol)property : property;
			return new TransferMember(property, storage);
		}).ToArray();
		IEnumerable<TransferMember> fields = type.GetMembers().OfType<IFieldSymbol>().Where(field => !field.IsStatic && !field.IsConst && !field.IsImplicitlyDeclared
			&& field.DeclaredAccessibility is Accessibility.Public or Accessibility.Private && !HasDataAttribute(field, "SmartEnumIgnoreAttribute")
			&& !properties.Any(member => SymbolEqualityComparer.Default.Equals(member.Storage, field))).Select(field => new TransferMember(field, field));
		return properties.Concat(fields).OrderBy(member => member.Name, StringComparer.Ordinal).ToArray();
	}

	private static TransferMember[] StrategyData(INamedTypeSymbol type, INamedTypeSymbol root) => GetPath(type, root).SelectMany(StrategyData).ToArray();

	private static string StrategyNullableType(TransferMember member) => member.Nullable && !IsNullableValue(member.Type)
		? TypeName(member.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)) + "?" : TypeName(member.Type);

	private static string StrategyRead(TransferMember member) => "this.SmartEnumRead" + member.Name + "()";
	private static string StrategyValue(TransferMember member) => "flattened." + Identifier(member.Name)
		+ (member.NeedsFallback ? $" ?? {member.Parameter}()" : string.Empty);
	private static string StrategyFallback(TransferMember member) => $"global::System.Func<{TypeName(member.Type)}> {member.Parameter}";

	private static string StrategyConstructor(INamedTypeSymbol type, INamedTypeSymbol root, bool concrete)
	{
		TransferMember[] data = StrategyData(type, root);
		TransferMember[] own = StrategyData(type);
		if (!concrete && type.InstanceConstructors.Any(constructor => !constructor.IsImplicitlyDeclared
			&& constructor.Parameters.Select(parameter => parameter.Type).SequenceEqual(data.Select(member => member.Type), SymbolEqualityComparer.Default)))
		{
			return string.Empty;
		}

		string parameters = concrete ? FlattenedTypeName(type) + " flattened" + string.Concat(data.Where(member => member.NeedsFallback).Select(member => ", " + StrategyFallback(member)))
			: string.Join(", ", data.Select(member => TypeName(member.Type) + " " + Identifier(member.Name)));
		string argument(TransferMember member) => concrete ? StrategyValue(member) : Identifier(member.Name);
		string parent = SameDefinition(type, root) ? string.Empty : " : base(" + string.Join(", ", StrategyData(type.BaseType!, root).Select(argument)) + ")";
		IEnumerable<string> assignments = own.OrderBy(member => member.Storage is IFieldSymbol).Select(member => $"this.{Identifier(member.Storage.Name)} = {argument(member)};");
		string required = data.Any(member => member.Property is IPropertySymbol { IsRequired: true } || member.Storage is IFieldSymbol { IsRequired: true })
			? "[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]" : string.Empty;
		return $$"""
			{{required}}
			{{(concrete ? "private" : "protected")}} {{Identifier(type.Name)}}({{parameters}}){{parent}}
			{
				{{Indent(string.Join("\n", assignments))}}
				{{(concrete ? "this.FlattenedData = flattened;" : string.Empty)}}
			}
			""";
	}

	private static string StrategyReaders(INamedTypeSymbol type) => string.Join("\n", StrategyData(type).Select(member =>
		$"{(type.IsSealed ? "private" : "protected")} {TypeName(member.Type)} SmartEnumRead{member.Name}() => this.{Identifier(member.Storage.Name)};"));

	private static string StrategyDtoDeclaration(INamedTypeSymbol type, bool abstractType, string? parent = null)
	{
		string declaration = Declaration(type).Replace("partial record class", "partial class").Replace("partial class", (abstractType ? "abstract" : "sealed") + " partial class")
			.Replace(Identifier(type.Name) + (type.Arity == 0 ? string.Empty : "<"), Identifier(type.Name + "Flattened") + (type.Arity == 0 ? string.Empty : "<"));
		string access = type.DeclaredAccessibility == Accessibility.Public ? "public" : "internal";
		declaration = access + " " + declaration + (parent is null ? string.Empty : " : " + parent);
		foreach (ITypeParameterSymbol parameter in type.TypeParameters)
		{
			List<string> constraints = new();
			if (parameter.HasUnmanagedTypeConstraint) constraints.Add("unmanaged");
			else if (parameter.HasValueTypeConstraint) constraints.Add("struct");
			else if (parameter.HasReferenceTypeConstraint) constraints.Add(parameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated ? "class?" : "class");
			else if (parameter.HasNotNullConstraint) constraints.Add("notnull");
			constraints.AddRange(parameter.ConstraintTypes.Select(TypeName));
			if (parameter.HasConstructorConstraint) constraints.Add("new()");
			if (constraints.Count > 0) declaration += $"\nwhere {Identifier(parameter.Name)} : {string.Join(", ", constraints)}";
		}

		return declaration;
	}

	private static bool HasImplementedGetter(IPropertySymbol property) => property.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is PropertyDeclarationSyntax declaration
		&& (declaration.ExpressionBody is not null || declaration.AccessorList?.Accessors.Any(accessor => accessor.Keyword.ValueText == "get" && (accessor.Body is not null || accessor.ExpressionBody is not null)) == true));
}