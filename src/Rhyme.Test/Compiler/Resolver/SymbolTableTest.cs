using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Parsing;
using Rhyme.TypeSystem;
using Rhyme.Resolving;

namespace Rhyme.Test.Compiler.Resolving
{
    [TestClass]
    public class SymbolTableTest
    {

        [TestMethod]
        public void AlreadyExists1()
        {
            SymbolTable table = new SymbolTable();
            table.Define(new Declaration(RhymeType.I32, "x"));
            var result = table.Define(new Declaration(RhymeType.I32, "x"));
            Assert.AreEqual(result, ResolutionResult.AlreadyExists);
        }

        [TestMethod]
        public void Nesting1()
        {
            SymbolTable table = new SymbolTable();
            table.Define(new Declaration(RhymeType.I32, "x"));
            table.StartScope();
            table.Define(new Declaration(RhymeType.I32, "y"));
            table.EndScope();

            var navigator = table.GetNavigator();
            navigator.NextScope();

            Assert.AreEqual(RhymeType.I32, navigator["y"]);

        }

        [TestMethod]
        public void Nesting2()
        {
            SymbolTable table = new SymbolTable();
            table.Define(new Declaration(RhymeType.I32, "x"));
            table.StartScope();
            table.Define(new Declaration(RhymeType.I32, "y"));
            table.StartScope();
            table.Define(new Declaration(RhymeType.F64, "f"));
            table.EndScope();
            table.EndScope();

            var navigator = table.GetNavigator();
            navigator.NextScope();
            navigator.NextScope();

            Assert.AreEqual(RhymeType.F64, navigator["f"]);

        }
    }
}
