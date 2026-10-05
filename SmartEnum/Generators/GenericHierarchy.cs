namespace SmartEnum.Generators;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

public sealed partial class SmartEnumGenerator
{
	// Infer a descendant's type arguments from the root's type arguments. This
	// supports renamed/reordered parameters without guessing closed specializations.
	private static INamedTypeSymbol? MapToRoot(INamedTypeSymbol type, INamedTypeSymbol root)
	{
		List<INamedTypeSymbol> path = GetPath(type, root);
		if (path.Count == 0) return null;
		Dictionary<ITypeParameterSymbol, ITypeSymbol> substitutions = new(SymbolEqualityComparer.Default);
		return !InferArguments(path[0], root, substitutions) ? null : Substitute(type, substitutions) as INamedTypeSymbol;
	}

	private static bool InferArguments(ITypeSymbol pattern, ITypeSymbol target, Dictionary<ITypeParameterSymbol, ITypeSymbol> substitutions)
	{
		if (pattern is ITypeParameterSymbol parameter)
		{
			if (substitutions.TryGetValue(parameter, out ITypeSymbol? previous)) return SymbolEqualityComparer.Default.Equals(previous, target);
			substitutions.Add(parameter, target);
			return true;
		}

		return pattern is not INamedTypeSymbol named || target is not INamedTypeSymbol targetNamed || !SameDefinition(named, targetNamed)
			? SymbolEqualityComparer.Default.Equals(pattern, target)
			: (named.ContainingType is null || (targetNamed.ContainingType is not null && InferArguments(named.ContainingType, targetNamed.ContainingType, substitutions))) && named.TypeArguments.Zip(targetNamed.TypeArguments, (left, right) => InferArguments(left, right, substitutions)).All(result => result);
	}

	private static ITypeSymbol? Substitute(ITypeSymbol type, Dictionary<ITypeParameterSymbol, ITypeSymbol> substitutions)
	{
		if (type is ITypeParameterSymbol parameter) return substitutions.TryGetValue(parameter, out ITypeSymbol? target) ? target : null;
		if (type is not INamedTypeSymbol named) return type;
		INamedTypeSymbol definition = named.OriginalDefinition;
		if (named.ContainingType is not null)
		{
			if (Substitute(named.ContainingType, substitutions) is not INamedTypeSymbol container) return null;
			definition = container.GetTypeMembers(named.Name, named.Arity).First(candidate => SameDefinition(candidate, named));
		}

		if (named.Arity == 0) return definition;
		ITypeSymbol?[] arguments = named.TypeArguments.Select(argument => Substitute(argument, substitutions)).ToArray();
		return arguments.Any(argument => argument is null) ? null : definition.Construct(arguments!);
	}

	private static bool SatisfiesConstraints(INamedTypeSymbol type, Compilation compilation)
	{
		Dictionary<ITypeParameterSymbol, ITypeSymbol> substitutions = new(SymbolEqualityComparer.Default);
		for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
		{
			for (int index = 0; index < current.Arity; index++) substitutions[current.OriginalDefinition.TypeParameters[index]] = current.TypeArguments[index];
		}

		foreach (KeyValuePair<ITypeParameterSymbol, ITypeSymbol> substitution in substitutions)
		{
			ITypeParameterSymbol parameter = substitution.Key;
			ITypeSymbol argument = substitution.Value;
			ITypeParameterSymbol? argumentParameter = argument as ITypeParameterSymbol;
			if (parameter.HasReferenceTypeConstraint && !argument.IsReferenceType) return false;
			if (parameter.HasReferenceTypeConstraint && parameter.ReferenceTypeConstraintNullableAnnotation != NullableAnnotation.Annotated
				&& argumentParameter?.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated)
			{
				return false;
			}

			if (parameter.HasValueTypeConstraint && !argument.IsValueType) return false;
			if (parameter.HasUnmanagedTypeConstraint && !argument.IsUnmanagedType) return false;
			if (parameter.HasNotNullConstraint && !(argumentParameter?.HasNotNullConstraint == true || argument.IsValueType
				|| (argument.IsReferenceType && argument.NullableAnnotation != NullableAnnotation.Annotated)))
			{
				return false;
			}

			if (parameter.HasConstructorConstraint && !(argument.IsValueType || argumentParameter?.HasConstructorConstraint == true
				|| (argument is INamedTypeSymbol { IsAbstract: false } named && named.InstanceConstructors.Any(constructor => constructor.DeclaredAccessibility == Accessibility.Public && constructor.Parameters.IsEmpty))))
			{
				return false;
			}

			foreach (ITypeSymbol constraint in parameter.ConstraintTypes)
			{
				ITypeSymbol? required = Substitute(constraint, substitutions);
				if (required is null || !compilation.ClassifyCommonConversion(argument, required).IsImplicit) return false;
			}
		}

		return true;
	}
}