using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.PortableExecutable;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;
using Item;

namespace Prorigh.Reporter
{
    // ---
    // public class Header
    // ---
    // 「@compiler」から一部を抜粋
    // ---

    public class Header
    {
        public void Set(Parser paser)
        {
            for (int i = 0; i < Data1.Length; i++)
            {
                foreach (var item2 in Data1[i].Item1)
                {
                    paser.global_name.Add(item2);
                    paser.global_name2.Add((Data1[i].Item2));
                }
            }
            for (int i = 0; i < Data2.Length; i++)
            {
                foreach (var item2 in Data2[i].Item1)
                {
                    paser.global_name.Add(item2);
                    paser.global_name2.Add((Data2[i].Item2));
                }
            }
        }

        //関数の登録順の修正は未遂行
        public Header()
        {
            Data = new(Data1.Length + Data2.Length);
            for (int i = 0; i < Data1.Length; i++)
            {
                Data.Add(new(Data1[i], ""));
            }
            for (int i = 0; i < Data2.Length; i++)
            {
                Data.Add(new(Data2[i], ""));
            }
        }

        internal List<HeadNode> Data;


        //順番をそろえて登録
        public static (string[], (int, int, VarType, object?), string[]?)[] Data1 = new (string[], (int, int, VarType, object?), string[]?)[]
        {
#region
            (new string[]
                {
                    "TextToNum", "テキストを整数にする",
                }, (0, 0, VarType.Number, new VarType[1]{ VarType.String }),
            new string[0]{  }),
            (new string[]
                {
                    "NumToText", "整数をテキストにする",
                }, (0, 0, VarType.Char_Array, new VarType[1]{ VarType.Number }),
            new string[0]{  }),
            (new string[]
                {
                    "CharToText", "文字をテキストにする",
                }, (0, 0, VarType.Char_Array, new VarType[1]{ VarType.Char }),
            new string[0]{  }),
            (new string[]
                {
                    "TextToChar", "テキストを文字にする",
                }, (0, 0, VarType.Char, new VarType[1]{ VarType.Char_Array }),
            new string[0]{  }),
            (new string[]
                {
                    "NumToDecimal", "整数を小数にする",
                }, (0, 0, VarType.Decimal, new VarType[1]{ VarType.Number }),
            new string[0]{  }),
            (new string[]
                {
                    "DecimalToNum", "小数を整数にする",
                }, (0, 0, VarType.Number, new VarType[1]{ VarType.Decimal }),
            new string[0]{  }),
            (new string[]
                {
                    "Delay", "待つ",
                }, (0, 0, VarType.Unknown, new VarType[1]{ VarType.Number }),
            new string[0]{  }),
            (new string[]
                {
                    "SetTitle", "タイトルの設定",
                }, (0, 0, VarType.Unknown, new VarType[1]{ VarType.Char_Array }),
            new string[0]{  }),
            (new string[]
                {
                    "Clear", "クリア",
                }, (0, 0, VarType.Unknown, new VarType[0]),
            new string[0]{  }),
            (new string[]
                {
                    "Write", "書き込む",
                }, (0, 0, VarType.Unknown, new VarType[1]{ VarType.String }),
            new string[0]{  }),
            (new string[]
                {
                    "Read", "読み込む",
                }, (0, 0, VarType.String, new VarType[1]{ VarType.Number }),
            new string[0]{  }),
            (new string[]
                {
                    "len", "要素数",
                }, (0, 0, VarType.Number, new VarType[1]{ VarType.Array }),
            new string[0]{  }),
            (new string[]
                {
                    "Random", "ランダム",
                }, (0, 0, VarType.Number, new VarType[2]{ VarType.Number, VarType.Number }),
            new string[0]{  }),
            (new string[]
                {
                    "IsNaN", "非数か",
                }, (0, 0, VarType.Boolean, new VarType[1]{ VarType.Decimal }),
            new string[0]{  }),


            (new string[]
                {
                    "Log", "対数",
                }, (0, 0, VarType.Decimal, new VarType[1]{ VarType.Decimal }),
            new string[2]{ "ln2", "NaN" }),
            (new string[]
                {
                    "Exp", "指数",
                }, (0, 0, VarType.Decimal, new VarType[1]{ VarType.Decimal }),
            new string[1]{ "ln2" }),
            (new string[]
                {
                    "Pow", "べき乗",
                }, (0, 0, VarType.Decimal, new VarType[2]{ VarType.Decimal, VarType.Decimal }),
            new string[2]{ "Log", "Exp" }),
            (new string[]
                {
                    "Root", "ルート",
                }, (0, 0, VarType.Decimal, new VarType[1]{ VarType.Decimal }),
            new string[0]{  }),
            (new string[]
                {
                    "Sin", "サイン",
                }, (0, 0, VarType.Decimal, new VarType[1]{ VarType.Decimal }),
            new string[5]{ "tau","hpi","NegZero","pi", "PosZero" }),
            (new string[]
                {
                    "Cos", "コサイン",
                }, (0, 0, VarType.Decimal, new VarType[1]{ VarType.Decimal }),
            new string[5]{ "tau","hpi","NegZero","pi", "PosZero" }),
            (new string[]
                {
                    "Tan", "タンジェント",
                }, (0, 0, VarType.Decimal, new VarType[1]{ VarType.Decimal }),
            new string[4]{ "Sin", "Cos", "PosInfinity", "NegInfinity" }),


            (new string[]
                {
                    "TextToDecimal", "テキストを小数にする",
                }, (0, 0, VarType.Decimal, new VarType[1]{ VarType.Char_Array }),
            new string[10]{ "NaN","len","PosInfinity","CharToText","TextToNum","NumToDecimal","Pow", "PosZero", "NegZero", "IsNaN", }),
            (new string[]
                {
                    "DecimalToText", "小数をテキストにする",
                }, (0, 0, VarType.String, new VarType[1]{ VarType.Decimal }),
            new string[8]{ "IsNaN","PosInfinity","NegInfinity","DecimalToNum","NumToText","NumToDecimal","PosZero","NegZero", }),

            (new string[]
                {
                    "EvenRound", "偶数丸め",
                }, (0, 0, VarType.Decimal, new VarType[1]{ VarType.Decimal }),
            new string[0]{  }),
            (new string[]
                {
                    "Floor", "切り捨て",
                }, (0, 0, VarType.Decimal, new VarType[1]{ VarType.Decimal }),
            new string[0]{  }),
            (new string[]
                {
                    "Ceil", "切り上げ",
                }, (0, 0, VarType.Decimal, new VarType[1]{ VarType.Decimal }),
            new string[0]{  }),
            (new string[]
                {
                    "Truncate", "ゼロ丸め",
                }, (0, 0, VarType.Decimal, new VarType[1]{ VarType.Decimal }),
            new string[0]{  }),
            (new string[]
                {
                    "Round", "四捨五入",
                }, (0, 0, VarType.Decimal, new VarType[1]{ VarType.Decimal }),
            new string[2]{ "NaN", "NegZero" }),
            (new string[]
                {
                    "ReplaceItem", "要素の置き換え",
                }, (0, 0, VarType.Arrable, new VarType[3]{ VarType.Array, VarType.Valable, VarType.Valable }),
            new string[0]{  }),

            (new string[]
                {
                    "SearchText", "テキストの場所",
                }, (0, 0, VarType.Number, new VarType[3]{ VarType.String, VarType.String, VarType.Number }),
            new string[0]{  }),

            (new string[]
                {
                    "CountItem", "要素の個数",
                }, (0, 0, VarType.Number, new VarType[2]{ VarType.Array, VarType.Valable }),
            new string[0]{  }),
            (new string[]
                {
                    "CountText", "テキストの個数",
                }, (0, 0, VarType.Number, new VarType[2]{ VarType.String, VarType.String }),
            new string[1]{ "SearchText" }),
            (new string[]
                {
                    "ReplaceText", "テキストの置き換え",
                }, (0, 0, VarType.String, new VarType[3]{ VarType.String, VarType.String, VarType.String }),
            new string[3]{ "CountText", "len", "SearchText" }),


            (new string[]
                {
                    "SearchItem", "要素の場所",
                }, (0, 0, VarType.Number, new VarType[3]{ VarType.Array, VarType.Valable, VarType.Number }),
            new string[0]{  }),


            (new string[]
                {
                    "RemoveItem", "要素の削除",
                }, (0, 0, VarType.Arrable, new VarType[2]{ VarType.Array, VarType.Valable }),
            new string[1]{ "CountItem" }),
            (new string[]
                {
                    "RemoveText", "テキストの削除",
                }, (0, 0, VarType.String, new VarType[2]{ VarType.String, VarType.String }),
            new string[1]{ "ReplaceText" }),
            (new string[]
                {
                    "CreateFile", "ファイル作成",
                }, (0, 0, VarType.Unknown, new VarType[1]{ VarType.String }),
            new string[0]{  }),
            (new string[]
                {
                    "WriteFile", "ファイル書き込み",
                }, (0, 0, VarType.Unknown, new VarType[2]{ VarType.String, VarType.String }),
            new string[0]{  }),
            (new string[]
                {
                    "ReadFile", "ファイル読み込み",
                }, (0, 0, VarType.String, new VarType[1]{ VarType.String }),
            new string[0]{  }),

            (new string[]
                {
                    "DeleteItem", "要素の消去",
                }, (0, 0, VarType.Arrable, new VarType[2]{ VarType.Array, VarType.Number }),
            new string[0]{  }),

            (new string[]
                {
                    "GetFileSize", "ファイルサイズの取得",
                }, (0, 0, VarType.Number, new VarType[1]{ VarType.String }),
            new string[0]{  }),
            (new string[]
                {
                    "FileExists", "ファイルが存在するか",
                }, (0, 0, VarType.Boolean, new VarType[1]{ VarType.String }),
            new string[0]{  }),

            (new string[]
                {
                    "LoadCharArray", "文字配列の読み込み",
                }, (0, 0, VarType.Char_Array, new VarType[1]{ VarType.String }),
            new string[0]{  }),
            (new string[]
                {
                    "LoadNumArray", "整数配列の読み込み",
                }, (0, 0, VarType.Number_Array, new VarType[1]{ VarType.String }),
            new string[1]{ "LoadCharArray" }),
            (new string[]
                {
                    "LoadDecimalArray", "小数配列の読み込み",
                }, (0, 0, VarType.Decimal_Array, new VarType[1]{ VarType.String }),
            new string[1]{ "LoadCharArray" }),
            (new string[]
                {
                    "LoadBoolArray", "真偽配列の読み込み",
                }, (0, 0, VarType.Boolean_Array, new VarType[1]{ VarType.String }),
            new string[1]{ "LoadCharArray" }),
            (new string[]
                {
                    "LoadTypeArray", "タイプ配列の読み込み",
                }, (0, 0, VarType.Type_Array, new VarType[1]{ VarType.String }),
            new string[1]{ "LoadCharArray" }),
            (new string[]
                {
                    "SaveArray", "配列の保存",
                }, (0, 0, VarType.Unknown, new VarType[2]{ VarType.String, VarType.Array }),
            new string[0]{  }),

            (new string[]
                {
                    "AddItem", "要素の追加",
                }, (0, 0, VarType.Arrable, new VarType[3]{ VarType.Array, VarType.Valable, VarType.Number }),
            new string[0]{  }),
            (new string[]
                {
                    "AppendItem", "要素の末尾追加",
                }, (0, 0, VarType.Arrable, new VarType[2]{ VarType.Array, VarType.Valable }),
            new string[0]{  }),

            (new string[]
                {
                    "ClipArray", "配列の切り取り",
                }, (0, 0, VarType.Arrable, new VarType[3]{ VarType.Array, VarType.Number, VarType.Number }),
            new string[0]{  }),
            (new string[]
                {
                    "ReverseArray", "配列の反転",
                }, (0, 0, VarType.Arrable, new VarType[1]{ VarType.Array }),
            new string[0]{  }),

            (new string[]
                {
                    "UpperChar", "文字の大文字化",
                }, (0, 0, VarType.Char, new VarType[1]{ VarType.Char }),
            new string[0]{  }),
            (new string[]
                {
                    "LowerChar", "文字の小文字化",
                }, (0, 0, VarType.Char, new VarType[1]{ VarType.Char }),
            new string[0]{  }),

            (new string[]
                {
                    "GetYear", "年の取得",
                }, (0, 0, VarType.Number, new VarType[0]{  }),
            new string[0]{  }),
            (new string[]
                {
                    "GetMonth", "月の取得",
                }, (0, 0, VarType.Number, new VarType[0]{  }),
            new string[0]{  }),
            (new string[]
                {
                    "GetDayOfWeek", "曜日の取得",
                }, (0, 0, VarType.Number, new VarType[0]{  }),
            new string[0]{  }),
            (new string[]
                {
                    "GetDay", "日の取得",
                }, (0, 0, VarType.Number, new VarType[0]{  }),
            new string[0]{  }),
            (new string[]
                {
                    "GetHour", "時の取得",
                }, (0, 0, VarType.Number, new VarType[0]{  }),
            new string[0]{  }),
            (new string[]
                {
                    "GetMinute", "分の取得",
                }, (0, 0, VarType.Number, new VarType[0]{  }),
            new string[0]{  }),
            (new string[]
                {
                    "GetSecond", "秒の取得",
                }, (0, 0, VarType.Number, new VarType[0]{  }),
            new string[0]{  }),
            (new string[]
                {
                    "GetMilliseconds", "ミリ秒の取得",
                }, (0, 0, VarType.Number, new VarType[0]{  }),
            new string[0]{  }),

            (new string[]
                {
                    "UpperText", "テキストの大文字化",
                }, (0, 0, VarType.String, new VarType[1]{ VarType.String }),
            new string[0]{  }),
            (new string[]
                {
                    "LowerText", "テキストの小文字化",
                }, (0, 0, VarType.String, new VarType[1]{ VarType.String }),
            new string[0]{  }),
#endregion

            (new string[]
                {
                    "TextArray_Length", "テキスト配列_要素数",
                }, (0, 0, VarType.Number, new VarType[1]{ VarType.String }),
            new string[2]{ "TextArray_MarkChar", "CountItem",  }),
            (new string[]
                {
                    "TextArray_AppendItem", "テキスト配列_要素の末尾追加",
                }, (0, 0, VarType.String, new VarType[2]{ VarType.String, VarType.String }),
            new string[1]{ "TextArray_MarkText",  }),
            (new string[]
                {
                    "TextArray_AddItem", "テキスト配列_要素の追加",
                }, (0, 0, VarType.String, new VarType[3]{ VarType.String, VarType.String, VarType.Number }),
            new string[3]{ "TextArray_MarkChar", "TextArray_MarkText", "ClipArray",  }),
            (new string[]
                {
                    "TextArray_CountItem", "テキスト配列_要素の個数",
                }, (0, 0, VarType.Number, new VarType[2]{ VarType.String, VarType.String }),
            new string[3]{ "TextArray_MarkText", "CountText", "SearchText",  }),
            (new string[]
                {
                    "TextArray_SearchItem", "テキスト配列_要素の場所",
                }, (0, 0, VarType.Number, new VarType[3]{ VarType.String, VarType.String, VarType.Number }),
            new string[3]{ "TextArray_MarkChar", "TextArray_MarkText", "SearchText",  }),
            (new string[]
                {
                    "TextArray_GetItem", "テキスト配列_要素の取得",
                }, (0, 0, VarType.String, new VarType[2]{ VarType.String, VarType.Number }),
            new string[2]{ "TextArray_MarkChar", "ClipArray",  }),
            (new string[]
                {
                    "TextArray_SetItem", "テキスト配列_要素の設定",
                }, (0, 0, VarType.String, new VarType[3]{ VarType.String, VarType.Number, VarType.String }),
            new string[3]{ "TextArray_MarkChar", "TextArray_MarkText", "ClipArray",  }),
            (new string[]
                {
                    "TextArray_ReplaceItem", "テキスト配列_要素の置き換え",
                }, (0, 0, VarType.String, new VarType[3]{ VarType.String, VarType.String, VarType.String }),
            new string[3]{ "TextArray_MarkText", "ReplaceText", "ClipArray",  }),
            (new string[]
                {
                    "TextArray_RemoveItem", "テキスト配列_要素の削除",
                }, (0, 0, VarType.String, new VarType[2]{ VarType.String, VarType.String }),
            new string[3]{ "TextArray_MarkText", "ReplaceText", "ClipArray",  }),
            (new string[]
                {
                    "TextArray_DeleteItem", "テキスト配列_要素の消去",
                }, (0, 0, VarType.String, new VarType[2]{ VarType.String, VarType.Number }),
            new string[2]{ "TextArray_MarkChar", "ClipArray",  }),
            (new string[]
                {
                    "TextArray_SplitText", "テキスト配列_テキストの分割",
                }, (0, 0, VarType.String, new VarType[2]{ VarType.String, VarType.String }),
            new string[2]{ "TextArray_MarkText", "ReplaceText",  }),

            (new string[]
                {
                    "TextArray_ToText", "テキスト配列_テキストにする",
                }, (0, 0, VarType.String, new VarType[1]{ VarType.String }),
            new string[3]{ "ClipArray", "TextArray_MarkText", "ReplaceText",  }),
            (new string[]
                {
                    "TextArray_ReverseArray", "テキスト配列_配列の反転",
                }, (0, 0, VarType.String, new VarType[1]{ VarType.String }),
            new string[3]{ "TextArray_Length", "TextArray_GetItem", "TextArray_AppendItem",  }),
            (new string[]
                {
                    "TextArray_ClipArray", "テキスト配列_配列の切り取り",
                }, (0, 0, VarType.String, new VarType[3]{ VarType.String, VarType.Number, VarType.Number }),
            new string[2]{ "TextArray_MarkChar", "ClipArray",  }),


            (new string[]
                {
                    "ParseNumExpr", "整数式の解析",
                }, (0, 0, VarType.Number, new VarType[1]{ VarType.String }),
            new string[8]{ "TextToNum", "TextArray_ClipArray", "TextArray_Length", "TextArray_Default", "ClipArray", "TextArray_AppendItem", "CharToText", "TextArray_GetItem", }),
            (new string[]
                {
                    "ParseDecimalExpr", "小数式の解析",
                }, (0, 0, VarType.Decimal, new VarType[1]{ VarType.String }),
            new string[8]{ "TextToDecimal", "TextArray_ClipArray", "TextArray_Length", "TextArray_Default", "ClipArray", "TextArray_AppendItem", "CharToText", "TextArray_GetItem", }),

            (new string[]
                {
                    "GetTimeValue", "時間値の取得",
                }, (0, 0, VarType.Number, new VarType[0]{ }),
            new string[0]{  }),

        };

