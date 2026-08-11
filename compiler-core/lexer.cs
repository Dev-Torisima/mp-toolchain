using Item;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml.Linq;


namespace Prorigh.Compiler
{   
    internal class Lexer
    {
        public void Init()
        {
            _error = 0;
            _line = 1;
            _row = 1;
            _pos = 0;
        }

        public readonly string _code;

        public static readonly Token NullToken = new Token(TokenType.Unknown, "");

        public int _error = 0;

        public List<(int, int, int)> _warning = new List<(int, int, int)>();

        public int _line = 1;
        public int _row = 1;

        int _pos;

        bool _end => (_pos >= _code.Length) || (_pos is < 0);

        char _current => (_end ? '\0' : _code[_pos]);

        public Lexer(string code)
        {
            _code = code + "\n";
            _pos = 0;
        }

        public Token Next()
        {
            nint que = 0;
            while (true)
            {
                que = SkipComment();
                if (que is -1) return NullToken;
                if (que is 0 && SkipSpace() is 0)
                {
                    que = 0;
                    while (_current is '\n')
                    {
                        que++;

                        _pos--;
                        if (_current is '\r') _pos--;

                        bool flag = _current is not '\n' and not '\0';

                        _pos++;
                        if (_current is '\r') _pos++;

                        Advance();

                        if (flag) return new Token(TokenType.NewLine, "");
                    }

                    if (que is 0) break;
                }
            }

            Debug.WriteLine("Lex : " + _current);

            if (_current is '\0') return new Token(TokenType.EOF, "");

            else if (char.IsDigit(_current)) return ReadNumberOrDecimal();
            else if (char.IsLetter(_current) || _current is '_') return ReadIdentifierOrKeyword();
            else if (_current is '\'') return ReadChar();
            else if (_current is '\"') return ReadString();
            else return ReadSymbol();
        }

        public string GetParts()
        {
            char[] ch = new char[10];
            for (byte i = 0; i < 10; i++)
            {
                ch[i] = _current;
                _pos++;
            }
            _pos -= 10;
            return new string(ch);
        }

        public Token ReadIdentifierOrKeyword()
        {
            int _start = _pos;

            if (!char.IsLetter(_current) && _current is not '_')
            {
                _error = 0x102;
                return NullToken;
            }


            while (char.GetUnicodeCategory(_current) is UnicodeCategory.LetterNumber or UnicodeCategory.OtherLetter or UnicodeCategory.ModifierLetter or UnicodeCategory.DecimalDigitNumber or UnicodeCategory.ConnectorPunctuation or UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.LowercaseLetter or UnicodeCategory.UppercaseLetter || _current is '_')
            {
                Advance();
            }

            string text = _code.Substring(_start, _pos - _start);
            TokenType type;
            switch (text)
            {
                case "func" or "関数":
                    type = TokenType.Function;
                    text = "function";
                    break;
                case "def" or "定義":
                    type = TokenType.Def;
                    text = "def";
                    break;
                case "var" or "変数":
                    type = TokenType.Var;
                    text = "var";
                    break;
                case "if" or "もし":
                    type = TokenType.If;
                    break;
                case "elif" or "さらにもし":
                    type = TokenType.Elseif;
                    break;
                case "else" or "ほか":
                    type = TokenType.Else;
                    break;
                case "end" or "おわり":
                    type = TokenType.End;
                    break;
                case "loop" or "ループ":
                    type = TokenType.Loop;
                    break;
                case "break" or "抜ける":
                    type = TokenType.Break;
                    break;
                case "continue" or "続ける":
                    type = TokenType.Continue;
                    break;
                case "return" or "返す":
                    type = TokenType.Return;
                    break;
                case "is" or "が":
                    type = TokenType.Is;
                    break;
                case "true" or "はい":
                    type = TokenType.Boolean;
                    text = "true";
                    break;
                case "false" or "いいえ":
                    type = TokenType.Boolean;
                    text = "false";
                    break;
                case "char" or "文字":
                    type = TokenType.Type;
                    text = "char";
                    break;
                case "text" or "テキスト":
                    type = TokenType.Type;
                    text = "text";
                    break;
                case "type" or "タイプ":
                    type = TokenType.Type;
                    text = "type";
                    break;
                case "num" or "整数":
                    type = TokenType.Type;
                    text = "num";
                    break;
                case "decimal" or "小数":
                    type = TokenType.Type;
                    text = "decimal";
                    break;
                case "bool" or "真偽":
                    type = TokenType.Type;
                    text = "bool";
                    break;
                case "void" or "なし":
                    type = TokenType.Void;
                    text = "void";
                    break;
                case "typeof" or "タイプ取得":
                    type = TokenType.Typeof;
                    text = "typeof";
                    break;
                case "TextArray" or "テキスト配列":
                    type = TokenType.TextArray;
                    text = "TextArray";
                    break;
                default:
                    type = TokenType.Identifier;
                    break;
            }

            return new Token(type, text);
        }

