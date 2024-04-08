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
using LLVMSharp.Interop;

namespace Rhyme
{
    public class CompilerParameters
    {
        public string ExecutableName { get; set; } 
    }

    public record CompilerResults(
        bool HadError,
        IReadOnlyCollection<PassError> Errors,
        string PathToExecutable
    );

    public class RhymeCompiler
    {
        public CompilerParameters Parameters { get; set; } = new CompilerParameters();

        
        public CompilerResults CompileFromFiles(params string[] files)
        {
            var stopwatch = Stopwatch.StartNew();

            var syntax_trees = new List<Node.CompilationUnit>();

            foreach (var file_path in files)
            {

                var lexer = new Lexer(file_path);
                var tokens = lexer.Scan();
                if (lexer.HadError)
                    return new CompilerResults(true, lexer.Errors, null);

                var parser = new Parser(tokens, file_path);
                syntax_trees.Add(parser.Parse());
                if (parser.HadError)
                    return new CompilerResults(true, parser.Errors, null);
            }

            var resolver = new Resolver(syntax_trees.ToArray());
            var modules = resolver.Resolve();
            if (resolver.HadError)
                return new CompilerResults(true, resolver.Errors, null);

            var typechecker = new TypeChecker(modules);
            typechecker.Check();

            if (typechecker.HadError)
                return new CompilerResults(true, typechecker.Errors, null);

            var generator = new CodeGenerator(modules);
            var ll_codes = generator.Generate();
            if (generator.HadError)
                return new CompilerResults(true, generator.Errors, null);

            foreach(var code in ll_codes)
            {
                Debug.WriteLine(code.llvmCode);
                
                File.WriteAllText($"{code.moduleName}.ll", code.llvmCode);
            }

            var clang_process = Process.Start(new ProcessStartInfo("clang", $"{string.Join(' ', ll_codes.Select(ll => ll.moduleName + ".ll"))} -o program.exe -g -gcodeview"));
            clang_process.WaitForExit();
            stopwatch.Stop();

            if(clang_process.ExitCode == 0)
            {
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
