/*
    The Rhyme Programming Language
    ------------------------------
    Author: Zeyad Ahmed
 */

#define DEBUG_TOKENS
#define PARSER
#define RESOLVER

using System.Diagnostics;

using Rhyme.Scanner;
using Rhyme.Parser;
using Rhyme.Resolver;

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

if (scanner.HadError)
{
    string[] lines = source.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

    foreach (var error in scanner.Errors)
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
        Console.WriteLine($"/!\\ [{error.Line}]: {error.Message}: ");
        Console.ResetColor();
        Console.WriteLine(lines[error.Line - 1]);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(new string(' ', relative_start) + '^' + new string('~', error.Length - 1));
        Console.ResetColor();
    }

    Console.WriteLine();
    return;
}

#if PARSER
Parser parser = new Parser(scanner.Scan());
var root = parser.Parse();

if (parser.HadError)
    return;
#endif

#if RESOLVER
Resolver resolver = new Resolver();
resolver.Resolve(root);

if (resolver.HadError)
    return;

#endif

stopwatch.Stop();
Console.WriteLine($"Compilation done at {stopwatch.ElapsedMilliseconds}ms.");