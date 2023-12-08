using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhyme.Scanner;

namespace Rhyme.Parser
{

    /*
grammar rhyme;

file
    : decl
    | using_stmt
    ;

decl
    : type IDENTIFIER ('=' init)? ';'
    ;

using_stmt: 'using' STRING_LITERAL 'as' IDENTIFIER ;

type : PRIMITIVE | func_type | IDENTIFIER;
func_type: type '(' ')';

init: ;
PRIMITIVE: ('u' | 'i') ('8' | '16' | '32' | '64') ;


     */

    internal class Parser
    {
        LinkedList<Token> _tokens;
        LinkedListNode<Token> _current;

        public Parser(IEnumerable<Token> Tokens) { 
            _tokens = new LinkedList<Token>(Tokens);
            _current = _tokens.First;
        }


        public Expression Parse()
        {
            return CompilationUnit();
        }

        private Expression CompilationUnit()
        {
            if (_current.Value.Type == TokenType.Using)
                return UsingStatement();

            return Declaration();
        }

        private Expression Declaration()
        {
            Expression type = Type();
            
            return null;
            
        }
        
        private void Advance()
        {
            _current = _current.Next;
        }

        public bool Match(TokenType type)
        {
            if(_current.Value.Type == type)
            {
                Advance();
                return true;
            }

            return false;
        }
        private Expression Type()
        {
            
            if (Match(TokenType.LeftParen)) // Tuple
            {

            } 
            switch (_current.Value.Type)
            {
                case TokenType.Void:
                case TokenType.U8:
                case TokenType.U16:
                case TokenType.U32:
                case TokenType.U64:
                    Console.WriteLine(_current.Value.Type);
                    Advance();
                    break;
            }
           
            if (Match(TokenType.LeftParen)) // Function
            {
               
            }

            if (Match(TokenType.Identifier)) // Binding
            {

            }

            return null;
        }


        private Expression UsingStatement()
        {
            throw new NotImplementedException();
        }
    }
}
