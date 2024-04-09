using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhyme
{
    public record CommandLineValues(FileInfo[] SourceFiles, FileInfo OutputFile);

    public static class CommandLineInterface
    {
        public static CommandLineValues GetParametersFromArguments(string[] args)
        {
            FileInfo[] source_files = null;
            FileInfo output_executable = null;

            var outputExecutableOption = new Option<FileInfo>(
                aliases: ["-o", "--output"],
                getDefaultValue: () => new FileInfo("program.exe"),
                description: "Output executable file"
            );

            var sourceFilesArgument = new Argument<FileInfo[]>("files", "Input source files")
            {
                Arity = ArgumentArity.OneOrMore,
            };
            
            var rootCommand = new RootCommand
            {
                sourceFilesArgument,
                outputExecutableOption,
            };

            rootCommand.SetHandler((InvocationContext ctx) =>
            {
                var results = ctx.ParseResult;
                source_files = results.GetValueForArgument(sourceFilesArgument);

                foreach(var file in source_files)
                {
                    if (file.Exists == false)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("error: ");
                        Console.ResetColor();
                        Console.WriteLine($"Can't find file {file.FullName}");
                        Environment.Exit(-1);
                    }
                }
                output_executable = results.GetValueForOption(outputExecutableOption);
            });

            if (rootCommand.Invoke(args) != 0)
                Environment.Exit(-1);

            if (source_files == null)
                Environment.Exit(-1);

            return new CommandLineValues(source_files, output_executable);
        }
    }
}
