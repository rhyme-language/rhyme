using Rhyme.Parsing;
using System.Diagnostics;


namespace Rhyme.Modularizing
{
    public class Modularizer : ICompilerPass
    {

        List<PassError> _errors = new();
        public bool HadError { get; private set; }
        public IReadOnlyCollection<PassError> Errors { get; private set; }

        CompilationUnit[] _units;
        public Modularizer(CompilationUnit[] units)
        {
            _units = units;
            Errors = _errors;
        }
        void Error(string filePath, Position position, string message)
        {
            HadError = true;
            Debug.WriteLine(message);
            _errors.Add(new PassError(filePath, position, message));
        }
        public record ModulesInfo(
            IReadOnlyDictionary<string, IReadOnlyCollection<Node>> Exports
        );

        public ModulesInfo Modularize()
        {
            Dictionary<string, HashSet<Node>>  moduleExports = new();
            Dictionary<string, HashSet<string>>  moduleExportSymbols = new();

            foreach (var unit in _units)
            {
                var tree = unit.SyntaxTree;

                var moduleName = tree.ModuleDeclaration.Identifier.Lexeme;
                if (!moduleExports.ContainsKey(moduleName))
                {
                    moduleExports[moduleName] = new HashSet<Node>();
                    moduleExportSymbols[moduleName] = new HashSet<string>();
                }

                foreach (var topLevelDecl in tree.TopLevelDeclarations)
                {
                    if (topLevelDecl.Modifier == DeclarationAccessModifier.Global)
                    {
                        if (topLevelDecl.DeclarationNode is Node.BindingDeclaration bindDecl)
                        {
                            foreach (var declarator in bindDecl.Declarators)
                            {
                                if (!moduleExportSymbols[moduleName].Add(declarator.Identifier.Lexeme))
                                {
                                    Error(unit.FilePath, declarator.Identifier.Position, $"'{declarator.Identifier.Lexeme} is already global in module {moduleName}'");
                                }
                            }
                            moduleExports[moduleName].Add(bindDecl);
                        }
                    }
                }

                HashSet<string> importedModules = new();

                foreach(var import in tree.ImportStatements)
                {
                    var importName = import.Name.Lexeme;
                    if(importName == moduleName)
                    {
                        Error(unit.FilePath, import.Position, $"Importing the current module is impossible");
                    }
                    else if(!importedModules.Add(importName)) 
                    {
                        Error(unit.FilePath, import.Position, $"'{importName}' is already imported");
                    }else if (!moduleExports.ContainsKey(importName))
                    {
                        Error(unit.FilePath, import.Position, $"Module '{importName} can't be found");
                    }
                }


            }
            var exports = moduleExports.ToDictionary(k => k.Key, p => (IReadOnlyCollection<Node>)p.Value).AsReadOnly();
            return new ModulesInfo(exports);
        }
    }
}
