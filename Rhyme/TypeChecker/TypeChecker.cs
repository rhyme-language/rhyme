using Rhyme.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;
using Rhyme.Parser;
using Rhyme.Resolver;

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
            if (literalExpr.ValueToken.Value is int)
                return RhymeType.U32;

            if (literalExpr.ValueToken.Value is string)
                return RhymeType.Str;

            
            return RhymeType.NoneType;
        }

        public RhymeType Visit(Node.Binary binaryExpr)
        {
            throw new NotImplementedException();
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

        public RhymeType Visit(Node.BindingDeclaration bindingDecl)
        {
            var rhs_type = check(bindingDecl.expression);
            var decl = bindingDecl.Declaration;
            var decl_type = _symbolTable[decl.Identifier];
            if (!decl_type.Equals(rhs_type)){
                Error(decl.Identifier.Line, decl.Identifier.Start, decl.Identifier.Lexeme.Length,
                    $"Can not implicitly convert type '{rhs_type}' to type '{decl_type}'");
            }
            
            return RhymeType.NoneType;
        }

        void Error(Token error_token, string message)
        {
            Error(error_token.Line, error_token.Start, error_token.Lexeme.Length, message);
        }
        void Error(int line, int start, int length, string message)
        {
            HadError = true;
            _errors.Add(new PassError(line, start, length, message));
        }
        public RhymeType Visit(Node.If ifStmt)
        {
            throw new NotImplementedException();
        }

        public RhymeType Visit(Node.Assignment assignment)
        {
            var rhs = check(assignment.expression);
            var lhs = _symbolTable[assignment.binding];
            var eval_result = TypeEvaluate(rhs, Operator.Assignment, lhs);

            if (eval_result.valid)
            {
                return eval_result.result;
            }
            else
            {
                Error(assignment.binding,
                    $"Can't implicitly assign value of type '{rhs}' to a binding of type '{lhs}'");
                return RhymeType.NoneType;
            }
        }

        
        (bool valid, RhymeType result) TypeEvaluate(RhymeType lhs, Operator type, RhymeType rhs)
        {
            if(type == Operator.Assignment)
            {
                if (rhs == lhs)
                    return (true, rhs);

                if(lhs is RhymeType.Primitive && rhs is RhymeType.Primitive)
                {
                    if(lhs is RhymeType.Numeric && rhs is RhymeType.Numeric)
                    {
                        // Only assignment from lower numeric types to higher ones is allowed (e.g. u16 to f64)

                        bool valid = (RhymeType.Numeric)lhs > (RhymeType.Numeric)rhs;

                        if (valid)
                            return (valid, RhymeType.Numeric.Max((RhymeType.Numeric)lhs, (RhymeType.Numeric)rhs));
                        else
                            return (false, RhymeType.NoneType);
                    }
                }
            }

            return (false, RhymeType.NoneType);
        }

        public RhymeType Visit(Node.Binding binding)
        {
            if (_symbolTable.Contains(binding.Identifier))
                return _symbolTable[binding.Identifier];
            else
                return RhymeType.NoneType;
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
    }
}
