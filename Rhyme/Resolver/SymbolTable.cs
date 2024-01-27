using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;
using Rhyme.Parser;

namespace Rhyme.Resolver
{

    internal class Scope
    {
        Dictionary<Token, RhymeType> _symbols = new Dictionary<Token, RhymeType>();

        public Scope Enclosing { get; private set; }

        public bool Contains(Token token)
        {
            return _symbols.ContainsKey(token);
        }

        public Scope(Scope enclosingScope = null) { Enclosing = enclosingScope; }

        public bool Define(Token token, RhymeType type)
        {
            if (!_symbols.ContainsKey(token))
            {
                _symbols.Add(token, type);
                return true;
            }

            return false;
        }


        public RhymeType this[Token identifier]
        {
            get { return _symbols[identifier]; }
            set { _symbols[identifier] =  value; }
        }

    }
    internal class SymbolTable
    {
        Scope _current = new Scope();

        Stack<Scope> _scopeStack = new Stack<Scope>();
        List<Scope> _scopes = new List<Scope>();

        public void StartScope()
        {
            var new_scope = new Scope(_current);
            _scopeStack.Push(new_scope);
            _scopes.Add(new_scope);
            _current = new_scope;
        }

        public void EndScope()
        {
            _current = _scopeStack.Pop();
        }

        public void Define(Declaration declaration)
        {
            _current.Define(declaration.identifier, declaration.type);
        }

        public RhymeType this[Token identifier]
        {
            get => _current[identifier];
            set => _current[identifier] = value;
        }
    }
}
