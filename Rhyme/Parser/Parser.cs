using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Rhyme.Scanner;

namespace Rhyme.Parsing
{
    /// <summary>
    /// Parser compiler pass: <br/>
    /// - Parses the source code given its <see cref="Token"/>s. <br/>
    /// - Generating the abstract syntax tree (AST). <br/>
    /// - Reports syntactical errors. <br/>
    /// - Annotates tree <see cref="Node"/>s with <see cref="Rhyme.Parsing.RhymeType"/>s. <br/>
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
            Errors = _errors;
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

            Error(_current.Value.Position, errorMessage);
            return null;
        }

        #endregion

        #region Statements
        private Node.CompilationUnit CompilationUnit()
        {
            var units = new List<Node>();

            if (!Match(TokenType.Module))
            {
                Error(_current.Value.Position, "Expects a module declaration");
                return null;
            }

            var module_identifier = Consume(TokenType.Identifier, "Expects a module name");
            Consume(TokenType.Semicolon, "';' Expected");
            units.Add(new Node.Module(module_identifier));

            do
            {
                if (Match(TokenType.Import))
                    units.Add(Import());
                else
                    units.Add(Binding(Match(TokenType.Extern)));
            } while (!AtEnd());

            return new Node.CompilationUnit(units);
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
                case TokenType.I8:
                case TokenType.I16:
                case TokenType.I32:
                case TokenType.I64:
                case TokenType.U8:
                case TokenType.U16:
                case TokenType.U32:
                case TokenType.U64:
                case TokenType.F32:
                case TokenType.F64:
                case TokenType.Str:
                case TokenType.Bol:
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

                return new RhymeType.Function(returnType, arguments.ToArray());

            }

            return returnType;
        }

        private Node UsingStatement()
        {
            throw new NotImplementedException();
        }

        private Node Binding(bool external)
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
            return new Node.BindingDeclaration(decl, expr, external);
        }

        private Node Import()
        {
            var module = Consume(TokenType.Identifier, "Expects a module name");
            Consume(TokenType.Semicolon, "';' Expected");
            return new Node.Import(module);
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
                if (((RhymeType.Function)type).Parameters.Length == 0)
                {
                    if (Match(TokenType.Identifier))
                    {
                        identifierToken = _current.Previous.Value;
                        return new Declaration(type, identifierToken.Lexeme);
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

            if(_current.Value.Type != TokenType.Identifier)
            {
                // Normal argument call, rollback!
                _current = current;
                return null;
            }

            return new Declaration(type, Consume(TokenType.Identifier, "Expects a binding name.").Lexeme);
             
        }

        private Node Statement()
        {

            var node = Binding(false);

            if (node != null)
                return node;

            if (Match(TokenType.Return))
                node = Return();
            else
                node = Assignment();

            Consume(TokenType.Semicolon, "';' Expected");
            return node;
        }

        private Node Return()
        {
            return new Node.Return(Expression());
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
            return new Node.If(condition, then_body, else_body);

        }

        private Node While()
        {
            Node condition = Expression();
            Node body = Expression();
            return new Node.While(condition, body);
        }
        private Node Get()
        {
            var accessed = Primary();

            while (Match(TokenType.Dot))
            {
                var identifier = Consume(TokenType.Identifier, "Expects an Identifier");
                accessed = new Node.Get(accessed, identifier);
            }

            return accessed;
        }

        private Node Directive()
        {
            var identifer = Consume(TokenType.Identifier, "Expects a directive name");
            Consume(TokenType.LeftParen, "'(' Expected");

            List<Node> args = new List<Node>();
            if (_current.Value.Type != TokenType.RightParen)
            {
                do
                {
                    args.Add(Expression());
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.RightParen, "Expect ')' after arguments.");
            return new Node.Directive(identifer, args.ToArray());
        }
        private Node Assignment()
        {
            var lhs = Equality();

            while (Match(TokenType.Equal))
            {
                var rhs = Assignment();
                return new Node.Assignment(lhs, rhs);
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
                return new Node.Binary(lhs, op, rhs);
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
                return new Node.Binary(lhs, op, rhs);
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
                return new Node.Binary(lhs, op, rhs);
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
                return new Node.Binary(lhs, op, rhs);
            }

            return lhs;
        }

        private Node Unary()
        {
            if (Match(TokenType.Bang, TokenType.Minus))
            {
                var operand = Call();
                var op = _current.Previous.Previous.Value;
                return new Node.Unary(op, operand);
            }

            return Call();
        }

        private Node Call()
        {
            var callee = Get();

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
                return new Node.FunctionCall(callee, args.ToArray());
            }
            return callee;
        }

        bool MatchLiteral()
        {
            return Match(TokenType.Integer) || Match(TokenType.String) || Match(TokenType.Float)
                || Match(TokenType.True) || Match(TokenType.False);
        }
        // primary    : | IDENTIFIER | '(' expression ')';
        private Node Primary()
        {
            Node node = null;

            if (Match(TokenType.Identifier))
                return new Node.Binding(_current.Previous.Value);

            if (MatchLiteral())
                return new Node.Literal(_current.Previous.Value);

            // Statement-like
            if (Match(TokenType.LeftCurly))
                return Block();

            if (Match(TokenType.If))
                return If();

            if (Match(TokenType.While))
                return While();

            // Compiler Directive
            if (Match(TokenType.Percent))
                return Directive();

            else
                Error(_current.Value.Position, "An expression expected!");
            return node;
        }

        private Node Block()
        {

            var statements = new List<Node>();
            while (!Match(TokenType.RightCurly))
            {
                statements.Add(Statement());
            }
            return new Node.Block(statements);
        }

        #endregion

        void Error(Position at,  string message)
        {
            Console.WriteLine(message);
            HadError = true;
            _errors.Add(new PassError(at.Line, at.Start, at.Length, message));

            // Error recovery!
            while (_current.Next != null && _current.Value.Type != TokenType.Semicolon)
                _current = _current.Next;
        }
    }
}
