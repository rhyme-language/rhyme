using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;

namespace Rhyme.Parsing
{
    internal record Declaration(RhymeType Type, string Identifier);

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
            T Visit(While whileStmt);
            T Visit(Get member);
            T Visit(Return returnStmt);
            T Visit(Assignment assignment);
            T Visit(FunctionCall callExpr);
            T Visit(Binding binding);
            T Visit(Grouping grouping);
            T Visit(Import importStmt);
            T Visit(Module moduleDecl);
            T Visit(Directive directive);
            T Visit(CompilationUnit compilationUnit);
           
        }
        public T Accept<T>(IVisitor<T> visitor);
        public Position Position { get; }
        

        public record CompilationUnit(IReadOnlyCollection<Node> Units) : Node
        {
            public Position Position => Position.NonePosition;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record Literal(Token ValueToken) : Node
        {
            public Position Position => ValueToken.Position;

            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record Binary(Node Left, Token Op, Node Right) : Node
        {
            public Position Position => new Position(Left.Position.Line, Left.Position.Start, Right.Position.End);
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

        }

        public record Unary(Token Op, Node Operand) : Node
        {
            public Position Position => new Position(Op.Position.Line, Operand.Position.Start, Operand.Position.End);
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

        }

        public record FunctionCall(Node Callee, params Node[] Args) : Node
        {
            public Position Position => new Position(Callee.Position.Line, Callee.Position.Start, Args.Length > 0 ? Args[^1].Position.End : Callee.Position.End);
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record Block(IReadOnlyCollection<Node> ExpressionsStatements) : Node
        {
            public Position Position => Position.NonePosition;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record BindingDeclaration(Declaration Declaration, Node Expression, bool Export) : Node
        {
            public Position Position => Expression.Position;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

        }

        public record Assignment(Node Assignee, Node Expression) : Node
        {
            public Position Position => new Position(Assignee.Position.Line, Assignee.Position.Start, Expression.Position.End);
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

        }

        public record Binding(Token Identifier) : Node
        {
            public Position Position => Identifier.Position;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record Grouping(Node Expression) : Node
        {
            public Position Position => Expression.Position;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record If(Node condition, Node thenBody, Node elseBody) : Node
        {
            public Position Position => Position.NonePosition;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record While(Node Condition, Node LoopBody) :  Node
        {
            public Position Position => Position.NonePosition;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record Get(Node Accessed, Token Member) : Node
        {
            public Position Position => Position.NonePosition;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record Import(Token Identifier) : Node
        {
            public Position Position => Identifier.Position;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }
        public record Module(Token Identifier) : Node
        {
            public Position Position => Identifier.Position;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }
        public record Directive(Token Identifier, params Node[] Arguments) : Node
        {
            public Position Position => Identifier.Position;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }
        public record Return(Node RetrunExpression) : Node
        {
            public Position Position => RetrunExpression.Position;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

        }
    }
}
