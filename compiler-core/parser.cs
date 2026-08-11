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
    internal class Parser
    {
        public void Init()
        {
            _error = 0;
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
            local_name.Add(new());
            local_name2.Add(new());

            DF_warning.Clear();
        }



        public int _error = 0;

        public List<(int, int, int)> _warning = new List<(int, int, int)>();

        public List<(DefFuncNode, (int, int, int))> DF_warning = new();

        internal Lexer _lexer;
        public Token? _peek = null;

        internal VarType _cache1 = VarType.Unknown;
        internal int _cache2 = 0;
        internal bool Infunc = false;
        internal byte InLoop = 0;

        internal List<string> global_name = new List<string>();
        internal List<((int, int, VarType, object?), Node?)> global_name2 = new();

        internal List<List<string>> local_name = new List<List<string>>();
        internal List<List<((int, int, VarType, object?), Node?)>> local_name2 = new();

        internal List<List<string>> func_name = new List<List<string>>();
        internal List<List<((int, int, VarType, object?), Node?)>> func_name2 = new ();

        internal int local_post = 0;
        internal int func_post = 0;

        public Header _header;

        public Parser(Lexer lexer, Header header)
        {
            _lexer = lexer;
            _header = header;
        }

        //コードが空の場合は「null」が返されますが、Error=0でエラーではありません
        //If the code is empty, this returns null but error is 0 and not occured.
        public List<Node>? Parse()
        {
            List<Node>? retu = null;

            Init();

            List<Node> node = new List<Node>();
            _header.Set(this);
            node.AddRange(_header.Data);

            Peek();
            while (_peek._type is not TokenType.EOF)
            {
                Debug.WriteLine(_peek._type);

                Node? an = null;
                if (_peek._type is TokenType.Def) an = ParseDef();
                else if (_peek._type is TokenType.Function) an = ParseFunc();
                else if (_peek._type is TokenType.NewLine)
                {
                    Advance();
                    Peek();
                    continue;
                }
                else an = ParseStatement();

                if (an is null) goto RETU;
                node.Add(an);

                if (GetError() is not 0) goto RETU;

                Consume(TokenType.NewLine);
                if (_error is not 0)
                {
                    _error = 0x401;
                    goto RETU;
                }
                Peek();

                Debug.WriteLine("Par : " + an.ToString());
            }

            retu = node;

        RETU:
            foreach (var item in DF_warning)
            {
                _warning.Add(item.Item2);
            }
            return retu;
        }

        //注意：最後にAdvance()を含む
        public Node? ParseDef()
        {
            Advance();

            Peek();
            if (_peek._type is TokenType.Function) return ParseDef_Func();
            else if (_peek._type is TokenType.Type) return ParseDef_Const();

            _error = 0x410;
            return null;
        }

        //注意：最後にAdvance()を含む
        public DefFuncNode? ParseDef_Func()
        {
            DefFuncNode node = new DefFuncNode() { line = _lexer._line, row = _lexer._row };
            Advance();
            Peek();
            if (_peek._type is not TokenType.Identifier)
            {
                _error = 0x420;
                return null;
            }
            node.name = _peek._text;
            (int, int) fqq = (_lexer._line, _lexer._row);

            var ty = ParseFunc_Param();
            if (GetError() is not 0) return null;
            node.param = (List<ParamNode>)ty;

            VarType[] ghhp = new VarType[node.param.Count];
            for (int i = 0; i < ghhp.Length; i++)
            {
                ghhp[i] = node.param[i].type;
            }

            Advance();
            if (Peek()._type is not TokenType.Colon)
            {
                _error = 0x422;
                return null;
            }

            Advance();
            Peek();
            if (_peek._type is not TokenType.Type and not TokenType.Void)
            {
                _error = 0x424;
                return null;
            }
            node.type = StrToType(_peek._text);

            Advance();

            if (Peek()._type is TokenType.Bracket_L)
            {
                Advance();
                if (Peek()._type is not TokenType.Bracket_R)
                {
                    _error = 0x477;
                    return null;
                }
                Advance();
                node.type = ValuToArry(node.type);
            }

            if (!RegisterName(node.name, node.type, ghhp, node, true))
            {
                _lexer._line = fqq.Item1;
                _lexer._row = fqq.Item2;
                _error = 0x463;
                return null;
            }

            
            DF_warning.Add((node, (0x411, fqq.Item1, fqq.Item2)));

            return node;
        }

        //注意：最後にAdvance()を含む
        public DefConstNode? ParseDef_Const()
        {
            DefConstNode node = new DefConstNode() { line = _lexer._line, row = _lexer._row };
            node.type = StrToType(ParseType());
            if (_error is not 0) return null;

            Peek();
            if (_peek._type is not TokenType.Identifier)
            {
                _error = 0x430;
                return null;
            }
            node.name = _peek._text;
            if (!RegisterName(node.name, node.type, null, node, true))
            {
                _error = 0x461;
                return null;
            }

            Advance();
            if (Peek()._type is not TokenType.Assign)
            {
                _error = 0x431;
                return null;
            }

            Advance();
            ValueNode? _ah = ParseValue(node.type);
            if (_ah is null) return null;
            if (SerLiteral(_ah))
            {
                _error = 0x433;
                return null;
            }
            node.value = _ah;

            return node;
        }

        public List<ParamNode>? ParseFunc_Param()
        {
            List<ParamNode> node = new List<ParamNode>();
            Advance();
            if (Peek()._type is not TokenType.Paren_L)
            {
                _error = 0x421;
                return null;
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

                ParamNode pn = new ParamNode() { line = _lexer._line, row = _lexer._row };
                pn.type = StrToType(Advance()._text);

                if (Peek()._type is TokenType.Bracket_L)
                {
                    Advance();
                    if (Peek()._type is not TokenType.Bracket_R)
                    {
                        _error = 0x477;
                        return null;
                    }
                    Advance();
                    Peek();
                    pn.type = ValuToArry(pn.type);
                }

                pn.name = Consume(TokenType.Identifier)._text;
                if (GetError() is not 0)
                {
                    _error = 0x423;
                    return null;
                }
                Peek();
                node.Add(pn);
            }

            if (_peek._type is not TokenType.Paren_R)
            {
                _error = 0x421;
                return null;
            }

            return node;
        }

        //注意：最後にAdvance()を含む
        public FuncNode? ParseFunc()
        {
            FuncNode node = new FuncNode() { line = _lexer._line, row = _lexer._row };

            Advance();
            Peek();
            if (_peek._type is not TokenType.Identifier)
            {
                _error = 0x420;
                return null;
            }
            node.name = _peek._text;
            (int, int) fqq = (_lexer._line, _lexer._row);


            var ty = ParseFunc_Param();
            if (GetError() is not 0) return null;
            node.param = (List<ParamNode>)ty;

            Advance();
            if (Peek()._type is not TokenType.Colon)
            {
                _error = 0x422;
                return null;
            }

            Advance();
            Peek();
            if (_peek._type is not TokenType.Type and not TokenType.Void)
            {
                _error = 0x424;
                return null;
            }
            node.type = StrToType(_peek._text);

            Advance();

            if (Peek()._type is TokenType.Bracket_L)
            {
                Advance();
                if (Peek()._type is not TokenType.Bracket_R)
                {
                    _error = 0x477;
                    return null;
                }
                Advance();
                node.type = ValuToArry(node.type);
            }

            VarType[] ghhp = new VarType[node.param.Count];
            for (int i = 0; i < ghhp.Length; i++)
            {
                ghhp[i] = node.param[i].type;
            }

            
            //globalだけでNodeの取得を行う
            int yuqq = global_name.IndexOf(node.name);
            int ghj = -1;
            if (yuqq is not -1)
            {
                var gh2 = global_name2[yuqq].Item2;
                int i = 0;

                foreach (var item in DF_warning)
                {
                    if (item.Item1 == gh2)
                    {
                        ghj = i;
                        break;
                    }
                    i++;
                }
            }
            
            if (ghj is -1)
            {
                if (!RegisterName(node.name, node.type, ghhp, node, true))
                {
                    _lexer._line = fqq.Item1;
                    _lexer._row = fqq.Item2;
                    _error = 0x463;
                    return null;
                }
            }
            else
            {
                if (global_name2[yuqq].Item1.Item3 != node.type)
                {
                    //タイプ不一致
                    _error = 0x415;
                    return null;
                }

                if (global_name2[yuqq].Item1.Item4 is null)
                {
                    //定数
                    _error = 0x41A;
                    return null;
                }

                if (global_name2[yuqq].Item1.Item4 is VarType[] aptu)
                {
                    if (aptu.Length != ghhp.Length)
                    {
                        //引数不一致
                        _error = 0x417;
                        return null;
                    }

                    for (int bh = 0; bh < aptu.Length; bh++)
                    {
                        if (aptu[bh] != ghhp[bh])
                        {
                            //引数タイプ不一致
                            _error = 0x416;
                            return null;
                        }
                    }
                }
                else
                {
                    //引数の存在がバグ
                    _error = 0x419;
                    return null;
                }

                global_name2[yuqq] = ((_lexer._line, _lexer._row, node.type, ghhp), node);

                DF_warning[ghj].Item1.define = node;
                node.define = DF_warning[ghj].Item1;

                DF_warning.RemoveAt(ghj);
            }

            _cache1 = node.type;

            if (Peek()._type is not TokenType.NewLine)
            {
                _error = 0x400;
                return null;
            }

            func_name.Clear();
            func_name2.Clear();
            func_name.Add(new List<string>());
            func_name2.Add(new());
            func_post = 0;
            Infunc = true;
            _cache2 = 0;
            foreach (var item in node.param)
            {
                RegisterName(item.name, item.type, null, null);
            }
            Advance();
            Peek();
            while (_peek._type is not TokenType.End)
            {
                var tt = ParseStatement();
                if (GetError() is not 0) return null;
                if (tt is not null)
                {
                    node.statements.Add(tt);

                    Peek();
                    if (_peek._type is not TokenType.NewLine)
                    {
                        _error = 0x400;
                        return null;
                    }
                    Advance();
                }
                Peek();
            }
            Infunc = false;

            if (_cache2 is 0 && _cache1 is not VarType.Unknown)
            {
                _error = 0x46C;
                return null;
            }

            Advance();
            if (Peek()._type is TokenType.Function) Advance();

            return node;
        }

        //nullが返されるときは、エラーが発生している or NewLine処理をcaller側で行われる必要がある
        //注意：最後にAdvance()を含む
        public StatementNode? ParseStatement()
        {
            Peek();
            if (_peek._type is TokenType.Return) return ParseReturn();
            else if (_peek._type is TokenType.Continue or TokenType.Break)
            {
                if (InLoop is 0)
                {
                    _error = 0x442;
                    return null;
                }

                return new StatementNode() { line = _lexer._line, row = _lexer._row, Kind = (Advance()._type is TokenType.Continue ? NodeType.Continue : NodeType.Break) };
            }
            else if (_peek._type is TokenType.Type or TokenType.Var) return ParseVarDec();
            else if (_peek._type is TokenType.Identifier)
            {
                byte id = GetNameInfo(_peek._text);

                if (id is 0) return ParseVarIn();
                else if (id is 1) return ParseFuncUse();

                _error = 0x464;
                return null;
            }

            else if (_peek._type is TokenType.Loop) return ParseLoop();
            else if (_peek._type is TokenType.If) return ParseIf();

            else if (_peek._type is TokenType.NewLine)
            {
                Advance();
                return null;
            }

            _error = 0x402;
            if (_peek._type is TokenType.Else or TokenType.Elseif) _error = 0x481;
            else if (_peek._type is TokenType.TextArray) _error = 0x511;
            else if (_peek._type is TokenType.Typeof) _error = 0x510;

            return null;
        }

        //注意：最後にAdvance()を含む
        public ReturnNode? ParseReturn()
        {
            if (!Infunc)
            {
                _error = 0x46A;
                return null;
            }
            if (func_post is not 0)
            {
                _error = 0x46B;
                return null;
            }

            _cache2++;

            ReturnNode node = new ReturnNode() { line = _lexer._line, row = _lexer._row };
            Advance();
            Peek();
            if (_cache1 is not VarType.Unknown)
            {
                ValueNode? _ah = ParseValue(_cache1);
                if (_ah is null) return null;
                node.value = _ah;

                Peek();
            }
            else node.IsVoid = true;

            return node;
        }

        //注意：最後にAdvance()を含む
        public VarDecNode? ParseVarDec()
        {
            VarDecNode node = new VarDecNode() { line = _lexer._line, row = _lexer._row };

            node.type = TokToType(Peek());

            Advance();
            Peek();

            if (_peek._type is TokenType.Bracket_L)
            {
                Advance();
                Peek();
                if (_peek._type is not TokenType.Bracket_R)
                {
                    _error = 0x403;
                    return null;
                }
                node.type = node.type is VarType.Unknown ? VarType.Unknown : ValuToArry(node.type);
                Advance();
                Peek();
            }

            if (_peek._type is not TokenType.Identifier)
            {
                _error = 0x450;
                return null;
            }
            node.name = _peek._text;
            Advance();
            Peek();

            if (_peek._type is TokenType.Assign)
            {
                Advance();
                Peek();

                node.value = ParseValue(node.type);
                if (node.value is null || GetError() is not 0)
                {
                    if (_error is 0x453) _error = 0x451;
                    return null;
                }

                if (node.type is VarType.Unknown) node.type = node.value.type;


                if (!RegisterName(node.name, node.type, null, node))
                {
                    _error = 0x461;
                    return null;
                }
            }
            else
            {
                if (node.type is VarType.Unknown)
                {
                    _error = 0x473;
                    return null;
                }

                if (!RegisterName(node.name, node.type, null, node))
                {
                    _error = 0x461;
                    return null;
                }
            }

            return node;
        }

        //注意：最後にAdvance()を含む
        public VarInNode? ParseVarIn()
        {
            ValueNode? indexer = null;
            VarInNode node = new VarInNode() { line = _lexer._line, row = _lexer._row };
            Peek();
            node.name = _peek._text;

            node.type = SearchName(node.name);
            if (node.type is VarType.Unknown)
            {
                _error = 0x471;
                return null;
            }
            if (SearchName3(node.name))
            {
                _error = 0x472;
                return null;
            }

            Advance();
            Peek();

            if (_peek._type is TokenType.Bracket_L)
            {
                if (node.type is < VarType.Char_Array)
                {
                    _error = 0x478;
                    return null;
                }

                VarSetNode ccv = new VarSetNode() { line = node.line, row = node.row };
                ccv.type = ArryToValu(node.type);
                ccv.name = node.name;
                
                Advance();

                
                ccv.index = ParseValue(VarType.Number);
                if (ccv.index is null) return null;
                indexer = ccv.index;

                if (Peek()._type is not TokenType.Bracket_R)
                {
                    _error = 0x477;
                    return null;
                }

                Advance();
                Peek();

                node = ccv;
            }

            if (_peek._type is < TokenType.PlusAssign or > TokenType.BarAssign and not TokenType.Assign)
            {
                _error = 0x470;
                return null;
            }
            var atch = _peek._type;

            if (node.type is VarType.Type)
            {
                if (atch is not TokenType.Assign)
                {
                    _error = 0x493;
                    return null;
                }
            }
            else if (node.type is >= VarType.Char_Array)
            {
                if (atch is not TokenType.PlusAssign and not TokenType.Assign)
                {
                    _error = 0x492;
                    return null;
                }
            }
            else if (node.type is VarType.Boolean)
            {
                if (atch is not TokenType.BarAssign and not TokenType.AmpAssign and not TokenType.Assign)
                {
                    _error = 0x494;
                    return null;
                }
            }
            else if (node.type is VarType.Decimal)
            {
                if (atch is TokenType.BarAssign or TokenType.AmpAssign or TokenType.PercentAssign)
                {
                    _error = 0x49A;
                    return null;
                }
            }
            else
            {
                if (atch is TokenType.BarAssign or TokenType.AmpAssign)
                {
                    _error = 0x491;
                    return null;
                }
            }

            Advance();
            node.value = ParseValue(node.type);
            if (GetError() is not 0) return null;

            if (atch is not TokenType.Assign)
            {
                CaluNode node2 = new CaluNode() { line = node.line, row = node.row };

                node2.type = node.type;
                if (indexer is null) node2.value1 = new VarUseNode() { line = node.line, row = node.row, name = node.name, type = node.type };
                else
                {
                    var tr = new VarUseNode() { line = node.line, row = node.row, name = node.name, type = ValuToArry(node.type) };
                    node2.value1 = new IndexNode() { line = node.line, row = node.row, type = node.type, value = tr, index = indexer };
                }
                node2.value2 = node.value;
                node2.symbol = (byte)(atch - TokenType.PlusAssign);

                node.value = node2;
            }

            return node;
        }

        //注意：最後にAdvance()を含む
        public FuncUseNode? ParseFuncUse()
        {
            FuncUseNode node = new FuncUseNode() { line = _lexer._line, row = _lexer._row };

            //Unknowの時は void or unknown
            var ype = SearchName(Peek()._text);

            node.name = _peek._text;
            VarType[]? fpa = SearchName2(node.name);

            if (fpa is null)
            {
                _error = 0x465;
                return null;
            }

            Advance();
            Peek();

            if (_peek._type != TokenType.Paren_L)
            {
                _error = 0x467;
                return null;
            }

            Advance();
            Peek();

            int iff = 0;
            VarType suiso = VarType.Unknown;
            while (_peek._type != TokenType.Paren_R && iff < fpa.Length)
            {
                if (node.args is null) node.args = new List<ValueNode>(fpa.Length);

                
                var uuh = fpa[iff];
                if (uuh is VarType.Valable)
                {
                    if (suiso is < VarType.Char_Array or VarType.Arrable or VarType.Array)
                    {
                        _error = 0x006;
                        return null;
                    }

                    uuh = ArryToValu(suiso);
                }
                else if (uuh is VarType.Arrable)
                {
                    if (suiso is < VarType.Char_Array or VarType.Arrable or VarType.Array)
                    {
                        _error = 0x006;
                        return null;
                    }
                    uuh = suiso;
                }

                var vvb = ParseValue(uuh);
                if (vvb is null) return null;
                node.args.Add(vvb);
                if (uuh is VarType.Array) suiso = vvb.type;
                if (uuh is VarType.Value) suiso = ValuToArry(vvb.type);


                if (iff != fpa.Length - 1)
                {
                    if (Peek()._type is not TokenType.Comma)
                    {
                        _error = 0x468;
                        return null;
                    }

                    Advance();
                }

                Peek();
                iff++;
            }

            if (iff != fpa.Length)
            {
                _error = 0x46F;
                return null;
            }

            if (_peek._type != TokenType.Paren_R)
            {
                _error = 0x469;
                return null;
            }

            Advance();

            return node;
        }


        //注意：最後にAdvance()を含む
        public LoopNode? ParseLoop()
        {
            LoopNode node = new LoopNode() { line = _lexer._line, row = _lexer._row };

            Advance();
            Peek();
            if (_peek._type is TokenType.Paren_L)
            {
                Advance();
                Peek();
                if (_peek._type is not TokenType.Paren_R)
                {
                    node.infinity = false;

                    var ty = ParseValue(VarType.Number);
                    if (ty is null) return null;
                    node.num = ty;

                    Peek();

                    if (_peek._type is not TokenType.Paren_R)
                    {
                        _error = 0x440;
                        return null;
                    }
                }

                Advance();
                Peek();
            }
            if (_peek._type is not TokenType.NewLine)
            {
                _error = 0x401;
                return null;
            }

            Advance();
            Peek();
            Inc_post();
            InLoop++;
            while (_peek._type is not TokenType.End)
            {
                var tyy = ParseStatement();
                if (GetError() is not 0) return null;
                if (tyy is not null)
                {
                    node.statements.Add(tyy);

                    Peek();
                    if (_peek._type is not TokenType.NewLine)
                    {
                        _error = 0x401;
                        return null;
                    }

                    Advance();
                }
                Peek();
            }
            InLoop--;
            Dec_post();

            Advance();
            if (Peek()._type is TokenType.Loop) Advance();

            return node;
        }

        //注意：最後にAdvance()を含む
        public IfNode? ParseIf()
        {
            IfNode node = new IfNode() { line = _lexer._line, row = _lexer._row };

            int i = 0;
            bool fin = false;
            while (_peek._type is not TokenType.End && !fin)
            {
                if (_peek._type is TokenType.If or TokenType.Elseif)
                {
                    Advance();
                    if (Peek()._type is not TokenType.Paren_L)
                    {
                        _error = 0x483;
                        return null;
                    }

                    Advance();
                    Peek();
                    var tyy = ParseValue(VarType.Boolean);
                    if (tyy is null) return null;
                    node.condition.Add(tyy);

                    if (Peek()._type is not TokenType.Paren_R)
                    {
                        _error = 0x484;
                        return null;
                    }
                }
                else if (_peek._type is TokenType.Else)
                {
                    node.condition.Add(null);
                    fin = !fin;
                }
                else
                {
                    _error = 2;
                    return null;
                }

                Advance();
                if (Peek()._type is not TokenType.NewLine)
                {
                    _error = 0x401;
                    return null;
                }

                Advance();
                Peek();
                Inc_post();
                node.statements.Add(new List<StatementNode>());
                while (_peek._type is not TokenType.End and not TokenType.Elseif and not TokenType.Else)
                {
                    var tyy2 = ParseStatement();
                    if (GetError() is not 0) return null;
                    if (tyy2 is not null)
                    {
                        node.statements[i].Add(tyy2);

                        Peek();
                        if (_peek._type is not TokenType.NewLine)
                        {
                            _error = 0x401;
                            return null;
                        }

                        Advance();
                    }
                    Peek();
                }
                Dec_post();
                i++;
            }
            Advance();
            if (Peek()._type is TokenType.If) Advance();

            return node;
        }



        public bool SerLiteral(ValueNode value)
        {
            if (value is LiteralArrayNode vv)
            {
                foreach (var item in vv.value)
                {
                    if (item.Kind is not NodeType.Value_Literal) return true;
                }
                return false;
            }

            return (value.Kind is not NodeType.Value_Literal);
        }

        //1:func_name
        //0xff:not found
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
                        if (yu is not -1) return (func_name2[i][yu].Item1.Item4 is null) ? (byte)0 : (byte)1;
                    }
                }
                else
                {
                    for (int i = 0; i < local_name.Count; i++)
                    {
                        yu = local_name[i].IndexOf(name);
                        if (yu is not -1) return (local_name2[i][yu].Item1.Item4 is null) ? (byte)0 : (byte)1;
                    }
                }
                return 0xff;
            }
            else return (global_name2[yu].Item1.Item4 is null) ? (byte)0 : (byte)1;
        }

        public string ParseType()
        {
            string ret = Advance()._text;
            Peek();
            if (_peek._type is not TokenType.Bracket_L) return ret;

            Advance();
            Peek();
            if (_peek._type is not TokenType.Bracket_R)
            {
                _error = 0x403;
                return "";
            }

            Advance();
            ret += "[]";
            return ret;
        }

        public ValueNode? ParseValue(VarType type, bool no = false)
        {
            Peek();

            var ty = ParseLogicExpr();
            if (ty is null) return null;

            //ty.typeがArrayで返ってくる＝＞空配列の場合＝＞タイプ補正
            if (ty.type is VarType.Array)
            {
                if (type is VarType.Array) ty = new LiteralArrayNode() { type = VarType.Char_Array };
                else if (type is >= VarType.Char_Array) ty = new LiteralArrayNode() { type = type };
                else if (type is VarType.Unknown)
                {
                    if (no) ty = new LiteralArrayNode() { type = VarType.Char_Array };
                    else
                    {
                        _error = 0x453;
                        return null;
                    }
                }
                else ty = new LiteralArrayNode() { type = VarType.Char_Array };
            }

            if (ty.type != type && !no && type is not VarType.Unknown)
            {
                bool we = (ty.type is VarType.String or VarType.Char_Array && type is VarType.String or VarType.Char_Array);

                if (type is VarType.Array && ty.type is >= VarType.Char_Array) we = true;

                if (!we)
                {
                    _error = 0x409;
                    if (type is VarType.Valable or VarType.Arrable) _error = 0x006;
                    return null;
                }
            }

            return ty;
        }

        //注意：最後にAdvance()を含む
        public ValueNode? ParseLogicExpr()
        {
            var gh = ParseLogicCom();
            if (gh is null) return null;

            Peek();
            while (_peek._type is TokenType.Amp or TokenType.Bar)
            {
                CaluNode node = new CaluNode() { line = _lexer._line, row = _lexer._row };
                node.symbol = (byte)(_peek._type is TokenType.Amp ? 5 : 6);
                node.value1 = gh;
                if (node.value1.type is not VarType.Boolean)
                {
                    _error = 0x490;
                    return null;
                }
                Advance();
                Peek();
                node.value2 = ParseLogicCom();
                if (node.value2 is null) return null;

                if (node.value2.type is not VarType.Boolean)
                {
                    _error = 0x490;
                    return null;
                }
                node.type = VarType.Boolean;

                gh = node;
            }
            return gh;
        }

        //注意：最後にAdvance()を含む
        public ValueNode? ParseLogicCom()
        {
            var gh = ParseAddExpr();
            if (gh is null) return null;
            Peek();
            if (_peek._type is TokenType.Is || _peek._type is >= TokenType.Equal and <= TokenType.Less)
            {
                CaluNode node = new CaluNode() { line = _lexer._line, row = _lexer._row };
                node.symbol = (byte)(_peek._type is TokenType.Is ? 7 : 7 + (byte)((byte)_peek._type - (byte)TokenType.Equal));
                node.value1 = gh;
                Advance();
                Peek();
                node.value2 = ParseAddExpr();
                node.type = VarType.Boolean;
                if (node.value2 is null) return null;

                if (node.value1.type != node.value2.type)
                {
                    _error = 0x479;
                    return null;
                }

                if (node.symbol is not 7 and not 8)
                {
                    if (node.value1.type is not VarType.Number and not VarType.Decimal and not VarType.Char)
                    {
                        _error = 0x497;
                        return null;
                    }
                }

                return node;
            }
            return gh;
        }

        //注意：最後にAdvance()を含む
        public ValueNode? ParseUnary()
        {
            Peek();

            if (_peek._type is TokenType.Typeof)
            {
                LiteralValueNode nod = new LiteralValueNode() { line = _lexer._line, row = _lexer._row };
                nod.type = VarType.Type;

                Advance();
                if (Peek()._type is not TokenType.Paren_L)
                {
                    _error = 0x47A;
                    return null;
                }

                Advance();
                Peek();
                var ty = ParseValue(VarType.Unknown, true);
                if (ty is null) return null;

                nod.value = Pcom_dat.TypeToStr(ty.type);


                if (Peek()._type is not TokenType.Paren_R)
                {
                    _error = 0x476;
                    return null;
                }
                Advance();
                return nod;
            }
            else if (_peek._type is TokenType.TextArray)
            {
                var nod = new TextArrayNode() { line = _lexer._line, row = _lexer._row };

                Advance();
                if (Peek()._type is not TokenType.Paren_L)
                {
                    _error = 0x520;
                    return null;
                }

                Advance();
                byte h = 0;
                while(Peek()._type is not TokenType.Paren_R)
                {
                    if (h is 0) h++;
                    else
                    {
                        if (_peek._type is not TokenType.Comma)
                        {
                            _error = 0x521;
                            return null;
                        }
                        Advance();
                        Peek();
                    }

                    var ett = ParseValue(VarType.Unknown, true);
                    if (ett is null)
                    {
                        break;
                    }
                    else if (ett.type is not VarType.Char_Array and not VarType.String)
                    {
                        _error = 0x522;
                        return null;
                    }
                    nod.items.Add(ett);
                }

                if (Peek()._type is not TokenType.Paren_R)
                {
                    _error = 0x476;
                    return null;
                }

                Advance();
                return nod;
            }
            
            ValueNode? node = null;

            (int, int) bacl = (_lexer._line, _lexer._row);
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
                var gsh = ParseValue(VarType.Unknown, true);
                if (gsh is null) return null;
                if (Peek()._type is not TokenType.Paren_R)
                {
                    _error = 0x476;
                    return null;
                }
                Advance();
                node = gsh;
            }
            else
            {
                _error = 0x402;
                return null;
            }

            Peek();

            if (_peek._type is TokenType.Bracket_L)
            {
                if (node.type is < VarType.Char_Array)
                {
                    _error = 0x478;
                    return null;
                }
                else if (node.type is VarType.Array)
                {
                    _error = 0x453;
                    return null;
                }
                else
                {
                    IndexNode ccv = new IndexNode() { line = _lexer._line, row = _lexer._row };
                    ccv.value = node;
                    Advance();

                    ccv.index = ParseValue(VarType.Number);
                    if (ccv.index is null) return null;

                    if (Peek()._type is not TokenType.Bracket_R)
                    {
                        _error = 0x477;
                        return null;
                    }

                    Advance();

                    ccv.type = ArryToValu(node.type);

                    node = ccv;
                }
            }
            else if (_peek._type is TokenType.Paren_L)
            {
                if (node.type is VarType.Type)
                {
                    ArrayNode ccv = new ArrayNode() { line = node.line, row = node.row };

                    var by = node as LiteralValueNode;
                    if (by is null)
                    {
                        _error = 0x47C;
                        return null;
                    }
                    ccv.type = NtexToType(by.value);

                    if (ccv.type is < VarType.Char_Array)
                    {
                        _error = 0x47B;
                        return null;
                    }

                    Advance();

                    ccv.length = ParseValue(VarType.Number);
                    if (ccv.length is null) return null;

                    if (Peek()._type is not TokenType.Paren_R)
                    {
                        _error = 0x476;
                        return null;
                    }

                    Advance();

                    node = ccv;
                }
                else
                {
                    _error = 0x47D;
                    return null;
                }
            }

            if (stat is not 0)
            {
                CaluNode ap = new CaluNode() { line = bacl.Item1, row = bacl.Item2 };
                ap.value1 = node;
                ap.symbol = (byte)(12 + stat);//13:!,14:minus
                ap.type = node.type;


                if (stat is 1 && ap.type is not VarType.Boolean)
                {
                    _lexer._line = bacl.Item1;
                    _lexer._row = bacl.Item2;
                    _error = 0x498;
                    return null;
                }

                if (stat is 2 && ap.type is not VarType.Number and not VarType.Decimal)
                {
                    _lexer._line = bacl.Item1;
                    _lexer._row = bacl.Item2;
                    _error = 0x499;
                    return null;
                }

                node = ap;
            }

            return node;
        }

        //注意：最後にAdvance()を含む
        public ValueNode? ParseMulExpr()
        {
            var gh = ParseUnary();
            if (gh is null) return null;

            Peek();
            while (_peek._type is TokenType.Star or TokenType.Slash or TokenType.Percent)
            {
                CaluNode node = new CaluNode() { line = _lexer._line, row = _lexer._row };
                node.symbol = (byte)(2 + _peek._type - TokenType.Star);
                node.value1 = gh;
                if (node.value1.type is VarType.Type)
                {
                    _error = 0x493;
                    return null;
                }
                if (node.value1.type is VarType.Boolean)
                {
                    _error = 0x494;
                    return null;
                }
                if (node.type is >= VarType.Char_Array)
                {
                    _error = 0x492;
                    return null;
                }
                if (node.symbol is 4 && node.type is VarType.Decimal)
                {
                    _error = 0x496;
                    return null;
                }
                node.type = node.value1.type;
                Advance();
                Peek();
                node.value2 = ParseUnary();
                if (node.value2 is null) return null;

                if (node.type != node.value2.type)
                {
                    _error = 0x495;
                    return null;
                }

                gh = node;
            }
            return gh;
        }

        //注意：最後にAdvance()を含む
        public ValueNode? ParseAddExpr()
        {
            var gh = ParseMulExpr();
            if (gh is null) return null;

            Peek();
            while (_peek._type is TokenType.Plus or TokenType.Minus)
            {
                CaluNode node = new CaluNode() { line = _lexer._line, row = _lexer._row };
                node.symbol = (byte)(_peek._type - TokenType.Plus);
                node.value1 = gh;
                if (node.value1.type is VarType.Type)
                {
                    _error = 0x493;
                    return null;
                }
                if (node.value1.type is VarType.Boolean)
                {
                    _error = 0x494;
                    return null;
                }
                if (node.symbol is 1 && node.type is >= VarType.Char_Array)
                {
                    _error = 0x492;
                    return null;
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
                    _error = 0x495;
                    return null;
                }

                gh = node;
            }
            return gh;
        }

        //注意：最後にAdvance()を含む
        public ValueNode? ParseLiteral()
        {
            if (_peek._type is TokenType.Bracket_L)
            {
                LiteralArrayNode node = new LiteralArrayNode() { line = _lexer._line, row = _lexer._row };
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
                    else vf = true;

                    var fsp = ParseValue(VarType.Unknown, true);
                    if (fsp is null) return null;

                    if (fsp.type is VarType.String)
                    {
                        _error = 0x405;
                        return null;
                    }
                    else if (fsp.type is >= VarType.Char_Array)
                    {
                        _error = 0x407;
                        return null;
                    }

                    if (node.type is VarType.Unknown) node.type = ValuToArry(fsp.type);
                    else if (ArryToValu(node.type) != fsp.type)
                    {
                        _error = 0x409;
                        return null;
                    }

                    node.value.Add(fsp);

                    Peek();
                }

                if (!vf) node.type = VarType.Array;

                Advance();

                return node;
            }
            else
            {
                var node = new LiteralValueNode() { type = TotToType(_peek._type), value = _peek._text, line = _lexer._line, row = _lexer._row };
                Advance();

                if (node.type is VarType.Type)
                {
                    if (Peek()._type is TokenType.Bracket_L)
                    {
                        node.value += "[]";
                        Advance();
                        if (Peek()._type is not TokenType.Bracket_R)
                        {
                            _error = 0x477;
                            return null;
                        }
                        Advance();
                    }
                    node.value = Pcom_dat.TypeToStr(StrToType(node.value));
                }

                return node;
            }
        }

        //注意：最後にAdvance()を含む
        //Peek()が前提
        public ValueNode? ParseVarUse()
        {
            var ype = SearchName(_peek._text);
            if (ype is VarType.Unknown)
            {
                _error = 0x408;
                return null;
            }

            var ghs = SearchName2(_peek._text);
            if (ghs is null)
            {
                VarUseNode node = new VarUseNode() { line = _lexer._line, row = _lexer._row };
                node.type = ype;
                node.name = _peek._text;
                Advance();
                if (Peek()._type is not TokenType.Bracket_L) return node;

                if (node.type is < VarType.Char_Array)
                {
                    _error = 0x478;
                    return null;
                }

                IndexNode node2 = new IndexNode() { line = _lexer._line, row = _lexer._row };
                node2.type = ArryToValu(node.type);

                Advance();
                Peek();
                var gs = ParseValue(VarType.Number);
                if (gs is null) return null;
                node2.index = gs;

                node2.value = node;

                Advance();
                return node2;
            }
            else
            {
                CallNode node = new CallNode() { line = _lexer._line, row = _lexer._row };
                node.type = ype;
                node.name = _peek._text;

                Advance();
                Peek();
                if (_peek._type is not TokenType.Paren_L)
                {
                    _error = 0x467;
                    return null;
                }

                Advance();
                int i = 0;
                node.args = new ValueNode[ghs.Length];
                VarType suiso = VarType.Unknown; //配列型のタイプで保存
                while (i < ghs.Length)
                {
                    Peek();
                    if (i is not 0)
                    {
                        if (_peek._type is not TokenType.Comma)
                        {
                            _error = 0x468;
                            return null;
                        }

                        Advance();
                        Peek();
                    }

                    
                    VarType uuh = ghs[i];
                    if (uuh is VarType.Valable)
                    {
                        if (suiso is < VarType.Char_Array or VarType.Arrable or VarType.Array)
                        {
                            _error = 0x006;
                            return null;
                        }

                        uuh = ArryToValu(suiso);
                    }
                    else if (uuh is VarType.Arrable)
                    {
                        if (suiso is < VarType.Char_Array or VarType.Arrable or VarType.Array)
                        {
                            _error = 0x006;
                            return null;
                        }
                        uuh = suiso;
                    }

                    var ty = ParseValue(uuh);
                    if (ty is null) return null;
                    node.args[i] = ty;
                    if (uuh is VarType.Array) suiso = ty.type;
                    if (uuh is VarType.Value) suiso = ValuToArry(ty.type);

                    i++;
                }

                
                if (ype is VarType.Valable)
                {
                    if (suiso is < VarType.Char_Array or VarType.Arrable or VarType.Array)
                    {
                        _error = 0x006;
                        return null;
                    }

                    node.type = ArryToValu(suiso);
                    ype = node.type;
                }
                else if (ype is VarType.Arrable)
                {
                    if (suiso is < VarType.Char_Array or VarType.Arrable or VarType.Array)
                    {
                        _error = 0x006;
                        return null;
                    }
                    node.type = suiso;
                    ype = node.type;
                }

                Peek();
                if (_peek._type is not TokenType.Paren_R)
                {
                    _error = 0x469;
                    return null;
                }

                Advance();
                if (Peek()._type is not TokenType.Bracket_L) return node;

                if (node.type is < VarType.Char_Array)
                {
                    _error = 0x478;
                    return null;
                }

                IndexNode node2 = new IndexNode() { line = _lexer._line, row = _lexer._row };
                node2.type = ArryToValu(node.type);

                Advance();
                Peek();
                var gs = ParseValue(VarType.Number);
                if (gs is null) return null;
                node2.index = gs;

                node2.value = node;

                Advance();
                return node2;
            }
        }


        //Num tex to type
        public VarType NtexToType(string str)
        {
            if (!byte.TryParse(str, out var type)) type = 0;
            return (VarType)type;
        }


        public Token Advance()
        {
            if (_peek is null) return _lexer.Next();
            else
            {
                var ret = _peek;
                _peek = null;
                return ret;
            }
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
                _error = 0x400;
                return Lexer.NullToken;
            }
            return Advance();
        }

        public int GetError()
        {
            return (_lexer._error is 0) ? _error : _lexer._error;
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
                func_name.Add(new());
                func_name2.Add(new());
            }
            else
            {
                local_post++;
                local_name.Add(new());
                local_name2.Add(new());
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

        //型の取得
        internal VarType SearchName(string name)
        {
            int yu = global_name.IndexOf(name);
            if (yu is not -1)
            {
                if (global_name2[yu].Item2 is not null) global_name2[yu].Item2._must = true;
                return global_name2[yu].Item1.Item3;
            }

            if (Infunc)
            {
                for (int i = 0; i < func_name.Count; i++)
                {
                    yu = func_name[i].IndexOf(name);
                    if (yu is not -1)
                    {
                        if (func_name2[i][yu].Item2 is not null) func_name2[i][yu].Item2._must = true;
                        return func_name2[i][yu].Item1.Item3;
                    }
                }
            }
            else
            {
                for (int i = 0; i < local_name.Count; i++)
                {
                    yu = local_name[i].IndexOf(name);
                    if (yu is not -1)
                    {
                        if (local_name2[i][yu].Item2 is not null) local_name2[i][yu].Item2._must = true;
                        return local_name2[i][yu].Item1.Item3;
                    }
                }
            }
            return VarType.Unknown;
        }
        //引数の取得
        internal VarType[]? SearchName2(string name)
        {
            int yu = global_name.IndexOf(name);
            if (yu is not -1) return (VarType[]?)global_name2[yu].Item1.Item4;

            if (Infunc)
            {
                for (int i = 0; i < func_name.Count; i++)
                {
                    yu = func_name[i].IndexOf(name);
                    if (yu is not -1) return (VarType[]?)func_name2[i][yu].Item1.Item4;
                }
            }
            else
            {
                for (int i = 0; i < local_name.Count; i++)
                {
                    yu = local_name[i].IndexOf(name);
                    if (yu is not -1) return (VarType[]?)local_name2[i][yu].Item1.Item4;
                }
            }
            return null;
        }
        //IsConst（定数取得のためにglobalで調べるだけ）
        internal bool SearchName3(string name)
        {
            return global_name.IndexOf(name) is not -1;
        }


        internal bool RegisterName(string name, VarType type, object? arg, Node? node, bool global = false)
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
                global_name2.Add(((_lexer._line, _lexer._row, type, arg), node));
            }
            else if (Infunc)
            {
                func_name[func_post].Add(name);
                func_name2[func_post].Add(((_lexer._line, _lexer._row, type, arg), node));
            }
            else
            {
                local_name[local_post].Add(name);
                local_name2[local_post].Add(((_lexer._line, _lexer._row, type, arg), node));
            }

            if (node is not null && node is not VarDecNode and not VarInNode) node._must = false;

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