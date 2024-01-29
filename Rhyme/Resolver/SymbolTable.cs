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
        Dictionary<string, RhymeType> _symbols = new Dictionary<string, RhymeType>();

        public Scope Enclosing { get; private set; }

        public bool Contains(Token token)
        {
            return _symbols.ContainsKey(token.Lexeme);
        }

        public Scope(Scope enclosingScope = null) { Enclosing = enclosingScope; }

        public bool Define(Token token, RhymeType type)
        {
            if (!_symbols.ContainsKey(token.Lexeme))
            {
                _symbols.Add(token.Lexeme, type);
                return true;
            }

            return false;
        }


        public RhymeType this[string identifier]
        {
            get { return _symbols[identifier]; }
            set { _symbols[identifier] =  value; }
        }

    }

    internal interface IReadOnlySymbolTable
    {
        public void OpenScope();
        public void CloseScope();

        public void Reset();
        public RhymeType this[Token identifier] { get; }
    }
    internal class SymbolTable :  IReadOnlySymbolTable
    {
        Scope _current = new Scope();

        Stack<Scope> _scopeStack = new Stack<Scope>();
        List<Scope> _scopes = new List<Scope>();

        int _index = 0;

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

        public void OpenScope()
        {
            _index++;
        }

        public void CloseScope()
        {
            _index--;
        }

        public void Reset()
        {
            _index = 0; 
        }

        public bool Define(Declaration declaration)
        {
            return _current.Define(declaration.Identifier, declaration.Type);
        }

        public RhymeType this[Token identifier]
        {
            get => _scopes[_index][identifier.Lexeme];
            set => _scopes[_index][identifier.Lexeme] = value;
        }
    }
}
