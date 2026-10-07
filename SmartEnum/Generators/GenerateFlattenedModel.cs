namespace SmartEnum.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

public sealed partial class SmartEnumGenerator
{
	private static bool IsBranchSpecific(DataMember data, INamedTypeSymbol root, INamedTypeSymbol[] members)
		=> members.Where(member => !member.IsAbstract).Any(member => !GetData(member, root).Concat(GetPath(member, root).SelectMany(GetOwnSnapshots)).Any(candidate => candidate.Name == data.Name));

	private static bool IsNullableValue(ITypeSymbol type) => type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

	private static bool NeedsOptionalValue(DataMember data, INamedTypeSymbol root, INamedTypeSymbol[] members)
		=> data.Type is ITypeParameterSymbol { IsReferenceType: false, IsValueType: false } && IsBranchSpecific(data, root, members);

	private static DataMember[] GetOwnSnapshots(INamedTypeSymbol type) => type.GetMembers().OfType<IPropertySymbol>()
		.Where(property => HasDataAttribute(property, "SmartEnumSnapshotAttribute") && !HasDataAttribute(property, "SmartEnumIgnoreAttribute")
			&& !property.IsStatic && !property.IsIndexer && !property.IsAbstract && property.GetMethod is not null
			&& property.ExplicitInterfaceImplementations.IsEmpty && !GetOwnData(type).Any(data => data.Name == property.Name))
		.Select(property => new DataMember(property.Name, property.Type, property)).OrderBy(member => member.Name, StringComparer.Ordinal).ToArray();

	private static DataMember[] GetFlattenedMembers(INamedTypeSymbol root, INamedTypeSymbol[] members)
		=> members.SelectMany(member => GetData(member, root).Concat(GetPath(member, root).SelectMany(GetOwnSnapshots)))
			.GroupBy(member => member.Name).Select(group => group.First()).ToArray();

