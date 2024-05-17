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
        public record LLVMRef(LLVMTypeRef Type, LLVMValueRef Value) {
            public static implicit operator LLVMTypeRef(LLVMRef @ref) => @ref.Type;
            public static implicit operator LLVMValueRef(LLVMRef @ref) => @ref.Value;
            public static LLVMValueRef ToValue(object obj)
            {
                if(obj is LLVMValueRef)
                    return (LLVMValueRef)obj;

                if (obj is LLVMRef @ref)
                    return @ref.Value;

                throw new Exception("Not a LLVMValueRef/LLVMRef");
            }
        }

        private List<PassError> _errors = new();

        private Dictionary<string, LLVMRef> _locals = new();
        private Dictionary<string, LLVMRef> _params = new();
        private Dictionary<string, LLVMRef> _globals = new();

        private string _moduleName;
        private LLVMModuleRef _module;
        private LLVMBuilderRef _builder;


        private bool _global = true;

        Dictionary<string, string> _mangledIdentifiers = new();

        Dictionary<string, Action<Node.FunctionCall>> _debugBuiltIns = new();

        CompilationUnit _unit;
        public CodeGenerator(CompilationUnit unit)
        {
            _unit = unit;
            Errors = _errors;
        }

        public bool HadError { get; private set; }
        public IReadOnlyCollection<PassError> Errors { get; }


        public string Generate()
        {
            return (string)Visit(_unit.SyntaxTree);
        }
        
        object Visit(Node node)
        {
            return node.Accept(this);
        }


        #region Visitors
        #region Expressions
        public object Visit(Node.Literal literalExpr)
        {
            switch (literalExpr.ValueToken.Type)
            {
                case TokenType.Integer:
                    return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, Convert.ToUInt64(literalExpr.ValueToken.Value));
                default:
                    throw new NotImplementedException("Literal type not supported");
            }

        }

        public object Visit(Node.Binding binding)
        {
            var bindingName = binding.Identifier.Lexeme;

            return GetNamed(bindingName);

            if (_locals.ContainsKey(bindingName))
            {
                var local_ptr = _locals[bindingName];
                var load = _builder.BuildLoad2(local_ptr.Type, local_ptr.Value, $"l_{bindingName}");
                return new LLVMRef(load.TypeOf, load);

            }

            throw new NotImplementedException("Binding...");
        }
        public object Visit(Node.Binary binaryExpr)
        {
            var lhs_type = GetType(binaryExpr.Left);
            var rhs_type = GetType(binaryExpr.Right);

            var lhs = Visit(binaryExpr.Left);
            var rhs = Visit(binaryExpr.Right);

            if (lhs_type is RhymeType.Numeric && rhs_type is RhymeType.Numeric)
            {
                var lhs_val = LLVMRef.ToValue(lhs);
                var rhs_val = LLVMRef.ToValue(rhs);
                switch (binaryExpr.Op.Type)
                {
                    case TokenType.Plus:
                        return _builder.BuildAdd(lhs_val, rhs_val);
                }
            }

            throw new NotImplementedException("Binary");
        }


        public object Visit(Node.FunctionCall funcCall)
        {
            var func = (LLVMRef)Visit(funcCall.Callee);

            return _builder.BuildCall2(
                func.Type,
                func.Value,
                funcCall.Arguments.Select(arg => LLVMRef.ToValue(Visit(arg))).ToArray()
            );
        }
        #endregion

        #region Statements
        public object Visit(Node.Return returnStmt)
        {
            var retVal = Visit(returnStmt.RetrunExpression);
            return _builder.BuildRet(LLVMRef.ToValue(retVal));
        }
        #endregion

        #region Declarations
     
        public object Visit(Node.FunctionDeclaration funcDecl)
        {
            return DefineFunction(
                funcDecl.Identifier.Lexeme, 
                (RhymeType.Function)_unit.TypedTree[funcDecl],
                funcDecl.Parameters.Select(p => p.Identifier.Lexeme).ToArray(),
                funcDecl.Block
            );
        }
        public object Visit(Node.TopLevelDeclaration topLevelDeclaration)
        {
            return Visit(topLevelDeclaration.DeclarationNode);
        }
        #endregion

        public object Visit(Node.CompilationUnit compilationUnit)
        {
            _moduleName = $"{_unit.ModuleName}__{Path.GetFileNameWithoutExtension(_unit.FilePath)}";
            _module = LLVMModuleRef.CreateWithName(_moduleName);
            _builder = _module.Context.CreateBuilder();


            foreach (var topDecl in compilationUnit.TopLevelDeclarations)
            {
                Visit(topDecl);
            }
            Debug.WriteLine(_module.ToString());
            _module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
            return _module.ToString();
        }
        #endregion

        #region Helpers

        LLVMTypeRef LLVMType(RhymeType type)
        {
            var _rhymeTypeLLVMType = new Dictionary<RhymeType, LLVMTypeRef>()
            {
                //{ RhymeType.Void, LLVMTypeRef.Void },
                { RhymeType.U8, LLVMTypeRef.Int8 },
                { RhymeType.U16, LLVMTypeRef.Int16 },
                { RhymeType.U32, LLVMTypeRef.Int32 },
                { RhymeType.I8, LLVMTypeRef.Int8 },
                { RhymeType.I16, LLVMTypeRef.Int16 },
                { RhymeType.I32, LLVMTypeRef.Int32 },
                { RhymeType.I64, LLVMTypeRef.Int64 },
               // { RhymeType.Bol, LLVMTypeRef.Int1 }, // or int8 for alignment??
            };

            if(type is RhymeType.Function funcType)
            {
                return LLVMTypeRef.CreateFunction(
                    LLVMType(funcType.ReturnType),
                    funcType.Parameters.Select(p => LLVMType(p)).ToArray()
                );
            }

            if(type is RhymeType.Numeric)
                return _rhymeTypeLLVMType[type];

            return null;
        }


        LLVMRef GetGlobal(string name)
        {
            return _globals[name];
        }

        LLVMRef DeclareFunction(RhymeType.Function functionDeclaration, string name)
        {
            var type = LLVMType(functionDeclaration);
            var value = _module.AddFunction(name, type);

            _globals.Add(name, new LLVMRef(type, value));

            return new LLVMRef(type, value);
        }
        LLVMRef DefineFunction(string name, RhymeType.Function functionType, string[] parameters, Node.Block body)
        {

            if(parameters.Length != functionType.Parameters.Length)
            {
                throw new Exception("CC: Arguments mismatch");
            }

            LLVMRef func = null;

            func = DeclareFunction(functionType, name);
     
            // It's private linked by default
            //unsafe { LLVM.SetLinkage(func.Value, LLVMLinkage.LLVMLinkerPrivateLinkage); }


            _builder.PositionAtEnd(func.Value.AppendBasicBlock("entry"));

            _locals.Clear();
            _params.Clear();


            // Parameters
            for (int i = 0; i < functionType.Parameters.Length; i++)
            {
                var type = LLVMType(functionType.Parameters[i]);
                _params.Add(parameters[i], new LLVMRef(type, func.Value.Params[i]));
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

                // Locals
                if(stmt is Node.BindingDeclaration bindDecl)
                {
                    foreach(var declarator in bindDecl.Declarators)
                    {
                        var local_type = LLVMType(_unit.TypedTree[declarator]);

                        var local_value = _builder.BuildAlloca(local_type);
                        _locals.Add(declarator.Identifier.Lexeme, new LLVMRef(local_type, local_value));

                        if(declarator.Initializer != null)
                        { 
                            _builder.BuildStore((LLVMValueRef)Visit(declarator.Initializer), local_value);
                        }
                    }
                }
                Visit(stmt);
            }


            if(LLVMType(functionType.ReturnType) == LLVMTypeRef.Void)
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
                var m = _module.GetNamedFunction("puts");
                _builder.BuildCall2(_globals["puts"].Type, _globals["puts"].Value, new[] { (LLVMValueRef)Visit(fc.Arguments[0]) }, "puts");

            });

            _debugBuiltIns.Add("dprint_int", fc =>
            {
                var args = fc.Arguments.Select(arg => (LLVMValueRef)Visit(arg)).ToList();
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

            var puts_value = _module.AddFunction("puts", puts);
            var printf_value = _module.AddFunction("printf", printf);

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

        RhymeType GetType(Node node) => _unit.TypedTree[node];

        #endregion
    }
}
