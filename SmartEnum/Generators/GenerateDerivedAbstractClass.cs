namespace SmartEnum.Generators;

using Microsoft.CodeAnalysis;

public sealed partial class SmartEnumGenerator
{
	private static string GenerateDerivedAbstractClass(INamedTypeSymbol type, INamedTypeSymbol root)
		=> GenerateConstructor(type, root, "protected");
}