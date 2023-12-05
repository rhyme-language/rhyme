using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;

namespace Rhyme.Parser
{
    internal class Parser
    {
        IEnumerable<Token> _tokens;

        public Parser(IEnumerable<Token> Tokens) { 
            _tokens = Tokens;

            
        }


        public Expression Parse()
        {
            return BinaryExpression();
        }

        private Expression BinaryExpression()
        {
            throw new NotImplementedException();
        }






    }
}
