using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhyme.Scanner
{

    internal class Scanner : ICompilerPass
    {
        string _source;
        int _line;
        int _pos;
        List<PassError> _errors = new List<PassError>();

        public bool HadError { get; private set; }
        public IReadOnlyCollection<PassError> Errors { get; private set; }

        Dictionary<string, TokenType> _keywords = new Dictionary<string, TokenType>() {
            { "if", TokenType.If },
            { "using", TokenType.Using },
            { "for", TokenType.For },
            { "while", TokenType.While },

            { "var", TokenType.Var },

            // Primitive Types
            { "void", TokenType.Void }, {"str", TokenType.Str },
            { "u8", TokenType.U8 }, { "u16", TokenType.U16 }, { "u32", TokenType.U32 }, { "u64", TokenType.U64 },
            { "i8", TokenType.I8 }, { "i16", TokenType.I16 }, { "i32", TokenType.I32 }, { "i64", TokenType.I64 },

        };

        public Scanner(string source)
        {
            _source = source;
            Errors = _errors;
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
                TokenType token_type = TokenType.None;

                var start = _pos;
                switch (Current)
                {
                    case '/':
                        Advance();
                        if (Match('/'))
                        {
                            Comment();
                            continue;
                        }
                        if (Match('*'))
                        {
                            Comment(true);
                            continue;
                        }
                        break;

                    case '(': token_type = TokenType.LeftParen; break;
                    case ')': token_type = TokenType.RightParen; break;
                    case '{': token_type = TokenType.LeftCurly; break;
                    case '}': token_type = TokenType.RightCurly; break;

                    
                    case '>':
                        Advance();
                        if (Match('=', false)) 
                        {
                            token_type = TokenType.GreaterEqual;
                            break;
                        }
                        token_type = TokenType.GreaterThan;
                        break;

                    case '<':
                        Advance();
                        if (Match('=', false)) 
                        { 
                            token_type = TokenType.SmallerEqual; 
                            break;
                        }
                        token_type = TokenType.SmallerThan; 
                        break;

                    case ';': token_type = TokenType.Semicolon; break;

                    case '*': token_type = TokenType.Asterisk; break;

                    case '+': token_type = TokenType.Plus; break;
                    case '-': token_type = TokenType.Minus; break;
                    case '#': token_type = TokenType.Hash; break;

                    case ',': token_type = TokenType.Comma; break;

                    case '=':
                        Advance();
                        if (Match('=', false)) { 
                            token_type = TokenType.EqualEqual; break; }
                        token_type = TokenType.Equal; break;

                    case '!':
                        Advance();
                        if (Match('=', false)) { token_type = TokenType.NotEqual; break; }
                        token_type = TokenType.Bang; break;

                    case '"':
                    case '\'':
                        yield return String(Current);
                        continue;
                    case '\n':
                        _line++;
                        continue;

                    case ' ':
                    case '\r':
                    case '\t':
                        continue;
                }

                if (token_type != TokenType.None) { 
                    yield return new Token(_source.Substring(start, _pos - start + 1), token_type, _line, start, _pos, null);
                }
                else if (char.IsLetter(Current) || Current == '_')
                {
                    yield return Identifier();
                } 
                else if (char.IsDigit(Current))
                {
                    yield return Number();
                }
                else
                {
                    HadError = true;
                    Error(_line, _pos, 1, "Unexpected character");
                }


                // TODO: Pass Errors.
               
            }


        }

        #region Helpers
        bool Match(char c, bool advance = true)
        {
            if (!AtEnd)
            {
                if (Current != c)
                    return false;

                if (advance)
                    Advance();

                return true;
            }
            return false;
        }
        void Advance(int steps = 1)
        {
            if (!AtEnd)
            {
                _pos += steps;
            }
        }

        bool AtEnd { get => _pos >= _source.Length; }
        char Current
        {
            get
            {
                if (!AtEnd)
                    return _source[_pos];
                else
                    return '\0';
            }
        }

        bool TryMatch(params char[] characters)
        {
            foreach (var c in characters)
            {
                if (!AtEnd)
                {
                    if (c != Current)
                        return false;

                    Advance();
                    continue;
                }
            }

            return true;
        }

        void Error(int line, int start, int length, string message)
        {
            HadError = true;
            _errors.Add(new PassError(line, start, length, message));
        }
        #endregion

        #region Lexical Rules
        void Comment(bool multiline = false)
        {
            if (multiline)
            {
                Advance();
                while (!TryMatch('*', '/'))
                {
                    if (AtEnd)
                    {

                    }
                    if (Current == '\n') _line++;
                    Advance();
                }

                Advance(2);
            }
            else
            {
                while (!AtEnd && !Match('\n', false))
                    Advance();

                _line++;
            }


        }
        Token String(char stringQuote)
        {
            int start = _pos;
            Advance();

            while (Current != stringQuote)
            {
                if (AtEnd)
                {
                    Error(_line, start, _pos - start, "Unterminated string.");
                    return null;
                }

                Advance();
            }

            //Advance();

            string lexeme = _source.Substring(start, _pos - start + 1);

            return new Token(lexeme, TokenType.String, _line, start, _pos, lexeme.Substring(1, lexeme.Length - 2));
        }
        Token Identifier()
        {

            int start = _pos;
            Advance();

            while (!AtEnd && (char.IsLetterOrDigit(Current) || Current == '_'))
                Advance();

            string lexeme = _source.Substring(start, _pos - start);

            _pos--;

            if (_keywords.ContainsKey(lexeme))
                return new Token(lexeme, _keywords[lexeme], _line, start, _pos);

            return new Token(lexeme, TokenType.Identifier, _line, start, _pos);
        }

        Token Number()
        {
            int start = _pos;
            Advance();

            while (!AtEnd && char.IsNumber(Current))
                Advance();

            _pos--;
            return new Token(_source.Substring(start, _pos - start + 1), TokenType.Integer, _line, start, _pos, int.Parse(_source.Substring(start, _pos - start + 1)));
        }
        #endregion

    }
}
