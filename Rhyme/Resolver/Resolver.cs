using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;
using Rhyme.Parser;
using System.Xml.Linq;
using LLVMSharp;

namespace Rhyme.Resolver
{
    internal record Function(string Name, RhymeType.Function Type, Declaration[] Locals);

    internal class Resolver : Node.IVisitor<object>, ICompilerPass
    {
        public bool HadError { get; private set; }

        public IReadOnlyCollection<PassError> Errors { get; private set; }
        List<PassError> _errors = new List<PassError>();

        void Error(Position position, string message)
        {
            Error(position.Line, position.Start, position.Length, message);
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
                new RhymeType.Function(RhymeType.Void, new Declaration(RhymeType.Str, "string")),
                "dprint"
            ));

            _symbolTable.Define(new Declaration(
                new RhymeType.Function(RhymeType.Void, new Declaration(RhymeType.I32, "int")),
                "dprint_int"
            ));

            _symbolTable.Define(new Declaration(
                new RhymeType.Function(RhymeType.Void, new Declaration(RhymeType.F32, "float")),
                "dprint_flt"
            ));
        }
        public SymbolTable Resolve(Node.CompilationUnit program)
        {
            ResolveNode(program);

            return _symbolTable;
        }

        void ResolveNode(Node node)
        {
            node.Accept(this);
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

        List<Declaration> _functionLocals = new List<Declaration>();

        public object Visit(Node.Block blockExpr)
        {
            _symbolTable.StartScope();

            foreach (var stmt in blockExpr.ExpressionsStatements)
            {
                ResolveNode(stmt);
            }

            _symbolTable.EndScope();

            return null;
        }

        public object Visit(Node.BindingDeclaration bindingDecl)
        {
            var result = _symbolTable.Define(bindingDecl.Declaration);

            if (result == ResolutionResult.AlreadyExists)
                Error(bindingDecl.Position, $"'{bindingDecl.Declaration.Identifier}' is already defined in this scope");

            if (result == ResolutionResult.Shadowed)
                Error(bindingDecl.Position, $"'{bindingDecl.Declaration.Identifier}' shadows an outer declaration");

            if(bindingDecl.Expression is Node.Block block)
            {
                _symbolTable.StartScope();

                // Resolve Parameters
                if(bindingDecl.Declaration.Type is RhymeType.Function func_type)
                {
                    foreach(var param in func_type.Parameters)
                        _symbolTable.Define(param);

                    foreach (var stmt in block.ExpressionsStatements)
                        ResolveNode(stmt);

                    _symbolTable.EndScope();
                    return null;
                } 
            }

            ResolveNode(bindingDecl.Expression);
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
            if (!_symbolTable.Contains(binding.Identifier.Lexeme))
                Error(binding.Position, $"'{binding.Identifier.Lexeme}' is not defined in this scope");

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
            foreach(var arg in callExpr.Args)
                ResolveNode(arg);

            return null;
        }

        public object Visit(Node.Return returnStmt)
        {
            ResolveNode(returnStmt.RetrunExpression);
            return null;
        }
    }
}
