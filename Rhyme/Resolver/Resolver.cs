using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;
using Rhyme.Parsing;
using System.Xml.Linq;
using LLVMSharp;
using System.Xml.Serialization;
using System.Threading.Tasks.Dataflow;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace Rhyme.Resolving
{
    internal record Function(string Name, RhymeType.Function Type, Declaration[] Locals);

    internal record Module(
        string Name,
        (Node.CompilationUnit SyntaxTree, IReadOnlySymbolTable SymbolTable)[] ResolvedSyntaxTree,
        IReadOnlyDictionary<string, Declaration> Exports
    );

    internal class Resolver : Node.IVisitor<object>, ICompilerPass
    {
        public bool HadError { get; private set; }

        public IReadOnlyCollection<PassError> Errors { get; private set; }
        List<PassError> _errors = new List<PassError>();

        void Error(Position position, string message)
        {
            HadError = true;
            Console.WriteLine($"[X] Resolver @ {position.Line}: {message}");
            _errors.Add(new PassError(position, message));
        }

        SymbolTable _symbolTable = new SymbolTable();
        Node.CompilationUnit[] trees;
        public Resolver(params Node.CompilationUnit[] programs)
        {
            trees = programs;
            Errors = _errors;
        }

        void DefineDebugBuiltIns()
        {
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

        Dictionary<string, Dictionary<string, Declaration>> _moduleExports = new Dictionary<string, Dictionary<string, Declaration>>();
        string _currentModuleName = "UNNAMED_MODULE";

        public Module[] Resolve()
        {
            var module_infos = new List<Module>();
            
            // Extract modules
            foreach(var tree in trees)
            {
                // We are sure syntactically that the first node is the module node
                var module_name = ((Node.Module)tree.Units.First()).Name.Lexeme;

                if (!_moduleExports.ContainsKey(module_name))
                    _moduleExports[module_name] = new Dictionary<string, Declaration>();

                foreach(var unit in tree.Units)
                {
                    if(unit is Node.BindingDeclaration binding)
                    {
                        if(binding.Export)
                            _moduleExports[module_name].Add(binding.Declaration.Identifier, binding.Declaration);
                    }
                }
            }

            var moduleTrees = new List<Node.CompilationUnit>();

            // Now resolve each module
            foreach(var tree in trees)
            {
                _symbolTable = new SymbolTable();
                DefineDebugBuiltIns();
                _currentModuleName = ((Node.Module)tree.Units.First()).Name.Lexeme;
                ResolveNode(tree);
            }

            // Group abstract syntax trees by module name
            foreach(var group in trees.GroupBy(ast => ((Node.Module)ast.Units.First()).Name.Lexeme))
            {
                var ast_symboltable_tuple = new List<(Node.CompilationUnit SyntaxTree, IReadOnlySymbolTable SymbolTable)>();

                foreach (var ast in group)
                {
                    ast_symboltable_tuple.Add((ast, _symbolTable));
                }

                module_infos.Add(new Module(group.Key, ast_symboltable_tuple.ToArray(), _moduleExports[group.Key]));
            }

            return module_infos.ToArray();
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
            {
                if (_moduleExports[_currentModuleName].ContainsValue(bindingDecl.Declaration))
                    Error(bindingDecl.Position, $"'{bindingDecl.Declaration.Identifier}' is already defined in '{_currentModuleName}' module");
                else
                    Error(bindingDecl.Position, $"'{bindingDecl.Declaration.Identifier}' is already defined in this scope");
                return null;
            }

            if (result == ResolutionResult.Shadowed)
            {
                if (_moduleExports[_currentModuleName].ContainsValue(bindingDecl.Declaration))
                    Error(bindingDecl.Position, $"'{bindingDecl.Declaration.Identifier}' is shadows a declaration in '{_currentModuleName}' module");
                else
                    Error(bindingDecl.Position, $"'{bindingDecl.Declaration.Identifier}' shadows an outer declaration");
            }

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
            ResolveNode(ifStmt.condition);
            ResolveNode(ifStmt.thenBody);
            if (ifStmt.elseBody != null)
                ResolveNode(ifStmt.elseBody);

            return null;
        }

        public object Visit(Node.While whileStmt)
        {
            ResolveNode(whileStmt.Condition);
            ResolveNode(whileStmt.LoopBody);

            return null;
        }
        public object Visit(Node.Assignment assignment)
        {
            return null;
        }

        public object Visit(Node.Binding binding)
        {
            if (!_symbolTable.Contains(binding.Identifier.Lexeme))
            {
                if (!_moduleExports[_currentModuleName].ContainsKey(binding.Identifier.Lexeme))
                    Error(binding.Position, $"'{binding.Identifier.Lexeme}' is not defined in this scope");
            }
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

        public object Visit(Node.Get member)
        {
            throw new NotImplementedException();
        }

        public object Visit(Node.Directive directive)
        {
            if(directive.Identifier.Lexeme == "import")
            {
                if (directive.Arguments.Length != 1)
                {
                    Error(directive.Position, "Directive 'import' expects 1 argument");
                    return null;
                }

                if (directive.Arguments[0] is Node.Literal import_file && import_file.ValueToken.Value is string file_name)
                {
                    if (!File.Exists(file_name))
                    {
                        Error(directive.Position, $"File '{file_name}' doesn't exist");
                        return null;
                    }

                    //Import(file_name);

                }
                else
                {
                    Error(directive.Position, $"Directive 'import' expects a str for the file path");
                }
            }
            return null;
        }

        public object Visit(Node.Import importStmt)
        {
            var module_name = importStmt.Name.Lexeme;
            if (!_moduleExports.ContainsKey(module_name))
            {
                Error(importStmt.Position, $"Module {importStmt} doesn't exist");
            }

            return null;
        }

        public object Visit(Node.Module moduleDecl)
        {
            //foreach(var decl in _moduleExports[moduleDecl.Name.Lexeme])
                //_symbolTable.Define(decl);

            return null;
        }
    }
}
