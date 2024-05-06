using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.TypeSystem;

using LLVMSharp.Interop;
using LLVMSharp;

namespace Rhyme.CodeGeneration
{


    /// <summary>
    /// Represents a LLVM module
    /// </summary>
    public class LLVMModule
    {
        Dictionary<string, string> _mangledIdentifiers = new();

        public string Name { get; }

        public LLVMBuilderRef Builder { get; }
        public LLVMModuleRef Module { get; }

        public LLVMModule(string name)
        {
            Module = LLVMModuleRef.CreateWithName(name);
            Builder = Module.Context.CreateBuilder(); 
           
        }


    }
}
