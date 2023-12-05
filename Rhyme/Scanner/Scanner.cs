using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhyme.Scanner
{

    internal class Scanner
    {
        string _source;
        int _line;
        int _pos;

        public Scanner(string source)
        {
            _source = source;
        }

        public static Scanner FromFile(string filePath)
        {
            return new Scanner(File.ReadAllText(filePath));
        }

        public IEnumerable<Token> Scan()
        {
            _line = 1;

            for (_pos = 0; _pos < _source.Length; _pos++)
            {
                switch (Current)
                {
                    case '/':
                        if (Peek() == '/') Comment();
                        if (Peek() == '*') Comment(true);
                        break;

                    case '(': yield return new Token("(", TokenType.LeftParen, _line); break;
                    case ')': yield return new Token(")", TokenType.RightParen, _line); break;
                    case '{': yield return new Token("{", TokenType.LeftCurly, _line); break;
                    case '}': yield return new Token("}", TokenType.RightCurly, _line); break;

                    case ';': yield return new Token(";", TokenType.Semicolon, _line); break;

                    case '*': yield return new Token("*", TokenType.Star, _line); break;

                    case '+': yield return new Token("+", TokenType.Plus, _line); break;
                    case '-': yield return new Token("-", TokenType.Minus, _line); break;

                    case '#': yield return new Token("#", TokenType.Hash, _line); break;

                    case '=':
                        if (Peek() == '=') { yield return new Token("==", TokenType.EqualEqual, _line); break; }
                        yield return new Token("=", TokenType.Equal, _line); break;

                    case '!':
                        if (Peek() == '=') { yield return new Token("!=", TokenType.NotEqual, _line); break; }
                        yield return new Token("!", TokenType.Bang, _line); break;

                    case '\n': _line++; break;


                }

                if (char.IsLetter(Current) || Current == '_')  yield return Identifier();
                if (char.IsDigit(Current)) { yield return Number(); }

            }


        }

        void Comment(bool multiline = false)
        {
            if (multiline)
            {
                _pos += 2; // /*
                while (!AtEnd && Current != '*' && Peek() != '/')
                {
                    if (Current == '\n') _line++;
                    _pos++;
                }

                _pos += 2;
            }
            else
            {
                while (Peek() != '\n')
                    _pos++;
            }


        }

        Token Identifier()
        {
            int start = _pos;
            Advance();

            while (char.IsLetter(Current) || Current == '_' || char.IsDigit(Current))
                _pos++;

            _pos--;
            return new Token(_source.Substring(start, _pos - start + 1), TokenType.Identifier, _line);
        }

        Token Number()
        {
            int start = _pos;
            Advance();

            while (!AtEnd && char.IsNumber(Current))
                _pos++;

            _pos--;
            return new Token(_source.Substring(start, _pos - start + 1), TokenType.Integer, _line, int.Parse(_source.Substring(start, _pos - start + 1)));
        }

        void Advance()
        {
            _pos++;
        }

        bool AtEnd { get => _pos >= _source.Length - 1; }
        char Current { get => _source[_pos]; }

        char Peek()
        {
            if (!AtEnd)
                return _source[_pos + 1];
            else
                return '\0';
        }

    }
}