        public Token ReadNumberOrDecimal()
        {
            int start = _pos;
            string text = "";
            bool Isdecimal = false;

            if (_current is '0')
            {
                char nex = Peek();

                if (nex is 'x' or 'X')
                {
                    Advance();
                    Advance();

                    if (!Uri.IsHexDigit(_current))
                    {
                        _error = 0x200;
                        return NullToken;
                    }

                    while (Uri.IsHexDigit(_current))
                    {
                        Advance();
                    }

                    try
                    {
                        text = Convert.ToInt64(_code.Substring(start + 2, _pos - start - 2), 16).ToString();
                    }
                    catch (Exception)
                    {
                        _error = 0x201;
                        return NullToken;
                    }
                    goto Ret;
                }
                if (nex is 'o' or 'O')
                {
                    Advance();
                    Advance();

                    if (_current is < '0' or > '7')
                    {
                        _error = 0x200;
                        return NullToken;
                    }

                    while (_current is >= '0' and <= '7')
                    {
                        Advance();
                    }

                    try
                    {
                        text = Convert.ToInt64(_code.Substring(start + 2, _pos - start - 2), 8).ToString();
                    }
                    catch (Exception)
                    {
                        _error = 0x201;
                        return NullToken;
                    }
                    goto Ret;
                }
                if (nex is 'b' or 'B')
                {
                    Advance();
                    Advance();

                    if (_current is not '0' and not '1')
                    {
                        _error = 0x200;
                        return NullToken;
                    }

                    while (_current is '0' or '1')
                    {
                        Advance();
                    }

                    try
                    {
                        text = Convert.ToInt64(_code.Substring(start + 2, _pos - start - 2), 2).ToString();
                    }
                    catch (Exception)
                    {
                        _error = 0x201;
                        return NullToken;
                    }
                    goto Ret;
                }
            }

            while (char.IsDigit(_current))
            {
                Advance();
            }

            if (_current is '.')
            {
                Isdecimal = true;
                Advance();

                while (char.IsDigit(_current))
                {
                    Advance();
                }
            }

            if (_current is 'e' or 'E')
            {
                Isdecimal = true;
                Advance();

                if (_current is '+' or '-')
                {
                    Advance();
                }

                if (!char.IsDigit(_current))
                {
                    _error = 0x204;
                    return NullToken;
                }

                while (char.IsDigit(_current))
                {
                    Advance();
                }

                double _uu = 0;
                if (!double.TryParse(_code.Substring(start, _pos - start), out _uu))
                {
                    _error = 0x203;
                    return NullToken;
                }

                text = _uu.ToString();

                if (_current is 'h' or 'H' or 'f' or 'F' or 'd' or 'D')
                {
                    Advance();
                }
            }
            else
            {
                text = _code.Substring(start, _pos - start);

                if (_current is 'h' or 'H' or 'f' or 'F' or 'd' or 'D')
                {
                    Isdecimal = true;
                    Advance();
                }
            }

            if (Isdecimal)
            {
                double _uu = 0;
                if (!double.TryParse(text, out _uu))
                {
                    _error = 0x203;
                    return NullToken;
                }

                text = "0x" + BitConverter.ToString(BitChanger.ToBytes(_uu, ByteOrder.BigEndian)).Replace("-", "");
            }
            else
            {
                long _uu = 0;
                if (!long.TryParse(text, out _uu))
                {
                    _error = 0x203;
                    return NullToken;
                }
            }

        Ret:
            return new Token(Isdecimal ? TokenType.Decimal : TokenType.Number, text);
        }

        public nint SkipSpace()
        {
            nint i = 0;
            while (_current is ' ' or '　' or '\t' or '\r')
            {
                Advance();
                i++;
            }
            return i;
        }

