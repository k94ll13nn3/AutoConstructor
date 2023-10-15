using AutoConstructor.Generator.Core;
using AutoConstructor.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AutoConstructor.Generator;

internal sealed class CodeGenerator
{
    private MemberDeclarationSyntax? _current;
    private bool _addNullableAnnotation;
    private string? _constructorDocumentationComment;

    internal static readonly string GeneratorVersion = typeof(CodeGenerator).Assembly.GetName().Version!.ToString();

    public CodeGenerator AddNullableAnnotation()
    {
        if (_current is not null)
        {
            throw new InvalidOperationException($"Method {nameof(AddNullableAnnotation)} must be called before adding syntax.");
        }

        _addNullableAnnotation = true;
        return this;
    }

    public CodeGenerator AddDocumentation(string? constructorDocumentationComment)
    {
        if (string.IsNullOrWhiteSpace(constructorDocumentationComment))
        {
            throw new InvalidOperationException("Invalid documentation.");
        }

        _constructorDocumentationComment = constructorDocumentationComment;
        return this;
    }

    public CodeGenerator AddNamespace(string namespaceSymbolDisplayString)
    {
        if (_current is not null)
        {
            throw new InvalidOperationException($"Method {nameof(AddNamespace)} must be called first.");
        }

        _current = GetNamespace(namespaceSymbolDisplayString, _addNullableAnnotation);
        return this;
    }

    public CodeGenerator AddClass(NamedTypeSymbolInfo classSymbol)
    {
        string identifier = classSymbol.Name;
        bool isStatic = classSymbol.IsStatic;
        EquatableArray<string> typeParameterList = classSymbol.TypeParameters;

        ClassDeclarationSyntax classSyntax = GetClass(
            identifier,
            _current is null,
            _addNullableAnnotation,
            isStatic,
            typeParameterList);

        if (_current is null)
        {
            _current = classSyntax;
        }
        else if (_current is BaseNamespaceDeclarationSyntax namespaceDeclarationSyntax)
        {
            ClassDeclarationSyntax lastClassSyntax = namespaceDeclarationSyntax.DescendantNodes().OfType<ClassDeclarationSyntax>().LastOrDefault();
            _current = lastClassSyntax is not null
                ? namespaceDeclarationSyntax.ReplaceNode(lastClassSyntax, lastClassSyntax.AddMembers(classSyntax))
                : namespaceDeclarationSyntax.AddMembers(classSyntax);
        }
        else if (_current is ClassDeclarationSyntax classDeclarationSyntax)
        {
            ClassDeclarationSyntax lastClassSyntax = classDeclarationSyntax.DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().LastOrDefault();
            _current = lastClassSyntax is not null
                ? classDeclarationSyntax.ReplaceNode(lastClassSyntax, lastClassSyntax.AddMembers(classSyntax))
                : classDeclarationSyntax.AddMembers(classSyntax);
        }
        else
        {
            throw new InvalidOperationException($"Cannot run {nameof(AddClass)}");
        }

        return this;
    }

    public CodeGenerator AddConstructor(EquatableArray<FieldInfo> parameters, bool symbolHasParameterlessConstructor, bool emitNullChecks)
    {
        if (_current is ClassDeclarationSyntax classDeclarationSyntax)
        {
            ClassDeclarationSyntax lastClassSyntax = classDeclarationSyntax.DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().LastOrDefault();
            _current = classDeclarationSyntax.ReplaceNode(lastClassSyntax, lastClassSyntax.AddMembers(GetConstructor(lastClassSyntax.Identifier, parameters, _constructorDocumentationComment, symbolHasParameterlessConstructor, emitNullChecks)));
        }
        else if (_current is BaseNamespaceDeclarationSyntax namespaceDeclarationSyntax && namespaceDeclarationSyntax.Members.First() is ClassDeclarationSyntax)
        {
            ClassDeclarationSyntax lastClassSyntax = namespaceDeclarationSyntax.DescendantNodes().OfType<ClassDeclarationSyntax>().LastOrDefault();
            if (lastClassSyntax is null)
            {
                throw new InvalidOperationException("No class was added to the generator.");
            }

            _current = namespaceDeclarationSyntax.ReplaceNode(lastClassSyntax, lastClassSyntax.AddMembers(GetConstructor(lastClassSyntax.Identifier, parameters, _constructorDocumentationComment, symbolHasParameterlessConstructor, emitNullChecks)));
        }
        else if (_current is null)
        {
            throw new InvalidOperationException("No class was added to the generator.");
        }
        else
        {
            throw new InvalidOperationException($"Cannot run {nameof(AddConstructor)}");
        }

        return this;
    }

