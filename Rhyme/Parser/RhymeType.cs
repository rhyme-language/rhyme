using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;

namespace Rhyme.Parser
{
    internal record RhymeType
    {
        internal enum Primitives {
            Void, U8, U16, U32, U64, I8, I16, I32, I64, F32, F64,
        }
        internal record Primitive(Primitives type) : RhymeType;
        internal record Function(RhymeType Return, params RhymeType[] Parameters) : RhymeType;

        public static RhymeType NoneType = new RhymeType();

        public static Primitives FromToken(TokenType type)
        {
            switch(type)
            {
                case TokenType.Void: return Primitives.Void;
                case TokenType.U8:  return Primitives.U8;
                case TokenType.U16: return Primitives.U16;
                case TokenType.U32: return Primitives.U32;
                case TokenType.U64: return Primitives.U64;
                case TokenType.I8:  return Primitives.I8;
                case TokenType.I16: return Primitives.I16;
                case TokenType.I32: return Primitives.I32;
                case TokenType.I64: return Primitives.I64;
                default : return Primitives.Void;
            };
        }
    }


}
