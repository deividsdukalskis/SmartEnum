namespace SmartEnum.Shared;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

public static partial class EnumHierarchy
{
	private static Result<HierarchyError.DuplicateKeyNamesFound> EnforceNoDuplicateKeyNames(HierarchyData data)
	{
		if (data is not HierarchyData.BaseTypeData baseType)
		{
			return new Success<HierarchyError.DuplicateKeyNamesFound>();
		}

		IEnumerable<object?> duplicateValues = baseType.EnumAttributes
			.GroupBy(x => x.ConstructorArguments.FirstOrDefault().Value)
			.Where(group => group.Count() > 1)
			.Select(group => group.Key);

		if (!duplicateValues.Any())
		{
			return new Success<HierarchyError.DuplicateKeyNamesFound>();
		}

		List<string> duplicateNames = new();
		foreach (object? item in duplicateValues)
		{
			if (item is not string s)
			{
				continue;
			}

			duplicateNames.Add(s);
		}

		return new Failure<HierarchyError.DuplicateKeyNamesFound>(new(data.Type, duplicateNames.ToImmutableArray()));
	}
}