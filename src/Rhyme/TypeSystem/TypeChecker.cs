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
using Rhyme.Modularizing;

namespace Rhyme.TypeSystem
{

    public class TypeChecker : Node.IVisitor<RhymeType>, ICompilerPass
    {
        
        private List<PassError> _errors = new();

        CompilationUnit _unit;

        private readonly Modularizer[] _modules;
        private Modularizer _currentModule;

        private RhymeType.Function _currentFunction = null;


        Dictionary<Node, RhymeType> _typedAST = new();

        public enum Operator
        {
            Assignment,
            Arithmetic,
            Bitwise
        }

        public TypeChecker(CompilationUnit unit)
        {
            _unit = unit;
            Errors = _errors;   
        }

        public bool HadError { get; private set; }
        public IReadOnlyCollection<PassError> Errors { get; private set; }

        public IReadOnlyDictionary<Node, RhymeType> Check()
        {
            Visit(_unit.SyntaxTree);
            return _typedAST;
        }

        RhymeType Visit(Node node)
        {
            return node.Accept(this);
        }


        #region Visitors

        public RhymeType Visit(Node.TopLevelDeclaration topLevelDeclaration)
        {
            return Visit(topLevelDeclaration.DeclarationNode);
        }

        public RhymeType Visit(Node.Literal literalExpr)
        {
            switch (literalExpr.ValueToken.Type)
            {
                case TokenType.Integer:
                    return _typedAST[literalExpr] = RhymeType.I64;
                case TokenType.String:
                    return _typedAST[literalExpr] = RhymeType.Str;
                case TokenType.Float:
                    return _typedAST[literalExpr] = RhymeType.F64;
            }

            return RhymeType.NoneType;
        }

        public RhymeType Visit(Node.Binary binaryExpr)
        {
            throw new NotImplementedException();
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
