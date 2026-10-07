namespace SmartEnum.Generators;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

public sealed partial class SmartEnumGenerator
{
	private static string GenerateBaseClass(INamedTypeSymbol type, INamedTypeSymbol[] members, INamedTypeSymbol attribute)
	{
		AttributeData[] keys = GetAttributes(type, attribute);
		DataMember[] data = GetFlattenedMembers(type, members);
		string[] keyParameters = GetKeyParameters(keys, data);
		IEnumerable<string> keyPresence = keys.Select((key, index) => $"if (!data.__SmartEnumHas{index}) throw new global::System.ArgumentException(\"Discriminator '{keyParameters[index].Substring(1)}' was not supplied.\", nameof(data));");
		IEnumerable<string> branches = members.Where(member => !member.IsAbstract).Select(concrete =>
		{
			List<INamedTypeSymbol> path = GetPath(concrete, type);
			IEnumerable<string> conditions = keys.Select((key, index) =>
				$"global::System.Collections.Generic.EqualityComparer<{TypeName(key.AttributeClass!.TypeArguments[0])}>.Default.Equals(data.{keyParameters[index]}, {TypeName(path[index + 1])}.{Identifier((string)key.ConstructorArguments[0].Value!)})");
			HashSet<string> selected = new(GetData(concrete, type).Select(member => member.Name));
			IEnumerable<string> presenceChecks = data.Select((member, index) => (member, index)).Where(item => selected.Contains(item.member.Name)
				&& item.member.Type.NullableAnnotation != NullableAnnotation.Annotated && !IsNullableValue(item.member.Type))
				.Select(item => $"if (!data.__SmartEnumHas{keys.Length + item.index}) throw new global::System.ArgumentException(\"Property '{item.member.Name}' was not supplied.\", nameof(data));");
			IEnumerable<string> unusedChecks = members.SelectMany(member => GetData(member, type)).GroupBy(member => member.Name).Select(group => group.First())
				.Where(member => !selected.Contains(member.Name)).Select(member =>
					$"if (rejectUnusedData && data.{Identifier(member.Name)} is not null) throw new global::System.ArgumentException(\"Property '{member.Name}' does not belong to the selected SmartEnum type.\", nameof(data));");
			return $$"""
				if ({{string.Join(" && ", conditions)}})
				{
					{{Indent(string.Join("\n", unusedChecks))}}
					{{Indent(string.Join("\n", presenceChecks))}}
					return {{TypeName(concrete)}}.ConstructUnvalidated({{string.Join(", ", GetData(concrete, type).Select(member => FlattenedArgument(member, type, members)))}});
				}
				""";
		});
		return $$"""
			{{GenerateConstructor(type, type, "protected")}}

			/// <summary>
			/// Warning: Maps data to a concrete type while bypassing the normal domain validation workflow.
			/// Use only when plain data transfer is necessary, such as materializing ORM data into a business model.
			/// </summary>
			/// <remarks>
			/// Custom constructors and property setters still execute and may perform their own validation.
			/// </remarks>
			public static {{TypeName(type)}} MapFromFlattenedDataUnvalidated({{FlattenedTypeName(type)}} data, bool rejectUnusedData = true)
			{
				if (data is null) throw new global::System.ArgumentNullException(nameof(data));
				CustomizeFlattenedInput(ref data);
				if (data is null) throw new global::System.ArgumentNullException(nameof(data));
				{{Indent(string.Join("\n", keyPresence))}}
				{{Indent(string.Join("\n", branches))}}
				throw new global::System.ArgumentException("No SmartEnum type matches the supplied key combination.");
			}

			/// <summary>Copies this instance's data and discriminator values into a flattened transfer model.</summary>
			public {{FlattenedTypeName(type)}} Flatten()
			{
				if ({{string.Join(" && ", members.Where(member => !member.IsAbstract).Select(member => $"this.GetType() != typeof({TypeName(member)})").DefaultIfEmpty($"this.GetType() != typeof({TypeName(type)})"))}})
					throw new global::System.InvalidOperationException("The runtime type is not part of the generated SmartEnum hierarchy.");
				{{FlattenedTypeName(type)}} data = new {{FlattenedTypeName(type)}}();
				this.__SmartEnumWriteFlattenedData(data);
				this.CustomizeFlattenedData(data);
				return data;
			}

			/// <summary>Optional hook for cloning, computed transfer values, or redaction after flattening.</summary>
			partial void CustomizeFlattenedData({{FlattenedTypeName(type)}} data);
			/// <summary>Optional hook for cloning or adapting incoming transfer data before construction.</summary>
			static partial void CustomizeFlattenedInput(ref {{FlattenedTypeName(type)}} data);

			{{GenerateLegacyMapper(type, members, attribute)}}
			""";
	}

	private static string[] GetKeyParameters(AttributeData[] keys, DataMember[] data)
	{
		HashSet<string> names = new(data.Select(member => member.Name), System.StringComparer.Ordinal);
		return keys.Select(key =>
		{
			string name = (string)key.ConstructorArguments[0].Value!;
			while (!names.Add(name)) name = "key_" + name;
			return Identifier(name);
		}).ToArray();
	}
}