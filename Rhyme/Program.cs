/*
    The Rhyme Programming Language
    ------------------------------
    Author: Zeyad Ahmed
 */

using Rhyme.Scanner;
using Rhyme.Parser;

Scanner scanner = Scanner.FromFile("code.rhm");
foreach (var token in scanner.Scan())
{
    Console.WriteLine(token);
}


Parser parser = new Parser(scanner.Scan());
var root = parser.Parse();

Console.WriteLine("Done.");