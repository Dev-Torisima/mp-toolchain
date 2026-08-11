using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.PortableExecutable;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;
using Item;

namespace Prorigh
{
    public enum TokenType
    {
        Identifier,
        Number,
        Decimal,
        Char,
        String,
        Boolean,
        Type,
        Void,


        Function,
        Def,
        Var,
        If,
        Elseif,
        Else,
        End,
        Loop,
        Break,
        Continue,
        Return,

        Is,
        Not, //未使用
        And, //未使用
        Or,  //未使用

        //()
        Paren_L,
        Paren_R,

        //[]
        Bracket_L,
        Bracket_R,

        Colon,
        Comma,
        Bang,

        Plus, Minus, Star, Slash, Percent, Amp, Bar,
        PlusAssign, MinusAssign, StarAssign, SlashAssign, PercentAssign, AmpAssign, BarAssign,

        Equal, NotEqual, GreaterEqual, Greater, LessEqual, Less,

        Assign,// =

        Typeof,

        TextArray,

        NewLine,

        Unknown,

        EOF,
    }

    public enum VarType
    {
        Unknown,
        Char = 1,
        Number = 2,
        Decimal = 3,
        Boolean = 4,
        Type = 5,
        Char_Array = 16,
        Number_Array,
        Decimal_Array,
        Boolean_Array,
        Type_Array,

        String,

        /// <summary>特殊値／引数のみ／scalar／いかなる値</summary>
        Value = 14,//非対応／将来の拡張のために確保

        /// <summary>特殊値／引数のみ／array／いかなる値</summary>
        Array = 0xff,

        /// <summary>特殊値／引数と返り値／scalar／直前のValue,Array引数から推測</summary>
        Valable = 15,

        /// <summary>特殊値／引数と返り値／array／直前のValue,Array引数から推測</summary>
        Arrable = 0xfe,
    }

    public class Token
    {
        public TokenType _type;
        public string _text;

        public Token()
        {
            _type = TokenType.Unknown;
            _text = "";
        }

        public Token(TokenType type, string text)
        {
            _type = type;
            _text = text;
        }

        public override string ToString()
        {
            return "(" + _type.ToString() + ", " + _text + ")";
        }
    }

}
