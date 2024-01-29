using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;
using Rhyme.Parser;

namespace Rhyme.Resolver
{
    internal class Resolver : Node.IVisitor<object>, ICompilerPass
    {
        public bool HadError { get; private set; }

        public IReadOnlyCollection<PassError> Errors { get; private set; }
        List<PassError> _errors = new List<PassError>();

        void Error(Token error_token, string message)
        {
            Error(error_token.Line, error_token.Start, error_token.Lexeme.Length, message);
        }
        void Error(int line, int start, int length, string message)
        {
            HadError = true;
            _errors.Add(new PassError(line, start, length, message));
        }

        SymbolTable _symbolTable = new SymbolTable();


        public SymbolTable Resolve(Node.CompilationUnit program)
        {
            Errors = _errors;

            // Global scope
            _symbolTable.StartScope();

            ResolveNode(program);

            return _symbolTable;
        }

        void ResolveNode(Node node)
        {
            node.Accept(this);
        }

        void Bind(Declaration declaration)
        {
            if (!_symbolTable.Define(declaration))
                Error(declaration.Identifier, $"'{declaration.Identifier.Lexeme}' is already defined in this scope");
        }

        public object Visit(Node.Literal literalExpr)
        {
            return null;
        }

        public object Visit(Node.Binary binaryExpr)
        {
            throw new NotImplementedException();
        }

        public object Visit(Node.Unary unaryExpr)
        {
            throw new NotImplementedException();
        }

        public object Visit(Node.Block blockExpr)
        {
            _symbolTable.StartScope();

            foreach (var stmt in blockExpr.ExpressionsStatements)
                ResolveNode(stmt);

            _symbolTable.EndScope();

            return null;
        }

        public object Visit(Node.BindingDeclaration bindingDecl)
        {
            Bind(bindingDecl.Declaration);
            ResolveNode(bindingDecl.expression);
            return null;
        }

        public object Visit(Node.If ifStmt)
        {
            throw new NotImplementedException();
        }

        public object Visit(Node.Assignment assignment)
        {
            return null;
        }

        public object Visit(Node.Binding binding)
        {

            return null;
        }

        public object Visit(Node.Grouping grouping)
        {
            throw new NotImplementedException();
        }

        public object Visit(Node.CompilationUnit compilationUnit)
        {
            foreach(var unit in compilationUnit.Units)
            {
                ResolveNode(unit);
            }
            return null;
        }
    }
}
