using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhyme.Scanner
{
    public enum TokenType
    {

        SingleQuote, DoubleQuote,
        OpenParen, CloseParen, OpenCurly, CloseCurly,
        Comma, Colon, Semicolon, BackTick, Dot,

        Hash, Caret, Ampersand, Percent, Dollar,

        Identifier, 

        Plus, Minus, Asterisk, Slash, Bang,

        Equal, EqualEqual, NotEqual,
        GreaterThan, SmallerThan, GreaterEqual, SmallerEqual,

        // Keywords
        If, Else, For, While, Using, Return,
        Extern, Global, Const, Fn,
        Module, Import,

        // Literals
        True, False, Null, Integer, Float, String,

        None,
    }

    public record Token(string Lexeme, TokenType Type, Position Position, object Value = null);

}
