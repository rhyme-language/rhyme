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
        GreaterThan, SmallerThan, GreaterEqual, SmallerEqual,

        // Keywords
        If, For, While, Using, 


        // Types
        Void,
        U8, U16, U32, U64, I8, I16, I32, I64, Flt32, Flt64,
    }

    internal record Token(string Lexeme, TokenType Type, int Line, object Value = null);
    // ->


}
