using Rhyme.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;
using Rhyme.Parser;
using Rhyme.Resolver;
using System.Collections;
using LLVMSharp;

namespace Rhyme.TypeChecker
{
    
    internal class TypeChecker : Node.IVisitor<RhymeType>, ICompilerPass
    {
        

        private readonly IReadOnlySymbolTable _symbolTable;

        public bool HadError { get; private set; }

        List<PassError> _errors = new List<PassError>();

        public IReadOnlyCollection<PassError> Errors { get; private set; }

        internal enum Operator
        {
            Assignment,
            Arithmetic,
            Bitwise
        }

        public TypeChecker(IReadOnlySymbolTable symbolTable)
        {
            _symbolTable = symbolTable;
            _symbolTable.Reset();
            Errors = _errors;   
        }

        public bool Check(Node.CompilationUnit program)
        {
            check(program);
            return true;
        }

        RhymeType check(Node node)
        {
            return node.Accept(this);
        }
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

            var lhs = check(binaryExpr.Left);
            var rhs = check(binaryExpr.Right);

            var eval_result = TypeEvaluate(lhs, binaryExpr.Op.Type, rhs);

            if (eval_result.valid)
                return eval_result.result;
            else
                return null;
                //Error(binaryExpr.Op, $"Can't apply operator '{binaryExpr.Op.Lexeme}' on types '{lhs}' and '{rhs}'");

            return RhymeType.NoneType;
        }

        public RhymeType Visit(Node.Unary unaryExpr)
        {
            throw new NotImplementedException();
        }

        public RhymeType Visit(Node.Block blockExpr)
        {
            _symbolTable.OpenScope();

            foreach(var exprstmt in blockExpr.ExpressionsStatements)
                check(exprstmt);

            _symbolTable.CloseScope();

            return new RhymeType.Function(RhymeType.Void);
        }

        RhymeType.Function _currentFunction = null;
        public RhymeType Visit(Node.BindingDeclaration bindingDecl)
        {
            var decl = bindingDecl.Declaration;
            var decl_type = _symbolTable[decl.Identifier];

            if(bindingDecl.Expression is Node.Block block)
            {
                _currentFunction = (RhymeType.Function)bindingDecl.Declaration.Type;

                check(block);

                if(bindingDecl.Declaration.Type is not RhymeType.Function)
                {
                    Error(bindingDecl.Position, $"Binding '{bindingDecl.Declaration.Identifier}' is not a function type");
                    return RhymeType.NoneType;
                }

                return RhymeType.NoneType;
            }

            var rhs_type = check(bindingDecl.Expression);

            if (rhs_type == RhymeType.NoneType)
                return RhymeType.NoneType;

            if (!decl_type.Equals(rhs_type)){
                Error(bindingDecl.Position, $"Can not implicitly convert type '{rhs_type}' to a binding of type '{decl_type}'");
            }
            
            return RhymeType.NoneType;
        }

        void Error(Position at, string message)
        {
            HadError = true;
            _errors.Add(new PassError(at.Line, at.Start, at.Length, message));
        }
        public RhymeType Visit(Node.If ifStmt)
        {
            return RhymeType.Bol;
        }

        public RhymeType Visit(Node.Assignment assignment)
        {
            var rhs = check(assignment.Expression);

            if (assignment.Assignee is not Node.Binding)
                throw new Exception("Unassignable target.");

            var lhs = _symbolTable[((Node.Binding)assignment.Assignee).Identifier.Lexeme];
            var eval_result = TypeEvaluate(rhs, TokenType.Equal, lhs);

            if (eval_result.valid)
            {
                return eval_result.result;
            }
            else
            {
                var token = ((Node.Binding)assignment.Assignee).Identifier;
                Error(token.Position, $"Can't implicitly assign value of type '{rhs}' to a binding of type '{lhs}'");
                return RhymeType.NoneType;
            }
        }

        
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
                        if (lhs is RhymeType.Numeric lhs_num && rhs is RhymeType.Numeric rhs_num)
                        {
                            // Only assignment from lower numeric types to higher ones is allowed (e.g. u16 to f64)

                            bool valid = lhs_num > rhs_num;

                            if (valid)
                                return (valid, RhymeType.Numeric.Max(lhs_num, rhs_num));
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

        public RhymeType Visit(Node.Binding binding)
        {
            return _symbolTable[binding.Identifier.Lexeme];
        }

        public RhymeType Visit(Node.Grouping grouping)
        {
            throw new NotImplementedException();
        }

        public RhymeType Visit(Node.CompilationUnit compilationUnit)
        {
            foreach(var unit in compilationUnit.Units)
            {
                check(unit);
            }
            return RhymeType.NoneType;
        }

        public RhymeType Visit(Node.FunctionCall callExpr)
        {
            var type = check(callExpr.Callee);

            if(type is RhymeType.Function)
            {
                var func_type = (RhymeType.Function)type;

                if (func_type.Parameters.Length == callExpr.Args.Length)
                {
                    for (int i = 0; i < func_type.Parameters.Length; i++)
                    {
                        var arg_type = check(callExpr.Args[i]);

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
            var return_type = check(returnStmt.RetrunExpression);

            if (return_type != _currentFunction.ReturnType)
                Error(returnStmt.Position, $"A value of type '{return_type}' can't be returned from a function of return type '{_currentFunction.ReturnType}'");

            return check(returnStmt.RetrunExpression);

        }
    }
}
