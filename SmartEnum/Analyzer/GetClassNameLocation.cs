namespace SmartEnum.Analyzer;

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public sealed partial class SmartEnumAnalyzer
{
	private Location GetClassNameLocation(INamedTypeSymbol type, CancellationToken cancellationToken)
	{
		SyntaxNode? syntax = type.DeclaringSyntaxReferences
					.FirstOrDefault()?
					.GetSyntax(cancellationToken);

		if (syntax is ClassDeclarationSyntax classSyntax)
		{
			return classSyntax.Identifier.GetLocation();
		}

		return Location.None;
	}
}