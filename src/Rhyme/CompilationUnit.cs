using Rhyme.Modularizing;
using Rhyme.Parsing;
using Rhyme.Resolving;
using Rhyme.Scanner;
using Rhyme.TypeSystem;

namespace Rhyme
{
    public class CompilationUnit
    {
        public string FilePath {get; private set;}
        public string ModuleName { get; private set;}
        public Node.CompilationUnit SyntaxTree {get; private set;}
        public IReadOnlyCollection<Node> Imports {get; private set;}
        public IReadOnlyDictionary<Node, RhymeType> TypedTree { get; set; }

        public CompilationUnit(string filePath)
        {
            FilePath = filePath;
        }

        public IReadOnlyCollection<PassError> Parse()
        {
            var parser = new Parser(FilePath);

            if (parser.HadError)
                return parser.Errors;
            SyntaxTree = parser.Parse();

            ModuleName = SyntaxTree.ModuleDeclaration.Identifier.Lexeme;

            return parser.Errors;
        }

        public void Modularize(Modularizer.ModulesInfo modulesInfo)
        {
            Imports = modulesInfo.Exports[ModuleName];
        }
        public IReadOnlyCollection<PassError> Resolve()
        {
            var resolver = new Resolver(this);
            resolver.Resolve();
            return resolver.Errors;
        }

        public IReadOnlyCollection<PassError> TypeCheck()
        {
            var checker = new TypeChecker(this);
            var typedTree = checker.Check();
            if (checker.HadError)
                return checker.Errors;

            TypedTree = typedTree;
            return checker.Errors;
        }
    }
}