	private static string FlattenedPropertyType(DataMember data, INamedTypeSymbol root, INamedTypeSymbol[] members)
		=> NeedsOptionalValue(data, root, members) ? FlattenedTypeName(root) + ".OptionalValue<" + TypeName(data.Type) + ">?"
			: IsBranchSpecific(data, root, members) && !IsNullableValue(data.Type)
			? TypeName(data.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)) + "?" : TypeName(data.Type);

	private static string FlattenedArgument(DataMember data, INamedTypeSymbol root, INamedTypeSymbol[] members)
	{
		string expression = "data." + Identifier(data.Name);
		string missing = $"throw new global::System.ArgumentException(\"The selected SmartEnum type requires '{data.Name}'.\", nameof(data))";
		if (NeedsOptionalValue(data, root, members)) expression = $"({expression} ?? {missing}).Value";
		else if (IsBranchSpecific(data, root, members) && data.Type.IsValueType && !IsNullableValue(data.Type)) return expression + " ?? " + missing;
		return !data.Type.IsValueType && data.Type.NullableAnnotation == NullableAnnotation.NotAnnotated
			? expression + " ?? " + missing : expression + "!";
	}

	private static string FlattenedTypeName(INamedTypeSymbol root)
	{
		string container = root.ContainingType is not null ? TypeName(root.ContainingType) + "."
			: root.ContainingNamespace.IsGlobalNamespace ? "global::" : "global::" + root.ContainingNamespace.ToDisplayString() + ".";
		string arguments = root.Arity == 0 ? string.Empty : "<" + string.Join(", ", root.TypeArguments.Select(TypeName)) + ">";
		return container + Identifier(root.Name + "Flattened") + arguments;
	}

	private static string GenerateTransferProperty(string type, string name, int index)
		=> $$"""
			private {{type}} __SmartEnumValue{{index}} = default!;
			internal bool __SmartEnumHas{{index}} { get; private set; }
			public {{type}} {{name}}
			{
				get => this.__SmartEnumValue{{index}};
				set { this.__SmartEnumValue{{index}} = value; this.__SmartEnumHas{{index}} = true; }
			}
			""";

	private static string GenerateFlattenedModel(INamedTypeSymbol root, INamedTypeSymbol[] members, INamedTypeSymbol attribute)
	{
		AttributeData[] keys = GetAttributes(root, attribute);
		DataMember[] data = GetFlattenedMembers(root, members);
		string[] keyNames = GetKeyParameters(keys, data);
		IEnumerable<string> properties = keys.Select((key, index) => GenerateTransferProperty(TypeName(key.AttributeClass!.TypeArguments[0]), keyNames[index], index))
			.Concat(data.Select((member, index) => GenerateTransferProperty(FlattenedPropertyType(member, root, members), Identifier(member.Name), keys.Length + index)));
		string accessibility = root.DeclaredAccessibility switch
		{
			Accessibility.Public => "public",
			Accessibility.Private => "private",
			Accessibility.Protected => "protected",
			Accessibility.ProtectedOrInternal => "protected internal",
			Accessibility.ProtectedAndInternal => "private protected",
			_ => "internal"
		};
		string parameters = root.Arity == 0 ? string.Empty : "<" + string.Join(", ", root.TypeParameters.Select(parameter => Identifier(parameter.Name))) + ">";
		string declaration = $"{accessibility} sealed partial class {Identifier(root.Name + "Flattened")}{parameters}";
		foreach (ITypeParameterSymbol parameter in root.TypeParameters)
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

		HashSet<string> parameterNames = new(StringComparer.Ordinal);
		for (INamedTypeSymbol? container = root; container is not null; container = container.ContainingType)
			foreach (ITypeParameterSymbol parameter in container.TypeParameters) _ = parameterNames.Add(parameter.Name);
		string valueParameter = "TValue";
		while (parameterNames.Contains(valueParameter)) valueParameter = "T" + valueParameter;
		string optional = data.Any(member => NeedsOptionalValue(member, root, members)) ? $$"""
			/// <summary>Distinguishes absent generic branch data from a present value, including zero or null.</summary>
			public struct OptionalValue<{{valueParameter}}>
			{
				public {{valueParameter}} Value { get; set; }
				public OptionalValue({{valueParameter}} value) => this.Value = value;
				public static implicit operator OptionalValue<{{valueParameter}}>({{valueParameter}} value) => new OptionalValue<{{valueParameter}}>(value);
			}
			""" : string.Empty;
		return WrapDeclaration(root, declaration, string.Join("\n", properties) + "\n" + optional);
	}

	private static string GenerateFlattenWriter(INamedTypeSymbol type, INamedTypeSymbol root, INamedTypeSymbol[] members, INamedTypeSymbol attribute, Compilation compilation)
	{
		List<INamedTypeSymbol> path = GetPath(type, root);
		bool isRoot = SameDefinition(type, root);
		List<string> assignments = new();
		if (isRoot)
		{
			foreach (DataMember member in GetExternalData(type))
			{
				ISymbol readable = GetExternalReadableMember(type, member, compilation)!;
				string receiver = SameDefinition(readable.ContainingType, type) ? "this" : "base";
				assignments.Add($"data.{Identifier(member.Name)} = {receiver}.{Identifier(readable.Name)};");
			}
		}
		else
		{
			assignments.Add("base.__SmartEnumWriteFlattenedData(data);");
			AttributeData[] keys = GetAttributes(root, attribute);
			DataMember[] data = GetFlattenedMembers(root, members);
			int keyIndex = path.Count - 2;
			assignments.Add($"data.{GetKeyParameters(keys, data)[keyIndex]} = {TypeName(type)}.{Identifier((string)keys[keyIndex].ConstructorArguments[0].Value!)};");
		}

		assignments.AddRange(GetOwnData(type).Concat(GetOwnSnapshots(type)).Select(member =>
		{
			DataMember mapped = GetFlattenedMembers(root, members).First(candidate => candidate.Name == member.Name);
			string value = "this." + Identifier(member.Name);
			if (NeedsOptionalValue(mapped, root, members)) value = $"new {FlattenedTypeName(path[0])}.OptionalValue<{TypeName(member.Type)}>({value})";
			return $"data.{Identifier(member.Name)} = {value};";
		}));
		return $$"""
			protected {{(isRoot ? "virtual" : "override")}} void __SmartEnumWriteFlattenedData({{FlattenedTypeName(path[0])}} data)
			{
				{{Indent(string.Join("\n", assignments))}}
			}
			""";
	}

	// External constructor arguments need readable state to support the reverse mapping.
	private static ISymbol? GetExternalReadableMember(INamedTypeSymbol root, DataMember data, Compilation compilation)
	{
		HashSet<ISymbol> visited = new(SymbolEqualityComparer.Default);
		HashSet<string> hidden = new(StringComparer.Ordinal);
		AttributeData? mapping = root.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == "SmartEnum.SmartEnumParentDataAttribute"
			&& (string?)attribute.ConstructorArguments[0].Value == data.Symbol.Name);
		string name = mapping is null ? data.Symbol.Name : (string?)mapping.ConstructorArguments[1].Value ?? string.Empty;
		for (INamedTypeSymbol? parent = mapping is null ? root.BaseType : root; parent is not null && visited.Add(parent); parent = parent.BaseType)
		{
			ISymbol[] candidates = parent.GetMembers().Where(member => !hidden.Contains(member.Name) && !member.IsStatic
				&& string.Equals(member.Name, name, mapping is null ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
				&& compilation.IsSymbolAccessibleWithin(member, root)
				&& ((member is IPropertySymbol { GetMethod: not null, IsIndexer: false } property
					&& compilation.IsSymbolAccessibleWithin(property.GetMethod, root) && SymbolEqualityComparer.Default.Equals(property.Type, data.Type))
					|| (member is IFieldSymbol field && SymbolEqualityComparer.Default.Equals(field.Type, data.Type)))).ToArray();
			if (candidates.Length > 0) return candidates.FirstOrDefault(member => member.Name == name) ?? (candidates.Length == 1 ? candidates[0] : null);
			foreach (ISymbol member in parent.GetMembers()) _ = hidden.Add(member.Name);
		}

		return null;
	}
}