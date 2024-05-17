using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;
using Rhyme.Parsing;
using System.Security.Cryptography.X509Certificates;

namespace Rhyme.TypeSystem
{
    public abstract class RhymeType
    {
        public abstract RhymeType ApplyOperator(RhymeType rhs, Token operatorToken);

        public class Function : RhymeType
        {
            public readonly RhymeType ReturnType;
            public readonly RhymeType[] Parameters;

            public Function(RhymeType returnType, params RhymeType[] parameters)
            {
                ReturnType = returnType;
                Parameters = parameters;
            }

            public override string ToString() => $"{ReturnType}({string.Join(',', Parameters.Select(p => p.ToString()))})";

            public static bool operator ==(Function lhs, Function rhs) => lhs.Equals(rhs);

            public static bool operator !=(Function lhs, Function rhs) => !(lhs == rhs);

            public override bool Equals(object obj)
            {
                if (obj is not Function)
                    return false;

                return ReturnType == ((Function)obj).ReturnType && Parameters.SequenceEqual(((Function)obj).Parameters);
            }

            public override RhymeType ApplyOperator(RhymeType rhs, Token operatorToken)
            {
                throw new NotImplementedException();
            }
        }



        public class Reference : RhymeType
        {
            public readonly string Name;

            public Reference(string name)
            {
                Name = name;
            }

            public override RhymeType ApplyOperator(RhymeType rhs, Token operatorToken)
            {
                throw new NotImplementedException();
            }

            public override string ToString() => Name;
        }

        public class Numeric : RhymeType
        {
            public enum NumericKind
            {
                U8, U16, U32, U64, I8, I16, I32, I64, F32, F64,
            }

            public NumericKind Kind { get; set; }

            public Numeric(NumericKind kind)
            {
                Kind = kind;
            }

            static Numeric Max(Numeric lhs, Numeric rhs) => rhs.Kind > lhs.Kind ? rhs : lhs;

            public override RhymeType ApplyOperator(RhymeType rhs, Token operatorToken)
            {
                switch(operatorToken.Type) {
                    case TokenType.Plus:
                    case TokenType.Minus:
                    case TokenType.Asterisk:
                    case TokenType.Percent:
                    case TokenType.Slash:
                        {
                            if (rhs is Numeric numRhs)
                                return Max(this, numRhs);
                            else
                                return NoneType;
                        }

                    default:
                        return NoneType;
                }
            }

            public override string ToString() => Kind.ToString();
            public override bool Equals(object obj)
            {
                if (obj is Numeric)
                    return true;

                return false;
            }
        }

        public static readonly RhymeType U8 = new Numeric(Numeric.NumericKind.U8);
        public static readonly RhymeType U16 = new Numeric(Numeric.NumericKind.U16);
        public static readonly RhymeType U32 = new Numeric(Numeric.NumericKind.U32);
        public static readonly RhymeType U64 = new Numeric(Numeric.NumericKind.U64);
        public static readonly RhymeType I8 = new Numeric(Numeric.NumericKind.I8);
        public static readonly RhymeType I16 = new Numeric(Numeric.NumericKind.I16);
        public static readonly RhymeType I32 = new Numeric(Numeric.NumericKind.I32);
        public static readonly RhymeType I64 = new Numeric(Numeric.NumericKind.I64);
        public static readonly RhymeType F32 = new Numeric(Numeric.NumericKind.F32);

        public static readonly RhymeType NoneType = new Reference("<none>");

        public static RhymeType FromToken(Token token)
        {
            switch (token.Lexeme)
            {
                case "i8": return I8;
                case "i16": return I16;
                case "i32": return I32;
                case "i64": return I64;
                default: return NoneType;
            };
        }
    }


}
