using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AutoConstructor.Generator;

public class CodeGenerator
{
    private MemberDeclarationSyntax? _current;
    private bool _addNullableAnnotation;
    private string? _constructorDocumentationComment;

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

    public CodeGenerator AddNamespace(string identifier)
    {
        if (_current is not null)
        {
            throw new InvalidOperationException($"Method {nameof(AddNamespace)} must be called first.");
        }

        _current = GetNamespace(identifier, _addNullableAnnotation);
        return this;
    }

    public CodeGenerator AddClass(string identifier, bool isStatic = false)
    {
        ClassDeclarationSyntax classSyntax = GetClass(identifier, _current is null, _addNullableAnnotation, isStatic);

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

    public CodeGenerator AddConstructor(IEnumerable<FieldInfo> parameters)
    {
        if (_current is ClassDeclarationSyntax classDeclarationSyntax)
        {
            ClassDeclarationSyntax lastClassSyntax = classDeclarationSyntax.DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().LastOrDefault();
            _current = classDeclarationSyntax.ReplaceNode(lastClassSyntax, lastClassSyntax.AddMembers(GetConstructor(lastClassSyntax.Identifier, parameters, _constructorDocumentationComment)));
        }
        else if (_current is BaseNamespaceDeclarationSyntax namespaceDeclarationSyntax && namespaceDeclarationSyntax.Members.First() is ClassDeclarationSyntax)
        {
            ClassDeclarationSyntax lastClassSyntax = namespaceDeclarationSyntax.DescendantNodes().OfType<ClassDeclarationSyntax>().LastOrDefault();
            if (lastClassSyntax is null)
            {
                throw new InvalidOperationException("No class was added to the generator.");
            }

            _current = namespaceDeclarationSyntax.ReplaceNode(lastClassSyntax, lastClassSyntax.AddMembers(GetConstructor(lastClassSyntax.Identifier, parameters, _constructorDocumentationComment)));
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

        using var workspace = new AdhocWorkspace();

        OptionSet options = workspace.Options;

        CompilationUnitSyntax compilationUnit = CompilationUnit().AddMembers(_current);
        SyntaxNode formattedNode = Formatter.Format(compilationUnit, workspace, options);
        SyntaxTree tree = SyntaxTree(formattedNode.WithTrailingTrivia(CarriageReturnLineFeed));

        return tree.ToString();
    }

    private static SyntaxTriviaList GetHeaderTrivia(bool addNullableAnnotation)
    {
        var trivias = new List<SyntaxTrivia> { Comment("// <auto-generated />"), CarriageReturnLineFeed };
        if (addNullableAnnotation)
        {
            trivias.Add(Trivia(
                NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true)
                .WithNullableKeyword(Token(TriviaList(), SyntaxKind.NullableKeyword, TriviaList(Space)))
                .WithEndOfDirectiveToken(Token(TriviaList(), SyntaxKind.EndOfDirectiveToken, TriviaList(CarriageReturnLineFeed)))
            ));
        }

        return TriviaList(trivias);
    }

    private static BaseNamespaceDeclarationSyntax GetNamespace(string identifier, bool addNullableAnnotation)
    {
        return NamespaceDeclaration(IdentifierName(identifier))
            .WithNamespaceKeyword(Token(GetHeaderTrivia(addNullableAnnotation), SyntaxKind.NamespaceKeyword, TriviaList()));
    }

    private static ClassDeclarationSyntax GetClass(string identifier, bool addHeaderTrivia, bool addNullableAnnotation, bool isStatic)
    {
        SyntaxToken firstModifier = Token(isStatic ? SyntaxKind.StaticKeyword : SyntaxKind.PartialKeyword);
        if (addHeaderTrivia)
        {
            firstModifier = Token(GetHeaderTrivia(addNullableAnnotation), SyntaxKind.PartialKeyword, TriviaList());
        }

        ClassDeclarationSyntax declaration = ClassDeclaration(identifier).AddModifiers(firstModifier);
        if (isStatic)
        {
            declaration = declaration.AddModifiers(Token(SyntaxKind.PartialKeyword));
        }

        return declaration;
    }

    private static ConstructorDeclarationSyntax GetConstructor(SyntaxToken identifier, IEnumerable<FieldInfo> parameters, string? constructorDocumentationComment)
    {
        var constructorParameters = parameters
            .GroupBy(x => x.ParameterName)
            .Select(x => x.Any(c => c.Type is not null) ? x.First(c => c.Type is not null) : x.First())
            .ToList();

        SyntaxToken modifiers = Token(SyntaxKind.PublicKeyword);
        if (!string.IsNullOrWhiteSpace(constructorDocumentationComment))
        {
            modifiers = Token(TriviaList(Trivia(GetDocumentation(constructorDocumentationComment, parameters))), SyntaxKind.PublicKeyword, TriviaList());
        }

        return ConstructorDeclaration(identifier)
            .AddModifiers(modifiers)
            .AddParameterListParameters(constructorParameters.Select(GetParameter).ToArray())
            .AddBodyStatements(parameters.Select(p => GetParameterAssignement(p)).ToArray());
    }

    private static DocumentationCommentTriviaSyntax GetDocumentation(string? constructorDocumentationComment, IEnumerable<FieldInfo> parameters)
    {
        var nodes = new List<XmlNodeSyntax>
        {
            XmlText().WithTextTokens(TokenList(XmlTextLiteral(TriviaList(DocumentationCommentExterior("///")), " ", " ", TriviaList()))),
            XmlExampleElement(SingletonList<XmlNodeSyntax>(  XmlText() .WithTextTokens(
                TokenList(
                    new[]{
                        XmlTextNewLine(TriviaList(), "\r\n", "\r\n", TriviaList()),
                        XmlTextLiteral(TriviaList(DocumentationCommentExterior("///")), $" {constructorDocumentationComment}", $" {constructorDocumentationComment}", TriviaList()),
                        XmlTextNewLine(TriviaList(), "\r\n", "\r\n", TriviaList()),
                        XmlTextLiteral(TriviaList(DocumentationCommentExterior("///")), " ", " ", TriviaList())})
            )))
            .WithStartTag(XmlElementStartTag(XmlName(Identifier("summary"))))
            .WithEndTag(XmlElementEndTag(XmlName(Identifier("summary"))))
        };

        foreach (FieldInfo parameter in parameters)
        {
            nodes.Add(GetCommentLineStart());
            nodes.Add(GetParameterDocumentation(parameter));
        }

        nodes.Add(XmlText().WithTextTokens(TokenList(XmlTextNewLine(TriviaList(), "\r\n", "\r\n", TriviaList()))));

        return DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia, List(nodes));
    }

