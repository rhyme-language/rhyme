using Rhyme.Scanner;
using Rhyme.Parsing;
using ClangSharp;

namespace Rhyme.TypeSystem
{

    public class TypeChecker : Node.IVisitor<RhymeType>, ICompilerPass
    {
        
        private List<PassError> _errors = new();

        CompilationUnit _unit;

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
        void Error(Position at, string message)
        {

            Console.WriteLine(message);
            HadError = true;
            _errors.Add(new PassError(_unit.FilePath, at, message));
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


        #region Expressions
        public RhymeType Visit(Node.Literal literalExpr)
        {

            switch (literalExpr.ValueToken.Type)
            {
                case TokenType.Integer:
                    return _typedAST[literalExpr] = RhymeType.I64;
                case TokenType.String:
                    return _typedAST[literalExpr] = RhymeType.NoneType;
                case TokenType.Float:
                    return _typedAST[literalExpr] = RhymeType.NoneType;
            }

            return RhymeType.NoneType;
        }

        Dictionary<string, RhymeType> _locals = new();

        public RhymeType Visit(Node.Binding binding)
        {
            var bindName = binding.Identifier.Lexeme;
            var type = _locals.ContainsKey(bindName) ? _locals[bindName] : RhymeType.NoneType;
            _typedAST[binding] = type;
            return type;
        }
        public RhymeType Visit(Node.Binary binaryExpr)
        {
            var lhs = Visit(binaryExpr.Left);
            var rhs = Visit(binaryExpr.Right);
            var result = lhs.ApplyOperator(rhs, binaryExpr.Op);

            _typedAST[binaryExpr] = result;

            return result;
        }

        #endregion


        #region Statements

        #endregion


        #region Declarations
        public RhymeType Visit(Node.TopLevelDeclaration topLevelDeclaration)
        {
            return Visit(topLevelDeclaration.DeclarationNode);
        }

        public RhymeType Visit(Node.BindingDeclaration bindDecl)
        {
            if(bindDecl.Type is Node.IdentifierType identifierType)
            {
                if(identifierType.Identifier.Lexeme == "var")
                {
                    if(bindDecl.Declarators.Length == 1)
                    {
                        if (bindDecl.Declarators[0].Initializer == null)
                        {
                            Error(bindDecl.Position, "Implicitly-Typed declaration must have an initialization value");
                            return RhymeType.NoneType;
                        }
                        else
                        {
                            var expType = Visit(bindDecl.Declarators[0].Initializer);
                            _typedAST[bindDecl.Declarators[0]] = expType;
                            _locals[bindDecl.Declarators[0].Identifier.Lexeme] = expType;
                            return expType;
                        }
                    }
                    else
                    {
                        Error(bindDecl.Position, $"Implicitly-Typed declaration can't applid for multiple declarators");
                        return RhymeType.NoneType;
                    }
                }
                else
                {
                    var type = RhymeType.FromToken(identifierType.Identifier);
                    foreach(var declarator in bindDecl.Declarators)
                    {
                        if(declarator.Initializer != null)
                        {
                            var initType = Visit(declarator.Initializer);

                            if (!type.Equals(initType))
                            {
                                Error(declarator.Position, $"Can't initialize a binding of type '{type}' to value of type '{initType}'");
                                return RhymeType.NoneType;
                            }
                        }
                        
                        _typedAST[declarator] = type;
                        _locals[declarator.Identifier.Lexeme] = type;                     
                    }
                }
            }

            return RhymeType.NoneType;
        }

        public RhymeType Visit(Node.Type type)
        {
            if(type is Node.IdentifierType idType)
            {
                return RhymeType.FromToken(idType.Identifier);
            }

            return RhymeType.NoneType;
        }
        public RhymeType Visit(Node.FunctionDeclaration funcDecl)
        {
            _locals.Clear();
            var retType = Visit(funcDecl.ReturnType);

            foreach(var param in funcDecl.Parameters)
            {
                var paramType = Visit(param.Type);
                _locals.Add(param.Identifier.Lexeme, paramType);
                _typedAST[param] = paramType;
            }
            foreach(var blockStmt in funcDecl.Block.ExpressionsStatements)
            {
                if(blockStmt is Node.Return returnStmt)
                {
                    var returnValueType = Visit(returnStmt.RetrunExpression);
                    if (!returnValueType.Equals(retType))
                    {
                        Error(returnStmt.Position, $"Can't return a value of type '{returnValueType}', '{retType}' Expected");
                    }
                }
                Visit(blockStmt);
            }
            
            var funcType = new RhymeType.Function(Visit(funcDecl.ReturnType), funcDecl.Parameters.Select(p => Visit(p.Type)).ToArray());

            _typedAST[funcDecl] = funcType;

            return funcType;
        }
        #endregion

       
        public RhymeType Visit(Node.CompilationUnit compilationUnit)
        {
            foreach(var topDecl in compilationUnit.TopLevelDeclarations)
            {
                Visit(topDecl);
            }

            return RhymeType.NoneType;
        }

        #endregion
        #region Helpers
        #endregion

    }
}
