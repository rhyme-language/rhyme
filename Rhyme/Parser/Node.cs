using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;

namespace Rhyme.Parser
{
    internal record Declaration(RhymeType Type, Token Identifier);
    internal record Position(int Line, int Start, int End)
    {
        public static Position FromToken(Token token) => new Position(token.Line, token.Start, token.End);
    };


    /// <summary>
    /// Represents a node in an abstract syntax tree
    /// </summary>
    internal interface Node
    {
        /// <summary>       
        /// Vistitor for traversing the tree.
        /// </summary>
        /// <typeparam name="T">Return type of a visit operation.</typeparam>
        public interface IVisitor<T>
        {
            T Visit(Literal literalExpr);
            T Visit(Binary binaryExpr);
            T Visit(Unary unaryExpr);
            T Visit(Block blockExpr);
            T Visit(BindingDeclaration bindingDecl);
            T Visit(If ifStmt);
            T Visit(Assignment assignment);
            T Visit(FunctionCall callExpr);
            T Visit(Binding binding);
            T Visit(Grouping grouping);
            T Visit(CompilationUnit compilationUnit);

        }
        public T Accept<T>(IVisitor<T> visitor);
        public Position Position { get; }
        

        public record CompilationUnit(IReadOnlyCollection<Node> Units, Position Position) : Node
        {
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record Literal(Token ValueToken, Position Position) : Node
        {
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record Binary(Node Left, Token Op, Node Right, Position Position) : Node
        {
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

        }

        public record Unary(Token Op, Node Operand, Position Position) : Node
        {
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

        }

        public record FunctionCall(Position Position, Node Callee, params Node[] Args) : Node
        {
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record Block(IReadOnlyCollection<Node> ExpressionsStatements, Position Position) : Node
        {
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record BindingDeclaration(Declaration Declaration, Node expression, Position Position) : Node
        {
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

        }

        public record Assignment(Node Assignee, Node Expression, Position Position) : Node
        {
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

        }

        public record Binding(Token Identifier, Position Position) : Node
        {
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record Grouping(Node Expression, Position Position) : Node
        {
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record If(Node condition, Node thenBody, Node elseBody, Position Position) : Node
        {
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }
    }
}
