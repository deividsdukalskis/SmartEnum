namespace SmartEnum.Shared;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static partial class EnumHierarchy
{
	private static Result<HierarchyError.InvalidKeyName> EnforceProperKeyName(HierarchyData data)
	{
		if (data is not HierarchyData.BaseTypeData baseType)
		{
			return new Success<HierarchyError.InvalidKeyName>();
		}

		List<(AttributeData, string)> attributesWithInvalidKeyNames = new();
		IEnumerable<(AttributeData, object?)> keyNames = baseType.EnumAttributes.Select(arg => (arg, arg.ConstructorArguments.FirstOrDefault().Value));
		foreach ((AttributeData attrib, object? keyName) tuple in keyNames)
		{
			if (tuple.keyName is string name && !SyntaxFacts.IsValidIdentifier(name))
			{
				attributesWithInvalidKeyNames.Add((tuple.attrib, name));
			}
		}

		if (!attributesWithInvalidKeyNames.Any())
		{
			return new Success<HierarchyError.InvalidKeyName>();
		}

		return new Failure<HierarchyError.InvalidKeyName>(new(baseType.Type, attributesWithInvalidKeyNames.ToImmutableArray()));
	}
}