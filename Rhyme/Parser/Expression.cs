using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;

namespace Rhyme.Parser
{
    abstract internal record Expression
    {

        public interface Visitor<T>
        {
            T Visit(Literal literalExpr);
            T Visit(Binary binaryExpr);
        }
        abstract public T Accept<T>(Visitor<T> visitor);

        public record Literal(object Value) : Expression
        {
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }

        public record Binary(Expression left, Token op, Expression right) : Expression
        {
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);

        }
    }
}
