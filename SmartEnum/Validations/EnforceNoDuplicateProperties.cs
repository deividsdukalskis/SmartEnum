namespace SmartEnum.Shared;

using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

public static partial class EnumHierarchy
{
	private static Result<HierarchyError.DuplicatePropertyDefinitionsFound> EnforceNoDuplicateProperties(HierarchyData data)
	{
		(INamedTypeSymbol BaseType, ImmutableArray<string> ParentTypeNames) = data switch
		{
			HierarchyData.BaseTypeData => (data.Type, ImmutableArray.Create(data.Type.ToDisplayString())),
			HierarchyData.DerivedTypeData derivedType => (derivedType.BaseType, derivedType.ParentTypeNames),
			_ => throw new InvalidOperationException()
		};

		IEnumerable<IPropertySymbol> properties = data.Type
			.GetMembers()
			.OfType<IPropertySymbol>();

		IEnumerable<IGrouping<(string, IPropertySymbol), string>> duplicateProperties = properties.SelectMany((property) => HierarchyDatas
			.Where(secondType =>
			{
				INamedTypeSymbol? secondBaseType = null;
				if (secondType is Success<HierarchyData, HierarchyError.DuplicateAttributesFound> successSecondType
					&& !SymbolEqualityComparer.Default.Equals(successSecondType.Value.Type, data.Type))
				{
					secondBaseType = successSecondType.Value switch
					{
						HierarchyData.BaseTypeData => successSecondType.Value.Type,
						HierarchyData.DerivedTypeData secondDerivedType => secondDerivedType.BaseType,
						_ => throw new InvalidOperationException()
					};
				}

				return SymbolEqualityComparer.Default.Equals(BaseType.OriginalDefinition, secondBaseType?.OriginalDefinition);
			})
			.OfType<Success<HierarchyData, HierarchyError.DuplicateAttributesFound>>()
			.SelectMany(secondType => secondType.Value.Type
				.GetMembers()
				.OfType<IPropertySymbol>()
				.Where(secondProperty => secondProperty.Name == property.Name)
				.Select(secondProperty =>
				{
					string commonType = secondType.Value switch
					{
						HierarchyData.BaseTypeData => secondType.Value.Type.ToDisplayString(),
						HierarchyData.DerivedTypeData derivedType => derivedType.ParentTypeNames.First(x => ParentTypeNames.Any(y => x == y)),
						_ => throw new InvalidOperationException()
					};

					return (Property: secondProperty, CommonTypeName: commonType);
				})
				.GroupBy(x => (x.CommonTypeName, property), x => x.Property.ContainingType.ToDisplayString())));

		if (duplicateProperties.Any())
		{
			return new Failure<HierarchyError.DuplicatePropertyDefinitionsFound>(new(duplicateProperties.ToImmutableArray()));
		}

		return new Success<HierarchyError.DuplicatePropertyDefinitionsFound>();
	}
}