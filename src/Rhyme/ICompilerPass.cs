using Rhyme.Scanner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhyme
{
    public record PassError(string SourcePath, Position Position, string Message);

    public interface ICompilerPass
    {
        public bool HadError { get; }
        IReadOnlyCollection<PassError> Errors { get; }

    }
}
