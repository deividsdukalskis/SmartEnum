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

			/// <summary>
			/// Warning: Constructs an instance while bypassing the normal domain validation workflow.
			/// Use only when plain data transfer is necessary, such as materializing ORM data into a business model.
			/// </summary>
			/// <remarks>
			/// Custom constructors and property setters still execute and may perform their own validation.
			/// </remarks>
			public static {{TypeName(type)}} ConstructUnvalidated({{string.Join(", ", data.Select(Parameter))}})
				=> new {{TypeName(type)}}({{Arguments(data)}});
			""";
	}
}