        public static (string[], (int, int, VarType, object?), string[]?)[] Data2 = new (string[], (int, int, VarType, object?), string[]?)[]
        {
            (new string[]
                {
                    "pi", "パイ",
                }, (0, 0, VarType.Decimal, null), null),
            (new string[]
                {
                    "tau", "タウ",
                }, (0, 0, VarType.Decimal, null), null),
            (new string[]
                {
                    "e",
                }, (0, 0, VarType.Decimal, null), null),
            (new string[]
                {
                    "hpi",
                }, (0, 0, VarType.Decimal, null), null),
            (new string[]
                {
                    "NaN", "非数",
                }, (0, 0, VarType.Decimal, null), null),
            (new string[]
                {
                    "PosInfinity", "正の無限大",
                }, (0, 0, VarType.Decimal, null), null),
            (new string[]
                {
                    "NegInfinity", "負の無限大",
                }, (0, 0, VarType.Decimal, null), null),
            (new string[]
                {
                    "PosZero", "正のゼロ",
                }, (0, 0, VarType.Decimal, null), null),
            (new string[]
                {
                    "NegZero", "負のゼロ",
                }, (0, 0, VarType.Decimal, null), null),
            (new string[]
                {
                    "root2", "ルート２",
                }, (0, 0, VarType.Decimal, null), null),
            (new string[]
                {
                    "ln2",
                }, (0, 0, VarType.Decimal, null), null),
            (new string[]
                {
                    "ln3",
                }, (0, 0, VarType.Decimal, null), null),

            (new string[]
                {
                    "TextArray_MarkChar", "テキスト配列_区切り文字"
                }, (0, 0, VarType.Char, null), null),
            (new string[]
                {
                    "TextArray_MarkText", "テキスト配列_区切りテキスト"
                }, (0, 0, VarType.Char_Array, null), null),
            (new string[]
                {
                    "TextArray_Default", "テキスト配列_初期値"
                }, (0, 0, VarType.Char_Array, null), null),
        };

    }
}