        public nint SkipComment()
        {
            if (_current is '/')
            {
                if (Peek() is '/')
                {
                    Advance();
                    Advance();
                    if (_current is '/')
                    {
                        Advance();

                        int ct = 0;
                        while (true)
                        {
                            if (_current is '\0')
                            {
                                _error = 0x106;
                                return -1;
                            }

                            if (_current is '/') ct++;
                            else ct = 0;

                            Advance();

                            if (ct is 3) break;
                        }
                    }
                    else
                    {
                        while (_current is not '\n' and not '\0')
                        {
                            Advance();
                        }
                    }
                    return 1;
                }

                return 0;
            }

            return 0;
        }

        public Token ReadChar()
        {
            Advance();
            Token rt;
            if (_current is '\\')
            {
                Advance();

                switch (_current)
                {
                    case '\'':
                        Advance();
                        rt = new Token(TokenType.Char, "\'");
                        break;
                    case '\\':
                        Advance();
                        rt = new Token(TokenType.Char, "\\");
                        break;
                    case '\"':
                        rt = new Token(TokenType.Char, "\"");
                        Advance();
                        break;
                    case '0':
                        rt = new Token(TokenType.Char, "\0");
                        Advance();
                        break;
                    case 'n':
                        rt = new Token(TokenType.Char, "\n");
                        Advance();
                        break;
                    case 'r':
                        rt = new Token(TokenType.Char, "\r");
                        Advance();
                        break;
                    case 't':
                        rt = new Token(TokenType.Char, "\t");
                        Advance();
                        break;
                    case 'u':
                        Advance();
                        char[] str = new char[4];
                        ulong sf = 0;
                        while (sf is < 4)
                        {
                            str[sf] = (_current);
                            Advance();
                            sf++;
                        }
                        try
                        {
                            sf = Convert.ToUInt64(new string(str), 16);
                            rt = new Token(TokenType.Char, char.ConvertFromUtf32((int)(sf & 0xFFFFFFFF)));
                        }
                        catch (Exception)
                        {
                            _error = 0x105;
                            return NullToken;
                        }
                        break;
                    case 'U':
                        Advance();
                        char[] str2 = new char[8];
                        ulong sf2 = 0;
                        while (sf2 is < 8)
                        {
                            str2[sf2] = (_current);
                            Advance();
                            sf2++;
                        }
                        try
                        {
                            sf2 = Convert.ToUInt64(new string(str2), 16);
                            rt = new Token(TokenType.Char, char.ConvertFromUtf32((int)(sf2 & 0xFFFFFFFF)));
                        }
                        catch (Exception)
                        {
                            _error = 0x105;
                            return NullToken;
                        }
                        break;
                    default:
                        _error = 0x108;
                        return NullToken;
                }
            }
            else if (_current is '\'')
            {
                rt = new Token(TokenType.Char, "\0");
            }
            else if (_current is >= (char)0xD800 and <= (char)0xDBFF)
            {
                char ty = _current;
                Advance();
                rt = new Token(TokenType.Char, new string(new char[] { ty, _current }));
                Advance();
            }
            else
            {
                rt = new Token(TokenType.Char, _current.ToString());
                Advance();
            }

            if (_current is not '\'')
            {
                _error = 0x109;
                return NullToken;
            }
            Advance();
            return rt;
        }

        public Token ReadString()
        {
            Advance();
            List<char> text = new List<char>(10);

            while (_current is not '\"' and not '\0')
            {
                if (_current is '\\')
                {
                    Advance();

                    switch (_current)
                    {
                        case '\\':
                            Advance();
                            text.Add('\\');
                            break;
                        case 'n':
                            Advance();
                            text.Add('\n');
                            break;
                        case 'r':
                            Advance();
                            text.Add('\r');
                            break;
                        case '0':
                            Advance();
                            text.Add('\0');
                            break;
                        case 't':
                            Advance();
                            text.Add('\t');
                            break;
                        case '\"':
                            Advance();
                            text.Add('\"');
                            break;
                        case '\'':
                            Advance();
                            text.Add('\'');
                            break;
                        case 'u':
                            Advance();
                            char[] str = new char[4];
                            ulong sf = 0;
                            while (sf is < 4)
                            {
                                str[sf] = (_current);
                                Advance();
                                sf++;
                            }
                            try
                            {
                                sf = Convert.ToUInt64(new string(str), 16);
                                text.AddRange(char.ConvertFromUtf32((int)(sf & 0xFFFFFFFF)).ToArray());
                            }
                            catch (Exception)
                            {
                                _error = 0x105;
                                return NullToken;
                            }
                            break;
                        case 'U':
                            Advance();
                            char[] str2 = new char[8];
                            ulong sf2 = 0;
                            while (sf2 is < 8)
                            {
                                str2[sf2] = (_current);
                                Advance();
                                sf2++;
                            }
                            try
                            {
                                sf2 = Convert.ToUInt64(new string(str2), 16);
                                text.AddRange(char.ConvertFromUtf32((int)(sf2 & 0xFFFFFFFF)).ToArray());
                            }
                            catch (Exception)
                            {
                                _error = 0x105;
                                return NullToken;
                            }
                            break;
                        default:
                            text.Add('\\');
                            text.Add(_current);
                            _warning.Add((0x107, _line, _row));
                            Advance();
                            break;
                    }
                }
                else
                {
                    text.Add(_current);
                    Advance();
                }
            }

            if (_current is not '\"')
            {
                _error = 0x104;
                return NullToken;
            }
            Advance();
            return new Token(TokenType.String, new string(text.ToArray()));
        }

