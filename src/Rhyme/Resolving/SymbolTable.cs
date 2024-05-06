using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CommandLine;
using System.Runtime.InteropServices;

using Rhyme.Scanner;
using Rhyme.Parsing;
using Rhyme.TypeSystem;
using System.Reflection;

namespace Rhyme.Resolving
{

    public enum ResolutionResult
    {
        Defined,
        Shadowed,
        AlreadyExists,
    }
    public class Scope
    {
        List<string> _symbols = new ();

        public Scope Enclosing { get; private set; }

        public bool Contains(string token) {
            if (_symbols.Contains(token))
                return true;

            if (Enclosing != null) 
                return Enclosing.Contains(token);

            return false;
        }


        string get(string token)
        {
            if (_symbols.Contains(token))
            {
                return _symbols.Find(s => s == token);
            }

            if (Enclosing != null) 
                return Enclosing.get(token);
                
            return null;
           
        }
        public Scope(Scope enclosingScope = null) { Enclosing = enclosingScope; }

        public ResolutionResult Define(string token)
        {
            if (Enclosing != null && Enclosing.get(token) != null)
                return ResolutionResult.Shadowed;

            if (!_symbols.Contains(token))
            {
                _symbols.Add(token);
                return ResolutionResult.Defined;
            }



            return ResolutionResult.AlreadyExists;
        }


        public string this[string identifier]
        {
            get { return get(identifier); }
        }

    }

    public interface IReadOnlySymbolTable
    {
        public void OpenScope();
        public void CloseScope();
        public void Reset();
        public bool Contains(string identifier);
        public RhymeType this[string identifier] { get; }
    }
    public class SymbolTable
    {
        Scope _current = new();

        List<Scope> _scopes = new();



        public SymbolTable()
        {
            _scopes.Add(_current);
        }

        public void StartScope()
        {
            var new_scope = new Scope(_current);
            _scopes.Add(new_scope);
            _current = new_scope;
        }

        public void EndScope()
        {
            _current = _current.Enclosing;
        }



        public ResolutionResult Define(string identifer)
        {
            return _current.Define(identifer);
        }

        public bool Contains(string identifier)
        {
            return _current.Contains(identifier);
        }
    }

}
