﻿using System;
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

        Identifier, Integer, Float, String,

        Plus, Minus, Asterisk, Slash, Bang,

        Equal, EqualEqual, NotEqual,
        GreaterThan, SmallerThan, GreaterEqual, SmallerEqual,

        // Keywords
        If, Else, For, While, Using, Var,


        // Types
        Void,
        U8, U16, U32, U64, I8, I16, I32, I64, F32, F64, Str,
        None,
    }

    internal record Token(string Lexeme, TokenType Type, Position Position, object Value = null);

}
