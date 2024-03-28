using Rhyme.Scanner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhyme
{
    public record PassError(Position position, string Message);

    public interface ICompilerPass
    {
        public bool HadError { get; }
        IReadOnlyCollection<PassError> Errors { get; }

    }
}
