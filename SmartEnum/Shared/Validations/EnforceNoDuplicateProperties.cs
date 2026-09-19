namespace SmartEnum.Shared;

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using static HierarchyError.DuplicatePropertyDefinitionsFound;

public abstract partial class ValidatedHierarchyData
{
	private static Result<HierarchyError.DuplicatePropertyDefinitionsFound> EnforceNoDuplicateProperties(HierarchyData value, Lazy<ImmutableArray<DuplicatePropertyData>> duplicateProperties)
	{
		ImmutableArray<DuplicatePropertyData> currentAndOtherTypeDuplicateProperties = duplicateProperties.Value
			.Where(x => x.DuplicateProperties.Any(y => SymbolEqualityComparer.Default.Equals(y.ContainingType, value.Type)))
			.ToImmutableArray();

		if (currentAndOtherTypeDuplicateProperties.Any())
		{
			ImmutableArray<DuplicateProperties> currentTypeDuplicateProperties = currentAndOtherTypeDuplicateProperties
				.Select(x => new DuplicateProperties(
					x.DuplicateProperties.First(y => SymbolEqualityComparer.Default.Equals(y.ContainingType, value.Type)),
					x.CommonParentType.ToDisplayString(),
					x.DuplicateProperties.Where(y => !SymbolEqualityComparer.Default.Equals(y.ContainingType, value.Type)).Select(y => y.ContainingType.ToDisplayString()).ToImmutableArray()))
				.ToImmutableArray();

			return new Failure<HierarchyError.DuplicatePropertyDefinitionsFound>(new(currentTypeDuplicateProperties));
		}

		return new Success<HierarchyError.DuplicatePropertyDefinitionsFound>();
	}
}