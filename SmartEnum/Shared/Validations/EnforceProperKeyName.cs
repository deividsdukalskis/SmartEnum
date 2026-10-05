namespace SmartEnum.Shared;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public abstract partial class ValidatedHierarchyData
{
	private static Result<HierarchyError.InvalidKeyName> EnforceProperKeyName(HierarchyData data)
	{
		if (data is not HierarchyData.BaseTypeData baseType)
		{
			return new Success<HierarchyError.InvalidKeyName>();
		}

		ImmutableArray<(AttributeData, string)>.Builder attributesWithInvalidKeyNames = ImmutableArray.CreateBuilder<(AttributeData, string)>();
		ImmutableArray<(AttributeData, object?)> keyNames = baseType.EnumAttributes.Select(arg => (arg, arg.ConstructorArguments.FirstOrDefault().Value)).ToImmutableArray();
		foreach ((AttributeData attrib, object? keyName) tuple in keyNames)
		{
			if (tuple.keyName is not string name || (!SyntaxFacts.IsValidIdentifier(name) && SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None) || name.StartsWith("@"))
			{
				attributesWithInvalidKeyNames.Add((tuple.attrib, tuple.keyName as string ?? "<null>"));
			}
		}

		return !attributesWithInvalidKeyNames.Any()
			? new Success<HierarchyError.InvalidKeyName>()
			: new Failure<HierarchyError.InvalidKeyName>(new(baseType.Type, attributesWithInvalidKeyNames.ToImmutable()));
	}
}