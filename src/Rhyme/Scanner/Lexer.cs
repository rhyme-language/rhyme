using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhyme.Scanner
{
    public class Lexer : ICompilerPass
    {
        string _source;
        int _line;
        int _pos;
        List<PassError> _errors = new();

        string path;

        public bool HadError { get; private set; }
        public IReadOnlyCollection<PassError> Errors { get; private set; }

        Dictionary<string, TokenType> _keywords = new() {
            { "if", TokenType.If },
            { "else", TokenType.Else },
            { "using", TokenType.Using },
            { "for", TokenType.For },
            { "while", TokenType.While },
            { "return", TokenType.Return },
            { "global", TokenType.Global },
            { "extern", TokenType.Extern },
            { "module", TokenType.Module },
            { "import", TokenType.Import },
            { "fn", TokenType.Fn },
            // Literals
            {"true", TokenType.True}, {"false", TokenType.False}, {"null", TokenType.Null}
        };

        public Lexer(string filePath)
        {
            path = filePath;
            _source = File.ReadAllText(filePath);
            Errors = _errors;
        }
        public IEnumerable<Token> Scan()
        {
            _line = 1;
            

            for (_pos = 0; _pos < _source.Length;_pos++)
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

                    case '(': token_type = TokenType.OpenParen; break;
                    case ')': token_type = TokenType.CloseParen; break;
                    case '{': token_type = TokenType.OpenCurly; break;
                    case '}': token_type = TokenType.CloseCurly; break;

                    
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
                    case '%': token_type = TokenType.Percent; break;

                    case '#': token_type = TokenType.Hash; break;

                    case ',': token_type = TokenType.Comma; break;
                    case '.': token_type = TokenType.Dot; break;
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
                    yield return new Token(_source.Substring(start, _pos - start + 1), token_type, new Position(_line, start, _pos), null);
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
            _errors.Add(new PassError(path, new Position(line, start, start + length), message));
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

                {

                }
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

            return new Token(lexeme, TokenType.String, new Position(_line, start, _pos), lexeme.Substring(1, lexeme.Length - 2));
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
                return new Token(lexeme, _keywords[lexeme], new Position(_line, start, _pos));

            return new Token(lexeme, TokenType.Identifier, new Position(_line, start, _pos));
        }

        Token Number()
        {
            int start = _pos;
            var type = TokenType.Integer;

            if (Current == '0')
            {
                var _base = 0;
                Advance();

                switch(Current)
                {
                    case 'b':
                        Advance();
                        while (!AtEnd && (Current == '0' || Current == '1'))
                            Advance();

                        _base = 2;
                        break;
                    case 'o':
                        Advance();
                        while (!AtEnd && (Current >= '0' || Current <= '7'))
                            Advance();

                        _base = 8;
                        break;
                    case 'x':
                        Advance();

                        while (!AtEnd && char.IsDigit(Current) || (char.ToLower(Current) >= 'a' && char.ToLower(Current) <= 'f'))
                            Advance();

                        _base = 16;
                        break;
                    default:
                        break;
                }
                _pos--;

                var len = _pos - start + 1;
                var lexeme = _source.Substring(start, len);
                if (lexeme == "0")
                    return new Token("0", TokenType.Integer, new Position(_line, start, _pos), 0);
               
                return new Token(lexeme, TokenType.Integer, new Position(_line, start, _pos), Convert.ToInt32(lexeme.Remove(0,2), _base));

            }
            Advance();


            bool dot = false;
            while ((!AtEnd && char.IsNumber(Current)) || Current == '.')
            {
                if(Current == '.')
                {
                    type = TokenType.Float;

                    if (!dot)
                    {
                        dot = true;
                        Advance();
                        continue;
                    }
                    
                    break;
                }
                Advance();
            }

            _pos--;

            var length = _pos - start + 1;
            return new Token(
                _source.Substring(start, length),
                type,
                new Position(_line, start, _pos),
                Value: type switch {
                    TokenType.Integer => int.Parse(_source.Substring(start, length)),
                    TokenType.Float => float.Parse(_source.Substring(start, length))
                }
            );
        }
        #endregion

    }
}