    public override string ToString()
    {
        if (_current is null)
        {
            return string.Empty;
        }

        SyntaxTree? tree = SyntaxTree(CompilationUnit()
            .AddMembers(_current)
            .NormalizeWhitespace()
            .WithTrailingTrivia(CarriageReturnLineFeed));

        // NormalizeWhitespace is not rendering well xml comments.
        return tree.ToString().Replace("name = \"", "name=\"").Replace("///", "/// ");
    }

    private static SyntaxTriviaList GetHeaderTrivia(bool addNullableAnnotation)
    {
        var trivias = new List<SyntaxTrivia> { Comment($@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the {nameof(AutoConstructor)} source generator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------") };
        if (addNullableAnnotation)
        {
            trivias.Add(Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true)));
        }

        return TriviaList(trivias);
    }

    private static BaseNamespaceDeclarationSyntax GetNamespace(string identifier, bool addNullableAnnotation)
    {
        return NamespaceDeclaration(IdentifierName(identifier))
            .WithNamespaceKeyword(Token(GetHeaderTrivia(addNullableAnnotation), SyntaxKind.NamespaceKeyword, TriviaList()));
    }

    private static ClassDeclarationSyntax GetClass(string identifier, bool addHeaderTrivia, bool addNullableAnnotation, bool isStatic, EquatableArray<string> typeParameterList)
    {
        SyntaxToken firstModifier = Token(isStatic ? SyntaxKind.StaticKeyword : SyntaxKind.PartialKeyword);
        if (addHeaderTrivia)
        {
            firstModifier = Token(GetHeaderTrivia(addNullableAnnotation), isStatic ? SyntaxKind.StaticKeyword : SyntaxKind.PartialKeyword, TriviaList());
        }

        ClassDeclarationSyntax declaration = ClassDeclaration(identifier).AddModifiers(firstModifier);
        if (isStatic)
        {
            declaration = declaration.AddModifiers(Token(SyntaxKind.PartialKeyword));
        }

        if (!typeParameterList.IsEmpty)
        {
            declaration = declaration.AddTypeParameterListParameters(typeParameterList.Select(GetTypeParameter).ToArray());
        }

        return declaration;
    }

    private static ConstructorDeclarationSyntax GetConstructor(SyntaxToken identifier, EquatableArray<FieldInfo> parameters, string? constructorDocumentationComment, bool generateThisInitializer, bool emitNullChecks)
    {
        FieldInfo[] constructorParameters = parameters
            .GroupBy(x => x.ParameterName)
            .Select(x => x.Any(c => c.Type is not null) ? x.First(c => c.Type is not null) : x.First())
            .ToArray();

        AttributeListSyntax attribute = GetGeneratedCodeAttributeSyntax();
        if (constructorDocumentationComment is string { Length: > 0 })
        {
            attribute = attribute.WithOpenBracketToken(
                Token(
                    TriviaList(Trivia(GetDocumentation(constructorDocumentationComment, constructorParameters))),
                    SyntaxKind.OpenBracketToken,
                    TriviaList()));
        }

        ConstructorDeclarationSyntax constructor = ConstructorDeclaration(identifier)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddAttributeLists(attribute)
            .AddParameterListParameters(Array.ConvertAll(constructorParameters, GetParameter))
            .AddBodyStatements(Array.ConvertAll(parameters.Where(p => p.FieldType.HasFlag(FieldType.Initialized)).ToArray(), p => GetParameterAssignement(p, emitNullChecks)));

        if (Array.Exists(constructorParameters, p => p.FieldType.HasFlag(FieldType.PassedToBase)))
        {
            return constructor.WithInitializer(ConstructorInitializer(SyntaxKind.BaseConstructorInitializer)
                .AddArgumentListArguments(Array.ConvertAll(constructorParameters.Where(p => p.FieldType.HasFlag(FieldType.PassedToBase)).ToArray(), GetArgument)));
        }
        else if (generateThisInitializer)
        {
            return constructor.WithInitializer(ConstructorInitializer(SyntaxKind.ThisConstructorInitializer));
        }

        return constructor;
    }

