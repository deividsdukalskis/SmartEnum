namespace SmartEnum.Generators;

using Microsoft.CodeAnalysis;

public static partial class GeneratorHelpers
{
	public static string GenerateNamespace(INamedTypeSymbol type)
	{
		string namespaceText = type.ContainingNamespace is not null ? $"""

			namespace {type.ContainingNamespace};

			""" :
			string.Empty;

		return namespaceText;
	}
}