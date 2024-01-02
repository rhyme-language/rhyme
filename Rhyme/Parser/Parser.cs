using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;

namespace Rhyme.Parser
{
    /*
    file
        : (decl | using_stmt)+
        ;

    bind
        : decl('=' expression)? ';'
        ;

    decl : type IDENTIFIER;

        using_stmt: 'using' STRING_LITERAL 'as' IDENTIFIER ;

    type : PRIM_TYPE | func_type | IDENTIFIER;
    func_type: type '(' (decl (',' decl)*)? ')';


    expression
        : comparison
        | stmt_expr
        ;

    block
        : '{' (decl | statement)* '}'
        ;

    stmt_expr
        : if_stmt
        | asgmnt
        | block
        ;

    statement
        : stmt_expr ';'
        ;

    if_stmt : 'if' expression stmt_expr('else' stmt_expr)?;
    asgmnt : IDENTIFIER '=' expression ';'
     */

    internal class Parser
    {
        LinkedList<Token> _tokens;
        LinkedListNode<Token> _current;

        public Parser(IEnumerable<Token> Tokens)
        {
            _tokens = new LinkedList<Token>(Tokens);
            _current = _tokens.First;
        }


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
                if (_current.Value.Type == t)
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
                    var token =  _current.Value;
                    Advance();
                    return token;
                }
            }
            throw new Exception(errorMessage);
        }


        public Node Parse()
        {
            return CompilationUnit();
        }

        private Node CompilationUnit()
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
            Type type = Type();
            var identifierToken = Consume(TokenType.Identifier, "Expects a binding name.");
            return new Declaration(type, identifierToken.Lexeme);
        }

        private Node Expression()
        {
            return Assignment();
        }

        private Node If()
        {
            var condition = Expression();
            var body = Statement();

            return null;

        }
        /*
         expression
                  : assignment
                  | block
                  | if_stmt
                  ;

         */
        private Node Statement()
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
            else if (!Match(TokenType.Equal))        // rule: bind
            {
                var identifier_token = Consume(TokenType.Identifier, "Expects a binding name.");
                Console.WriteLine($"decl: {identifier_token.Lexeme}:");

                Node expr = null;
                if (Match(TokenType.Equal))
                {
                    expr = Expression();
                }

                node = new Node.BindingDeclaration(new Declaration(type, identifier_token.Lexeme), expr);
            }
            else
            {
                node = Expression();
            }

            Consume(TokenType.Semicolon, "';' Expected");

            
            return node;
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
            
            while(Match(TokenType.EqualEqual, TokenType.NotEqual))
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
            if(Match(TokenType.Bang, TokenType.Minus))
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

            return null;
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

        private Type Type()
        {
            Type returnType = null;

            switch (_current.Value.Type)
            {
                case TokenType.Identifier:
                case TokenType.Void:
                case TokenType.U8:
                case TokenType.U16:
                case TokenType.U32:
                case TokenType.U64:
                    Console.WriteLine(_current.Value.Type);
                    Advance();

                    returnType = new Type.Primitive(Rhyme.Parser.Type.Primitives.Void);
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

                return new Type.Function(returnType, arguments.Select(a => a.type).ToArray());

            }

            return returnType;
        }


        private Node UsingStatement()
        {
            throw new NotImplementedException();
        }
    }
}
