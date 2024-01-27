using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;

namespace Rhyme.Parser
{
    internal record Declaration(Type type, string identifier);
    
    /// <summary>
    /// Represents a node in an abstract syntax tree
    /// </summary>
    internal interface Node
    {
        /// <summary>
        /// Vistitor for traversing the tree.
        /// </summary>
        /// <typeparam name="T">Return type of a visit operation.</typeparam>
        public interface Visitor<T>
        {
            T Visit(Literal literalExpr);
            T Visit(Binary binaryExpr);
            T Visit(Unary unaryExpr);
            T Visit(Block blockExpr);
            T Visit(BindingDeclaration bindingDecl);
            T Visit(IfExpression ifStmt);
            T Visit(Assignment assignment);
            T Visit(Binding binding);
            T Visit(Grouping grouping);
            T Visit(CompilationUnit compilationUnit);

        }
        public T Accept<T>(Visitor<T> visitor);


        public record CompilationUnit(IReadOnlyCollection<Node> Units) : Node
        {
            public T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public record Literal(Token Value) : Node
        {
            public T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }

        public record Binary(Node left, Token op, Node right) : Node
        {
            public T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);

        }

        public record Unary(Token Op, Node Operand) : Node
        {
            public T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);

        }

        public record Block(IReadOnlyCollection<Node> ExpressionsStatements) : Node
        {
            public T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }

        public record BindingDeclaration(Declaration declaration, Node expression) : Node
        {
            public T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);

        }

        public record Assignment(Token binding, Node expression) : Node
        {
            public T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);

        }

        public record Binding(Token Identifier) : Node
        {
            public T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }

        public record Grouping(Node Expression) : Node
        {
            public T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }

        public record If(Node condition, Node thenBody, Node elseBody) : Node
        {
            public T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
    }
}
