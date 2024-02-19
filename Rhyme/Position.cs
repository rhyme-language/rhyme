using Rhyme.Scanner;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhyme
{
    internal record Position(int Line, int Start, int End)
    {
        public int Length => End - Start + 1;
        public static readonly Position NonePosition = new Position(0, 0, 0);
    };
}
