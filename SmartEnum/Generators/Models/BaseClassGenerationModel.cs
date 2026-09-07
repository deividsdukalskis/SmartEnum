namespace SmartEnum.Generators.Models;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using SmartEnum.Shared;

public class BaseClassGenerationModel : GenerationModel
{
	public class DerivedConcreteClass
	{
		public INamedTypeSymbol Type { get; }
		public ImmutableArray<IFieldSymbol> KeyFields { get; }
		public string UsingNamespace { get; }

		private DerivedConcreteClass(INamedTypeSymbol type, ImmutableArray<IFieldSymbol> keyFields)
		{
			this.Type = type;
			this.KeyFields = keyFields;
			this.UsingNamespace = this.GenerateUsingNamespace(type);
		}

		private string GenerateUsingNamespace(INamedTypeSymbol type)
		{
			if (type.ContainingNamespace is null)
			{
				return string.Empty;
			}

			return $"using {type.ContainingNamespace};";
		}
	}

	public ImmutableArray<DerivedConcreteClass> DerivedConcreteClasses { get; }
	public string PropertyInitializers { get; }

	private BaseClassGenerationModel()
	{
		
	}

	public static BaseClassGenerationModel Create(ValidationResult validationResult)
	{
		if (validationResult.)
	}
}