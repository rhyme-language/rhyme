using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;
using Rhyme.Parsing;
using Rhyme.Resolving;

using LLVMSharp.Interop;
using System.Net.Http.Headers;
using System.Diagnostics;

namespace Rhyme.CodeGeneration
{
    internal class CodeGenerator : ICompilerPass, Node.IVisitor<object>
    {
        List<PassError> _errors = new List<PassError>();


        public bool HadError { get; private set; }
        public IReadOnlyCollection<PassError> Errors { get; }

        Dictionary<string, LLVMValueRef> _locals = new Dictionary<string, LLVMValueRef>();
        Dictionary<string, LLVMTypeRef> _globals = new Dictionary<string, LLVMTypeRef>();

        LLVMModuleRef _LLVMmodule;
        LLVMBuilderRef _builder;

        private IReadOnlySymbolTable _currentSymbolTable;
        string _moduleName;

        Dictionary<RhymeType, LLVMTypeRef> _rhymeTypeLLVMType = new Dictionary<RhymeType, LLVMTypeRef>()
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


        bool _global = true;

        Dictionary<string, Action<Node.FunctionCall>> _debugBuiltIns = new Dictionary<string, Action<Node.FunctionCall>>();

        readonly Resolving.Module[]  _programModules;
        Resolving.Module _currentRhymeModule = null;
        public CodeGenerator(Resolving.Module[] modules)
        {
            _programModules = modules;
        }
        
        public (string moduleName, string llvmCode)[] Generate()
        {
            var llvm_outputs = new List<(string module, string llvmCode)>();

            foreach(var module in _programModules)
            {
                _currentRhymeModule = module;
                
                foreach (var ast_tuple in module.ResolvedSyntaxTree)
                {
                    _currentSymbolTable = ast_tuple.SymbolTable;
                    _currentSymbolTable.Reset();

                    GenerateNode(ast_tuple.SyntaxTree);
                    Debug.WriteLine(_LLVMmodule.PrintToString());
                    llvm_outputs.Add((_moduleName, _LLVMmodule.PrintToString()));
                }
            }

            return llvm_outputs.ToArray();
        }
        
        object GenerateNode(Node node)
        {
            return node.Accept(this);
        }
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
            var lhs = (LLVMValueRef)GenerateNode(binaryExpr.Left);
            var rhs = (LLVMValueRef)GenerateNode(binaryExpr.Right);

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
                GenerateNode(stmt);
            }

