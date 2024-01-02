using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhyme.Parser
{
    internal record Type
    {
        internal enum Primitives {
            Void, U8, U16, U32, U64, I8, I16, I32, I64, Flt32, Flt64,
        }
        internal record Primitive(Primitives type) : Type;
        internal record Function(Type Return, params Type[] Parameters) : Type;

        
    }


}
