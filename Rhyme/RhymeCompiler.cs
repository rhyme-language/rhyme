using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme;
using Rhyme.Scanner;
using Rhyme.Parsing;
using Rhyme.Resolving;
using Rhyme.TypeSystem;
using Rhyme.CodeGeneration;
using System.Threading;
using System.Diagnostics;

namespace Rhyme
{
    internal class CompilerParameters
    {
        public string ExecutableName { get; set; } 
    }

    internal record CompilerResults(
        bool HadError,
        IReadOnlyCollection<PassError> Errors,
        string PathToExecutable
    );

    internal class RhymeCompiler
    {
        public CompilerParameters Parameters { get; set; } = new CompilerParameters();

        
        public CompilerResults CompileFromFile(params string[] files)
        {
            foreach(var file_path in files)
            {   
                var stopwatch = Stopwatch.StartNew();

                var lexer = new Lexer(File.ReadAllText(file_path));
                var tokens = lexer.Scan();
                if (lexer.HadError)
                    return new CompilerResults(true, lexer.Errors, null);

                var parser = new Parser(tokens);
                var ast = parser.Parse();
                if (parser.HadError)
                    return new CompilerResults(true, parser.Errors, null);

                var resolver = new Resolver();
                var module_info = resolver.Resolve(ast);
                if (resolver.HadError)
                    return new CompilerResults(true, resolver.Errors, null);

                var typechecker = new TypeChecker(module_info.SymbolTable);
                typechecker.Check(ast);
                if (resolver.HadError)
                    return new CompilerResults(true, typechecker.Errors, null);

                var generator = new CodeGenerator(ast, module_info.SymbolTable);
                var ll_code = generator.Generate();
                if (generator.HadError)
                    return new CompilerResults(true, generator.Errors, null);

                Debug.WriteLine(ll_code);
                File.WriteAllText("output.ll", ll_code);
                var clang_process = Process.Start(new ProcessStartInfo("clang", "output.ll -o program.exe"));
                clang_process.WaitForExit();

                stopwatch.Stop();
                Console.WriteLine($"Output: {Path.GetFullPath("program.exe")}");
                Console.WriteLine($"Compilation done at {stopwatch.ElapsedMilliseconds}ms.");
                Console.WriteLine("Running...\n");
                Thread.Sleep(500);
                Console.Clear();
                Process.Start("program.exe");

            }
            return new CompilerResults(false, null, Parameters.ExecutableName);
        }
    }
}
