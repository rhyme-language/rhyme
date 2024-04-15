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
        Dictionary<string, RhymeType> _symbols = new Dictionary<string, RhymeType>();

        public Scope Enclosing { get; private set; }

        public bool Contains(string token) {
            if (_symbols.ContainsKey(token))
                return true;

            if (Enclosing != null) 
                return Enclosing.Contains(token);

            return false;
        }


        RhymeType get(string token)
        {
            if (_symbols.ContainsKey(token))
                return _symbols[token];

            if (Enclosing != null) 
                return Enclosing.get(token);
                
            return RhymeType.NoneType;
           
        }
        public Scope(Scope enclosingScope = null) { Enclosing = enclosingScope; }

        public ResolutionResult Define(string token, RhymeType type)
        {
            if (Enclosing != null && Enclosing.get(token) != RhymeType.NoneType)
                return ResolutionResult.Shadowed;

            if (!_symbols.ContainsKey(token))
            {
                _symbols.Add(token, type);
                return ResolutionResult.Defined;
            }



            return ResolutionResult.AlreadyExists;
        }


        public RhymeType this[string identifier]
        {
            get { return get(identifier); }
            set { _symbols[identifier] =  value; }
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
        Scope _current = new Scope();

        List<Scope> _scopes = new List<Scope>();



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



        public ResolutionResult Define(Declaration declaration)
        {
            return _current.Define(declaration.Identifier, declaration.Type);
        }

        public bool Contains(string identifier)
        {
            return _current.Contains(identifier);
        }

        public SymbolTableNavigator GetNavigator()
        {
            return new SymbolTableNavigator(_scopes.ToArray());
        }
    }


    public class SymbolTableNavigator
    {

        Scope[] _scopes;
        int _index = 0;

        public SymbolTableNavigator(Scope[] scopes)
        {
            _scopes = scopes;  
        }

        public void NextScope()
        {
            _index++;
        }

        public void PreviousScope()
        {
            _index--;
        }

        public void Reset()
        {
            _index = 0;
        }

        public RhymeType this[string identifier]
        {
            get => _scopes[_index][identifier];
            set => _scopes[_index][identifier] = value;
        }
    }

}
