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
    // public class Parser
    // ---
    // 「@compiler」を元に改造
    // ---

    public class Parser
    {
        public void Init()
        {
            _error.Clear();
            _warning.Clear();
            _lexer.Init();

            _peek = null;

            _cache1 = VarType.Unknown;

            local_post = 0;
            func_post = 0;
            Infunc = false;

            global_name.Clear();
            global_name2.Clear();

            local_name.Clear();
            local_name2.Clear();
            local_name.Add(new List<string>());
            local_name2.Add(new List<(int, int, VarType, object?)>());

            DF_warning.Clear();
        }

        public List<(int, int, int)> _error = new List<(int, int, int)>();


        public List<(int, int, int)> _warning = new List<(int, int, int)>();

        //DefFuncNode node -> string name に変更
        public List<(string, (int, int, int))> DF_warning = new();

        internal Lexer _lexer;
        public Token? _peek = null;

        public int line = 0;
        public int row = 0;

        internal VarType _cache1 = VarType.Unknown;
        internal int _cache2 = 0;
        internal bool Infunc = false;
        internal byte InLoop = 0;


        internal List<string> global_name = new List<string>();
        internal List<(int, int, VarType, object?)> global_name2 = new List<(int, int, VarType, object?)>();

        internal List<List<string>> local_name = new List<List<string>>();
        internal List<List<(int, int, VarType, object?)>> local_name2 = new List<List<(int, int, VarType, object?)>>();

        internal List<List<string>> func_name = new List<List<string>>();
        internal List<List<(int, int, VarType, object?)>> func_name2 = new List<List<(int, int, VarType, object?)>>();

        internal int local_post = 0;
        internal int func_post = 0;

        public Header _header;

        public Parser(Lexer lexer, Header header)
        {
            _lexer = lexer;
            _header = header;
        }

        public void Parse()
        {
            Init();
            _header.Set(this);

            Peek();
            while (_peek._type is not TokenType.EOF)
            {
                if (_peek._type is TokenType.Def) ParseDef();
                else if (_peek._type is TokenType.Function) ParseFunc();
                else if (_peek._type is TokenType.NewLine)
                {
                    Advance();
                    Peek();
                    continue;
                }
                else ParseStatement();

                Consume(TokenType.NewLine);
                Peek();

                if (_peek._type is TokenType.Unknown)
                {
                    Advance();
                    Peek();
                }
            }

            foreach (var item in DF_warning)
            {
                _warning.Add(item.Item2);
            }
        }

        //注意：最後にAdvance()が含まれる
        public void ParseDef()
        {
            Advance();
            Peek();
            if (_peek._type is TokenType.Function) ParseDef_Func();
            else if (_peek._type is TokenType.Type) ParseDef_Const();
            else SetError(0x410);
        }

        //注意：最後にAdvance()が含まれる
        public void ParseDef_Func()
        {
            Advance();
            Peek();
            if (_peek._type is not TokenType.Identifier)
            {
                SetError(0x420);
            }
            var name = _peek._text;

            (int, int) fqq = (_lexer._line, _lexer._row);

            var param = ParseFunc_Param();

            VarType[] ghhp = new VarType[param.Count];
            for (int i = 0; i < ghhp.Length; i++)
            {
                ghhp[i] = param[i].type;
            }

            Advance();
            if (Peek()._type is not TokenType.Colon) SetError(0x422);
            else Advance();

            Peek();
            if (_peek._type is not TokenType.Type and not TokenType.Void)
            {
                SetError(0x424);
            }

            var type = StrToType(_peek._text);

            Advance();

            if (Peek()._type is TokenType.Bracket_L)
            {
                Advance();
                if (Peek()._type is not TokenType.Bracket_R)
                {
                    SetError(0x477);
                }
                Advance();
                type = ValuToArry(type);
            }

            if (!RegisterName(name, type, ghhp, true))
            {
                SetError(0x463, fqq.Item1, fqq.Item2);
            }

            DF_warning.Add((name, (0x411, fqq.Item1, fqq.Item2)));
        }

        //注意：最後にAdvance()が含まれる
        public void ParseDef_Const()
        {
            var type = StrToType(ParseType());

            Peek();
            if (_peek._type is not TokenType.Identifier) SetError(0x430);
            var name = _peek._text;
            if (!RegisterName(name, type, null, true))
            {
                SetError(0x461);
            }

            Advance();
            if (Peek()._type is not TokenType.Assign) SetError(0x431);
            else Advance();

            ValueNode? _ah = ParseValue(type);
            if (_ah is not null)
            {
                if (SerLiteral(_ah)) SetError(0x433);
            }
        }

        public List<ParamNode> ParseFunc_Param()
        {
            List<ParamNode> hasl = new List<ParamNode>();
            Advance();
            if (Peek()._type is not TokenType.Paren_L)
            {
                SetError(0x421);
                return hasl;
            }

            Advance();
            Peek();

            while (_peek._type is TokenType.Type or TokenType.Comma)
            {
                if (_peek._type is TokenType.Comma)
                {
                    Advance();
                    if (Peek()._type is not TokenType.Type) break;
                }
                var pn = new ParamNode();

                pn.type = StrToType(Advance()._text);

                if (Peek()._type is TokenType.Bracket_L)
                {
                    Advance();
                    if (Peek()._type is not TokenType.Bracket_R)
                    {
                        SetError(0x477);
                    }
                    Advance();
                    Peek();
                    pn.type = ValuToArry(pn.type);
                }

                if (_peek._type is TokenType.Identifier)
                {
                    pn.name = _peek._text;
                    Advance();
                    Peek();
                    hasl.Add(pn);
                }
                else { SetError(0x400); break; }
            }

            if (_peek._type is not TokenType.Paren_R) SetError(0x421);

            return hasl;
        }

        //注意：最後にAdvance()が含まれる
        public void ParseFunc()
        {
            Advance();
            Peek();
            if (_peek._type is not TokenType.Identifier) SetError(0x420);
            var name = _peek._text;
            (int, int) fqq = (_lexer._line, _lexer._row);

            var param = ParseFunc_Param();

            Advance();
            if (Peek()._type is not TokenType.Colon) SetError(0x422);
            else Advance();

            Peek();
            if (_peek._type is not TokenType.Type and not TokenType.Void) SetError(0x424);
            var type = StrToType(_peek._text);

            Advance();

            if (Peek()._type is TokenType.Bracket_L)
            {
                Advance();
                if (Peek()._type is not TokenType.Bracket_R)
                {
                    SetError(0x477);
                }
                Advance();
                type = ValuToArry(type);
            }

            VarType[] ghhp = new VarType[param.Count];
            for (int i = 0; i < ghhp.Length; i++)
            {
                ghhp[i] = param[i].type;
            }

            //globalだけで、Nodeの取得
            int yuqq = global_name.IndexOf(name);
            int ghj = -1;
            if (yuqq is not -1)
            {
                int i = 0;

                foreach (var item in DF_warning)
                {
                    if (item.Item1 == name)
                    {
                        ghj = i;
                        break;
                    }
                    i++;
                }
            }

            if (ghj is -1)
            {
                if (!RegisterName(name, type, ghhp, true))
                {
                    SetError(0x463, fqq.Item1, fqq.Item2);
                }
            }
            else
            {
                if (global_name2[yuqq].Item3 != type)
                {
                    //タイプ不一致
                    SetError(0x415);
                }

                if (global_name2[yuqq].Item4 is null)
                {
                    //定数
                    SetError(0x41A);
                }

                if (global_name2[yuqq].Item4 is VarType[] aptu)
                {
                    if (aptu.Length != ghhp.Length)
                    {
                        //引数不一致
                        SetError(0x417);

                        if (aptu.Length < ghhp.Length)
                        {
                            var g34 = new VarType[ghhp.Length];
                            for (int i = 0; i < g34.Length; i++)
                            {
                                if (i < aptu.Length) g34[i] = aptu[i];
                                else g34[i] = ghhp[i];
                            }
                            aptu = g34;
                        }
                    }

                    for (int bh = 0; bh < aptu.Length; bh++)
                    {
                        if (aptu[bh] != ghhp[bh])
                        {
                            //引数タイプ不一致
                            SetError(0x416);
                        }
                    }
                }
                else
                {
                    //引数の存在がbug
                    SetError(0x419);
                }

                global_name2[yuqq] = ((_lexer._line, _lexer._row, type, ghhp));
                DF_warning.RemoveAt(ghj);
            }


            _cache1 = type;

            if (Peek()._type is not TokenType.NewLine) SetError(0x400);

            func_name.Clear();
            func_name2.Clear();
            func_name.Add(new List<string>());
            func_name2.Add(new List<(int, int, VarType, object?)>());
            func_post = 0;
            Infunc = true;
            _cache2 = 0;
            foreach (var item in param)
            {
                RegisterName(item.name, item.type, null);
            }
            Advance();
            Peek();
            while (_peek._type is not TokenType.End)
            {
                ParseStatement();
                Peek();

                if (_peek._type is TokenType.Unknown or TokenType.EOF)
                {
                    SetError(0x501);
                    break;
                }
            }
            Infunc = false;

            if (_cache2 is 0 && _cache1 is not VarType.Unknown)
            {
                SetError(0x46C);
            }

            Advance();
            if (Peek()._type is TokenType.Function) Advance();
        }

        //nullの時にerror発生なしのNewLine処理はcaller側で行う必要がある
        //注意：最後にAdvance()が含まれる
        public void ParseStatement()
        {
            Peek();
            if (_peek._type is TokenType.Return) ParseReturn();
            else if (_peek._type is TokenType.Continue or TokenType.Break)
            {
                if (InLoop is 0) SetError(0x442);
                Advance();
            }
            else if (_peek._type is TokenType.Type or TokenType.Var) ParseVarDec();
            else if (_peek._type is TokenType.Identifier)
            {
                byte id = GetNameInfo(_peek._text);

                if (id is 0) ParseVarIn();
                else if (id is 1) ParseFuncUse();
                else
                {
                    SetError(0x464);
                    Advance();
                }
            }

            else if (_peek._type is TokenType.Loop) ParseLoop();
            else if (_peek._type is TokenType.If) ParseIf();

            else if (_peek._type is TokenType.NewLine) Advance();
            else
            {
                if (_peek._type is TokenType.Else or TokenType.Elseif) SetError(0x481);
                else SetError(0x402);

                Advance();
            }
        }

        //注意：最後にAdvance()が含まれる
        public void ParseReturn()
        {
            if (!Infunc) SetError(0x46A);
            if (func_post is not 0) SetError(0x46B);

            _cache2++;

            Advance();
            Peek();
            if (_cache1 is not VarType.Unknown)
            {
                ParseValue(_cache1);
                Peek();
            }
        }

        //注意：最後にAdvance()が含まれる
        public void ParseVarDec()
        {
            var type = TokToType(Peek());

            Advance();
            Peek();

            if (_peek._type is TokenType.Bracket_L)
            {
                Advance();
                Peek();
                if (_peek._type is not TokenType.Bracket_R) SetError(0x403);
                type = type is VarType.Unknown ? VarType.Unknown : ValuToArry(type);
                Advance();
                Peek();
            }

            if (_peek._type is not TokenType.Identifier) SetError(0x450);
            var name = _peek._text;
            Advance();
            Peek();

            if (_peek._type is TokenType.Assign)
            {
                Advance();
                Peek();

                var ty = ParseValue(type);
                if (ty is null)
                {
                    if (type is VarType.Unknown)
                    {
                        type = VarType.Number;
                    }

                    if (!RegisterName(name, type, null)) SetError(0x461);
                }
                else
                {
                    if (type is VarType.Unknown)
                    {
                        type = ty.type;
                    }

                    if (!RegisterName(name, type, null)) SetError(0x461);
                }
            }
            else
            {
                if (type is VarType.Unknown)
                {
                    SetError(0x473);
                    type = VarType.Number;
                }

                if (!RegisterName(name, type, null))
                {
                    SetError(0x461);
                }
            }
        }

        //注意：最後にAdvance()が含まれる
        public void ParseVarIn()
        {
            Peek();
            var name = _peek._text;

            var type = SearchName(name);
            if (type is VarType.Unknown)
            {
                SetError(0x471);
                type = VarType.Number;
            }
            if (SearchName3(name))
            {
                SetError(0x472);
            }

            Advance();
            Peek();

            
            if (_peek._type is TokenType.Bracket_L)
            {
                if (type is < VarType.Char_Array)
                {
                    SetError(0x478);
                    type = VarType.Char;
                }
                else type = ArryToValu(type);

                Advance();

                ParseValue(VarType.Number);

                if (Peek()._type is not TokenType.Bracket_R)
                {
                    SetError(0x477);
                }

                Advance();
                Peek();
            }

            var atch = _peek._type;
            if (_peek._type is < TokenType.PlusAssign or > TokenType.BarAssign and not TokenType.Assign)
            {
                SetError(0x470);
                atch = TokenType.Assign;
            }

            if (type is VarType.Type)
            {
                if (atch is not TokenType.Assign)
                {
                    SetError(0x493);
                }
            }
            else if (type is >= VarType.Char_Array)
            {
                if (atch is not TokenType.PlusAssign and not TokenType.Assign)
                {
                    SetError(0x492);
                }
            }
            else if (type is VarType.Boolean)
            {
                if (atch is not TokenType.BarAssign and not TokenType.AmpAssign and not TokenType.Assign)
                {
                    SetError(0x494);
                }
            }
            else if (type is VarType.Decimal)
            {
                if (atch is TokenType.BarAssign or TokenType.AmpAssign or TokenType.PercentAssign)
                {
                    SetError(0x49A);
                }
            }
            else
            {
                if (atch is TokenType.BarAssign or TokenType.AmpAssign)
                {
                    SetError(0x491);
                }
            }

            Advance();
            ParseValue(type);
        }

        //注意：最後にAdvance()が含まれる
        public void ParseFuncUse()
        {
            //Unknowの時は2種類あって、voidか未登録
            var ype = SearchName(Peek()._text);
            var name = _peek._text;

            VarType[]? fpa = SearchName2(name);

            if (fpa is null)
            {
                SetError(0x465);
                fpa = new VarType[0];
            }

            Advance();
            Peek();

            if (_peek._type != TokenType.Paren_L)
            {
                SetError(0x467);
            }
            else Advance();
            Peek();

            int iff = 0;
            VarType suiso = VarType.Unknown;
            while (_peek._type != TokenType.Paren_R && iff < fpa.Length)
            {
                var uuh = fpa[iff];
                if (uuh is VarType.Valable)
                {
                    if (suiso is < VarType.Char_Array or VarType.Arrable or VarType.Array)
                    {
                        SetError(0x006);
                        suiso = VarType.Char_Array;
                    }

                    uuh = ArryToValu(suiso);
                }
                else if (uuh is VarType.Arrable)
                {
                    if (suiso is < VarType.Char_Array or VarType.Arrable or VarType.Array)
                    {
                        SetError(0x006);
                        suiso = VarType.Char_Array;
                    }
                    uuh = suiso;
                }

                var vvb = ParseValue(uuh);

                if (uuh is VarType.Array)
                {
                    if (vvb is null) suiso = VarType.Char_Array;
                    else suiso = vvb.type;
                }
                if (uuh is VarType.Value)
                {
                    if (vvb is null) suiso = VarType.Char_Array;
                    else suiso = ValuToArry(vvb.type);
                }


                if (iff != fpa.Length - 1)
                {
                    if (Peek()._type is not TokenType.Comma)
                    {
                        SetError(0x468);
                    }

                    else Advance();
                }

                Peek();
                iff++;
            }

            if (iff != fpa.Length)
            {
                SetError(0x46F);
            }

            if (_peek._type != TokenType.Paren_R)
            {
                SetError(0x469);
            }

            else Advance();
        }


        //注意：最後にAdvance()が含まれる
        public void ParseLoop()
        {
            Advance();
            Peek();
            if (_peek._type is TokenType.Paren_L)
            {
                Advance();
                Peek();
                if (_peek._type is not TokenType.Paren_R)
                {
                    ParseValue(VarType.Number);
                    Peek();
                    if (_peek._type is not TokenType.Paren_R) SetError(0x440);
                }

                Advance();
                Peek();
            }
            if (_peek._type is not TokenType.NewLine) SetError(0x401);
            else Advance();
            Peek();
            Inc_post();
            InLoop++;
            while (_peek._type is not TokenType.End)
            {
                ParseStatement();
                Peek();

                if (_peek._type is TokenType.EOF or TokenType.Unknown)
                {
                    SetError(0x501);
                    break;
                }
            }
            InLoop--;
            Dec_post();

            Advance();
            if (Peek()._type is TokenType.Loop) Advance();
        }

        //注意：最後にAdvance()が含まれる
        public void ParseIf()
        {
            int i = 0;
            bool fin = false;
            while (_peek._type is not TokenType.End && !fin)
            {
                if (_peek._type is TokenType.If or TokenType.Elseif)
                {
                    Advance();
                    if (Peek()._type is TokenType.Paren_L)
                    {
                        Advance();
                        Peek();
                        var tyy = ParseValue(VarType.Boolean);

                        if (Peek()._type is not TokenType.Paren_R)
                        {
                            SetError(0x484);
                        }
                    }
                    else SetError(0x483);
                }
                else if (_peek._type is TokenType.Else)
                {
                    fin = !fin;
                }
                else
                {
                    SetError(2);
                    break;
                }

                Advance();
                if (Peek()._type is not TokenType.NewLine) SetError(0x401);
                else Advance();

                Peek();
                Inc_post();
                while (_peek._type is not TokenType.End and not TokenType.Elseif and not TokenType.Else)
                {
                    ParseStatement();
                    Peek();

                    if (_peek._type is TokenType.EOF or TokenType.Unknown)
                    {
                        SetError(0x501);
                        break;
                    }
                }
                Dec_post();
                i++;
            }
            Advance();
            if (Peek()._type is TokenType.If) Advance();
        }



        public bool SerLiteral(ValueNode value)
        {
            if (value is LiteralArrayNode vv)
            {
                foreach (var item in vv.value)
                {
                    if (item is not LiteralValueNode) return true;
                }
                return false;
            }

            return (value is not LiteralValueNode);
        }

        //1:func_name,0xff:not found
        public byte GetNameInfo(string name)
        {
            int yu = global_name.IndexOf(name);
            if (yu is -1)
            {
                if (Infunc)
                {
                    for (int i = 0; i < func_name.Count; i++)
                    {
                        yu = func_name[i].IndexOf(name);
                        if (yu is not -1) return (func_name2[i][yu].Item4 is null) ? (byte)0 : (byte)1;
                    }
                }
                else
                {
                    for (int i = 0; i < local_name.Count; i++)
                    {
                        yu = local_name[i].IndexOf(name);
                        if (yu is not -1) return (local_name2[i][yu].Item4 is null) ? (byte)0 : (byte)1;
                    }
                }
                return 0xff;
            }
            else return (global_name2[yu].Item4 is null) ? (byte)0 : (byte)1;
        }

        public string ParseType()
        {
            string ret = Advance()._text;
            Peek();
            if (_peek._type is not TokenType.Bracket_L) return ret;

            Advance();
            Peek();
            if (_peek._type is not TokenType.Bracket_R) SetError(0x403);
            else Advance();

            ret += "[]";
            return ret;
        }

        //nullになるのはタイプエラーのみ
        public ValueNode? ParseValue(VarType type, bool no = false)
        {
            Peek();

            var ty = ParseLogicExpr();

            //ty.typeがArrayで返ってくる＝＞空配列の場合＝＞タイプ補正
            if (ty.type is VarType.Array)
            {
                if (type is VarType.Array) ty = new LiteralArrayNode() { type = VarType.Char_Array };
                else if (type is >= VarType.Char_Array) ty = new LiteralArrayNode() { type = type };
                else if (type is VarType.Unknown)
                {
                    ty = new LiteralArrayNode() { type = VarType.Char_Array, NULL = true };
                    SetError(0x453);
                    return ty;
                }
                else ty = new LiteralArrayNode() { type = VarType.Char_Array };
            }

            if (ty.type != type && !no && type is not VarType.Unknown)
            {
                bool we = (ty.type is VarType.String or VarType.Char_Array && type is VarType.String or VarType.Char_Array);

                if (type is VarType.Array && ty.type is >= VarType.Char_Array) we = true;

                if (!we)
                {
                    if (type is VarType.Valable or VarType.Arrable) SetError(0x006);

                    SetError(0x409);
                    return null;
                }
            }

            return ty;
        }

        //注意：最後にAdvance()が含まれる
        public ValueNode ParseLogicExpr()
        {
            var gh = ParseLogicCom();

            Peek();
            while (_peek._type is TokenType.Amp or TokenType.Bar)
            {
                CaluNode node = new CaluNode();
                node.symbol = (byte)(_peek._type is TokenType.Amp ? 5 : 6);
                node.value1 = gh;
                if (node.value1.type is not VarType.Boolean)
                {
                    node.value1 = new ValueNode() { type = VarType.Boolean, NULL = true };
                    SetError(0x490);
                }
                Advance();
                Peek();
                node.value2 = ParseLogicCom();
                if (node.value2.type is not VarType.Boolean)
                {
                    node.value2 = new ValueNode() { type = VarType.Boolean, NULL = true };
                    SetError(0x490);
                }
                node.type = VarType.Boolean;

                gh = node;
            }
            return gh;
        }

        //注意：最後にAdvance()が含まれる
        public ValueNode ParseLogicCom()
        {
            var gh = ParseAddExpr();

            Peek();
            if (_peek._type is TokenType.Is || _peek._type is >= TokenType.Equal and <= TokenType.Less)
            {
                CaluNode node = new CaluNode();
                node.symbol = (byte)(_peek._type is TokenType.Is ? 7 : 7 + (byte)((byte)_peek._type - (byte)TokenType.Equal));
                node.value1 = gh;
                Advance();
                Peek();
                node.value2 = ParseAddExpr();
                node.type = VarType.Boolean;

                if (node.value1.type != node.value2.type)
                {
                    node.value2 = new ValueNode() { type = node.value1.type, NULL = true };
                    SetError(0x479);
                }

                if (node.symbol is not 7 and not 8)
                {
                    if (node.value1.type is not VarType.Number and not VarType.Decimal and not VarType.Char)
                    {
                        node.value1 = new ValueNode() { type = VarType.Number, NULL = true };
                        node.value2 = new ValueNode() { type = VarType.Number, NULL = true };

                        SetError(0x497);
                    }
                }

                return node;
            }
            return gh;
        }
        
        //注意：最後にAdvance()が含まれる
        public ValueNode ParseUnary()
        {
            Peek();

            if (_peek._type is TokenType.Typeof)
            {
                LiteralValueNode nod = new LiteralValueNode();
                nod.type = VarType.Type;

                Advance();
                if (Peek()._type is TokenType.Paren_L)
                {

                    Advance();
                    Peek();
                    var ty = ParseValue(VarType.Unknown, true);
                    if (ty is null) nod.value = TypeToStr(VarType.Number);
                    else nod.value = TypeToStr(ty.type);


                    if (Peek()._type is not TokenType.Paren_R) SetError(0x476);
                }
                else SetError(0x47A);

                Advance();
                return nod;
            }
            else if (_peek._type is TokenType.TextArray)
            {
                var nod = new TextArrayNode();

                Advance();
                if (Peek()._type is not TokenType.Paren_L)
                {
                    SetError(0x520);
                }

                else Advance();
                byte h = 0;
                while (Peek()._type is not TokenType.Paren_R and not TokenType.NewLine and not TokenType.EOF)
                {
                    Debug.WriteLine(Peek()._type + "/" + Peek()._text);

                    if (h is 0) h++;
                    else
                    {
                        if (_peek._type is not TokenType.Comma)
                        {
                            SetError(0x521);
                        }
                        else Advance();
                        Peek();
                    }

                    var ett = ParseValue(VarType.Unknown, true);
                    if (ett is not null && ett.type is not VarType.Char_Array and not VarType.String)
                    {
                        SetError(0x522);
                    }
                }


                if (Peek()._type is not TokenType.Paren_R)
                {
                    SetError(0x476);
                }

                else Advance();
                return nod;
            }
            

            ValueNode node;

            (int, int) bacl = (line, row);
            byte stat = 0;
            if (_peek._type is TokenType.Plus)
            {
                Advance();
                Peek();
            }
            else if (_peek._type is TokenType.Minus)
            {
                stat++;
                stat++;
                Advance();
                Peek();
            }
            else if (_peek._type is TokenType.Bang)
            {
                stat++;
                Advance();
                Peek();
            }

            if (_peek._type is TokenType.Identifier) node = ParseVarUse();
            else if (_peek._type is TokenType.String or TokenType.Number or TokenType.Decimal or TokenType.Type or TokenType.Char or TokenType.Boolean or TokenType.Bracket_L) node = ParseLiteral();
            else if (_peek._type is TokenType.Paren_L)
            {
                Advance();
                var gsh = (ValueNode)ParseValue(VarType.Unknown, true);

                if (Peek()._type is not TokenType.Paren_R)
                {
                    SetError(0x476);
                }
                else Advance();
                node = gsh;
            }
            else
            {
                SetError(0x402);
                node = new ValueNode() { type = VarType.Number, NULL = true };

                Advance();
            }

            Peek();

            if (_peek._type is TokenType.Bracket_L)
            {
                if (node.type is < VarType.Char_Array)
                {
                    SetError(0x478);
                    node = new ValueNode() { type = VarType.Number_Array, NULL = true };
                }
                else if (node.type is VarType.Array)
                {
                    SetError(0x453);
                    node = new ValueNode() { type = VarType.Number_Array, NULL = true };
                }
                else
                {
                    IndexNode ccv = new IndexNode();
                    ccv.value = node;
                    Advance();

                    ccv.index = ParseValue(VarType.Number);
                    if (ccv.index is null)
                    {
                        ccv.index = new ValueNode() { type = VarType.Number, NULL = true };
                    }

                    if (Peek()._type is not TokenType.Bracket_R)
                    {
                        SetError(0x477);
                    }
                    else Advance();

                    ccv.type = ArryToValu(node.type);

                    node = ccv;
                }
            }
            else if (_peek._type is TokenType.Paren_L)
            {
                if (node.type is VarType.Type)
                {
                    ArrayNode ccv = new ArrayNode();

                    var by = node as LiteralValueNode;
                    if (by is null)
                    {
                        SetError(0x47C);
                        ccv.type = VarType.Char_Array;
                    }
                    else ccv.type = NtexToType(by.value);


                    if (ccv.type is < VarType.Char_Array)
                    {
                        SetError(0x47B);
                        ccv.type = VarType.Char_Array;
                    }

                    Advance();

                    ccv.length = ParseValue(VarType.Number);
                    if (ccv.length is null)
                    {
                        ccv.length = new ValueNode() { type = VarType.Number, NULL = true };
                    }

                    if (Peek()._type is not TokenType.Paren_R)
                    {
                        SetError(0x476);
                    }
                    else Advance();

                    node = ccv;
                }
                else
                {
                    SetError(0x47D);
                    node = new ValueNode() { type = VarType.Number_Array, NULL = true };
                }
            }
            if (stat is not 0)
            {
                CaluNode ap = new CaluNode();
                ap.value1 = node;
                ap.symbol = (byte)(12 + stat);//13:!,14:minus
                ap.type = node.type;


                if (stat is 1 && ap.type is not VarType.Boolean)
                {
                    ap.value1 = new ValueNode() { type = VarType.Boolean, NULL = true };
                    SetError(0x498, bacl.Item1, bacl.Item2);
                }

                if (stat is 2 && ap.type is not VarType.Number and not VarType.Decimal)
                {
                    ap.value1 = new ValueNode() { type = VarType.Number, NULL = true };
                    SetError(0x499, bacl.Item1, bacl.Item2);
                }

                node = ap;
            }

            return node;
        }

        //注意：最後にAdvance()が含まれる
        public ValueNode ParseMulExpr()
        {
            var gh = ParseUnary();

            Peek();
            while (_peek._type is TokenType.Star or TokenType.Slash or TokenType.Percent)
            {
                CaluNode node = new CaluNode();
                node.symbol = (byte)(2 + _peek._type - TokenType.Star);
                node.value1 = gh;
                if (node.value1.type is VarType.Type)
                {
                    node.value1 = new ValueNode() { type = VarType.Number, NULL = true };
                    SetError(0x493);
                }
                if (node.value1.type is VarType.Boolean)
                {
                    node.value1 = new ValueNode() { type = VarType.Number, NULL = true };
                    SetError(0x494);
                }
                if (node.type is >= VarType.Char_Array)
                {
                    node.value1 = new ValueNode() { type = VarType.Number, NULL = true };
                    SetError(0x492);
                }
                if (node.symbol is 4 && node.type is VarType.Decimal)
                {
                    node.value1 = new ValueNode() { type = VarType.Number, NULL = true };
                    SetError(0x496);
                }
                node.type = node.value1.type;
                Advance();
                Peek();
                node.value2 = ParseUnary();

                if (node.type != node.value2.type)
                {
                    node.value2 = new ValueNode() { type = node.type, NULL = true };
                    SetError(0x495);
                }

                gh = node;
            }
            return gh;
        }

        //注意：最後にAdvance()が含まれる
        public ValueNode ParseAddExpr()
        {
            var gh = ParseMulExpr();

            Peek();
            while (_peek._type is TokenType.Plus or TokenType.Minus)
            {
                CaluNode node = new CaluNode();
                node.symbol = (byte)(_peek._type - TokenType.Plus);
                node.value1 = gh;
                if (node.value1.type is VarType.Type)
                {
                    node.value1 = new ValueNode() { type = VarType.Number, NULL = true };

                    SetError(0x493);
                }
                if (node.value1.type is VarType.Boolean)
                {
                    node.value1 = new ValueNode() { type = VarType.Number, NULL = true };

                    SetError(0x494);
                }
                if (node.symbol is 1 && node.type is >= VarType.Char_Array)
                {
                    node.value1 = new ValueNode() { type = VarType.Number, NULL = true };

                    SetError(0x492);
                }
                node.type = node.value1.type;
                Advance();
                Peek();
                node.value2 = ParseMulExpr();
                if (node.value2 is null) return null;

                if (node.type is VarType.String or VarType.Char_Array) { node.type = VarType.Char_Array; node.value1.type = VarType.Char_Array; }
                if (node.value2.type is VarType.String or VarType.Char_Array) node.value2.type = VarType.Char_Array;


                if (node.type != node.value2.type)
                {
                    node.value2 = new ValueNode() { type = node.type, NULL = true };

                    SetError(0x495);
                }

                gh = node;
            }
            return gh;
        }

        //注意：最後にAdvance()が含まれる
        public ValueNode ParseLiteral()
        {
            if (_peek._type is TokenType.Bracket_L)
            {
                LiteralArrayNode node = new LiteralArrayNode();
                Advance();
                Peek();
                bool vf = false;
                while (_peek._type is not TokenType.Bracket_R)
                {
                    if (vf && _peek._type is TokenType.Comma)
                    {
                        Advance();
                        Peek();
                    }
                    else
                    {
                        if (vf) break;
                        vf = true;
                    }

                    var fsp = (ValueNode)ParseValue(VarType.Unknown, true);

                    if (fsp.type is VarType.String)
                    {
                        fsp = new ValueNode() { type = VarType.Char, NULL = true };
                        SetError(0x405);
                    }
                    else if (fsp.type is >= VarType.Char_Array)
                    {
                        fsp = new ValueNode() { type = VarType.Number, NULL = true };
                        SetError(0x407);
                    }

                    if (node.type is VarType.Unknown) node.type = ValuToArry(fsp.type);
                    else if (ArryToValu(node.type) != fsp.type)
                    {
                        fsp = new ValueNode() { type = ArryToValu(node.type), NULL = true };
                        SetError(0x409);
                    }

                    node.value.Add(fsp);

                    Peek();
                }

                if (!vf)
                {
                    node.type = VarType.Array;
                }

                Advance();

                return node;
            }
            else
            {
                var node = new LiteralValueNode() { type = TotToType(_peek._type), value = _peek._text };
                Advance();

                if (node.type is VarType.Type)
                {
                    if (Peek()._type is TokenType.Bracket_L)
                    {
                        node.value += "[]";
                        Advance();
                        if (Peek()._type is not TokenType.Bracket_R)
                        {
                            SetError(0x477);
                        }
                        else Advance();
                    }
                    node.value = TypeToStr(StrToType(node.value));
                }

                return node;
            }
        }

        //注意：最後にAdvance()が含まれる
        //Peek()が前提
        public ValueNode ParseVarUse()
        {
            var ype = SearchName(_peek._text);
            if (ype is VarType.Unknown)
            {
                SetError(0x408);
                Advance();
                return new ValueNode() { type = VarType.Number, NULL = true };
            }

            var ghs = SearchName2(_peek._text);
            if (ghs is null)
            {
                VarUseNode node = new VarUseNode();
                node.type = ype;
                node.name = _peek._text;
                Advance();
                if (Peek()._type is not TokenType.Bracket_L) return node;

                if (node.type is < VarType.Char_Array)
                {
                    SetError(0x478);
                    return node;
                }

                IndexNode node2 = new IndexNode();
                node2.type = ArryToValu(node.type);

                Advance();
                Peek();
                var gs = ParseValue(VarType.Number);
                if (gs is null) gs = new ValueNode() { type = VarType.Number, NULL = true };
                node2.index = gs;

                node2.value = node;

                Advance();
                return node2;
            }
            else
            {
                CallNode node = new CallNode();
                node.type = ype;
                node.name = _peek._text;

                Advance();
                Peek();
                if (_peek._type is TokenType.Paren_L)
                {
                    Advance();
                    int i = 0;
                    node.args = new ValueNode[ghs.Length];
                    VarType suiso = VarType.Unknown;
                    //コメント:suisoは配列型のタイプで保存
                    while (i < ghs.Length)
                    {
                        Peek();
                        if (i is not 0)
                        {
                            if (_peek._type is not TokenType.Comma)
                            {
                                SetError(0x468);
                                break;
                            }

                            Advance();
                            Peek();
                        }

                        VarType uuh = ghs[i];
                        if (uuh is VarType.Valable)
                        {
                            if (suiso is < VarType.Char_Array or VarType.Arrable or VarType.Array)
                            {
                                SetError(0x006);
                                suiso = VarType.Char_Array;
                            }

                            uuh = ArryToValu(suiso);
                        }
                        else if (uuh is VarType.Arrable)
                        {
                            if (suiso is < VarType.Char_Array or VarType.Arrable or VarType.Array)
                            {
                                SetError(0x006);
                                suiso = VarType.Char_Array;
                            }
                            uuh = suiso;
                        }

                        var ty = ParseValue(uuh);
                        if (ty is null) ty = new ValueNode() { type = uuh, NULL = true };
                        node.args[i] = ty;
                        if (uuh is VarType.Array) suiso = ty.type;
                        if (uuh is VarType.Value) suiso = ValuToArry(ty.type);

                        i++;
                    }

                    if (ype is VarType.Valable)
                    {
                        if (suiso is < VarType.Char_Array or VarType.Arrable or VarType.Array)
                        {
                            //引数側でエラーが出る
                            suiso = VarType.Char_Array;
                        }

                        node.type = ArryToValu(suiso);
                        ype = node.type;
                    }
                    else if (ype is VarType.Arrable)
                    {
                        if (suiso is < VarType.Char_Array or VarType.Arrable or VarType.Array)
                        {
                            //引数側でエラーが出る
                            suiso = VarType.Char_Array;
                        }
                        node.type = suiso;
                        ype = node.type;
                    }

                    Peek();
                    if (_peek._type is not TokenType.Paren_R)
                    {
                        SetError(0x469);
                    }
                    else Advance();

                    if (Peek()._type is not TokenType.Bracket_L) return node;

                    if (node.type is < VarType.Char_Array)
                    {
                        SetError(0x478);
                        return node;
                    }

                    IndexNode node2 = new IndexNode();
                    node2.type = ArryToValu(node.type);

                    Advance();
                    Peek();
                    var gs = ParseValue(VarType.Number);
                    if (gs is null) gs = new ValueNode() { type = VarType.Number, NULL = true };
                    node2.index = gs;

                    node2.value = node;

                    Advance();
                    return node2;
                }
                else
                {
                    SetError(0x467);
                    return new ValueNode() { type = VarType.Number, NULL = true };
                }
            }
        }

        //Type num str
        public string TypeToStr(VarType type)
        {
            if (type is VarType.String) type = VarType.Char_Array;
            return ((byte)type).ToString();
        }

        public VarType NtexToType(string str)
        {
            if (!byte.TryParse(str, out var type)) type = 0;
            return (VarType)type;
        }

        public Token Advance()
        {
            Token ret;
            if (_peek is null) ret = _lexer.Next();
            else
            {
                ret = _peek;
                _peek = null;
            }

            line = _lexer._line;
            row = _lexer._row;

            return ret;
        }

        public Token Peek()
        {
            if (_peek is null) _peek = _lexer.Next();
            return _peek;
        }

        public Token Consume(TokenType type)
        {
            Peek();
            if (_peek._type != type)
            {
                SetError(0x400);
                return Lexer.NullToken;
            }
            return Advance();
        }

        public (int, int, int)[] GetError()
        {
            return _error.ToArray();
        }

        public void SetError(int x)
        {
            SetError(x, line, row);
        }

        public void SetError(int x, int y, int z)
        {
            if (_lexer._error is not 0) x = _lexer._error;

            _error.Add((x, y, z));

            _lexer._error = 0;
        }

        public (int, int, int)[] GetWarning()
        {
            List<(int, int, int)> copy = new List<(int, int, int)>(_lexer._warning.Count + _warning.Count);
            copy.AddRange(_lexer._warning);
            copy.AddRange(_warning);

            return copy.ToArray();
        }


        internal void Inc_post()
        {
            if (Infunc)
            {
                func_post++;
                func_name.Add(new List<string>());
                func_name2.Add(new List<(int, int, VarType, object?)>());
            }
            else
            {
                local_post++;
                local_name.Add(new List<string>());
                local_name2.Add(new List<(int, int, VarType, object?)>());
            }
        }
        internal void Dec_post()
        {
            if (Infunc)
            {
                func_name.RemoveAt(func_post);
                func_name2.RemoveAt(func_post);
                func_post--;
            }
            else
            {
                local_name.RemoveAt(local_post);
                local_name2.RemoveAt(local_post);
                local_post--;
            }
        }

        internal VarType SearchName(string name)
        {
            int yu = global_name.IndexOf(name);
            if (yu is not -1) return global_name2[yu].Item3;

            if (Infunc)
            {
                for (int i = 0; i < func_name.Count; i++)
                {
                    yu = func_name[i].IndexOf(name);
                    if (yu is not -1) return func_name2[i][yu].Item3;
                }
            }
            else
            {
                for (int i = 0; i < local_name.Count; i++)
                {
                    yu = local_name[i].IndexOf(name);
                    if (yu is not -1) return local_name2[i][yu].Item3;
                }
            }
            return VarType.Unknown;
        }
        internal VarType[]? SearchName2(string name)
        {
            int yu = global_name.IndexOf(name);
            if (yu is not -1) return (VarType[]?)global_name2[yu].Item4;

            if (Infunc)
            {
                for (int i = 0; i < func_name.Count; i++)
                {
                    yu = func_name[i].IndexOf(name);
                    if (yu is not -1) return (VarType[]?)func_name2[i][yu].Item4;
                }
            }
            else
            {
                for (int i = 0; i < local_name.Count; i++)
                {
                    yu = local_name[i].IndexOf(name);
                    if (yu is not -1) return (VarType[]?)local_name2[i][yu].Item4;
                }
            }
            return null;
        }

        //IsConst
        internal bool SearchName3(string name)
        {
            return global_name.IndexOf(name) is not -1;
        }

        internal bool RegisterName(string name, VarType type, object? arg, bool global = false)
        {
            if (global_name.Contains(name)) return false;
            if (Infunc)
            {
                foreach (var item in func_name)
                {
                    if (item.Contains(name)) return false;
                }
            }
            else
            {
                foreach (var item in local_name)
                {
                    if (item.Contains(name)) return false;
                }
            }


            if (global)
            {
                global_name.Add(name);
                global_name2.Add((_lexer._line, _lexer._row, type, arg));
            }
            else if (Infunc)
            {
                func_name[func_post].Add(name);
                func_name2[func_post].Add((_lexer._line, _lexer._row, type, arg));
            }
            else
            {
                local_name[local_post].Add(name);
                local_name2[local_post].Add((_lexer._line, _lexer._row, type, arg));
            }

            return true;
        }

        private VarType ArryToValu(VarType type)
        {
            if (type is VarType.String) return VarType.Char;
            return type - 15;
        }

        private VarType ValuToArry(VarType type)
        {
            return type + 15;
        }

        private VarType TokToType(Token token)
        {
            if (token._type is TokenType.Type)
            {
                return StrToType(token._text);
            }

            return VarType.Unknown;
        }

        private VarType TotToType(TokenType type)
        {
            return type switch
            {
                TokenType.Var => VarType.Unknown,
                TokenType.Char => VarType.Char,
                TokenType.String => VarType.String,
                TokenType.Number => VarType.Number,
                TokenType.Decimal => VarType.Decimal,
                TokenType.Boolean => VarType.Boolean,
                TokenType.Type => VarType.Type,
                _ => VarType.Unknown
            };
        }

        private VarType StrToType(string type)
        {
            return type switch
            {
                "text" => VarType.String,
                "char" => VarType.Char,
                "decimal" => VarType.Decimal,
                "num" => VarType.Number,
                "bool" => VarType.Boolean,
                "type" => VarType.Type,
                "char[]" => VarType.Char_Array,
                "decimal[]" => VarType.Decimal_Array,
                "num[]" => VarType.Number_Array,
                "bool[]" => VarType.Boolean_Array,
                "type[]" => VarType.Type_Array,
                _ => VarType.Unknown
            };
        }
    }
}