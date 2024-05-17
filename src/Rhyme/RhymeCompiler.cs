using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhyme.Scanner;
using Rhyme.Parsing;
using Rhyme.Resolving;
using Rhyme.TypeSystem;
using Rhyme.CodeGeneration;
using System.Threading;
using System.Diagnostics;
using LLVMSharp.Interop;
using Rhyme.Modularizing;
using System.CommandLine.Parsing;

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


            var units = new List<CompilationUnit>(files.Select(f => new CompilationUnit(f)));
            foreach (var unit in units)
            {
                var parseErrors = unit.Parse();
                if (parseErrors.Count > 0)
                    return new CompilerResults(true, parseErrors, null);
            }

            var modularizer = new Modularizer(units.ToArray());
            var modulesInfo = modularizer.Modularize();
            if(modularizer.HadError)
                return new CompilerResults(true, modularizer.Errors, null);


            List<string> llvmfiles = new();
            foreach (var unit in units)
            { 
                unit.Modularize(modulesInfo);

                var directorErrors = unit.Direct();
                if (directorErrors.Count > 0)
                    return new CompilerResults(true, directorErrors, null);

                var resolveErrors = unit.Resolve();
                if (resolveErrors.Count > 0)
                    return new CompilerResults(true, resolveErrors, null);

                var typeErrors = unit.TypeCheck();
                if (typeErrors.Count > 0)
                    return new CompilerResults(true, typeErrors, null);

                var generateErrors = unit.CodeGenerate();
                if(generateErrors.Count > 0)
                    return new CompilerResults(true, generateErrors, null);

                var fileName = $"{unit.ModuleName}__{Path.GetFileNameWithoutExtension(unit.FilePath)}.ll";
                llvmfiles.Add(fileName);
                File.WriteAllText(fileName, unit.LLVMCode);
            }
            stopwatch.Stop();

            var clangProcess = Process.Start(
                new ProcessStartInfo(
                    "clang",
                    $"{string.Join(' ', llvmfiles.ToArray())} -o {Path.GetFileName(Parameters.ExecutableName)}"
                )
            );
            clangProcess.WaitForExit();

            foreach(var llvmFile in llvmfiles)
            {
                File.Delete(llvmFile);
            }

            if (clangProcess.ExitCode == 0)
            {
                Console.WriteLine($"Output: {Path.GetFullPath(Parameters.ExecutableName)}");
                Console.WriteLine($"Compilation done at {stopwatch.ElapsedMilliseconds}ms.");
                Console.WriteLine("Running...\n");
                Thread.Sleep(500);
                Console.Clear();
                Process.Start(Parameters.ExecutableName);
            }


            return new CompilerResults(false, null, Parameters.ExecutableName);

        }
    }
}
