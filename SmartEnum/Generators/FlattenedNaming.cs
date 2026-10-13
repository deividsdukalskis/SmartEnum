namespace SmartEnum.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

public sealed partial class SmartEnumGenerator
{
	private static string FlattenedName(string name) => string.Concat(name.Split('_').Where(part => part.Length > 0).Select(part =>
		char.ToUpperInvariant(part[0]) + (part.All(character => !char.IsLetter(character) || char.IsUpper(character))
			? part.Substring(1).ToLowerInvariant() : part.Substring(1))));

	private static string FlattenedIdentifier(string name) => Identifier(FlattenedName(name));

	private static string[] GetFlattenedKeyNames(AttributeData[] keys, DataMember[] data)
	{
		HashSet<string> names = new(data.Select(member => FlattenedName(member.Name)), StringComparer.Ordinal);
		return keys.Select(key =>
		{
			string name = FlattenedName((string)key.ConstructorArguments[0].Value!);
			while (!names.Add(name)) name = "Key" + name;
			return Identifier(name);
		}).ToArray();
	}
}