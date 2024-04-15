using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;
using Rhyme.Parsing;
using Rhyme.Resolving;
using System.Collections;
using System.Diagnostics;
using Rhyme.TypeSystem;

namespace Rhyme.TypeSystem
{

    public class TypeChecker : Node.IVisitor<RhymeType>, ICompilerPass
    {
        
        private List<PassError> _errors = new List<PassError>();

        Node.CompilationUnit _currentTree;

        private readonly Module[] _modules;
        private Module _currentModule;

        private SymbolTableNavigator _currentSymbolTable;

        private RhymeType.Function _currentFunction = null;

        public enum Operator
        {
            Assignment,
            Arithmetic,
            Bitwise
        }

        public TypeChecker(params Module[] moduleInfos)
        {
            _modules = moduleInfos;
            Errors = _errors;   
        }

        public bool HadError { get; private set; }
        public IReadOnlyCollection<PassError> Errors { get; private set; }

        public bool Check()
        {
            foreach(var module in _modules)
            {
                _currentModule = module;
                foreach(var astTuple in module.ResolvedSyntaxTree)
                {
                    _currentTree = astTuple.SyntaxTree;
                    _currentSymbolTable = astTuple.SymbolTable;
                    _currentSymbolTable.Reset();
                    Visit((Node)astTuple.SyntaxTree);
                }
            }

            return HadError;
        }

        RhymeType Visit(Node node)
        {
            return node.Accept(this);
        }

        #region Pass Visitors

        public RhymeType Visit(Node.Literal literalExpr)
        {
            switch (literalExpr.ValueToken.Type)
            {
                case TokenType.Integer:
                    return RhymeType.I32;

                case TokenType.String:
                    return RhymeType.Str;

                case TokenType.Float:
                    return RhymeType.F32;

                case TokenType.True:
                case TokenType.False:
                    return RhymeType.Bol;

                default:
                    return RhymeType.NoneType;
            }
        }

        public RhymeType Visit(Node.Binary binaryExpr)
        {

            var lhs = Visit(binaryExpr.Left);
            var rhs = Visit(binaryExpr.Right);

            var eval_result = TypeEvaluate(lhs, binaryExpr.Op.Type, rhs);

            if (eval_result.valid)
                return eval_result.result;
            else
                return RhymeType.NoneType;
                //Error(binaryExpr.Op, $"Can't apply operator '{binaryExpr.Op.Lexeme}' on types '{lhs}' and '{rhs}'");

            return RhymeType.NoneType;
        }

        public RhymeType Visit(Node.Unary unaryExpr)
        {
            throw new NotImplementedException();
        }

        public RhymeType Visit(Node.Block blockExpr)
        {
            _currentSymbolTable.NextScope();

            foreach(var exprstmt in blockExpr.ExpressionsStatements)
                Visit(exprstmt);

            return new RhymeType.Function(RhymeType.Void);
        }

        public RhymeType Visit(Node.BindingDeclaration bindingDecl)
        {
            var decl = bindingDecl.Declaration;
            var declType = _currentSymbolTable[decl.Identifier];

            if(bindingDecl.Expression is Node.Block block)
            {
                _currentFunction = (RhymeType.Function)bindingDecl.Declaration.Type;

                Visit((Node)block);

                if(bindingDecl.Declaration.Type is not RhymeType.Function)
                {
                    Error(bindingDecl.Position, $"Binding '{bindingDecl.Declaration.Identifier}' is not a function type");
                    return RhymeType.NoneType;
                }

                return RhymeType.NoneType;
            }

            var rhsType = Visit(bindingDecl.Expression);

            if (rhsType == RhymeType.NoneType)
                return RhymeType.NoneType;

            if (!declType.Equals(rhsType)){
                Error(bindingDecl.Position, $"Can not implicitly convert type '{rhsType}' to a binding of type '{declType}'");
            }
            
            return RhymeType.NoneType;
        }

        void Error(Position at, string message)
        {
            Debug.WriteLine($"[{_currentModule.Name}] TypeChecker @ {at.Line}: {message}");
            HadError = true;
            _errors.Add(new PassError(_currentTree.SourceFile, at, message));
        }
        public RhymeType Visit(Node.If ifStmt)
        {
            return RhymeType.Bol;
        }

        public RhymeType Visit(Node.While whileStmt)
        {
            return RhymeType.I32;
        }

