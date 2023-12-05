/*
    The Rhyme Programming Language
    ------------------------------
    Author: Zeyad Ahmed
 */

using Rhyme.Scanner;
using Rhyme.Parser;

Scanner scanner = Scanner.FromFile("code.rhm");
Parser parser = new Parser(scanner.Scan());

foreach (var token in scanner.Scan())
{
    Console.WriteLine(token);
}