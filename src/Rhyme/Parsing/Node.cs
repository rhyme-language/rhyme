using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;
using Rhyme.TypeSystem;

namespace Rhyme.Parsing
{
    /*TODO: REMOVE ASAP*/
    public record Declaration(RhymeType Type, string Identifier)
    {
        public static Declaration CreateFunction(string identifier, RhymeType returnType, params Declaration[] parameters)
        {
            return null;
        }
    };

    public enum DeclarationAccessModifier
    {
        None,
        Extern,
        Global,
    }

    /// <summary>
    /// Represents a node in an abstract syntax tree
    /// </summary>
    public interface Node
    {
        /// <summary>       
        /// Vistitor for traversing the tree.
        /// </summary>
        /// <typeparam name="T">Return type of a visit operation.</typeparam>
        public interface IVisitor<T>
        {
            T Visit(Literal literalExpr) => default;
            T Visit(Binary binaryExpr) => default;
            T Visit(Unary unaryExpr) => default;
            T Visit(Block blockExpr) => default;
            T Visit(FunctionCall callExpr) => default;
            T Visit(If ifStmt) => default;
            T Visit(While whileStmt) => default;
            T Visit(Grouping grouping) => default;
            T Visit(Binding binding) => default;
            T Visit(Assignment assignment) => default;
            T Visit(Return returnStmt) => default;
            T Visit(Get member) => default;
            T Visit(ParamDecl param) => default;
            T Visit(Directive directive) => default;
            T Visit(TopLevelDeclaration topLevelDeclaration) => default;
            T Visit(BindingDeclaration bindingDecl) => default;
            T Visit(FunctionDeclaration funcDecl) => default;
            T Visit(IdentifierType identiferType) => default;
            T Visit(FuncType funcType) => default;
            T Visit(ImportStmt importStmt) => default;
            T Visit(ModuleDecl moduleDecl) => default;
            T Visit(CompilationUnit compilationUnit) {
                Visit(compilationUnit.ModuleDeclaration);

                foreach (var imp in compilationUnit.ImportStatements)
                    Visit(imp);

                foreach (var tlDecl in compilationUnit.TopLevelDeclarations)
                    Visit(tlDecl);

                return default;
            }      
        }
        public T Accept<T>(IVisitor<T> visitor);
        public Position Position { get; }

        #region Expressions
        public record Literal(Token ValueToken) : Node
        {
            public Position Position => ValueToken.Position;

            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record Binary(Node Left, Token Op, Node Right) : Node
        {
            public Position Position => new(Left.Position.Line, Left.Position.Start, Right.Position.End);
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

        }

        public record Unary(Token Op, Node Operand) : Node
        {
            public Position Position => new(Op.Position.Line, Operand.Position.Start, Operand.Position.End);
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

        }

        public record FunctionCall(Node Callee, params Node[] Arguments) : Node
        {
            public Position Position => new(Callee.Position.Line, Callee.Position.Start, Arguments.Length > 0 ? Arguments[^1].Position.End : Callee.Position.End);
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }
        public record Binding(Token Identifier) : Node
        {
            public Position Position => Identifier.Position;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }
        public record Block(IReadOnlyCollection<Node> ExpressionsStatements) : Node
        {
            public Position Position => Position.NonePosition;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }
        public record Assignment(Node Assignee, Node Expression) : Node
        {
            public Position Position => new(Assignee.Position.Line, Assignee.Position.Start, Expression.Position.End);
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

        }

        public record If(Node condition, Node thenBody, Node elseBody) : Node
        {
            public Position Position => Position.NonePosition;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record While(Node Condition, Node LoopBody) : Node
        {
            public Position Position => Position.NonePosition;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record Get(Node Accessed, Token Member) : Node
        {
            public Position Position => Position.NonePosition;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record Grouping(Node Expression) : Node
        {
            public Position Position => Expression.Position;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record Directive(Token Identifier, params Node[] Arguments) :  Node
        {
            public Position Position => Identifier.Position;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }
        public record Return(Node RetrunExpression) : Node
        {
            public Position Position => RetrunExpression.Position;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

        }

        #endregion

        #region Declarations
        public record TopLevelDeclaration(Node DeclarationNode, DeclarationAccessModifier Modifier) : Node
        {
            public Position Position => DeclarationNode.Position;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }
        public record BindingDeclaration(Type Type, params Declarator[] Declarators) : Node
        {
            public Position Position => new Position(Declarators[0].Identifier.Position.Line, Declarators[0].Identifier.Position.Start, Declarators[0].Identifier.Position.End);
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

        }
        public record Declarator(Token Identifier, Node Initializer) : Node
        {
            public Position Position => new Position(Identifier.Position.Line, Identifier.Position.Start, Initializer.Position.End);

            public T Accept<T>(IVisitor<T> visitor)
            {
                throw new NotImplementedException();
            }
        }

        public record ImportStmt(Token Name) : Node
        {
            public Position Position => Name.Position;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }


        public abstract record Type : Node
        {
            public Position Position => Position.NonePosition;
            public T Accept<T>(IVisitor<T> visitor) => default;
        }

        public record FuncType(Type ReturnType, params ParamDecl[] Parameters) : Type
        {
            public new Position Position => Position.NonePosition;
            public new T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record IdentifierType(Token Identifier) : Type
        {
            public new Position Position => Identifier.Position;
            public new T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }

        public record ParamDecl(Type Type, Token Identifier) : Node
        {
            public Position Position => Position.FromTo(Type.Position, Identifier.Position);

            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        };

        public record ModuleDecl(Token Identifier) : Node
        {
            public Position Position => Position.NonePosition;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }
        
        public record FunctionDeclaration(Type ReturnType, Token Identifier, ParamDecl[] Parameters, Block Block) : Node
        {
            public Position Position => Position.NonePosition;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }
        #endregion 

        public record CompilationUnit(ModuleDecl ModuleDeclaration, IReadOnlyList<ImportStmt> ImportStatements, IReadOnlyList<TopLevelDeclaration> TopLevelDeclarations) : Node
        {
            public Position Position => Position.NonePosition;
            public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
        }
    }
}
