namespace SmartEnum.Shared;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

internal static class StrategyContract
{
	internal static bool IsExplicit(INamedTypeSymbol type)
	{
		HashSet<ISymbol> visited = new(SymbolEqualityComparer.Default);
		for (INamedTypeSymbol? current = type; current is not null && visited.Add(current); current = current.BaseType)
		{
			if (current.GetAttributes().Any(attribute => attribute.AttributeClass?.OriginalDefinition.ToDisplayString() == "SmartEnum.SmartEnumAttribute<TKey>"
				&& attribute.ConstructorArguments.Length == 3))
			{
				return true;
			}
		}

		return false;
	}
}