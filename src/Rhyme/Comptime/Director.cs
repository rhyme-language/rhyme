using Rhyme.C;
using Rhyme.Parsing;
using Rhyme.Scanner;
using System.Collections.Generic;
using System.Runtime.InteropServices.ObjectiveC;

namespace Rhyme.Comptime
{
    internal class Director : ICompilerPass, Node.IVisitor<object>
    {
        List<PassError> _errors = new();
        CompilationUnit _unit;
        Dictionary<Node, List<Node>> _evaluatedTree = new();
        
        public bool HadError { get; private set; }
        public IReadOnlyCollection<PassError> Errors { get; private set; }
        

        public Director(CompilationUnit unit)
        {
            _unit = unit;
            Errors = _errors;
        }

        public IReadOnlyDictionary<Node, IReadOnlyCollection<Node>> Direct()
        {
            Visit(_unit.SyntaxTree);
            return _evaluatedTree.ToDictionary(k => k.Key, v => (IReadOnlyCollection<Node>)v.Value);
        }

        void Error(Position at, string message)
        {

            Console.WriteLine(message);
            HadError = true;
            _errors.Add(new PassError(_unit.FilePath, at, message));
        }

        #region Visitors

        public object Visit(Node node)
        {
            return node.Accept(this);
        }

        CFile CInclude(Node.Directive directive)
        {
            var header = ((Node.Literal)directive.Arguments[0]).ValueToken.Value.ToString();
            string path = "";
            // It's a standard header
            if (File.Exists(Path.Join(CRT.IncludePath, header)))
            {
                path = Path.Join(CRT.IncludePath, header);
            }
            else
            {
                if (!File.Exists(header))
                {
                    Error(directive.Position, $"File {header} can't be found");
                    return null;
                }

                path = header;
            }

            return new CFile(path);
        }


        Node.Type TypeFromCDecl(string cdecl)
        {
            return new Node.IdentifierType(new Token(cdecl, TokenType.Identifier, null));
        }

        public object Visit(Node.Directive directive)
        {
            _evaluatedTree[directive] = new List<Node>();

            if(directive.Identifier.Lexeme == "cinclude")
            {
                var cfile = CInclude(directive);
                foreach(var decl in cfile.ThisDeclarations)
                {
                    if(decl is CFile.Declaration.Function declFunc)
                    {
                        _evaluatedTree[directive].Add(
                            new Node.FunctionDeclaration(
                                ReturnType: TypeFromCDecl(declFunc.ReturnType),
                                Identifier: new Token(declFunc.Name, TokenType.Identifier, Position.NonePosition),
                                Parameters: declFunc.Parameters.Select(p => new Node.ParamDecl(
                                    Type: TypeFromCDecl(p.Type),
                                    Identifier: new Token(p.Name, TokenType.Identifier, Position.NonePosition)
                                )).ToArray(),
                                Block: null
                            )
                        );
                    }
                }
                return null;
            }
            throw new NotImplementedException("Wrong directive");
        }
        public object Visit(Node.TopLevelDeclaration topDecl)
        {
            Visit(topDecl.DeclarationNode);
            return null;     
        }
        public object Visit(Node.CompilationUnit compilationUnit)
        {
            foreach(var topLevelDecl in compilationUnit.TopLevelDeclarations)
            {
                Visit(topLevelDecl);
            }

            return null;
        }
        #endregion
    }
}
