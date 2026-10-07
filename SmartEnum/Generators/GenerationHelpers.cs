namespace SmartEnum.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public sealed partial class SmartEnumGenerator
{
	private sealed class DataMember
	{
		public string Name { get; }
		public ITypeSymbol Type { get; }
		public ISymbol Symbol { get; }
		public bool IsRequired => this.Symbol is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true };
		public DataMember(string name, ITypeSymbol type, ISymbol symbol) => (this.Name, this.Type, this.Symbol) = (name, type, symbol);
	}

	private static string TypeName(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
		.WithMiscellaneousOptions(SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));
	private static string Identifier(string name) => "@" + name;
	private static string Indent(string text) => text.Replace("\n", "\n\t");
	private static bool SameDefinition(INamedTypeSymbol first, INamedTypeSymbol second)
		=> SymbolEqualityComparer.Default.Equals(first.OriginalDefinition, second.OriginalDefinition);

	private static AttributeData[] GetAttributes(INamedTypeSymbol type, INamedTypeSymbol attribute) => type.GetAttributes()
		.Where(item => SymbolEqualityComparer.Default.Equals(item.AttributeClass?.OriginalDefinition, attribute)).ToArray();

	private static List<INamedTypeSymbol> GetPath(INamedTypeSymbol type, INamedTypeSymbol root)
	{
		List<INamedTypeSymbol> path = new();
		HashSet<ISymbol> visited = new(SymbolEqualityComparer.Default);
		for (INamedTypeSymbol? current = type; current is not null && visited.Add(current); current = current.BaseType)
		{
			path.Add(current);
			if (!SameDefinition(current, root)) continue;
			path.Reverse();
			return path;
		}

		return new();
	}

	private static DataMember[] GetOwnData(INamedTypeSymbol type) => type.GetMembers()
		.Where(member => !HasDataAttribute(member, "SmartEnumIgnoreAttribute"))
		.Select(member => member switch
		{
			IPropertySymbol property when !property.IsStatic && !property.IsIndexer && !property.IsAbstract
				&& property.ExplicitInterfaceImplementations.IsEmpty && !property.IsImplicitlyDeclared
				&& (property.SetMethod is not null || property.DeclaringSyntaxReferences.Any(reference =>
					reference.GetSyntax() is PropertyDeclarationSyntax { AccessorList: { } list }
					&& list.Accessors.All(accessor => accessor.Body is null && accessor.ExpressionBody is null)))
				=> new DataMember(property.Name, property.Type, property),
			IFieldSymbol field when !field.IsStatic && !field.IsConst && !field.IsImplicitlyDeclared
				&& field.DeclaredAccessibility is Accessibility.Public or Accessibility.Private
				=> new DataMember(field.Name, field.Type, field),
			_ => null
		}).OfType<DataMember>().OrderBy(member => member.Name, StringComparer.Ordinal).ToArray();

	private static bool HasDataAttribute(ISymbol member, string name) => member.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == "SmartEnum." + name);

	private static IMethodSymbol? GetExternalConstructor(INamedTypeSymbol root)
	{
		if (root.BaseType is null || root.BaseType.SpecialType == SpecialType.System_Object) return null;
		IMethodSymbol[] constructors = root.BaseType.InstanceConstructors.Where(constructor => constructor.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal
			|| (SymbolEqualityComparer.Default.Equals(root.ContainingAssembly, constructor.ContainingAssembly)
				&& constructor.DeclaredAccessibility is Accessibility.Internal or Accessibility.ProtectedAndInternal)).ToArray();
		return constructors.FirstOrDefault(constructor => constructor.Parameters.Length == 0)
			?? (constructors.Length == 1 ? constructors[0] : null);
	}

	private static DataMember[] GetExternalData(INamedTypeSymbol root)
	{
		IMethodSymbol? constructor = GetExternalConstructor(root);
		if (constructor is null) return Array.Empty<DataMember>();
		HashSet<string> names = new(GetOwnData(root).Select(member => member.Name), StringComparer.Ordinal);
		return constructor.Parameters.Select(parameter =>
		{
			string name = "base_" + parameter.Name;
			while (!names.Add(name)) name = "base_" + name;
			return new DataMember(name, parameter.Type, parameter);
		}).ToArray();
	}

	private static DataMember[] GetData(INamedTypeSymbol type, INamedTypeSymbol root)
	{
		List<INamedTypeSymbol> path = GetPath(type, root);
		return path.Count == 0 ? Array.Empty<DataMember>() : GetExternalData(path[0]).Concat(path.SelectMany(GetOwnData))
			.GroupBy(member => member.Name).Select(group => group.Last()).ToArray();
	}

	private static string Parameter(DataMember member) => TypeName(member.Type) + " " + Identifier(member.Name);
	private static string Arguments(IEnumerable<DataMember> data) => string.Join(", ", data.Select(member => Identifier(member.Name)));
	private static IMethodSymbol? FindConstructor(INamedTypeSymbol type, DataMember[] data) => type.InstanceConstructors
		.FirstOrDefault(constructor => !constructor.IsImplicitlyDeclared && constructor.Parameters.Length == data.Length
			&& constructor.Parameters.All(parameter => parameter.RefKind == RefKind.None)
			&& constructor.Parameters.Select(parameter => parameter.Type).SequenceEqual(data.Select(member => member.Type), SymbolEqualityComparer.Default));

	private static string GenerateConstructor(INamedTypeSymbol type, INamedTypeSymbol root, string accessibility)
	{
		DataMember[] data = GetData(type, root);
		if (FindConstructor(type, data) is not null) return string.Empty;
		string required = data.Any(member => member.IsRequired) || (GetExternalConstructor(GetPath(type, root)[0]) is IMethodSymbol external && HasSetsRequiredMembers(external))
			? "[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]" : string.Empty;
		DataMember[] parentData = SameDefinition(type, root) ? GetExternalData(type) : GetData(type.BaseType!, root);
		string initializer = SameDefinition(type, root) && type.BaseType?.SpecialType == SpecialType.System_Object
			? string.Empty : $" : base({Arguments(parentData)})";
		// Setters may modify backing fields. Apply explicit field values last so they are preserved.
		IEnumerable<string> assignments = GetOwnData(type).OrderBy(member => member.Symbol is IFieldSymbol)
			.Select(member => $"this.{Identifier(member.Name)} = {Identifier(member.Name)};");
		return $$"""
			{{required}}
			{{accessibility}} {{Identifier(type.Name)}}({{string.Join(", ", data.Select(Parameter))}}){{initializer}}
			{
				{{Indent(string.Join("\n", assignments))}}
			}
			""";
	}

	private static string GetHintName(INamedTypeSymbol type)
	{
		using (SHA256 hash = SHA256.Create())
		{
			string suffix = string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
				.Select(value => value.ToString("x2")));
			return type.Name.Substring(0, Math.Min(type.Name.Length, 40)) + "." + suffix + ".SmartEnum.g.cs";
		}
	}

	private static string Declaration(INamedTypeSymbol type)
	{
		string kind = type.IsRecord ? (type.TypeKind == TypeKind.Struct ? "record struct" : "record class")
			: type.TypeKind == TypeKind.Struct ? "struct" : type.TypeKind == TypeKind.Interface ? "interface" : "class";
		string parameters = type.Arity == 0 ? string.Empty : "<" + string.Join(", ", type.TypeParameters.Select(parameter =>
			(parameter.Variance == VarianceKind.In ? "in " : parameter.Variance == VarianceKind.Out ? "out " : string.Empty) + Identifier(parameter.Name))) + ">";
		return $"partial {kind} {Identifier(type.Name)}{parameters}";
	}

	private static string WrapType(INamedTypeSymbol type, string body)
		=> WrapDeclaration(type, Declaration(type), body);

	private static string WrapDeclaration(INamedTypeSymbol type, string declaration, string body)
	{
		string source = $$"""
			{{declaration}}
			{
				{{Indent(body)}}
			}
			""";
		for (INamedTypeSymbol? container = type.ContainingType; container is not null; container = container.ContainingType)
		{
			source = $$"""
				{{Declaration(container)}}
				{
					{{Indent(source)}}
				}
				""";
		}

		string ns = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : $"namespace {type.ContainingNamespace.ToDisplayString()};";
		return $$"""
			// <auto-generated />
			#nullable enable
			{{ns}}

			{{source}}
			""";
	}
}