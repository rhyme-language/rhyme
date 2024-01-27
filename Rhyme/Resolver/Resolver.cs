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

        SymbolTable _symbolTable = new SymbolTable();

        public SymbolTable Resolve(Node.CompilationUnit program)
        {
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
            _symbolTable.Define(declaration);
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
            Bind(bindingDecl.declaration);
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
