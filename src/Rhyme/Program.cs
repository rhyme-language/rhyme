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