    private static DocumentationCommentTriviaSyntax GetDocumentation(string constructorDocumentationComment, FieldInfo[] parameters)
    {
        var nodes = new List<XmlNodeSyntax>
        {
            XmlSummaryElement(
                XmlNewLine("\r\n"),
                XmlText(constructorDocumentationComment),
                XmlNewLine("\r\n"))
        };

        foreach (FieldInfo parameter in parameters)
        {
            nodes.Add(XmlNewLine("\r\n"));
            nodes.Add(XmlParamElement(parameter.ParameterName, XmlText(parameter.Comment ?? parameter.ParameterName)));
        }

        nodes.Add(XmlText().WithTextTokens(TokenList(XmlTextNewLine(TriviaList(), "\r\n", "\r\n", TriviaList()))));

        return DocumentationComment(nodes.ToArray());
    }

    private static ParameterSyntax GetParameter(FieldInfo parameter)
    {
        string parameterType = parameter.Type ?? parameter.FallbackType;

        return Parameter(Identifier(parameter.ParameterName))
            .WithType(ParseTypeName(parameterType));
    }

    private static TypeParameterSyntax GetTypeParameter(string identifierName)
    {
        return TypeParameter(Identifier(identifierName));
    }

    private static ArgumentSyntax GetArgument(FieldInfo parameter)
    {
        return Argument(IdentifierName(parameter.ParameterName));
    }

    private static ExpressionStatementSyntax GetParameterAssignement(FieldInfo parameter, bool emitNullChecks)
    {
        ExpressionSyntax left = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(parameter.FieldName));
        ExpressionSyntax right = IdentifierName(parameter.Initializer);

        if (parameter.EmitArgumentNullException && emitNullChecks)
        {
            right =
                BinaryExpression(
                    SyntaxKind.CoalesceExpression,
                    right,
                    ThrowExpression(
                        ObjectCreationExpression(QualifiedName(IdentifierName("System"), IdentifierName("ArgumentNullException")))
                        .AddArgumentListArguments(Argument(NameOf(parameter.ParameterName)))));
        }

        return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, right));
    }

    private static InvocationExpressionSyntax NameOf(string identifier)
    {
        SyntaxToken nameofIdentifier = Identifier(TriviaList(), SyntaxKind.NameOfKeyword, "nameof", "nameof", TriviaList());
        return InvocationExpression(IdentifierName(nameofIdentifier))
            .AddArgumentListArguments(Argument(IdentifierName(identifier)));
    }

    private static AttributeListSyntax GetGeneratedCodeAttributeSyntax()
    {
        QualifiedNameSyntax attributeName =
            QualifiedName(
                QualifiedName(
                    QualifiedName(
                        AliasQualifiedName(
                            IdentifierName(
                                Token(SyntaxKind.GlobalKeyword)),
                            IdentifierName("System")),
                        IdentifierName("CodeDom")),
                    IdentifierName("Compiler")),
                IdentifierName("GeneratedCodeAttribute"));

        AttributeSyntax attribute = Attribute(attributeName)
            .AddArgumentListArguments(GetAttributeArgumentSyntax(nameof(AutoConstructor)), GetAttributeArgumentSyntax(GeneratorVersion));

        return AttributeList(SingletonSeparatedList(attribute));

        static AttributeArgumentSyntax GetAttributeArgumentSyntax(string value)
        {
            return AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(value)));
        }
    }
}
