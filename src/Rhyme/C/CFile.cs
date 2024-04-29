using System;

using Rhyme.Parsing;
using Rhyme.TypeSystem;

using ClangSharp;
using ClangSharp.Interop;

namespace Rhyme.C
{
    internal class CFile 
    {
        string _filePath;
        public CFile(string filePath)
        {
            _filePath = filePath;

            using var index = CXIndex.Create();

            using var unit = CXTranslationUnit.Parse(index, _filePath, [], ReadOnlySpan<CXUnsavedFile>.Empty, CXTranslationUnit_Flags.CXTranslationUnit_None);
            
            foreach(var msg in unit.DiagnosticSet)
            {
                Console.WriteLine($"{msg.Severity} ({msg.Location}): {msg.CategoryText}");
            }

            unsafe { 
                unit.Cursor.VisitChildren(cursorVisitor, default);
            }

            ThisDeclarations = thisDecl.ToArray();
            CompleteDeclarations = compDecl.ToArray();

        }

        /// <summary>
        /// Gets all the declarations after preprocessing the included headers
        /// </summary>
        public Declaration[] CompleteDeclarations { get; private set; }

        /// <summary>
        /// Gets the declarations declared only in the header
        /// </summary>
        public Declaration[] ThisDeclarations { get; private set; }


        Declaration GetFunctionFromCursor(CXCursor cursor)
        {
            var name = cursor.Spelling.ToString();

            List<Declaration> parameters = new();
            unsafe
            {
                // Parameters
                cursor.VisitChildren((cursor, parent, data) =>
                {
                    if (cursor.kind != CXCursorKind.CXCursor_ParmDecl)
                        return CXChildVisitResult.CXChildVisit_Continue;

                    parameters.Add(GetDeclarationFromCursor(cursor));
                    return CXChildVisitResult.CXChildVisit_Recurse;
                }, default);
            }
            return Declaration.CreateFunction(name, RhymeTypeFromCursorType(cursor.ReturnType), parameters.ToArray());
        }

        Declaration GetDeclarationFromCursor(CXCursor cursor)
        {
            var name = cursor.Spelling.ToString();

            switch (cursor.Kind)
            {
                case CXCursorKind.CXCursor_FunctionDecl:
                    return GetFunctionFromCursor(cursor);
                case CXCursorKind.CXCursor_ParmDecl:
                    return new Declaration(RhymeTypeFromCursorType(cursor.Type), name);
                case CXCursorKind.CXCursor_VarDecl:
                    return new Declaration(RhymeTypeFromCursorType(cursor.Type), name);
                default:
                    return null;
            }
        }

        RhymeType RhymeTypeFromCursorType(CXType type)
        {
            switch (type.ToString())
            {
                case "int":
                    return RhymeType.I32;
                case "void":
                    return RhymeType.Void;
                default:
                    return RhymeType.NoneType;
            }
        }

        List<Declaration> thisDecl = new();
        List<Declaration> compDecl = new();

        unsafe CXChildVisitResult cursorVisitor(CXCursor cursor, CXCursor parent, void* data)
        {
            Declaration decl = GetDeclarationFromCursor(cursor);

            if(decl == null)
            {
                return CXChildVisitResult.CXChildVisit_Continue;
            }

            if (cursor.Location.IsFromMainFile)
            {
                thisDecl.Add(decl);
                compDecl.Add(decl);
                

            }
            else
            {
                compDecl.Add(decl);
            }

            return CXChildVisitResult.CXChildVisit_Continue;
        }

    }
}
