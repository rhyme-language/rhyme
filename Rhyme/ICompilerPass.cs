using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhyme
{
    internal record PassError(int Line, int Start, int Length, string Message);

    internal interface ICompilerPass
    {
        public bool HadError { get; }
        IReadOnlyCollection<PassError> Errors { get; }

    }
}
