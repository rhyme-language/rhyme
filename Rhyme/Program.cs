/*
    The Rhyme Programming Language
    ------------------------------
    Author: Zeyad Ahmed
 */

//#define DEBUG_TOKENS
#define PARSER
#define RESOLVER
#define TYPE_CHECKER
#define CODE_GENERATOR

using System.Diagnostics;

using Rhyme;
using Rhyme.Scanner;
using Rhyme.Parser;
using Rhyme.Resolver;
using Rhyme.TypeChecker;
using Rhyme.CodeGenerator;

var source = File.ReadAllText("code.rhm");

var stopwatch = new Stopwatch();
stopwatch.Start();

Scanner scanner = Scanner.FromFile("code.rhm");

#if DEBUG_TOKENS
foreach (var token in scanner.Scan())
{
    Console.WriteLine(token);
}
#endif

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

if (scanner.HadError)
{

    foreach (var error in scanner.Errors)
    {
        ReportError(error);
    }
    return;
}

#if PARSER
Parser parser = new Parser(scanner.Scan());
var root = parser.Parse();

if (parser.HadError)
{
    foreach (var error in parser.Errors)
    {
        ReportError(error);
    }
    return;
}
#endif

#if RESOLVER
Resolver resolver = new Resolver();
var symbol_table = resolver.Resolve(root);

if (resolver.HadError)
{
    foreach (var error in resolver.Errors)
    {
        ReportError(error);
    }
    return;
}
#endif

#if TYPE_CHECKER
TypeChecker type_checker = new TypeChecker(symbol_table);
type_checker.Check(root);

if (type_checker.HadError)
{
    foreach (var error in type_checker.Errors)
    {
        ReportError(error);
    }
    return;
}

#endif

#if CODE_GENERATOR
CodeGenerator code_generator = new CodeGenerator(root, symbol_table);
var ll_code = code_generator.Generate();

if (code_generator.HadError)
{
    foreach (var error in code_generator.Errors)
    {
        ReportError(error);
    }
    return;
}
#endif

Console.WriteLine(ll_code);
File.WriteAllText("output.ll", ll_code);
var clang_process = Process.Start(new ProcessStartInfo("clang", "output.ll -o program.exe"));
clang_process.WaitForExit();

stopwatch.Stop();
Console.WriteLine($"Output: {Path.GetFullPath("program.exe")}");
Console.WriteLine($"Compilation done at {stopwatch.ElapsedMilliseconds}ms.");

Console.WriteLine("Running...\n");
Process.Start("program.exe");
