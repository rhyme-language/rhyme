/*
    The Rhyme Programming Language
    ------------------------------
    Author: Zeyad Ahmed
 */

using System.Diagnostics;
using System.CommandLine;
using System.CommandLine.Invocation;

using Rhyme;
using Rhyme.Scanner;
using Rhyme.Parsing;
using Rhyme.Resolving;
using Rhyme.TypeSystem;
using Rhyme.CodeGeneration;

var console_options = CommandLineInterface.GetParametersFromArguments(args);

var compiler = new RhymeCompiler();

compiler.Parameters = new CompilerParameters
{
    ExecutableName = console_options.OutputFile.FullName,
};

compiler.CompileFromFiles(console_options.SourceFiles.Select(f => f.FullName).ToArray());

/*
string[] lines = source.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

void ReportError(PassError error)
{
    var accum_start = 0;
    for (int i = 1; i < error.Line; i++)
    {
        accum_start += lines[i - 1].Length;

        // Because it counts \n\r (Needs investigation)
        accum_start += 2;
    }
    var relative_start = error.Start - accum_start;

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"ERR: [{error.Line}]: {error.Message}: ");
    Console.ResetColor();
    Console.WriteLine(lines[error.Line - 1]);
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(new string(' ', relative_start) + '^' + new string('~', error.Length - 1));
    Console.ResetColor();
    Console.WriteLine();
}
*/