using Rhyme.CodeGeneration;
using Rhyme.Comptime;
using Rhyme.Modularizing;
using Rhyme.Parsing;
using Rhyme.Resolving;
using Rhyme.Scanner;
using Rhyme.TypeSystem;
using System.Diagnostics;

namespace Rhyme
{
    public class CompilationUnit
    {
        public string FilePath { get; private set; }
        public string ModuleName { get; private set; }
        public Node.CompilationUnit SyntaxTree { get; private set; }
        public IReadOnlyCollection<string> ImportedModules { get; private set; }
        public IReadOnlyDictionary<string, IReadOnlyCollection<Node>> Imports { get; private set; }
        public IReadOnlyDictionary<Node, RhymeType> TypedTree { get; private set; }
        public IReadOnlyDictionary<Node, IReadOnlyCollection<Node>> DirectedTree {get; private set;}
        public string LLVMCode { get; private set; }
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
            ImportedModules = modulesInfo.ImportdModues[this];
            Imports = modulesInfo.Exports.Where(p => ImportedModules.Contains(p.Key)).ToDictionary();
        }

        public IReadOnlyCollection<PassError> Direct()
        {
            var director = new Director(this);
            var dtree = director.Direct();

            if (director.HadError)
                return director.Errors;

            DirectedTree = dtree;
            return director.Errors;
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

        public IReadOnlyCollection<PassError> CodeGenerate()
        {
            var generator = new CodeGenerator(this);
            var llvmsource= generator.Generate();

            Debug.WriteLine(llvmsource);

            if(generator.HadError)
                return generator.Errors;

            LLVMCode = llvmsource;

            return generator.Errors;
        }
    }
}
