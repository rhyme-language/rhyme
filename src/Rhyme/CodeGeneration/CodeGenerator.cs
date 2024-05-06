using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;
using Rhyme.Parsing;
using Rhyme.Resolving;
using Rhyme.TypeSystem;

using LLVMSharp.Interop;
using System.Net.Http.Headers;
using System.Diagnostics;
using LLVMSharp;
using ClangSharp;

namespace Rhyme.CodeGeneration
{
    /// <summary>
    /// Code generator compiler pass: <br />
    /// - Generates LLVM IR code. <br />
    /// - It's the last pass so it generates directly, it doesn't check any syntactics or symantics
    /// guaranting that previous passes did all the needed checks. <br />
    /// </summary>
    public class CodeGenerator : ICompilerPass, Node.IVisitor<object>
    {
        record LLVMRef(LLVMTypeRef Type, LLVMValueRef Value);

        private List<PassError> _errors = new();

        private Dictionary<string, LLVMRef> _locals = new();
        private Dictionary<string, LLVMRef> _params = new();
        private Dictionary<string, LLVMRef> _globals = new();

        private string _moduleName;
        private LLVMModuleRef _LLVMmodule;
        private LLVMBuilderRef _builder;


        private bool _global = true;

        Dictionary<string, string> _mangledIdentifiers = new();

        Dictionary<string, Action<Node.FunctionCall>> _debugBuiltIns = new();


        readonly Resolving.Module[] _programModules;
        Resolving.Module _currentRhymeModule = null;

        public CodeGenerator(Resolving.Module[] modules)
        {
            _programModules = modules;
        }

        public bool HadError { get; private set; }
        public IReadOnlyCollection<PassError> Errors { get; }


        public (string moduleName, string llvmCode)[] Generate()
        {
            var llvmOutputs = new List<(string module, string llvmCode)>();

            foreach(var module in _programModules)
            {
                _currentRhymeModule = module;
            }

            return llvmOutputs.ToArray();
        }
        
        object Visit(Node node)
        {
            return node.Accept(this);
        }


        #region Helpers

        LLVMTypeRef LLVMTypeFromRhymeType(RhymeType type)
        {
            var _rhymeTypeLLVMType = new Dictionary<RhymeType, LLVMTypeRef>()
            {
                { RhymeType.Void, LLVMTypeRef.Void },
                { RhymeType.U8, LLVMTypeRef.Int8 },
                { RhymeType.U16, LLVMTypeRef.Int16 },
                { RhymeType.U32, LLVMTypeRef.Int32 },
                { RhymeType.I8, LLVMTypeRef.Int8 },
                { RhymeType.I16, LLVMTypeRef.Int16 },
                { RhymeType.I32, LLVMTypeRef.Int32 },
                { RhymeType.I64, LLVMTypeRef.Int64 },
                { RhymeType.Bol, LLVMTypeRef.Int1 }, // or int8 for alignment??
            };

            if(type is RhymeType.Function funcType)
            {
                return LLVMTypeRef.CreateFunction(
                    LLVMTypeFromRhymeType(funcType.ReturnType),
                    funcType.Parameters.Select(p => LLVMTypeFromRhymeType(p.Type)).ToArray()
                );
            }

            if(type is RhymeType.Primitive)
                return _rhymeTypeLLVMType[type];

            return null;
        }


        LLVMRef GetGlobal(string name)
        {
            return _globals[name];
        }

        LLVMRef DeclareFunction(RhymeType.Function functionDeclaration, string name)
        {
            var type = LLVMTypeFromRhymeType(functionDeclaration);
            var value = _LLVMmodule.AddFunction(name, type);

            _globals.Add(name, new LLVMRef(type, value));

            return new LLVMRef(type, value);
        }
        LLVMRef DefineFunction(string name, RhymeType.Function functionType, Node.Block body)
        {

            LLVMRef func = null;

            if (!_globals.ContainsKey(name))
                func = DeclareFunction(functionType, name);
            else
                func = GetGlobal(name);

            // It's private linked by default
            unsafe { LLVM.SetLinkage(func.Value, LLVMLinkage.LLVMLinkerPrivateLinkage); }


            _builder.PositionAtEnd(func.Value.AppendBasicBlock("entry"));

            _locals.Clear();
            _params.Clear();

            // Parameters
            for (int i = 0; i < functionType.Parameters.Length; i++)
            {
                var type = LLVMTypeFromRhymeType(functionType.Parameters[i].Type);
                _params.Add(functionType.Parameters[i].Identifier, new LLVMRef(type, func.Value.Params[i]));
            }

            foreach (var stmt in body.ExpressionsStatements)
            {
                if(stmt is Node.Return)
                {
                    // If a block has a return, then the rest of instructions is dead code
                    // so don't visit the rest of statements, also it's for LLVM purpose
                    // to manage block terminators!
                    Visit(stmt);
                    return func;
                }

                Visit(stmt);
            }


            if(LLVMTypeFromRhymeType(functionType.ReturnType) == LLVMTypeRef.Void)
                _builder.BuildRetVoid();
            else
                _builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));

            return func;
        }

        LLVMRef GetNamed(string identifier)
        {
            if(_params.ContainsKey(identifier))
                return _params[identifier];

            if (_locals.ContainsKey(identifier))
            {            
                var local_ptr = _locals[identifier];
                var load = _builder.BuildLoad2(local_ptr.Type, local_ptr.Value, $"l_{identifier}");
                return new LLVMRef(load.TypeOf, load);
            }
            
            return _globals[GetMangle(identifier)];
        }
        void RhymeDebugBuiltIns()
        {
            _debugBuiltIns.Add("dprint", fc =>
            {
                var m = _LLVMmodule.GetNamedFunction("puts");
                _builder.BuildCall2(_globals["puts"].Type, _globals["puts"].Value, new[] { (LLVMValueRef)Visit(fc.Args[0]) }, "puts");

            });

            _debugBuiltIns.Add("dprint_int", fc =>
            {
                var args = fc.Args.Select(arg => (LLVMValueRef)Visit(arg)).ToList();
                args.Insert(0, _builder.BuildGlobalStringPtr("%d\n"));

                _builder.BuildCall2(_globals["printf"].Type, _globals["printf"].Value, args.ToArray(), "printf");
            });
        }
        void DefineBuiltIns()
        {
            // external forward declaration for 'puts()'
            var puts = LLVMTypeRef.CreateFunction(
                LLVMTypeRef.Int32,
                new LLVMTypeRef[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) }
            );
            // external forward declaration for printf()

            var printf = LLVMTypeRef.CreateFunction(
                LLVMTypeRef.Int32,
                new[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) },
                true
            );

            var puts_value = _LLVMmodule.AddFunction("puts", puts);
            var printf_value = _LLVMmodule.AddFunction("printf", printf);

            _globals.Add("puts", new LLVMRef(puts, puts_value));
            _globals.Add("printf", new LLVMRef(printf, printf_value));

        }

        string DefineMangled(string name, string mangledName)
        {
            _mangledIdentifiers[name] =  mangledName;
            return mangledName;
        }

        string GetMangle(string name)
        {
            if (_mangledIdentifiers.ContainsKey(name))
                return _mangledIdentifiers[name];
            else
                return name;
        }



        #endregion
    }
}
