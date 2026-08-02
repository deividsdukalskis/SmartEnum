namespace SmartEnum.Analyzer;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;
using System;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class SmartEnumAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
		MustBePartialClass,
		MustBeAbstractClass,
		InvalidKeyName,
		KeyFieldDoesNotExist,
		KeyIsNotPublic,
		KeyIsNotConst,
		InvalidKeyType);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(
			GeneratedCodeAnalysisFlags.None);

		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(compilationContext =>
		{
			INamedTypeSymbol? attributeDefinition =
				compilationContext.Compilation.GetTypeByMetadataName(
					"SmartEnum.SmartEnumAttribute`1");

			// The project being analyzed does not reference the attribute.
			if (attributeDefinition is null)
			{
				return;
			}

			compilationContext.RegisterSymbolAction((context) =>
			{
				INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
				RecursionResult recursionResult = this.RecurseEnumHierarchy(type, attributeDefinition, true);
				if (!recursionResult.EnumBaseClassFound)
				{
					return;
				}

				this.EnforceAbstractClass(recursionResult, context);
				this.EnforcePartialClass(recursionResult, context);
				this.EnforceProperKeyName(recursionResult, context);

			},
			SymbolKind.NamedType);
		});
	}

	private RecursionResult RecurseEnumHierarchy(
		INamedTypeSymbol type,
		INamedTypeSymbol attributeDefinition,
		bool originalClassMightHaveAttributes,
		RecursionResult? original = null,
		bool originalClassHasAttributesApplied = false,
		bool previousClassesHasAttributes = false)
	{
		ImmutableList<AttributeData> enumAttributes = type
						.GetAttributes()
						.Where(attrib => SymbolEqualityComparer.Default.Equals(attrib.AttributeClass?.OriginalDefinition, attributeDefinition))
						.ToImmutableList();

		if (original is null)
		{
			originalClassMightHaveAttributes = true;
			original = new(enumAttributes, type, false, false, true);
		}

		if (enumAttributes.Any())
		{
			if (originalClassHasAttributesApplied)
			{
				// Current class has enum attributes applied and child class also has. This will produce error on child class attributes.
				RecursionResult current = new(original.Attributes, original.Type, true, true, false);
				return current;
			}

			if (!originalClassHasAttributesApplied && previousClassesHasAttributes)
			{
				// Current class has enum attributes applied and one of previous class also has while original class has no enum attributes applied.
				// This sets original class validations to not happen until duplicate enum attribute conflicts are resolved.
				RecursionResult current = new(original.Attributes, original.Type, false, true, false);
				return current;
			}

			if (!previousClassesHasAttributes && !originalClassMightHaveAttributes)
			{
				// Current class has smart enum attributes applied but keep looking on parent class for duplicates and
				// check if original class should proceed with other validations.
				RecursionResult current = new(enumAttributes, original.Type, false, true, true);
				if (type.BaseType is null || type.BaseType.SpecialType == SpecialType.System_Object)
				{
					return current;
				}

				return this.RecurseEnumHierarchy(type.BaseType, attributeDefinition, originalClassMightHaveAttributes, current, originalClassHasAttributesApplied: false, previousClassesHasAttributes: true);
			}

			if (type.BaseType is null || type.BaseType.SpecialType == SpecialType.System_Object)
			{
				// Class has smart enum attributes and has no parent class.
				RecursionResult current = new(original.Attributes, original.Type, false, true, true);
				return current;
			}

			// Current class has smart enum attributes applied but keep looking on parent class if error should be shown.
			original = new(
				original.Attributes,
				original.Type,
				original.ShouldErrorBeShownOnAttributes,
				true,
				original.ShouldOriginalClassBeValidated);

			return this.RecurseEnumHierarchy(type.BaseType, attributeDefinition, originalClassMightHaveAttributes, original, originalClassHasAttributesApplied: true, previousClassesHasAttributes: false);
		}

		if (type.BaseType is null || type.BaseType.SpecialType == SpecialType.System_Object)
		{
			// Either one of previous classes has smart enum attributes and has no parent class or class is not in smart enum hierarchy.
			return original;
		}

		if (!originalClassHasAttributesApplied)
		{
			originalClassMightHaveAttributes = false;
		}

		if (original.EnumBaseClassFound)
		{
			return this.RecurseEnumHierarchy(type.BaseType, attributeDefinition, originalClassMightHaveAttributes, original, originalClassHasAttributesApplied, previousClassesHasAttributes);
		}

		// Current class is not topmost class so keep looking on parent class.
		RecursionResult recursionResult = 
			this.RecurseEnumHierarchy(type.BaseType, attributeDefinition, originalClassMightHaveAttributes, original, originalClassHasAttributesApplied, previousClassesHasAttributes);

		if (recursionResult.EnumBaseClassFound && recursionResult.ShouldOriginalClassBeValidated && !originalClassMightHaveAttributes)
		{
			recursionResult.EnumHierarchyLevel++;
		}

		return recursionResult;
	}

	private class RecursionResult
	{
		public ImmutableList<AttributeData> Attributes { get; }
		public INamedTypeSymbol Type { get; }
		public bool ShouldErrorBeShownOnAttributes { get; }
		public int EnumHierarchyLevel { get; set; } = -1;
		public bool EnumBaseClassFound { get; }
		public bool ShouldOriginalClassBeValidated { get; }

		public RecursionResult(
			ImmutableList<AttributeData> attributes,
			INamedTypeSymbol type,
			bool shouldErrorBeShownOnAttributes,
			bool enumBaseClassFound,
			bool shouldOriginalClassBeValidated)
		{
			this.Attributes = attributes;
			this.Type = type;
			this.ShouldErrorBeShownOnAttributes = shouldErrorBeShownOnAttributes;
			this.EnumBaseClassFound = enumBaseClassFound;
			this.ShouldOriginalClassBeValidated = shouldOriginalClassBeValidated;
		}
	}
}
