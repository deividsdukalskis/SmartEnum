namespace SmartEnum.Shared.Validations;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static partial class SharedValidationFunctions
{
	public static ImmutableList<AttributeData> EnforceProperKeyName(RecursionResult recursionResult)
	{
		if (!recursionResult.ShouldOriginalClassBeProcessedFurther)
		{
			return ImmutableList.Create<AttributeData>();
		}

		List<AttributeData> attributesWithInvalidKeyNames = new();
		IEnumerable<(AttributeData, object?)> keyNames = recursionResult.Attributes.Select(arg => (arg, arg.ConstructorArguments.FirstOrDefault().Value));
		foreach ((AttributeData attrib, object? keyName) tuple in keyNames)
		{
			if (tuple.keyName is string name && !SyntaxFacts.IsValidIdentifier(name))
			{
				attributesWithInvalidKeyNames.Add(tuple.attrib);
			}
		}

		return attributesWithInvalidKeyNames.ToImmutableList();
	}
}