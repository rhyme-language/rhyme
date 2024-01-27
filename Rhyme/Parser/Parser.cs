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

        private bool Match(params TokenType[] type)
        {
            foreach (TokenType t in type)
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

            return new Node.CompilationUnit(units);
        }
        private RhymeType Type()
        {
            RhymeType returnType = null;
            var type = _current.Value.Type;
            switch (type)
            {
                case TokenType.Identifier:
                case TokenType.Void:
                case TokenType.Var:
                case TokenType.U8:
                case TokenType.U16:
                case TokenType.U32:
                case TokenType.U64:
                case TokenType.f32:
                case TokenType.f64:
                case TokenType.Str:
                    Advance();

                    returnType = new RhymeType.Primitive(RhymeType.FromToken(type));
                    break;
            }

            if (Match(TokenType.LeftParen)) // Function
            {

                var arguments = new List<Declaration>();
                if (_current.Value.Type != TokenType.RightParen)
                {
                    do
                    {
                        arguments.Add(Declaration());
                    } while (Match(TokenType.Comma));
                }

                Consume(TokenType.RightParen, "Expect ')' after arguments.");

                // build the type descriptor

                return new RhymeType.Function(returnType, arguments.Select(a => a.type).ToArray());

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

            Node expr = null;
            if (Match(TokenType.Equal))
            {
                expr = Expression();
            }
            Consume(TokenType.Semicolon, "Expects a ';' after a binding value");
            return new Node.BindingDeclaration(decl, expr);

        }

        private Declaration Declaration()
        {
            RhymeType type = Type();
            var identifierToken = Consume(TokenType.Identifier, "Expects a binding name.");
            return new Declaration(type, identifierToken);
        }

        private Node Statement()
        {
            var node = Expression();
            Consume(TokenType.Semicolon, "';' Expected");
            return node;
        }


        #endregion

        #region Expressions
        private Node Expression()
        {
            var type = Type();
            Node node = null;

            if (Match(TokenType.Equal))    // rule: assignment
            {
                var identifier_token = _current.Previous.Previous.Value;            // (IDENTIFIER)<->(=)<->(expr)
                Console.WriteLine($"assignment: {identifier_token.Lexeme}");

                var assignment_expression = Expression();


                node = new Node.Assignment(identifier_token, assignment_expression);
            }
            else if (!Match(TokenType.Equal))
            {
                if (type != null)    // Valid type, then it's a bind
                {
                    var identifier_token = Consume(TokenType.Identifier, "Expects a binding name.");
                    Console.WriteLine($"decl: {identifier_token.Lexeme}:");

                    Node expr = null;
                    if (Match(TokenType.Equal))
                    {
                        expr = Expression();
                    }

                    node = new Node.BindingDeclaration(new Declaration(type, identifier_token), expr);
                }
                else    // Invalid type, it's an expression statement.
                {
                    node = Assignment();
                }

            }

            return node;
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


        private Node Assignment()
        {
            var lhs = Equality();

            while (Match(TokenType.Equal))
            {
                var rhs = Assignment();
                return new Node.Assignment(null, rhs);
            }


            return lhs;
        }

        private Node Equality()
        {
            var lhs = Comparison();

            while (Match(TokenType.EqualEqual, TokenType.NotEqual))
            {
                var rhs = Comparison();
                var op = _current.Previous.Value;
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
                var op = _current.Previous.Value;
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
                var op = _current.Previous.Value;
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
                var op = _current.Previous.Value;
                return new Node.Binary(lhs, op, rhs);
            }

            return lhs;
        }

        private Node Unary()
        {
            if (Match(TokenType.Bang, TokenType.Minus))
            {
                var operand = Primary();
                var op = _current.Previous.Value;
                return new Node.Unary(op, operand);
            }

            return Primary();
        }
        // primary    : | IDENTIFIER | '(' expression ')';
        private Node Primary()
        {

            if (Match(TokenType.Identifier))
                return new Node.Binding(_current.Previous.Value);

            if (Match(TokenType.Integer))
                return new Node.Literal(_current.Previous.Value);

            if (Match(TokenType.String))
                return new Node.Literal(_current.Previous.Value);

            if (Match(TokenType.LeftParen))
            {
                Expression();
                Consume(TokenType.RightParen, "Expects a ')'");
            }

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
            return new Node.Block(statements);
        }

        #endregion


    }
}
