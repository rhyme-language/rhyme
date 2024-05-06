using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ClangSharp;
using LLVMSharp;
using Rhyme.Scanner;
using Rhyme.TypeSystem;

namespace Rhyme.Parsing 
{ 

    /// <summary>
    /// Parser compiler pass: <br/>
    /// - Parses the source code given its <see cref="Token"/>s. <br/>
    /// - Generating the abstract syntax tree (AST). <br/>
    /// - Reports syntactical errors. <br/>
    /// - Annotates tree <see cref="Node"/>s with <see cref="RhymeType"/>s. <br/>
    /// </summary>
    public class Parser : ICompilerPass
    {
        LinkedList<Token> _tokens;
        LinkedListNode<Token> _current;

        List<PassError> _errors = new();
        string _filePath;

        
        public bool HadError { get; private set; }
        public IReadOnlyCollection<PassError> Errors { get; private set; }
        
        public Parser(string filePath)
        {
            _filePath = filePath;
            Errors = _errors;
        }

        public Node.CompilationUnit Parse()
        {
            var lexer = new Lexer(_filePath);
            var tokens = lexer.Scan();

            if (lexer.HadError)
            {
                Errors = lexer.Errors;
                return null;
            }

            _tokens = new LinkedList<Token>(tokens);
            _current = _tokens.First;

            return CompilationUnit();
        }

        #region Helpers

        bool TryParse(Func<Node> rule)
        {
            if (rule() == null)
                return false;

            return true;
        }
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

        private bool PeekOr(params TokenType[] types)
        {
            foreach (TokenType t in types)
            {
                if (!AtEnd() && _current.Value.Type == t)
                {
                    return true;
                }
            }
            return false;
        }

        private bool PeekAll(params TokenType[] types)
        {
            var revert = _current;
            foreach (TokenType t in types)
            {
                if (!Match(t))
                {
                    _current = revert;
                    return false;
                }
            }
            _current = revert;
            return true;
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
            var tlDecls = new List<Node.TopLevelDeclaration>();
            var imports = new List<Node.ImportStmt>();

            if (!Match(TokenType.Module))
            {
                Error(_current.Value.Position, "Expects a module declaration");
                return null;
            }

            var module_identifier = Consume(TokenType.Identifier, "Expects a module name");
            Consume(TokenType.Semicolon, "';' Expected");

            while (Match(TokenType.Import))
            {
                imports.Add(Import());
            }

            do
            {
                tlDecls.Add(TopLevelDeclaration());
            } while (!AtEnd());

            return new Node.CompilationUnit(new Node.ModuleDecl(module_identifier), imports, tlDecls);
        }

        private Node.TopLevelDeclaration TopLevelDeclaration()
        {
            bool _extern = Match(TokenType.Extern);
            bool _global = Match(TokenType.Global);

            var modifier = DeclarationAccessModifier.None;

            if (_extern)
                modifier = DeclarationAccessModifier.Extern;

            if (_global)
                modifier = DeclarationAccessModifier.Global;

            var revert = _current;
            var type = Type();


            if (Match(TokenType.Identifier))
            {
                if (Match(TokenType.OpenParen))
                {
                    _current = revert;
                    return new Node.TopLevelDeclaration(FunctionDeclaration(), modifier);
                }
            }

            _current = revert;
            var bindDecl = BindingDeclaration();
            Consume(TokenType.Semicolon, "';' expected");
            return new Node.TopLevelDeclaration(bindDecl, modifier);
        }

        private Node.FunctionDeclaration FunctionDeclaration(){
            var ret_type = Type();
            var identifier = Consume(TokenType.Identifier, "Expects a function name");
            Consume(TokenType.OpenParen, "'(' Expected");
            var parameters = Parameters();
            Consume(TokenType.CloseParen, "')' expected after arguments.");
            var block = Block();
            return new Node.FunctionDeclaration(ret_type, identifier, parameters, block);
        }


        private Node.Type Type()
        {
            if (Match(TokenType.Fn)) // Function Type
            {
                return FuncType();
            }

            return new Node.IdentifierType(Consume(TokenType.Identifier, "Expects an identifier for type"));
        }