        public RhymeType Visit(Node.Assignment assignment)
        {
            var rhs = Visit(assignment.Expression);

            if (assignment.Assignee is not Node.Binding)
                throw new Exception("Unassignable target.");

            var lhs = _currentSymbolTable[((Node.Binding)assignment.Assignee).Identifier.Lexeme];
            var evalResult = TypeEvaluate(rhs, TokenType.Equal, lhs);

            if (evalResult.valid)
            {
                return evalResult.result;
            }
            else
            {
                var token = ((Node.Binding)assignment.Assignee).Identifier;
                Error(token.Position, $"Can't implicitly assign value of type '{rhs}' to a binding of type '{lhs}'");
                return RhymeType.NoneType;
            }
        }

        

        public RhymeType Visit(Node.Binding binding)
        {
            if (_currentModule.Exports.ContainsKey(binding.Identifier.Lexeme))
                return _currentModule.Exports[binding.Identifier.Lexeme].Type;

            return _currentSymbolTable[binding.Identifier.Lexeme];
        }

        public RhymeType Visit(Node.Grouping grouping)
        {
            throw new NotImplementedException();
        }

        public RhymeType Visit(Node.CompilationUnit compilationUnit)
        {
            foreach(var unit in compilationUnit.Units)
            {
                Visit(unit);
            }
            return RhymeType.NoneType;
        }

        public RhymeType Visit(Node.FunctionCall callExpr)
        {
            var type = Visit(callExpr.Callee);

            if(type is RhymeType.Function)
            {
                var func_type = (RhymeType.Function)type;

                if (func_type.Parameters.Length == callExpr.Args.Length)
                {
                    for (int i = 0; i < func_type.Parameters.Length; i++)
                    {
                        var arg_type = Visit(callExpr.Args[i]);

                        if (arg_type != func_type.Parameters[i].Type)
                        {
                            Error(callExpr.Args[i].Position, $"Wrong argument type '{arg_type}', Expects: '{func_type.Parameters[i].Type}'");
                            return RhymeType.NoneType;
                        }
                    }
                    return func_type.ReturnType;
                }
                Error(callExpr.Position, $"{callExpr.Args.Length} arguments passed to the function and expects {func_type.Parameters.Length}");
                return RhymeType.NoneType;
            }
            Error(callExpr.Position, $"Uninvocable expression");
            return RhymeType.NoneType;
        }

        public RhymeType Visit(Node.Return returnStmt)
        {
            var return_type = Visit(returnStmt.RetrunExpression);

            if (return_type != _currentFunction.ReturnType)
                Error(returnStmt.Position, $"A value of type '{return_type}' can't be returned from a function of return type '{_currentFunction.ReturnType}'");

            return Visit(returnStmt.RetrunExpression);

        }

        public RhymeType Visit(Node.Get member)
        {
            throw new NotImplementedException();
        }

        public RhymeType Visit(Node.Directive directive)
        {

            return null;
        }

        public RhymeType Visit(Node.Import importStmt)
        {

            return RhymeType.NoneType;
        }

        #endregion


        #region Helpers
        (bool valid, RhymeType result) TypeEvaluate(RhymeType lhs, TokenType operatorToken, RhymeType rhs)
        {
            switch (operatorToken)
            {
                // Arithmetic
                case TokenType.Plus:
                case TokenType.Minus:
                case TokenType.Asterisk:
                case TokenType.Slash:
                case TokenType.Percent:
                    if (lhs is not RhymeType.Numeric || rhs is not RhymeType.Numeric)
                        return (false, RhymeType.NoneType);

                    if (lhs is RhymeType.Numeric && rhs is RhymeType.Numeric)
                        return (true, RhymeType.Numeric.Max((RhymeType.Numeric)lhs, (RhymeType.Numeric)rhs));

                    break;

                // Assignment
                case TokenType.Equal:
                    if (rhs == lhs)
                        return (true, rhs);

                    if (lhs is RhymeType.Primitive && rhs is RhymeType.Primitive)
                    {
                        if (lhs is RhymeType.Numeric lhsNum && rhs is RhymeType.Numeric rhsNum)
                        {
                            // Only assignment from lower numeric types to higher ones is allowed (e.g. u16 to f64)

                            bool valid = lhsNum > rhsNum;

                            if (valid)
                                return (valid, RhymeType.Numeric.Max(lhsNum, rhsNum));
                            else
                                return (false, RhymeType.NoneType);
                        }
                    }
                    break;

                // Comparison
                case TokenType.EqualEqual:
                case TokenType.NotEqual:
                case TokenType.GreaterThan:
                case TokenType.GreaterEqual:
                case TokenType.SmallerEqual:
                case TokenType.SmallerThan:
                    if (lhs == rhs)
                        return (true, RhymeType.Bol);
                    break;
                default:
                    return (false, RhymeType.NoneType);
            }

            return (false, RhymeType.NoneType);
        }

        #endregion

    }
}
