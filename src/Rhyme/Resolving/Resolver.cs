using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Rhyme.C;
using Rhyme.Parsing;
using Rhyme.Scanner;
using Rhyme.TypeSystem;


namespace Rhyme.Resolving
{
    public record Function(string Name, RhymeType.Function Type, Declaration[] Locals);

    public record Module(
        string Name,
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
        List<PassError> _errors = new();

        SymbolTable _symbolTable = new();
        
        CompilationUnit _unit;
        public Resolver(CompilationUnit compilationUnit)
        {
            Errors = _errors;
            _unit = compilationUnit;
        }
        
        public bool HadError { get; private set; }
        public IReadOnlyCollection<PassError> Errors { get; private set; }

        void Error(Position position, string message)
        {
            HadError = true;
            //Console.WriteLine($"[X] Resolver @ {position.Line}: {message}");
            _errors.Add(new PassError(_unit.FilePath, position, message));
        }

        Dictionary<string, Node> _externals = new();


        private void Define(Token identifier)
        {
            var result = _symbolTable.Define(identifier.Lexeme);

            if ( result == ResolutionResult.AlreadyExists)
            {
                Error(identifier.Position, $"'{identifier.Lexeme}' is already defined in the current scope");
                return;
            }

            if(result == ResolutionResult.Shadowed)
            {
                Error(identifier.Position, $"'{identifier.Lexeme}' shadows an outer declaration");
                return;
            }
            return;
        }
        private void Get(Token identifier)
        {
            if (!_symbolTable.Contains(identifier.Lexeme))
            {
                Error(identifier.Position, $"{identifier.Lexeme} is not defined in the current scope");
            }
        }
        
        public bool Resolve()
        {
            Visit(_unit.SyntaxTree);
            return !HadError;
        }

        public object Visit(Node.ModuleDecl moduleDecl){
            return null;
        }





        #region Pass Visitors


        #region Expressions
        public object Visit(Node.Binding binding)
        {
            Get(binding.Identifier);
            return null;
        }

        public object Visit(Node.Block block)
        {
            foreach (var stmt in block.ExpressionsStatements)
            {
                Visit(stmt);
            }

            return null;
        }
        #endregion
        #region Statements 

        public object Visit(Node node) => node.Accept(this);

        public object Visit(Node.Assignment assignment)
        {
            Visit(assignment.Assignee);
            Visit(assignment.Expression);

            return null;
        }
        #endregion

        #region Declarations
        public object Visit(Node.BindingDeclaration bindDecl)
        {
            foreach (var declarator in bindDecl.Declarators)
            {
                Define(declarator.Identifier);
                if(declarator.Initializer != null)
                {
                    Visit(declarator.Initializer);
                }
            }

            return null;
        }
        public object Visit(Node.TopLevelDeclaration topLevelDecl)
        {
            if (topLevelDecl.Modifier == DeclarationAccessModifier.Global)
            {

            }

            if (topLevelDecl.DeclarationNode is Node.BindingDeclaration bindDecl)
            {
                Visit(bindDecl);
            }

            // Function Declarations
            if (topLevelDecl.DeclarationNode is Node.FunctionDeclaration funcDecl)
            {
                Define(funcDecl.Identifier);

                _symbolTable.StartScope();
                Visit(funcDecl.Block);
                _symbolTable.EndScope();
            }

            return null;
        }

        #endregion

        public object Visit(Node.CompilationUnit compilationUnit)
        {
            Visit(compilationUnit.ModuleDeclaration);

            foreach (var decl in compilationUnit.TopLevelDeclarations)
            {
                Visit(decl);
            }

            return null;
        }
        #endregion
    }
}
