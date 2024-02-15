using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;

namespace Rhyme.Parser
{
    /// <summary>
    /// Parser compiler pass: <br/>
    /// - Parses the source code given its <see cref="Token"/>s. <br/>
    /// - Generating the abstract syntax tree (AST). <br/>
    /// - Reports syntactical errors. <br/>
    /// - Annotates tree <see cref="Node"/>s with <see cref="Rhyme.Parser.RhymeType"/>s. <br/>
    /// </summary>
    internal class Parser : ICompilerPass
    {
        LinkedList<Token> _tokens;
        LinkedListNode<Token> _current;

        List<PassError> _errors = new List<PassError>();
        public bool HadError { get; private set; }
        public IReadOnlyCollection<PassError> Errors { get; }

        public Parser(IEnumerable<Token> Tokens)
        {
            _tokens = new LinkedList<Token>(Tokens);
            _current = _tokens.First;
        }

        public Node.CompilationUnit Parse()
        {
            return CompilationUnit();
        }

        #region Helpers
        private void Advance()
        {
            _current = _current.Next;
        }

        private bool AtEnd() => _current == null;
        TokenType CurrentToken { get => _current.Value.Type; }

        private bool Match(params TokenType[] types)
        {
            foreach (TokenType t in types)
            {
                if (!AtEnd() && _current.Value.Type == t)
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private Token Consume(TokenType type, string errorMessage)
        {
            if (!AtEnd())
            {
                if (CurrentToken == type)
                {
                    var token = _current.Value;
                    Advance();
                    return token;
                }
            }
            throw new Exception(errorMessage);
        }

        #endregion

        #region Statements
        private Node.CompilationUnit CompilationUnit()
        {
            var units = new List<Node>();

            do
            {
                if (_current.Value.Type == TokenType.Using)
                    units.Add(UsingStatement());
                else
                    units.Add(Binding());
            } while (!AtEnd());

            return new Node.CompilationUnit(units, null);
        }
        private RhymeType Type()
        {
            RhymeType returnType = RhymeType.NoneType;
            var current = _current;
            
            var token = _current.Value;

            switch (token.Type)
            {
                case TokenType.Identifier:
                case TokenType.Void:
                case TokenType.Var:
                case TokenType.U8:
                case TokenType.U16:
                case TokenType.U32:
                case TokenType.U64:
                case TokenType.F32:
                case TokenType.F64:
                case TokenType.Str:
                    Advance();

                    returnType = RhymeType.FromToken(token);
                    break;
            }

            if (Match(TokenType.LeftParen)) // Function
            {

                var arguments = new List<Declaration>();
                if (_current.Value.Type != TokenType.RightParen)
                {
                    do
                    {
                        var decl = Declaration();

                        if (decl == null)
                        {
                            _current = current;
                            return RhymeType.NoneType;
                        }

                        arguments.Add(decl);
                    } while (Match(TokenType.Comma));
                }

                Consume(TokenType.RightParen, "Expect ')' after arguments.");

                return new RhymeType.Function(returnType, arguments.Select(a => a.Type).ToArray());

            }

            return returnType;
        }

        private Node UsingStatement()
        {
            throw new NotImplementedException();
        }
        private Node Binding()
        {

            Declaration decl = Declaration();

            if (decl == null)
                return null;

            Node expr = null;
            if (Match(TokenType.Equal))
            {
                expr = Expression();
            }
            Consume(TokenType.Semicolon, "Expects a ';' after a binding value");
            return new Node.BindingDeclaration(decl, expr, null);

        }

        private Declaration Declaration()
        {
            var current = _current;
            RhymeType type = Type();
            
            if (type == RhymeType.NoneType)
            {
                _current = current;
                return null;
            }

            Token identifierToken = null;

            if (type is RhymeType.Function)
            {
                if(((RhymeType.Function)type).Parameters.Length == 0)
                {
                    if (Match(TokenType.Identifier))
                    {
                        identifierToken = _current.Previous.Value;
                        return new Declaration(type, identifierToken);
                    }

                    if (Match(TokenType.Semicolon))
                    {
                        // Normal function call, rollback!
                        _current = current; 
                        return null;
                    }
                }
            }

            if (Match(TokenType.RightParen)) 
            {
                // Normal function call, rollback!
                _current = current;
                return null;
            }

            identifierToken = Consume(TokenType.Identifier, "Expects a binding name.");
            return new Declaration(type, identifierToken);
        }

        private Node Statement()
        {

            var binding = Binding();

            if (binding != null)
                return binding;
            
            Node node = Assignment();

            Consume(TokenType.Semicolon, "';' Expected");
            return node;
        }


        #endregion

        #region Expressions
        private Node Expression()
        {
            return Assignment();
        }

        private Node If()
        {
            Node condition = Expression();
            Node then_body = Expression();
            Node else_body = null;

            if (Match(TokenType.Else))
            {
                else_body = Expression();
            }
            return new Node.If(condition, then_body, else_body,null);

        }


        private Node Assignment()
        {
            var lhs = Equality();

            while (Match(TokenType.Equal))
            {
                var rhs = Assignment();
                return new Node.Assignment(lhs, rhs, null);
            }


            return lhs;
        }

        private Node Equality()
        {
            var lhs = Comparison();

            while (Match(TokenType.EqualEqual, TokenType.NotEqual))
            {
                var rhs = Comparison();
                var op = _current.Previous.Previous.Value;
                return new Node.Binary(lhs, op, rhs, new Position(lhs.Position.Line, lhs.Position.Start, rhs.Position.End));
            }

            return lhs;
        }

        private Node Comparison()
        {
            var lhs = Term();

            while (Match(TokenType.GreaterThan, TokenType.GreaterEqual, TokenType.SmallerThan, TokenType.SmallerEqual))
            {
                var rhs = Term();
                var op = _current.Previous.Previous.Value;
                return new Node.Binary(lhs, op, rhs, new Position(lhs.Position.Line, lhs.Position.Start, rhs.Position.End));
            }

            return lhs;
        }

        private Node Term()
        {
            var lhs = Factor();

            while (Match(TokenType.Plus, TokenType.Minus))
            {
                var rhs = Factor();
                var op = _current.Previous.Previous.Value;
                return new Node.Binary(lhs, op, rhs, new Position(lhs.Position.Line, lhs.Position.Start, rhs.Position.End));
            }

            return lhs;
        }
        private Node Factor()
        {
            var lhs = Unary();

            while (Match(TokenType.Asterisk, TokenType.Slash))
            {
                var rhs = Unary();
                var op = _current.Previous.Previous.Value;
                return new Node.Binary(lhs, op, rhs, new Position(lhs.Position.Line, lhs.Position.Start, rhs.Position.End));
            }

            return lhs;
        }

        private Node Unary()
        {
            if (Match(TokenType.Bang, TokenType.Minus))
            {
                var operand = Call();
                var op = _current.Previous.Previous.Value;
                return new Node.Unary(op, operand, new Position(op.Line, op.Start, operand.Position.End));
            }

            return Call();
        }

        private Node Call()
        {
            var callee = Primary();

            if (Match(TokenType.LeftParen))
            {
                List<Node> args = new List<Node>();
                if (_current.Value.Type != TokenType.RightParen)
                {
                    do
                    {
                        args.Add(Expression());
                    } while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightParen, "Expect ')' after arguments.");
                return new Node.FunctionCall(callee.Position, callee, args.ToArray());
            }
            return callee;
        }
        // primary    : | IDENTIFIER | '(' expression ')';
        private Node Primary()
        {

            if (Match(TokenType.Identifier))
                return new Node.Binding(_current.Previous.Value, Position.FromToken(_current.Previous.Value));

            if (Match(TokenType.Integer) || Match(TokenType.String))
                return new Node.Literal(_current.Previous.Value, Position.FromToken(_current.Previous.Value));


            // Statement-like
            if (Match(TokenType.LeftCurly))
                return Block();

            if (Match(TokenType.If))
                return If();

            throw new Exception();
        }

        private Node Block()
        {

            var statements = new List<Node>();
            while (!Match(TokenType.RightCurly))
            {
                statements.Add(Statement());
            }
            return new Node.Block(statements, null);
        }

        #endregion


    }
}
