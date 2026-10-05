namespace SmartEnum.Generators;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

public sealed partial class SmartEnumGenerator
{
	private static string GenerateBaseClass(INamedTypeSymbol type, INamedTypeSymbol[] members, INamedTypeSymbol attribute)
	{
		AttributeData[] keys = GetAttributes(type, attribute);
		DataMember[] data = members.SelectMany(member => GetData(member, type)).GroupBy(member => member.Name).Select(group => group.First()).ToArray();
		string[] keyParameters = GetKeyParameters(keys, data);
		IEnumerable<string> parameters = keys.Select((key, index) => TypeName(key.AttributeClass!.TypeArguments[0]) + " " + keyParameters[index])
			.Concat(data.Select(Parameter));
		IEnumerable<string> branches = members.Where(member => !member.IsAbstract).Select(concrete =>
		{
			List<INamedTypeSymbol> path = GetPath(concrete, type);
			IEnumerable<string> conditions = keys.Select((key, index) =>
				$"global::System.Collections.Generic.EqualityComparer<{TypeName(key.AttributeClass!.TypeArguments[0])}>.Default.Equals({keyParameters[index]}, {TypeName(path[index + 1])}.{Identifier((string)key.ConstructorArguments[0].Value!)})");
			return $$"""
				if ({{string.Join(" && ", conditions)}})
				{
					return {{TypeName(concrete)}}.ConstructUnvalidated({{Arguments(GetData(concrete, type))}});
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
			public static {{TypeName(type)}} MapDataToTypeUnvalidated({{string.Join(", ", parameters)}})
			{
				{{Indent(string.Join("\n", branches))}}
				throw new global::System.ArgumentException("No SmartEnum type matches the supplied key combination.");
			}
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