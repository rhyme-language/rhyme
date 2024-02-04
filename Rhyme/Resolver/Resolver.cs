﻿using System;
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

        public Resolver()
        {
            Errors = _errors;

            // For now we will have a mini environment for holding some globals that we will need
            // through the code until we make our own standard library.
            // dprint (debug print) a temporal function for printing to console stream.
            _symbolTable.Define(new Declaration(
                new RhymeType.Function(RhymeType.Void, RhymeType.Str),
                new Token("dprint", TokenType.Identifier, 0, 0, 0)
            ));

            _symbolTable.Define(new Declaration(
                new RhymeType.Function(RhymeType.Void, RhymeType.U32),
                new Token("dprint_int", TokenType.Identifier, 0, 0, 0)
            ));
        }
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
            if (!_symbolTable.Define(declaration))
                Error(declaration.Identifier, $"'{declaration.Identifier.Lexeme}' is already defined in this scope");
        }

        public object Visit(Node.Literal literalExpr)
        {
            return null;
        }

        public object Visit(Node.Binary binaryExpr)
        {
            ResolveNode(binaryExpr.Left);
            ResolveNode(binaryExpr.Right);
            return null;
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
            if (!_symbolTable.Contains(binding.Identifier))
                Error(binding.Identifier, $"'{binding.Identifier.Lexeme}' is not defined in this scope");

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

        public object Visit(Node.FunctionCall callExpr)
        {
            ResolveNode(callExpr.Callee);
            return null;
        }
    }
}
