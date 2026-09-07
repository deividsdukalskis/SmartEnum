namespace SmartEnum.Generators.Models;

using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using SmartEnum.Shared;

public abstract class GenerationModel
{
	public INamedTypeSymbol Type { get; }
	public ImmutableArray<IPropertySymbol> Properties { get; }
	public string ConstructorArguments { get; }
	public string ConstructorParameters { get; }
	public string Namespace { get; }

	protected GenerationModel(ImmutableArray<IPropertySymbol> properties, INamedTypeSymbol type)
	{
		this.Properties = properties;
		this.Type = type;
		this.ConstructorArguments = this.GenerateConstructorArguments(this.Properties);
		this.ConstructorParameters = this.GenerateConstructorParameters(this.Properties);
		this.Namespace = this.GenerateNamespace(this.Type);
	}

	public static GenerationModel? Create(ValidationResult validationResult)
	{
		if (!validationResult.IsInEnumHierarchy || !validationResult.HasAllChecksPassed || validationResult.BaseClass is null)
		{
			return null;
		}

		if (validationResult.IsBaseClass)
		{
			return BaseClassGenerationModel.Create(validationResult);
		}
	}

	public abstract string GenerateCode();

	private string GenerateConstructorParameters(ImmutableArray<IPropertySymbol> properties)
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
				result += $"{property.ToDisplayString()} {JsonNamingPolicy.CamelCase.ConvertName(property.Name)}";
			}
			else
			{
				result += $", {property.ToDisplayString()} {JsonNamingPolicy.CamelCase.ConvertName(property.Name)}";
			}
		}

		return result;
	}

	private string GenerateConstructorArguments(ImmutableArray<IPropertySymbol> properties)
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

	private string GenerateNamespace(INamedTypeSymbol type)
	{
		string namespaceText = type.ContainingNamespace is not null ? $"""

			namespace {type.ContainingNamespace};

			""" :
			string.Empty;

		return namespaceText;
	}
}