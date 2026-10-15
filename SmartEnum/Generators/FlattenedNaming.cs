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

}