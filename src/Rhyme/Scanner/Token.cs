﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhyme.Scanner
{
    public enum TokenType
    {

        SingleQuote, DoubleQuote,
        LeftParen, RightParen, LeftCurly, RightCurly,
        Comma, Colon, Semicolon, BackTick, Dot,

        Hash, Caret, Ampersand, Percent, Dollar,

        Identifier, 

        Plus, Minus, Asterisk, Slash, Bang,

        Equal, EqualEqual, NotEqual,
        GreaterThan, SmallerThan, GreaterEqual, SmallerEqual,

        // Keywords
        If, Else, For, While, Using, Var, Return,
        Extern, Module, Import,

        // Literals
        True, False, Null, Integer, Float, String,
        // Types
        Void,
        U8, U16, U32, U64, I8, I16, I32, I64, F32, F64, Str, Bol,
        None,
    }

    public record Token(string Lexeme, TokenType Type, Position Position, object Value = null);

}
