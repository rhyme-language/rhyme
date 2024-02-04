using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;
using Rhyme.Parser;

using LLVMSharp.Interop;
using Rhyme.Resolver;

namespace Rhyme.CodeGenerator
{
    internal class CodeGenerator : ICompilerPass, Node.IVisitor<object>
    {
        List<PassError> _errors = new List<PassError>();

        private readonly Node.CompilationUnit _compilationUnit;
        private readonly IReadOnlySymbolTable _symbolTable;

        public bool HadError { get; private set; }
        public IReadOnlyCollection<PassError> Errors { get; }

        Dictionary<string, (LLVMTypeRef typeRef, LLVMValueRef valueRef)> _functions = new Dictionary<string, (LLVMTypeRef typeRef, LLVMValueRef valueRef)>();

        LLVMModuleRef _module;
        LLVMBuilderRef _builder;

        #region Helpers
        void DefineFunction(Node.BindingDeclaration functionDefinition)
        {

        }
        void DeclareFunction(RhymeType.Function functionDeclaration, string name)
        {
            var type = LLVMTypeRef.CreateFunction(
                LLVMTypeFromRhymeType(functionDeclaration.ReturnType),
                functionDeclaration.Parameters.Select(p => LLVMTypeFromRhymeType(p)).ToArray()
            );

            _functions.Add(
                name,
                (type, _module.AddFunction(name, type))
            );
        }
        #endregion
        void DefineBuiltIns()
        {
            // external forward declaration for 'puts()'
            var puts = LLVMTypeRef.CreateFunction(
                LLVMTypeRef.Int32,
                new LLVMTypeRef[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) }
            );
            _functions.Add(
                "puts",
                (puts, _module.AddFunction("puts", puts))
            );

            // external forward declaration for printf()

            var printf = LLVMTypeRef.CreateFunction(
                LLVMTypeRef.Int32,
                new[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) },
                true
            );

            _functions.Add(
                "printf",
                (printf, _module.AddFunction("printf", printf))
            );
        
        }
        public CodeGenerator(Node.CompilationUnit compilationUnit, IReadOnlySymbolTable symbolTable)
        {
            _compilationUnit = compilationUnit;
            _symbolTable = symbolTable;

        }

        public string Generate()
        {
            return (string)GenerateNode(_compilationUnit);
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
                    break;

            }
            return null;

        }

        public object Visit(Node.Unary unaryExpr)
        {
            throw new NotImplementedException();
        }

        public object Visit(Node.Block blockExpr)
        {
            throw new NotImplementedException();
        }

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
        };
        LLVMTypeRef LLVMTypeFromRhymeType(RhymeType type)
        {
            return _rhymeTypeLLVMType[type];
        }
        public object Visit(Node.BindingDeclaration bindingDecl)
        {
            // Functions
            if(bindingDecl.Declaration.Type is RhymeType.Function)
            {
                var func_type = (RhymeType.Function)bindingDecl.Declaration.Type;

                var return_type = LLVMTypeFromRhymeType(func_type.ReturnType);
                var params_type = func_type.Parameters.Select(f_type => LLVMTypeFromRhymeType(f_type)).ToArray();
                var function = LLVMTypeRef.CreateFunction(return_type, params_type);

                var func = _module.AddFunction(bindingDecl.Declaration.Identifier.Lexeme, function).AppendBasicBlock("entry");
                _builder.PositionAtEnd(func);

                foreach(var stmt in ((Node.Block)bindingDecl.expression).ExpressionsStatements)
                {
                    GenerateNode(stmt);
                }
                _builder.BuildRetVoid();

               
            }

            return null;
        }

        public object Visit(Node.If ifStmt)
        {
            throw new NotImplementedException();
        }

        public object Visit(Node.Assignment assignment)
        {
            throw new NotImplementedException();
        }


        Dictionary<string, Action<Node.FunctionCall>> _debugBuiltIns = new Dictionary<string, Action<Node.FunctionCall>>();
        void RhymeDebugBuiltIns()
        {
            _debugBuiltIns.Add("dprint", fc =>
            {
                _builder.BuildCall2(_functions["printf"].typeRef, _functions["printf"].valueRef, new[] { (LLVMValueRef)GenerateNode(fc.Args[0]) }, "printf");

            });

            _debugBuiltIns.Add("dprint_int", fc =>
            {
                var args = fc.Args.Select(arg => (LLVMValueRef)GenerateNode(arg)).ToList();
                args.Insert(0, _builder.BuildGlobalStringPtr("%d"));

                _builder.BuildCall2(_functions["printf"].typeRef, _functions["printf"].valueRef, args.ToArray(), "printf");
            });

        }

        public object Visit(Node.FunctionCall callExpr)
        {
            // hardcoded dprint
            if(callExpr.Callee is Node.Binding)
            {
                var func_name = ((Node.Binding)callExpr.Callee).Identifier.Lexeme;

                if (_debugBuiltIns.ContainsKey(func_name)){
                    _debugBuiltIns[func_name](callExpr);
                }
            }
            return null;
        }

        public object Visit(Node.Binding binding)
        {
            return null;
        }

        public object Visit(Node.Grouping grouping)
        {
            throw new NotImplementedException();
        }

        public object Visit(Node.CompilationUnit compilationUnit)
        {
            _module = LLVMModuleRef.CreateWithName("first_modue");
            _builder = _module.Context.CreateBuilder();

            DefineBuiltIns();
            RhymeDebugBuiltIns();

            foreach (var unit in compilationUnit.Units)
            {
                GenerateNode(unit);
            }

            _module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
           
            var ll_code = _module.PrintToString();
            //Console.WriteLine(ll_code);
            return ll_code;
        }
    }
}
