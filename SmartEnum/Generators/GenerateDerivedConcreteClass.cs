namespace SmartEnum.Generators;

using System.Linq;
using Microsoft.CodeAnalysis;

public sealed partial class SmartEnumGenerator
{
	private static string GenerateDerivedConcreteClass(INamedTypeSymbol type, INamedTypeSymbol root)
	{
		DataMember[] data = GetData(type, root);
		return $$"""
			{{GenerateConstructor(type, root, "private")}}

			public static {{TypeName(type)}} ConstructUnvalidated({{string.Join(", ", data.Select(Parameter))}})
				=> new {{TypeName(type)}}({{Arguments(data)}});
			""";
	}
}