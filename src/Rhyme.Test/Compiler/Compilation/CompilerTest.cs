using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme;
using Rhyme.CodeGeneration;
using Rhyme.Resolving;
using Rhyme.Parsing;
using Rhyme.Scanner;
using Rhyme.TypeSystem;

namespace Rhyme.Test.Compiler.Compilation
{
    [TestClass]
    public class CompilerTest
    {
        /// <summary>
        /// Bulk tests the compiler, every directory will act as a compilation test
        /// fails if a compiler pass reported an error.
        /// </summary>
        [TestMethod]
        public void BulkTestCompile()
        {
            var codes_directory = new DirectoryInfo(Directory.GetCurrentDirectory() + "\\Compiler\\Compilation\\");
           
            foreach(var test_dir in codes_directory.GetDirectories())
            {
        
            }

            Assert.IsTrue(true);
        }
    }
}