            return null;
        }

        public object Visit(Node.BindingDeclaration bindingDecl)
        {


            // Functions
            if(bindingDecl.Declaration.Type is RhymeType.Function func_type)
            {
                
                _locals.Clear();

                var func_name = bindingDecl.Declaration.Identifier;

                var return_type = LLVMTypeFromRhymeType(func_type.ReturnType);
                var params_type = func_type.Parameters.Select(param => LLVMTypeFromRhymeType(param.Type)).ToArray();

                // It's an extern so it's already pre-declared
                if (!_funcs.ContainsKey(func_name))
                {
                    DeclareFunction(func_type, func_name);
                }

                var llvm_func_type = _funcs[func_name].typeref;

                var llvm_function = _funcs[func_name].valueref;

                _builder.PositionAtEnd(llvm_function.AppendBasicBlock("entry"));

                _currentSymbolTable.OpenScope();

                if (_global)
                    _globals[func_name] = llvm_func_type;
                else
                    _locals.Add(func_name, llvm_function);

                // Load parameters into locals
                for (int i = 0; i < func_type.Parameters.Length; i++)
                {
                    _locals.Add(func_type.Parameters[i].Identifier, llvm_function.Params[i]);
                }
                

                foreach (var stmt in ((Node.Block)bindingDecl.Expression).ExpressionsStatements)
                {
                    _global = false;
                    GenerateNode(stmt);
                }
               
                if(return_type == LLVMTypeRef.Void)
                    _builder.BuildRetVoid();
                return null;
            }
            

            var identifier = bindingDecl.Declaration.Identifier;
            var llvm_type = LLVMTypeFromRhymeType(bindingDecl.Declaration.Type);

            if (_global)
            {
                var global_var = _LLVMmodule.AddGlobal(llvm_type, identifier);

                if (bindingDecl.Expression != null)
                    global_var.Initializer = (LLVMValueRef)GenerateNode(bindingDecl.Expression);

                return null;
            }

            _locals.Add(identifier, _builder.BuildAlloca(llvm_type, identifier));
            
            _builder.BuildStore((LLVMValueRef)GenerateNode(bindingDecl.Expression), _locals[identifier]);
            return null;
        }

        public object Visit(Node.If ifStmt)
        {
            var condition = (LLVMValueRef)GenerateNode(ifStmt.condition);

            var then_block = _LLVMmodule.LastFunction.AppendBasicBlock("if");
            var else_block = _LLVMmodule.LastFunction.AppendBasicBlock("else");
            var end = _LLVMmodule.LastFunction.AppendBasicBlock("end");

            _builder.BuildCondBr(condition, then_block, else_block);

            _builder.PositionAtEnd(then_block);
            GenerateNode(ifStmt.thenBody);
            _builder.BuildBr(end);

            _builder.PositionAtEnd(else_block);

            if (ifStmt.elseBody != null)
            {                
                GenerateNode(ifStmt.elseBody);
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
            var condition = (LLVMValueRef)GenerateNode(whileStmt.Condition);
            _builder.BuildCondBr(condition, loop_block, end);

            // Loop Block
            _builder.PositionAtEnd(loop_block);
            GenerateNode(whileStmt.LoopBody);
            _builder.BuildBr(cond_block);

            // Exit
            _builder.PositionAtEnd(end);
            return condition;
        }
        public object Visit(Node.Assignment assignment)
        {
            return _builder.BuildStore(
                (LLVMValueRef)GenerateNode(assignment.Expression),
                GetValue(((Node.Binding)assignment.Assignee).Identifier.Lexeme) 
            );
        }


        public object Visit(Node.FunctionCall callExpr)
        {
            if(callExpr.Callee is Node.Binding binding)
            {
                LLVMValueRef func_value_ref = (LLVMValueRef)GenerateNode(binding);
                var func_name = binding.Identifier.Lexeme;

                if (_debugBuiltIns.ContainsKey(func_name)){
                    _debugBuiltIns[func_name](callExpr);

                    return null;
                }
                return _builder.BuildCall2(_funcs[func_name].typeref, _funcs[func_name].valueref, callExpr.Args.Select(arg => (LLVMValueRef)GenerateNode(arg)).ToArray());
            }
            return null;
        }

        public object Visit(Node.Binding binding)
        {
            var identifier = binding.Identifier.Lexeme;
            
            if(_currentSymbolTable[identifier] is RhymeType.Function)
            {
                return _LLVMmodule.GetNamedFunction(identifier);
            }

            var value =  GetValue(identifier);

            if (value.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
                return _builder.BuildLoad2(
                    LLVMTypeFromRhymeType(_currentSymbolTable[identifier]),
                    GetValue(identifier),
                    $"load_{identifier}"
                );
            else
                return value;

        }

        public object Visit(Node.Grouping grouping)
        {
            throw new NotImplementedException();
        }

        public object Visit(Node.CompilationUnit compilationUnit)
        {
            _moduleName = $"{_currentRhymeModule.Name}__{Path.GetFileNameWithoutExtension(compilationUnit.SourceFile.FullName)}";
            _LLVMmodule = LLVMModuleRef.CreateWithName(_moduleName);
            _builder = _LLVMmodule.Context.CreateBuilder();

            _funcs.Clear();
            DefineBuiltIns();
            _debugBuiltIns.Clear();
            RhymeDebugBuiltIns();

            // Declare Exports
            foreach (var exp in _currentRhymeModule.Exports.Values)
            {
                if (exp.Type is RhymeType.Function func_type)
                    DeclareFunction(func_type, exp.Identifier);
                else
                    _LLVMmodule.AddGlobal(LLVMTypeFromRhymeType(exp.Type), exp.Identifier);
            }

            foreach (var unit in compilationUnit.Units)
            {
                // Note: They are globals!
                _global = true;
                GenerateNode(unit);
            }

            _LLVMmodule.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
           
            var ll_code = _LLVMmodule.PrintToString();
            //Console.WriteLine(ll_code);
            return ll_code;
        }

       


        #region Helpers
        void DefineFunction(Node.BindingDeclaration functionDefinition)
        {

        }

        LLVMTypeRef LLVMTypeFromRhymeType(RhymeType type)
        {
            return _rhymeTypeLLVMType[type];
        }

        Dictionary<string, (LLVMTypeRef typeref, LLVMValueRef valueref)> _funcs = new Dictionary<string, (LLVMTypeRef typeref, LLVMValueRef valueref)>();
        LLVMTypeRef DeclareFunction(RhymeType.Function functionDeclaration, string name)
        {
            var type = LLVMTypeRef.CreateFunction(
                LLVMTypeFromRhymeType(functionDeclaration.ReturnType),
                functionDeclaration.Parameters.Select(p => LLVMTypeFromRhymeType(p.Type)).ToArray()
            );
            var value = _LLVMmodule.AddFunction(name, type);

            _funcs.Add(name, (type, value));
            return type;
        }

        LLVMValueRef GetValue(string identifier)
        {
            if (_locals.ContainsKey(identifier))
                return _locals[identifier];
            
            return _LLVMmodule.GetNamedGlobal(identifier);
        }
        void RhymeDebugBuiltIns()
        {
            _debugBuiltIns.Add("dprint", fc =>
            {
                var m = _LLVMmodule.GetNamedFunction("puts");
                _builder.BuildCall2(_funcs["puts"].typeref, _funcs["puts"].valueref, new[] { (LLVMValueRef)GenerateNode(fc.Args[0]) }, "puts");

            });

            _debugBuiltIns.Add("dprint_int", fc =>
            {
                var args = fc.Args.Select(arg => (LLVMValueRef)GenerateNode(arg)).ToList();
                args.Insert(0, _builder.BuildGlobalStringPtr("%d\n"));

                _builder.BuildCall2(_funcs["printf"].typeref, _funcs["printf"].valueref, args.ToArray(), "printf");
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

            _funcs.Add("puts", (puts, puts_value));
            _funcs.Add("printf_value", (printf, printf_value));

        }

        public object Visit(Node.Return returnStmt)
        {
            _builder.BuildRet((LLVMValueRef)GenerateNode(returnStmt.RetrunExpression));
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
            foreach(var exp in _programModules.First(m => m.Name == importStmt.Name.Lexeme).Exports.Values)
            {
                if(exp.Type is RhymeType.Function func_type)
                {
                    DeclareFunction(func_type, exp.Identifier);
                    return null;
                }
                // TODO: import variables
            }

            return null;
        }

        public object Visit(Node.Module moduleDecl)
        {
            return null;
            throw new NotImplementedException();
        }

        #endregion
    }
}
