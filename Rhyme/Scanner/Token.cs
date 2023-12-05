using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhyme.Scanner
{
    internal enum TokenType
    {
        
        SingleQuote, DoubleQuote,
        LeftParen, RightParen, LeftCurly, RightCurly,
        Comma, Colon, Semicolon, BackTick,

        Hash, Caret, Ampersand, Percent, Dollar,

        Identifier, Integer, Float, 

        Plus, Minus, Star, Slash, Bang,
        
        Equal, EqualEqual, NotEqual,
        GreaterThan, SmallerThan,
    }

    internal record Token(string Lexeme, TokenType Type, int Line, object Value = null);
    // ->


}
