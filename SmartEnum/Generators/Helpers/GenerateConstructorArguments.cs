namespace SmartEnum.Generators;

using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.CodeAnalysis;

public static partial class GeneratorHelpers
{
	public static string GenerateConstructorArguments(ImmutableArray<IPropertySymbol> properties)
	{
		string result = string.Empty;
		foreach (IPropertySymbol property in properties)
		{
			if (string.IsNullOrWhiteSpace(property.Name))
			{
				continue;
			}

			if (string.IsNullOrEmpty(result))
			{
				result += $"{JsonNamingPolicy.CamelCase.ConvertName(property.Name)}";
			}
			else
			{
				result += $", {JsonNamingPolicy.CamelCase.ConvertName(property.Name)}";
			}
		}

		return result;
	}
}