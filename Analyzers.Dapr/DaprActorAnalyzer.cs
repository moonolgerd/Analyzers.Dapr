using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Dapr.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DaprActorAnalyzer : DiagnosticAnalyzer
{
    // Actor validation rules
    public static readonly DiagnosticDescriptor ActorInterfaceMissingIActor = new(
        "DAPR001",
        "Actor interface should inherit from IActor",
        "Interface '{0}' used by Actor class should inherit from IActor",
        "Interface",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Interfaces implemented by Actor classes should inherit from IActor interface.");

    public static readonly DiagnosticDescriptor EnumMissingEnumMemberAttribute = new(
        "DAPR002",
        "Enum members in Actor types should use EnumMember attribute",
        "Enum member '{0}' in enum '{1}' should be decorated with [EnumMember] attribute for proper serialization",
        "Serialization",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Enum members used in Actor types should use [EnumMember] attribute for consistent serialization.");

    public static readonly DiagnosticDescriptor WeaklyTypedActorJsonPropertyRecommendation = new(
        "DAPR003",
        "Consider using JsonPropertyName for property name consistency",
        "Property '{0}' in Actor class '{1}' should consider using [JsonPropertyName] attribute for consistent naming",
        "Serialization",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Properties in Actor classes used with weakly-typed clients should consider [JsonPropertyName] attribute for consistent property naming.");

    public static readonly DiagnosticDescriptor ComplexTypeInActorNeedsAttributes = new(
        "DAPR004",
        "Complex types used in Actor methods need serialization attributes",
        "Type '{0}' used in Actor method should be decorated with [DataContract] and have [DataMember] on serializable properties",
        "Serialization",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Complex types used as parameters or return types in Actor methods should have proper serialization attributes.");

    public static readonly DiagnosticDescriptor ActorMethodParameterNeedsValidation = new(
        "DAPR005",
        "Actor method parameter needs proper serialization attributes",
        "Parameter '{0}' of type '{1}' in method '{2}' should have proper serialization attributes",
        "Serialization",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Parameters in Actor methods should use types with proper serialization attributes for reliable data transfer.");

    public static readonly DiagnosticDescriptor ActorMethodReturnTypeNeedsValidation = new(
        "DAPR006",
        "Actor method return type needs proper serialization attributes",
        "Return type '{0}' in method '{1}' should have proper serialization attributes",
        "Serialization",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Return types in Actor methods should have proper serialization attributes for reliable data transfer.");

    public static readonly DiagnosticDescriptor CollectionTypeInActorNeedsElementValidation = new(
        "DAPR007",
        "Collection types in Actor methods need element type validation",
        "Collection type '{0}' in Actor method contains elements of type '{1}' which needs proper serialization attributes",
        "Serialization",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Collection types used in Actor methods should contain elements with proper serialization attributes.");

    public static readonly DiagnosticDescriptor RecordTypeNeedsDataContractAttributes = new(
        "DAPR008",
        "Record types should use DataContract and DataMember attributes for Actor serialization",
        "Record '{0}' should be decorated with [DataContract] and have [DataMember] attributes on properties for proper Actor serialization",
        "Serialization",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Record types used in Actor methods should have [DataContract] attribute and [DataMember] attributes on all properties for reliable serialization.");

    public static readonly DiagnosticDescriptor ActorClassMissingInterface = new(
        "DAPR009",
        "Actor class implementation should implement an interface that inherits from IActor",
        "Actor class '{0}' should implement an interface that inherits from IActor",
        "Serialization",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Actor class implementations should implement an interface that inherits from IActor for proper Actor pattern implementation.");

    public static readonly DiagnosticDescriptor TypeMissingParameterlessConstructorOrDataContract = new(
        "DAPR010",
        "All types must either expose a public parameterless constructor or be decorated with the DataContractAttribute attribute",
        "Type '{0}' must either have a public parameterless constructor or be decorated with [DataContract] attribute for proper serialization",
        "Serialization",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All types used in Actor methods must either expose a public parameterless constructor or be decorated with the DataContractAttribute attribute for reliable serialization.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            ActorInterfaceMissingIActor,
            EnumMissingEnumMemberAttribute,
            WeaklyTypedActorJsonPropertyRecommendation,
            ComplexTypeInActorNeedsAttributes,
            ActorMethodParameterNeedsValidation,
            ActorMethodReturnTypeNeedsValidation,
            CollectionTypeInActorNeedsElementValidation,
            RecordTypeNeedsDataContractAttributes,
            ActorClassMissingInterface,
            TypeMissingParameterlessConstructorOrDataContract
        );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeInterfaceDeclaration, SyntaxKind.InterfaceDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeEnumDeclaration, SyntaxKind.EnumDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);

        if (classSymbol == null) return;

        // Check if class inherits from Actor
        if (!InheritsFromActor(classSymbol)) return;

        var className = classSymbol.Name;

        // Check implemented interfaces
        CheckActorInterfaces(context, classDeclaration, classSymbol);

        // DAPR009: Check if Actor class implements at least one interface that inherits from IActor
        CheckActorClassImplementsIActorInterface(context, classDeclaration, classSymbol);

        // Check method parameters and return types
        CheckActorMethodTypes(context, classSymbol);
    }

    private static void AnalyzeInterfaceDeclaration(SyntaxNodeAnalysisContext context)
    {
        var interfaceDeclaration = (InterfaceDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclaration);

        if (interfaceSymbol == null) return;

        // Check if this interface is implemented by any Actor classes
        // This is a simplified check - in a real analyzer, you might want to do a more comprehensive analysis
        if (interfaceDeclaration.Identifier.ValueText.EndsWith("Actor") && !InheritsFromIActor(interfaceSymbol))
        {
            var diagnostic = Diagnostic.Create(
                ActorInterfaceMissingIActor,
                interfaceDeclaration.Identifier.GetLocation(),
                interfaceSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeEnumDeclaration(SyntaxNodeAnalysisContext context)
    {
        var enumDeclaration = (EnumDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var enumSymbol = semanticModel.GetDeclaredSymbol(enumDeclaration);

        if (enumSymbol == null) return;

        // Check if enum is used in Actor context (simplified check)
        foreach (var member in enumSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (!HasAttribute(member, "EnumMemberAttribute", "EnumMember"))
            {
                var memberDeclaration = enumDeclaration.Members
                    .FirstOrDefault(m => m.Identifier.ValueText == member.Name);

                if (memberDeclaration != null)
                {
                    var diagnostic = Diagnostic.Create(
                        EnumMissingEnumMemberAttribute,
                        memberDeclaration.Identifier.GetLocation(),
                        member.Name,
                        enumSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static bool InheritsFromActor(INamedTypeSymbol classSymbol)
    {
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "Actor" && baseType.ContainingNamespace?.ToDisplayString() == "Dapr.Actors.Runtime")
            {
                return true;
            }
            baseType = baseType.BaseType;
        }
        return false;
    }

    private static bool InheritsFromIActor(INamedTypeSymbol interfaceSymbol)
    {
        return interfaceSymbol.AllInterfaces.Any(i =>
            i.Name == "IActor" && i.ContainingNamespace?.ToDisplayString() == "Dapr.Actors");
    }

    private static bool HasAttribute(ISymbol symbol, params string[] attributeNames)
    {
        return symbol.GetAttributes().Any(attr =>
            attributeNames.Contains(attr.AttributeClass?.Name) ||
            attributeNames.Contains(attr.AttributeClass?.MetadataName));
    }

    private static void CheckActorInterfaces(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration, INamedTypeSymbol classSymbol)
    {
        foreach (var interfaceType in classSymbol.Interfaces)
        {
            if (interfaceType.Name.EndsWith("Actor") && !InheritsFromIActor(interfaceType))
            {
                var diagnostic = Diagnostic.Create(
                    ActorInterfaceMissingIActor,
                    classDeclaration.Identifier.GetLocation(),
                    interfaceType.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static void CheckActorClassImplementsIActorInterface(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration, INamedTypeSymbol classSymbol)
    {
        // Check if the Actor class implements at least one interface that inherits from IActor
        bool implementsIActorInterface = classSymbol.Interfaces.Any(interfaceType => InheritsFromIActor(interfaceType));

        if (!implementsIActorInterface)
        {
            var diagnostic = Diagnostic.Create(
                ActorClassMissingInterface,
                classDeclaration.Identifier.GetLocation(),
                classSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void CheckActorMethodTypes(SyntaxNodeAnalysisContext context, INamedTypeSymbol classSymbol)
    {
        // Only check methods that are part of an IActor interface contract
        var iActorInterfaces = classSymbol.AllInterfaces.Where(InheritsFromIActor).ToList();

        foreach (var interfaceMethod in iActorInterfaces.SelectMany(i => i.GetMembers().OfType<IMethodSymbol>()))
        {
            var implementation = classSymbol.FindImplementationForInterfaceMember(interfaceMethod) as IMethodSymbol;
            if (implementation == null) continue;

            // Check return type
            if (!implementation.ReturnsVoid)
            {
                CheckMethodReturnType(context, implementation);
            }

            // Check parameter types
            foreach (var parameter in implementation.Parameters)
            {
                CheckMethodParameter(context, implementation, parameter);
            }
        }
    }

    private static void CheckMethodReturnType(SyntaxNodeAnalysisContext context, IMethodSymbol method)
    {
        var returnType = method.ReturnType;
        var location = method.Locations.FirstOrDefault();

        if (location == null) return;

        // Handle Task<T> return types
        if (returnType is INamedTypeSymbol namedReturnType &&
            namedReturnType.IsGenericType &&
            namedReturnType.Name == "Task" &&
            namedReturnType.TypeArguments.Length == 1)
        {
            returnType = namedReturnType.TypeArguments[0];
        }

        ValidateTypeForSerialization(context, returnType, location, method.Name, isParameter: false);
    }

    private static void CheckMethodParameter(SyntaxNodeAnalysisContext context, IMethodSymbol method, IParameterSymbol parameter)
    {
        var location = parameter.Locations.FirstOrDefault();
        if (location == null) return;

        ValidateTypeForSerialization(context, parameter.Type, location, method.Name, isParameter: true, parameter.Name);
    }

    private static void ValidateTypeForSerialization(SyntaxNodeAnalysisContext context, ITypeSymbol type, Location location, string methodName, bool isParameter, string? parameterName = null)
    {
        // Skip primitive types and known serializable types
        if (IsPrimitiveOrKnownType(type)) return;

        // Handle collection types
        if (IsCollectionType(type))
        {
            CheckCollectionElementType(context, type, location, methodName, isParameter, parameterName);
            return;
        }

        // Check if it's a complex type that needs attributes
        if (type is INamedTypeSymbol namedType &&
            (namedType.TypeKind == TypeKind.Class || namedType.TypeKind == TypeKind.Struct))
        {
            // DAPR008: Record types used in Actor methods must have DataContract/DataMember attributes
            if (namedType.IsRecord)
            {
                CheckRecordSymbolForDataContractAttributes(context, namedType, location);
                return;
            }

            // DAPR010: Check if type has parameterless constructor or DataContract attribute
            if (!HasParameterlessConstructorOrDataContract(namedType))
            {
                var diagnostic = Diagnostic.Create(
                    TypeMissingParameterlessConstructorOrDataContract,
                    location,
                    namedType.Name);
                context.ReportDiagnostic(diagnostic);
            }

            if (!HasProperSerializationAttributes(namedType))
            {
                if (isParameter)
                {
                    var diagnostic = Diagnostic.Create(
                        ActorMethodParameterNeedsValidation,
                        location,
                        parameterName,
                        type.Name,
                        methodName);
                    context.ReportDiagnostic(diagnostic);
                }
                else
                {
                    var diagnostic = Diagnostic.Create(
                        ActorMethodReturnTypeNeedsValidation,
                        location,
                        type.Name,
                        methodName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static bool IsCollectionType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType) return false;

        // Check for common collection interfaces and types
        var collectionTypeNames = new[]
        {
            "IEnumerable", "ICollection", "IList", "IDictionary",
            "List", "Array", "Dictionary", "HashSet", "Queue", "Stack"
        };

        return collectionTypeNames.Any(name =>
            namedType.Name == name ||
            namedType.AllInterfaces.Any(i => i.Name == name)) ||
            type.TypeKind == TypeKind.Array;
    }

    private static void CheckCollectionElementType(SyntaxNodeAnalysisContext context, ITypeSymbol collectionType, Location location, string methodName, bool isParameter, string? parameterName)
    {
        ITypeSymbol? elementType = null;

        if (collectionType.TypeKind == TypeKind.Array && collectionType is IArrayTypeSymbol arrayType)
        {
            elementType = arrayType.ElementType;
        }
        else if (collectionType is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            // For generic collections like List<T>, Dictionary<K,V>, etc.
            elementType = namedType.TypeArguments.FirstOrDefault();
        }

        if (elementType != null && !IsPrimitiveOrKnownType(elementType))
        {
            if (elementType is INamedTypeSymbol namedElementType &&
                (namedElementType.TypeKind == TypeKind.Class || namedElementType.TypeKind == TypeKind.Struct) &&
                !HasProperSerializationAttributes(namedElementType))
            {
                var diagnostic = Diagnostic.Create(
                    CollectionTypeInActorNeedsElementValidation,
                    location,
                    collectionType.Name,
                    elementType.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool HasProperSerializationAttributes(INamedTypeSymbol type)
    {
        // Check for DataContract or Serializable attributes
        return HasAttribute(type, "DataContractAttribute", "DataContract") ||
               HasAttribute(type, "SerializableAttribute", "Serializable") ||
               HasAttribute(type, "JsonObjectAttribute", "JsonObject") ||
               IsPrimitiveOrKnownType(type);
    }

    private static bool IsPrimitiveOrKnownType(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Enum) return true;

        var typeName = type.ToDisplayString();
        var knownTypes = new[]
        {
            "byte", "sbyte", "short", "int", "long", "ushort", "uint", "ulong",
            "float", "double", "bool", "char", "decimal", "object", "string",
            "System.DateTime", "System.TimeSpan", "System.Guid", "System.Uri",
            "System.Xml.XmlQualifiedName", "System.Threading.Tasks.Task", "void"
        };

        return knownTypes.Contains(typeName) ||
               typeName.StartsWith("System.Threading.Tasks.Task<") ||
               typeName == "System.Void";
    }

    private static void CheckRecordSymbolForDataContractAttributes(SyntaxNodeAnalysisContext context, INamedTypeSymbol recordType, Location usageLocation)
    {
        // DAPR008: record must carry [DataContract]
        if (!HasAttribute(recordType, "DataContractAttribute", "DataContract"))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                RecordTypeNeedsDataContractAttributes,
                usageLocation,
                recordType.Name));
            return;
        }

        // Every public property must carry [DataMember]
        foreach (var property in recordType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && !HasDataMemberAttribute(p)))
        {
            var propLocation = property.Locations.FirstOrDefault() ?? usageLocation;
            context.ReportDiagnostic(Diagnostic.Create(
                RecordTypeNeedsDataContractAttributes,
                propLocation,
                recordType.Name));
        }
    }

    private static bool HasDataMemberAttribute(IPropertySymbol property)
    {
        return HasAttribute(property, "DataMemberAttribute", "DataMember");
    }

    private static bool HasParameterlessConstructorOrDataContract(INamedTypeSymbol type)
    {
        // Check if type has DataContract attribute
        if (HasAttribute(type, "DataContractAttribute", "DataContract"))
        {
            return true;
        }

        // Check if type has a public parameterless constructor
        var constructors = type.Constructors;

        // If no constructors are explicitly defined, there's an implicit parameterless constructor for classes
        if (!constructors.Any() && type.TypeKind == TypeKind.Class)
        {
            return true;
        }

        // Check for explicitly defined public parameterless constructor
        return constructors.Any(c =>
            c.DeclaredAccessibility == Accessibility.Public &&
            c.Parameters.Length == 0);
    }
}
