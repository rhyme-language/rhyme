using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Rhyme.C;
using Rhyme.Parsing;
using Rhyme.TypeSystem;


namespace Rhyme.Resolving
{
    public record Function(string Name, RhymeType.Function Type, Declaration[] Locals);

    public record Module(
        string Name,
        (Node.CompilationUnit SyntaxTree, SymbolTableNavigator SymbolTable)[] ResolvedSyntaxTree,
        IReadOnlyDictionary<string, Declaration> Exports
    );

    /// <summary>
    /// Resolver compiler pass: <br />
    /// - Walks on a tree of <see cref="Node.CompilationUnit"></see> <br />
    /// - Checks identifiers and declarations scopes. <br />
    /// - Governs the static (lexical) life-time of declarations and their usage. <br />
    /// - Generates the <see cref="SymbolTableNavigator"/> of declarations. <br />
    /// </summary>
    public class Resolver : Node.IVisitor<object>, ICompilerPass
    {
        List<PassError> _errors = new List<PassError>();

        SymbolTable _symbolTable = new SymbolTable();
        Node.CompilationUnit[] _trees;
        Node.CompilationUnit _currentTree;

        Dictionary<string, Dictionary<string, Declaration>> _moduleExports = new Dictionary<string, Dictionary<string, Declaration>>();
        string _currentModuleName = "UNNAMED_MODULE";

        public Resolver(params Node.CompilationUnit[] programs)
        {
            _trees = programs;
            Errors = _errors;
        }
        
        public bool HadError { get; private set; }
        public IReadOnlyCollection<PassError> Errors { get; private set; }

        void Error(Position position, string message)
        {
            HadError = true;
            Console.WriteLine($"[X] Resolver @ {position.Line}: {message}");
            _errors.Add(new PassError(_currentTree.SourceFile, position, message));
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

        public Module[] Resolve()
        {
            var module_infos = new List<Module>();
            
            // Extract modules
            foreach(var tree in _trees)
            {
                _currentTree = tree;
                // We are sure syntactically that the first node is the module node
                var module_name = tree.ModuleName;

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
            var ast_symboltable_tuple = new List<(Node.CompilationUnit SyntaxTree, SymbolTableNavigator SymbolTable)>();

            // Now resolve each module
            foreach (var tree in _trees)
            {
                _symbolTable = new SymbolTable();
                DefineDebugBuiltIns();
                _currentModuleName = tree.ModuleName;
                ResolveNode(tree);
                ast_symboltable_tuple.Add((tree, _symbolTable.GetNavigator()));
            }

            // Group abstract syntax trees by module name
            var m = ast_symboltable_tuple.GroupBy(ast => ast.SyntaxTree.ModuleName);

            foreach (var group in ast_symboltable_tuple.GroupBy(ast => ast.SyntaxTree.ModuleName))
            {
                module_infos.Add(new Module(group.Key, group.Select(t => t).ToArray(), _moduleExports[group.Key]));
            }

            return module_infos.ToArray();
        }

        void ResolveNode(Node node)
        {
            node.Accept(this);
        }

        #region Pass Visitors

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

            if (bindingDecl.Expression is Node.Block block)
            {
                _symbolTable.StartScope();

                // Resolve Parameters
                if (bindingDecl.Declaration.Type is RhymeType.Function func_type)
                {
                    foreach (var param in func_type.Parameters)
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
            foreach (var unit in compilationUnit.Units)
            {
                ResolveNode(unit);
            }
            return null;
        }

        public object Visit(Node.FunctionCall callExpr)
        {
            ResolveNode(callExpr.Callee);
            foreach (var arg in callExpr.Args)
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

        void CInclude(Node.Directive directive)
        {
            var header = ((Node.Literal)directive.Arguments[0]).ValueToken.Value.ToString();
            string path = "";
            // It's a standard header
            if (File.Exists(Path.Join(CRT.IncludePath, header))) { 
                path = Path.Join(CRT.IncludePath, header);
            }
            else
            {
                if (!File.Exists(header))
                {
                    Error(directive.Position, $"File {header} can't be found");
                    return;
                }

                path = header;
            }

            var cfile = new CFile(path);
        }
        public object Visit(Node.Directive directive)
        {
            if (directive.Identifier.Lexeme == "cinclude")
            {
                if (directive.Arguments.Length != 1)
                {
                    Error(directive.Position, "Directive 'cinclude' expects 1 argument");
                    return null;
                }

                if (directive.Arguments[0] is Node.Literal import_file && import_file.ValueToken.Value is string file_name)
                {
                    CInclude(directive);
                }
                else
                {
                    Error(directive.Position, $"Directive 'cinclude' expects a str for the file path");
                }
            }
            return null;
        }

        public object Visit(Node.Import importStmt)
        {
            var module_name = importStmt.Name.Lexeme;
            if (!_moduleExports.ContainsKey(module_name))
            {
                Error(importStmt.Position, $"Module {importStmt.Name.Lexeme} doesn't exist");
                return null;
            }

            foreach (var exp in _moduleExports[module_name].Values)
            {
                _symbolTable.Define(exp);
            }

            return null;
        }
        #endregion
    }
}
