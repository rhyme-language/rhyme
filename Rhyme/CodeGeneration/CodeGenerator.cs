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

        private List<PassError> _errors = new List<PassError>();

        private Dictionary<string, LLVMRef> _locals = new Dictionary<string, LLVMRef>();
        private Dictionary<string, LLVMRef> _params = new Dictionary<string, LLVMRef>();
        private Dictionary<string, LLVMRef> _globals = new Dictionary<string, LLVMRef>();

        private string _moduleName;
        private LLVMModuleRef _LLVMmodule;
        private LLVMBuilderRef _builder;

        private SymbolTableNavigator _currentSymbolTable;

        private bool _global = true;

        Dictionary<string, string> _mangledIdentifiers = new Dictionary<string, string>();

 

        

        Dictionary<string, Action<Node.FunctionCall>> _debugBuiltIns = new Dictionary<string, Action<Node.FunctionCall>>();


        readonly Module[] _programModules;
        Module _currentRhymeModule = null;

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
                
                foreach (var ast_tuple in module.ResolvedSyntaxTree)
                {
                    _currentSymbolTable = ast_tuple.SymbolTable;
                    _currentSymbolTable.Reset();

                    Visit((Node)ast_tuple.SyntaxTree);
                    //Debug.WriteLine(_LLVMmodule.PrintToString());
                    llvmOutputs.Add((_moduleName, _LLVMmodule.PrintToString()));
                }

            }

            return llvmOutputs.ToArray();
        }
        
        object Visit(Node node)
        {
            return node.Accept(this);
        }

        #region Pass Visitors
        public object Visit(Node.Literal literalExpr)
        {
            var token = literalExpr.ValueToken;
            if (token.Type  ==  TokenType.String)
                return _builder.BuildGlobalStringPtr((string)token.Value);

            switch (token.Type)
            {
                case TokenType.String:
                    return _builder.BuildGlobalStringPtr((string)token.Value);

                case TokenType.Integer:
                    return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, Convert.ToUInt64(token.Value));

                case TokenType.True:
                    return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1);
                case TokenType.False:
                    return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0);
            }
            return null;
        }

        public object Visit(Node.Binary binaryExpr)
        {
            var lhs = (LLVMValueRef)Visit(binaryExpr.Left);
            var rhs = (LLVMValueRef)Visit(binaryExpr.Right);

            switch (binaryExpr.Op.Type)
            {
                case TokenType.Plus:
                    return _builder.BuildAdd(lhs, rhs);
                case TokenType.Minus:
                    return _builder.BuildSub(lhs, rhs);
                case TokenType.Asterisk:
                    return _builder.BuildMul(lhs, rhs);

                case TokenType.EqualEqual:
                    return _builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, lhs, rhs);
                case TokenType.NotEqual:
                    return _builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, lhs, rhs);
                    
            }
            return null;

        }

        public object Visit(Node.Unary unaryExpr)
        {
            throw new NotImplementedException();
        }

        public object Visit(Node.Block blockExpr)
        {
            foreach(var stmt in blockExpr.ExpressionsStatements)
            {
                Visit(stmt);
            }

            return null;
        }
        public object Visit(Node.Return returnStmt)
        {
            _builder.BuildRet((LLVMValueRef)Visit(returnStmt.RetrunExpression));
            return null;
        }

        public object Visit(Node.Get member)
        {
            throw new NotImplementedException();
        }

        public object Visit(Node.Directive directive)
        {
            throw new NotImplementedException();
        }



        public object Visit(Node.Import importStmt)
        {
            foreach (var exp in _programModules.First(m => m.Name == importStmt.Name.Lexeme).Exports.Values)
            {
                if (exp.Type is RhymeType.Function func_type)
                {
                    DefineMangled(exp.Identifier, $"{importStmt.Name.Lexeme}.{exp.Identifier}");
                    var func_ref = DeclareFunction(func_type, $"{importStmt.Name.Lexeme}.{exp.Identifier}");
                    unsafe { LLVM.SetLinkage(func_ref.Value, LLVMLinkage.LLVMExternalLinkage); }
                    return null;
                }
                // TODO: import variables
            }

            return null;
        }

        public object Visit(Node.BindingDeclaration bindingDecl)
        {
            var identifier = bindingDecl.Declaration.Identifier;

            // Functions
            if (bindingDecl.Declaration.Type is RhymeType.Function funcType)
            {
                DefineMangled(identifier, bindingDecl.Export ? $"{_currentRhymeModule.Name}.{identifier}" : identifier);

                var func = DefineFunction(GetMangle(identifier), funcType, ((Node.Block)bindingDecl.Expression));

                if(bindingDecl.Export)
                    unsafe { LLVM.SetLinkage(func.Value, LLVMLinkage.LLVMExternalLinkage); }

                if (_global)
                {
                    if(identifier == "main")
                    {
                        unsafe { LLVM.SetLinkage(func.Value, LLVMLinkage.LLVMExternalLinkage); }
                    }
                }
                return null;
            }
            

            var llvm_type = LLVMTypeFromRhymeType(bindingDecl.Declaration.Type);

            if (_global)
            {
                var global_var = _LLVMmodule.AddGlobal(llvm_type, identifier);

                if (bindingDecl.Expression != null)
                    global_var.Initializer = (LLVMValueRef)Visit(bindingDecl.Expression);

                return null;
            }

            _locals.Add(identifier, new LLVMRef(llvm_type, _builder.BuildAlloca(llvm_type, identifier)));
            
            _builder.BuildStore((LLVMValueRef)Visit(bindingDecl.Expression), _locals[identifier].Value);
            return null;
        }

        public object Visit(Node.If ifStmt)
        {
            var condition = (LLVMValueRef)Visit(ifStmt.condition);

            var then_block = _LLVMmodule.LastFunction.AppendBasicBlock("if");
            var else_block = _LLVMmodule.LastFunction.AppendBasicBlock("else");
            var end = _LLVMmodule.LastFunction.AppendBasicBlock("end");

            _builder.BuildCondBr(condition, then_block, else_block);

            _builder.PositionAtEnd(then_block);
            Visit(ifStmt.thenBody);
            _builder.BuildBr(end);

            _builder.PositionAtEnd(else_block);

            if (ifStmt.elseBody != null)
            {                
                Visit(ifStmt.elseBody);
            }

            _builder.BuildBr(end);


            _builder.PositionAtEnd(end);
            return condition;
        }

        public object Visit(Node.While whileStmt)
        {
            var cond_block = _LLVMmodule.LastFunction.AppendBasicBlock("cond_block");
            var loop_block = _LLVMmodule.LastFunction.AppendBasicBlock("loop");
            var end = _LLVMmodule.LastFunction.AppendBasicBlock("break");

            _builder.BuildBr(cond_block);

            // Condition Block
            _builder.PositionAtEnd(cond_block);
            var condition = (LLVMValueRef)Visit(whileStmt.Condition);
            _builder.BuildCondBr(condition, loop_block, end);

            // Loop Block
            _builder.PositionAtEnd(loop_block);
            Visit(whileStmt.LoopBody);
            _builder.BuildBr(cond_block);

            // Exit
            _builder.PositionAtEnd(end);
            return condition;
        }
        public object Visit(Node.Assignment assignment)
        {
            return _builder.BuildStore(
                (LLVMValueRef)Visit(assignment.Expression),
                GetNamed(((Node.Binding)assignment.Assignee).Identifier.Lexeme).Value
            );
        }


        public object Visit(Node.FunctionCall callExpr)
        {
            if(callExpr.Callee is Node.Binding binding)
            {
                var func_name = binding.Identifier.Lexeme;

                if (_debugBuiltIns.ContainsKey(func_name)){
                    _debugBuiltIns[func_name](callExpr);
                    return null;
                }

                LLVMValueRef func_value_ref = (LLVMValueRef)Visit((Node)binding);
                return _builder.BuildCall2(_globals[GetMangle(func_name)].Type, _globals[GetMangle(func_name)].Value, callExpr.Args.Select(arg => (LLVMValueRef)Visit(arg)).ToArray());
            }
            return null;
        }

        public object Visit(Node.Binding binding)
        {
            return GetNamed(GetMangle(binding.Identifier.Lexeme)).Value;
        }

        public object Visit(Node.Grouping grouping)
        {
            Visit(grouping.Expression);
            return null;
        }

        public object Visit(Node.CompilationUnit compilationUnit)
        {
            _moduleName = $"{_currentRhymeModule.Name}__{Path.GetFileNameWithoutExtension(compilationUnit.SourceFile.FullName)}";
            _LLVMmodule = LLVMModuleRef.CreateWithName(_moduleName);
            _builder = _LLVMmodule.Context.CreateBuilder();

            _globals.Clear();
            _mangledIdentifiers.Clear();

            DefineBuiltIns();
            _debugBuiltIns.Clear();
            RhymeDebugBuiltIns();

            foreach (var unit in compilationUnit.Units)
            {
                // Note: They are globals!
                _global = true;
                Visit(unit);
            }

            _LLVMmodule.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
           
            var ll_code = _LLVMmodule.PrintToString();
            return ll_code;
        }
        #endregion



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

            _currentSymbolTable.NextScope();
            _locals.Clear();
            _params.Clear();

            // Parameters
            for (int i = 0; i < functionType.Parameters.Length; i++)
            {
                var type = LLVMTypeFromRhymeType(functionType.Parameters[i].Type);
                _params.Add(functionType.Parameters[i].Identifier, new LLVMRef(type, func.Value.Params[i]));
            }

            foreach(var stmt in body.ExpressionsStatements)
            {
                Visit(stmt);
            }

            if (functionType.ReturnType == RhymeType.Void)
                _builder.BuildRetVoid();

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
            
            return _globals[identifier];
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

        void DefineMangled(string name, string mangledName)
        {
            _mangledIdentifiers.Add(name, mangledName);
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
