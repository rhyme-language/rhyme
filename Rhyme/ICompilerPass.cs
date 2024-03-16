using Rhyme.Scanner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhyme
{
    internal record PassError(Position position, string Message);

    internal interface ICompilerPass
    {
        public bool HadError { get; }
        IReadOnlyCollection<PassError> Errors { get; }

    }
}
