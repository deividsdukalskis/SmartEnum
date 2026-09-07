namespace SmartEnum.Generators.Models;

using Microsoft.CodeAnalysis;

public class DerivedClassGenerationModel : GenerationModel
{
	public INamedTypeSymbol Type { get; }
	public INamedTypeSymbol BaseType { get; }
	public IFieldSymbol KeyField { get; }

	private DerivedClassGenerationModel(INamedTypeSymbol type, INamedTypeSymbol baseType, IF)
	{
		
	}
}