        private Node.FuncType FuncType()
        {
            var retType = Type();
            Consume(TokenType.OpenParen, "'(' Expected");
            var parameters = Parameters();
            Consume(TokenType.CloseParen, "')' expected after arguments.");

            return new Node.FuncType(retType, parameters); 
        }
        private Node.ParamDecl[] Parameters()
        {
            List<Node.ParamDecl> parameters = new();
            
            if (!Match(TokenType.CloseParen))
            {
                do
                {
                    var param = ParamDecl();
                    parameters.Add(param);
                } while (Match(TokenType.Comma));
            }
            return parameters.ToArray();
        }
        private Node.ParamDecl ParamDecl()
        {
            var type = Type();
            
            var identifier = Consume(TokenType.Identifier, "Expects a parameter name");

            if (type == null || identifier == null)
                return null;

            return new Node.ParamDecl(type, identifier);
        }

        private Node.BindingDeclaration BindingDeclaration()
        {
            var type = Type();

            /*TODO: multiple declarators*/
            var declarator = Declarator();

            Node expr = null;
            if (Match(TokenType.Equal))
            {
                expr = Expression();
            }


            return new Node.BindingDeclaration(type, declarator);
        }

        Node.Declarator Declarator()
        {
            var identifier = Consume(TokenType.Identifier, "Expects a declarator identifier");

            Node expression = null;
            if (Match(TokenType.Equal))
            {
                expression = Expression();
            }

            return new Node.Declarator(identifier, expression);
        }
        private Node.ImportStmt Import()
        {
            var module = Consume(TokenType.Identifier, "Expects a module name");
            Consume(TokenType.Semicolon, "';' Expected");
            return new Node.ImportStmt(module);
        }

        private Node Statement()
        {
            Node node = null;

            if(PeekAll(TokenType.Identifier, TokenType.Equal))
            {
                node = Assignment();
                Consume(TokenType.Semicolon, "';' Expected");
                return node;
            }

            if (PeekAll(TokenType.Return))
            {
                node =  Return();
                Consume(TokenType.Semicolon, "';' Expected");
                return node;
            }

            node = BindingDeclaration();

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
            Consume(TokenType.OpenParen, "'(' Expected");

            List<Node> args = new();
            if (_current.Value.Type != TokenType.CloseParen)
            {
                do
                {
                    args.Add(Expression());
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.CloseParen, "Expect ')' after arguments.");
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
                var op = _current.Previous.Value;
                var rhs = Comparison();
                return new Node.Binary(lhs, op, rhs);
            }

            return lhs;
        }

        private Node Comparison()
        {
            var lhs = Term();

            while (Match(TokenType.GreaterThan, TokenType.GreaterEqual, TokenType.SmallerThan, TokenType.SmallerEqual))
            {
                var op = _current.Previous.Value;
                var rhs = Term();
                return new Node.Binary(lhs, op, rhs);
            }

            return lhs;
        }

        private Node Term()
        {
            var lhs = Factor();

            while (Match(TokenType.Plus, TokenType.Minus))
            {
                var op = _current.Previous.Value;
                var rhs = Factor();
                return new Node.Binary(lhs, op, rhs);
            }

            return lhs;
        }
        private Node Factor()
        {
            var lhs = Unary();

            while (Match(TokenType.Asterisk, TokenType.Slash))
            {
                var op = _current.Previous.Value;
                var rhs = Unary();
                return new Node.Binary(lhs, op, rhs);
            }

            return lhs;
        }

        private Node Unary()
        {
            if (Match(TokenType.Bang, TokenType.Minus))
            {
                var op = _current.Previous.Value;
                var operand = Call();
                return new Node.Unary(op, operand);
            }

            return Call();
        }

        private Node Call()
        {
            var callee = Get();

            if (Match(TokenType.OpenParen))
            {
                List<Node> args = new();
                if (_current.Value.Type != TokenType.CloseParen)
                {
                    do
                    {
                        args.Add(Expression());
                    } while (Match(TokenType.Comma));
                }
                Consume(TokenType.CloseParen, "Expect ')' after arguments.");
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
            if (Match(TokenType.OpenCurly))
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

        private Node.Block Block()
        {
            Consume(TokenType.OpenCurly, "'{' expected");
            var statements = new List<Node>();
            while (_current.Value.Type !=  TokenType.CloseCurly)
            {
                statements.Add(Statement());
            }
            Consume(TokenType.CloseCurly, "'}' expected");
            return new Node.Block(statements);
        }

        #endregion

        void Error(Position at,  string message)
        {

            Console.WriteLine(message);
            HadError = true;
            _errors.Add(new PassError(_filePath,at, message));

            // Error recovery!
            while (_current.Next != null && _current.Value.Type != TokenType.Semicolon)
                _current = _current.Next;
        }
    }
}
