using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme;
using Rhyme.CodeGeneration;
using Rhyme.Resolving;
using Rhyme.Parsing;
using Rhyme.Scanner;
using Rhyme.TypeSystem;

namespace Rhyme.Test.Compiler.Compilation
{
    [TestClass]
    public class CompilerTest
    {
        /// <summary>
        /// Bulk tests the compiler, every directory will act as a compilation test
        /// fails if a compiler pass reported an error.
        /// </summary>
        [TestMethod]
        public void BulkTestCompile()
        {
            var codes_directory = new DirectoryInfo(Directory.GetCurrentDirectory() + "\\Compiler\\Compilation\\");
           
            foreach(var test_dir in codes_directory.GetDirectories())
            {
                var files = test_dir.GetFiles("*.rhm").Select(f => f.FullName);

                var syntax_trees = new List<Node.CompilationUnit>();

                foreach (var file_path in files)
                {
                    var lexer = new Lexer(File.ReadAllText(file_path));
                    var tokens = lexer.Scan();
                    if (lexer.HadError)
                        Assert.Fail($"Lexer Failed", lexer.Errors);

                    var parser = new Parser(tokens, file_path);
                    syntax_trees.Add(parser.Parse());
                    if (parser.HadError)
                        Assert.Fail($"Parser Failed", parser.Errors);
                }

                var resolver = new Resolver(syntax_trees.ToArray());
                var modules = resolver.Resolve();
                if (resolver.HadError)
                    Assert.Fail($"Resolver Failed", resolver.Errors);

                var typechecker = new TypeChecker(modules);
                typechecker.Check();

                if (typechecker.HadError)
                    Assert.Fail($"Type Checker Failed", typechecker.Errors);

                var generator = new CodeGenerator(modules);
                var ll_codes = generator.Generate();
                if (generator.HadError)
                    Assert.Fail($"Code Generator Failed", generator.Errors);             
            }

            Assert.IsTrue(true);
        }
    }
}
