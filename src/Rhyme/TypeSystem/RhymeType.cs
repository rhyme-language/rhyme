using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;
using Rhyme.Parsing;

namespace Rhyme.TypeSystem
{
    public abstract class RhymeType
    {
        public class Function : RhymeType
        {
            public readonly RhymeType ReturnType;
            public readonly Declaration[] Parameters;

            public Function(RhymeType returnType, params Declaration[] parameters)
            {
                ReturnType = returnType;
                Parameters = parameters;
            }

            public override string ToString() => $"{ReturnType}({string.Join(',', Parameters.Select(p => p.Type))})";

            public static bool operator ==(Function lhs, Function rhs) => lhs.Equals(rhs);

            public static bool operator !=(Function lhs, Function rhs) => !(lhs == rhs);

            public override bool Equals(object obj)
            {
                if (obj is not Function)
                    return false;

                return ReturnType == ((Function)obj).ReturnType && Parameters.SequenceEqual(((Function)obj).Parameters);
            }
        }

        public class Namespace : RhymeType {}
        public class Primitive : RhymeType
        {
            private readonly string _name;

            public Primitive(string name)
            {
                _name = name;
            }

            public override string ToString() => _name;
        }

        public class Reference : RhymeType
        {
            public readonly string Name;

            public Reference(string name)
            {
                Name = name;
            }
            public override string ToString() => Name;

        }
        public class Numeric : Primitive
        {

            public enum NumericKind
            {
                U8, U16, U32, U64, I8, I16, I32, I64, F32, F64,
            }
            public static bool operator >(Numeric rhs, Numeric lhs)
            {
                return rhs._kind > lhs._kind;
            }

            public static bool operator <(Numeric rhs, Numeric lhs)
            {
                return lhs._kind < rhs._kind;
            }

            public static Numeric Max(Numeric lhs, Numeric rhs) => rhs._kind > lhs._kind ? rhs : lhs;

            private NumericKind _kind;
            public Numeric(string name, NumericKind kind) : base(name) { _kind = kind; }
        }

        public static readonly RhymeType Void = new Primitive("void");

        public static readonly RhymeType U8 = new Numeric("u8", Numeric.NumericKind.U8);
        public static readonly RhymeType U16 = new Numeric("u16", Numeric.NumericKind.U16);
        public static readonly RhymeType U32 = new Numeric("u32", Numeric.NumericKind.U32);
        public static readonly RhymeType U64 = new Numeric("u64", Numeric.NumericKind.U64);
        public static readonly RhymeType I8 = new Numeric("i8", Numeric.NumericKind.I8);
        public static readonly RhymeType I16 = new Numeric("i16", Numeric.NumericKind.I16);
        public static readonly RhymeType I32 = new Numeric("i32", Numeric.NumericKind.I32);
        public static readonly RhymeType I64 = new Numeric("i64", Numeric.NumericKind.I64);
        public static readonly RhymeType F32 = new Numeric("f32", Numeric.NumericKind.F32);
        public static readonly RhymeType F64 = new Numeric("f64", Numeric.NumericKind.F64);

        public static readonly RhymeType Str = new Primitive("str");
        public static readonly RhymeType Bol = new Primitive("bol");



        public static RhymeType FromToken(Token token)
        {
            switch (token.Type)
            {
                /*TODO*/
                case TokenType.Identifier: return new Reference(token.Lexeme);
                default: return NoneType;
            };
        }
    }


}
