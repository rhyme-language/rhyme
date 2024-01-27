using Rhyme.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Parser;

namespace Rhyme.Type_Checker
{
    internal class TypeChecker : Node.IVisitor<RhymeType>, ICompilerPass
    {
        public bool HadError => throw new NotImplementedException();

        public IReadOnlyCollection<PassError> Errors => throw new NotImplementedException();

        public RhymeType Check(Node program)
        {
            return program.Accept(this);
        }

        public RhymeType Visit(Node.Literal literalExpr)
        {
            return null; 
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
            throw new NotImplementedException();
        }

        public RhymeType Visit(Node.BindingDeclaration bindingDecl)
        {
            throw new NotImplementedException();
        }

        public RhymeType Visit(Node.If ifStmt)
        {
            throw new NotImplementedException();
        }

        public RhymeType Visit(Node.Assignment assignment)
        {
            throw new NotImplementedException();
        }

        public RhymeType Visit(Node.Binding binding)
        {
            throw new NotImplementedException();
        }

        public RhymeType Visit(Node.Grouping grouping)
        {
            throw new NotImplementedException();
        }

        public RhymeType Visit(Node.CompilationUnit compilationUnit)
        {
            foreach(var unit in compilationUnit.Units)
            {
                Check(unit);
            }
            return RhymeType.NoneType;
        }
    }
}
