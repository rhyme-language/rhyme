#define DEBUG_TOKENS

/*
    The Rhyme Programming Language
    ------------------------------
    Author: Zeyad Ahmed
 */

using Rhyme.Scanner;
using Rhyme.Parser;


var source = File.ReadAllText("code.rhm");
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
        Console.WriteLine($"/!\\ [{error.Line}]: {error.Message}: ");
        Console.WriteLine(lines[error.Line - 1].TrimStart());
        Console.WriteLine(new string(' ', error.Start) + '^' + new string('~', error.Length - 1));
        Console.WriteLine("***");
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


Console.WriteLine("Compilation Done.");