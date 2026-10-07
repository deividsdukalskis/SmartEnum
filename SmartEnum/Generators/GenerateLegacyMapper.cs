namespace SmartEnum.Generators;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

public sealed partial class SmartEnumGenerator
{
	private static string GenerateLegacyMapper(INamedTypeSymbol root, INamedTypeSymbol[] members, INamedTypeSymbol attribute)
	{
		AttributeData[] keys = GetAttributes(root, attribute);
		DataMember[] data = members.SelectMany(member => GetData(member, root)).GroupBy(member => member.Name).Select(group => group.First()).ToArray();
		ITypeSymbol[] types = keys.Select(key => key.AttributeClass!.TypeArguments[0]).Concat(data.Select(member => member.Type)).ToArray();
		if (root.Name == "MapDataToTypeUnvalidated" || root.GetMembers("MapDataToTypeUnvalidated").Any(member => member is not IMethodSymbol
			|| (member is IMethodSymbol { Arity: 0 } method && method.Parameters.Select(parameter => parameter.Type).SequenceEqual(types, SymbolEqualityComparer.Default))))
		{
			return string.Empty;
		}

		string[] keyParameters = GetKeyParameters(keys, data);
		string[] keyProperties = GetKeyParameters(keys, GetFlattenedMembers(root, members));
		IEnumerable<string> parameters = keys.Select((key, index) => TypeName(key.AttributeClass!.TypeArguments[0]) + " " + keyParameters[index]).Concat(data.Select(Parameter));
		IEnumerable<string> assignments = keys.Select((key, index) => keyProperties[index] + " = " + keyParameters[index])
			.Concat(data.Select(member => Identifier(member.Name) + " = " + (NeedsOptionalValue(member, root, members)
				? $"new {FlattenedTypeName(root)}.OptionalValue<{TypeName(member.Type)}>({Identifier(member.Name)})" : Identifier(member.Name))));
		return $$"""
			/// <summary>
			/// Compatibility adapter. Prefer MapFromFlattenedDataUnvalidated for new code.
			/// Warning: Bypasses normal domain validation; use only for plain data transfer such as ORM materialization.
			/// Custom constructors and setters still execute.
			/// </summary>
			[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
			public static {{TypeName(root)}} MapDataToTypeUnvalidated({{string.Join(", ", parameters)}})
				=> MapFromFlattenedDataUnvalidated(new {{FlattenedTypeName(root)}} { {{string.Join(", ", assignments)}} }, rejectUnusedData: false);
			""";
	}
}