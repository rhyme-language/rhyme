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

var results = compiler.CompileFromFiles(console_options.SourceFiles.Select(f => f.FullName).ToArray());

if (results.HadError)
{
    foreach(var error in results.Errors)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[{Path.GetFileName(error.SourcePath)}:{error.Position.Line}] {error.Message}");

        Console.ResetColor();
        var error_source = File.ReadAllText(error.SourcePath);

        // find line start
        int line_start = 0;
        for (int i = error.Position.Start; i >= 0; i--)
        { 
            line_start = i;
            if (error_source[i] == '\n')
                break;
        }

        var error_line = error_source.Split('\n')[error.Position.Line - 1];
        Console.WriteLine(error_line);
        Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine(
            new string(' ', error.Position.Start - line_start - 1) +    // Space
            '^' +                                                       // Carret
            new string('~', error.Position.Length - 1)                  // Underline
        );

        Console.ResetColor();
    }
}