        public Token ReadSymbol()
        {
            switch (_current)
            {
                case '(' or '（':
                    Advance();
                    return new Token(TokenType.Paren_L, "(");
                case ')' or '）':
                    Advance();
                    return new Token(TokenType.Paren_R, ")");
                case '[' or '［':
                    Advance();
                    return new Token(TokenType.Bracket_L, "[");
                case ']' or '］':
                    Advance();
                    return new Token(TokenType.Bracket_R, "]");

                case ':' or '：':
                    Advance();
                    return new Token(TokenType.Colon, ":");
                case ',' or '、':
                    Advance();
                    return new Token(TokenType.Comma, ",");

                case '+' or '＋':
                    Advance();
                    if (_current is '=' or '＝')
                    {
                        Advance();
                        return new Token(TokenType.PlusAssign, "+=");
                    }
                    return new Token(TokenType.Plus, "+");
                case '-' or 'ー':
                    Advance();
                    if (_current is '=' or '＝')
                    {
                        Advance();
                        return new Token(TokenType.MinusAssign, "-=");
                    }
                    return new Token(TokenType.Minus, "-");
                case '*' or '×' or '＊':
                    Advance();
                    if (_current is '=' or '＝')
                    {
                        Advance();
                        return new Token(TokenType.StarAssign, "*=");
                    }
                    return new Token(TokenType.Star, "*");
                case '/' or '÷' or '／':
                    Advance();
                    if (_current is '=' or '＝')
                    {
                        Advance();
                        return new Token(TokenType.SlashAssign, "/=");
                    }
                    return new Token(TokenType.Slash, "/");
                case '%' or '％':
                    Advance();
                    if (_current is '=' or '＝')
                    {
                        Advance();
                        return new Token(TokenType.PercentAssign, "%=");
                    }
                    return new Token(TokenType.Percent, "%");
                case '&' or '＆':
                    Advance();
                    if (_current is '=' or '＝')
                    {
                        Advance();
                        return new Token(TokenType.AmpAssign, "&=");
                    }
                    return new Token(TokenType.Amp, "&");
                case '|' or '｜':
                    Advance();
                    if (_current is '=' or '＝')
                    {
                        Advance();
                        return new Token(TokenType.BarAssign, "|=");
                    }
                    return new Token(TokenType.Bar, "|");
                case '=' or '＝':
                    Advance();
                    if (_current is '=' or '＝')
                    {
                        Advance();
                        return new Token(TokenType.Equal, "==");
                    }
                    return new Token(TokenType.Assign, "=");
                case '!' or '！':
                    Advance();
                    if (_current is '=' or '＝')
                    {
                        Advance();
                        return new Token(TokenType.NotEqual, "!=");
                    }
                    return new Token(TokenType.Bang, "!");
                case '>' or '＞':
                    Advance();
                    if (_current is '=' or '＝')
                    {
                        Advance();
                        return new Token(TokenType.GreaterEqual, ">=");
                    }
                    return new Token(TokenType.Greater, ">");
                case '<' or '＜':
                    Advance();
                    if (_current is '=' or '＝')
                    {
                        Advance();
                        return new Token(TokenType.LessEqual, "<=");
                    }
                    return new Token(TokenType.Less, "<");
            }

            _error = 0x100;
            return NullToken;
        }

        public char Peek()
        {
            _pos++;
            char ret = _current;
            _pos--;
            return ret;
        }

        public void Advance(bool sd = true)
        {
            if (_current is not '\0')
            {
                if (_current is '\n')
                {
                    _line++;
                    _row = 1;
                }
                else _row++;

                _pos++;
            }
        }
    }

}
