namespace SmartEnum.Generators;

using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.CodeAnalysis;

public static partial class GeneratorHelpers
{
	public static string GenerateInitializationList(ImmutableArray<IPropertySymbol> properties)
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
				result += $"this.{property.Name} = {JsonNamingPolicy.CamelCase.ConvertName(property.Name)};";
			}
			else
			{
				result += $"\nthis.{property.Name} = {JsonNamingPolicy.CamelCase.ConvertName(property.Name)};";
			}
		}

		return result;
	}
}