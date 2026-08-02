namespace SmartEnum.Generators;

using System;
using Microsoft.CodeAnalysis;

[Generator(LanguageNames.CSharp)]
public sealed class DependencyResolverGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context) { }
}