    private static XmlTextSyntax GetCommentLineStart()
    {
        return XmlText()
            .WithTextTokens(TokenList(new[] {
                XmlTextNewLine(TriviaList(), "\r\n", "\r\n", TriviaList()),
                XmlTextLiteral(TriviaList(DocumentationCommentExterior("///")), " ", " ", TriviaList())
            }));
    }

    private static XmlElementSyntax GetParameterDocumentation(FieldInfo parameter)
    {
        return XmlExampleElement(SingletonList<XmlNodeSyntax>(XmlText().WithTextTokens(TokenList(XmlTextLiteral(TriviaList(), parameter.Comment ?? parameter.ParameterName, parameter.Comment ?? parameter.ParameterName, TriviaList())))))
                .WithStartTag(
                    XmlElementStartTag(XmlName(Identifier(TriviaList(), SyntaxKind.ParamKeyword, "param", "param", TriviaList())))
                    .WithAttributes(SingletonList<XmlAttributeSyntax>(
                        XmlNameAttribute(
                            XmlName(Identifier(TriviaList(Space), "name", TriviaList())),
                            Token(SyntaxKind.DoubleQuoteToken),
                            IdentifierName(parameter.ParameterName),
                            Token(SyntaxKind.DoubleQuoteToken)
                            )
                        )
                    ))
                .WithEndTag(XmlElementEndTag(XmlName(Identifier(TriviaList(), SyntaxKind.ParamKeyword, "param", "param", TriviaList()))));
    }

    private static ParameterSyntax GetParameter(FieldInfo parameter)
    {
        // TODO: Use QualifiedName for XX.XX.XX types and PredefinedType for types with SpecialType.
        return Parameter(Identifier(parameter.ParameterName))
            .WithType(IdentifierName(parameter.Type ?? parameter.FallbackType));
    }

    private static ExpressionStatementSyntax GetParameterAssignement(FieldInfo parameter, bool emitArgumentNullException = false)
    {
        ExpressionSyntax left = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(parameter.FieldName));
        ExpressionSyntax right = IdentifierName(parameter.Initializer);
        if (emitArgumentNullException)
        {
            right = BinaryExpression(
                        SyntaxKind.CoalesceExpression,
                        right,
                        ThrowExpression(
                            ObjectCreationExpression(QualifiedName(IdentifierName("System"), IdentifierName("ArgumentNullException")))
                            .AddArgumentListArguments(Argument(NameOf("name2")))));
        }

        return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, right));
    }

    private static InvocationExpressionSyntax NameOf(string identifier)
    {
        SyntaxToken nameofIdentifier = Identifier(TriviaList(), SyntaxKind.NameOfKeyword, "nameof", "nameof", TriviaList());
        return InvocationExpression(IdentifierName(nameofIdentifier))
            .AddArgumentListArguments(Argument(IdentifierName(identifier)));
    }
}
