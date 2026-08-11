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
    internal enum NodeType
    {
        Unknown,
        Param,
        Literal,
        Literal_Scaler,
        Literal_Array,
        Def,
        Def_Const,
        Def_Func,
        Statement,
        Func,
        Return,
        Break,
        Continue,
        Var_Dec,
        Var_In,
        Var_Set,
        Value_Var,
        Value_Call,
        Value_Calu,
        Value,
        Var,
        Func_Use,
        Loop,
        If,
        Value_Literal,
        Value_LArray,
        Value_Array,
        Value_Index,

        HEAD_FUNC,
        HEAD_CONST,

        Base_Func,
        TextArray,
    }


    internal class Node
    {
        public int line = 0;
        public int row = 0;

        public bool _must = true;

        public NodeType Kind = NodeType.Unknown;

        public override string ToString()
        {
            return "loc:(" + line.ToString() + ", " + row.ToString() + "), Type:" + Kind.ToString();
        }

        internal virtual int Js_al => 10;

        internal virtual string Js(Pcom_dat data, Pcom_arg arg)
        {
            if (Kind is NodeType.Break) return "break;\n";
            else if (Kind is NodeType.Continue) return "continue;\n";

            throw new Exception();
        }

        internal virtual int Asm_al => 40;

        internal virtual string Asm(Pcom_dat data, Pcom_arg arg)
        {
            if (Kind is NodeType.Break) return "xor r15, r15\ninc r15\njmp .loop_" + arg._gid.ToString() + "_o\n";
            else if (Kind is NodeType.Continue) return "xor r15, r15\njmp .loop_" + arg._gid.ToString() + "_o\n";

            throw new Exception();
        }

        //Header（global）での呼び出しを想定
        internal virtual void Pre(Pcom_dat data)
        {
            return;
        }
    }

    internal class HeadNode : Node
    {
        public HeadNode((string[], (int, int, VarType, object?), string[]?) data, string code, bool must = true)
        {
            Name = data.Item1; Data = data.Item2; Code = code; Requir = (data.Item3 is null ? new string[0] : data.Item3);

            if (Data.Item4 is null) Kind = NodeType.HEAD_CONST;
            else Kind = NodeType.HEAD_FUNC;

            _must = must;
        }

        public string[] Name;

        public (int, int, VarType, object?) Data;

        public string Code;

        public string[] Requir;

        internal override int Js_al
        {
            get
            {
                if (Data.Item4 is null) return 0;
                return Code.Length;
            }
        }

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            if (Data.Item4 is null)
            {
                var call = Name[0];
                foreach (var item2 in Name)
                {
                    data.global_var.Add((item2, call, Pcom_dat.TypeToStr(Data.Item3)));
                }

                data._idata.Append(Code);
                return "";
            }
            else
            {
                var call = Header.GetName(Name[0]);
                foreach (var item2 in Name)
                {
                    data.global_var.Add((item2, call, (Data.Item3 is VarType.Unknown) ? "void" : ((Data.Item3 is >= VarType.Char_Array) ? "array" : "value")));
                }

                return Code;
            }
        }


        internal override int Asm_al
        {
            get
            {
                if (Data.Item4 is null) return 0;
                return Code.Length;
            }
        }

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            if (Data.Item4 is null)
            {
                var call = Name[0];
                foreach (var item2 in Name)
                {
                    data.global_var.Add((item2, (Data.Item3 is >= VarType.Char_Array ? call : "[" + call + "]"), Pcom_dat.TypeToStr(Data.Item3)));
                }

                data._idata.Append(Code);
                return "";
            }
            else
            {
                var call = Header.GetName(Name[0]);
                foreach (var item2 in Name)
                {
                    data.global_var.Add((item2, "call " + call, (Data.Item3 is VarType.Unknown) ? "void" : ((Data.Item3 is >= VarType.Char_Array) ? "array" : "value")));
                }

                return Code;
            }
        }

        internal override void Pre(Pcom_dat data)
        {
            if (!this._must) return;

            int i = 0;
            foreach (var item in data.Head.Data)
            {
                if (i == Requir.Length) break;

                foreach (var item2 in Requir)
                {
                    if (item.Name[0] == item2)
                    {
                        item._must = true;
                        i++;
                        break;
                    }
                }
            }
        }
    }

    internal class ParamNode : Node
    {
        public ParamNode()
        {
            Kind = NodeType.Param;
        }

        public VarType type = VarType.Unknown;
        public string name = "";

        public override string ToString()
        {
            return base.ToString() + ", type:" + type.ToString() + ", name:" + name;
        }

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            throw new Exception();
        }

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            throw new Exception();
        }
    }

    internal class LiteralValueNode : ValueNode
    {
        public LiteralValueNode()
        {
            Kind = NodeType.Value_Literal;
        }

        public string value = "";

        public override string ToString()
        {
            return base.ToString() + ", value:" + value.ToString();
        }

        internal override int Js_al => value.Length + 2;

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            if (type is VarType.Char) return "\"" + Pcom_dat.EscaJs(value) + "\"";
            else if (type is VarType.String or VarType.Char_Array) return "\"" + Pcom_dat.EscaJs(value) + "\".split(\"\")";
            else return value;
        }

        internal override int Asm_al => (type is VarType.String ? (value.Length * 40 + 70) : 25);

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            StringBuilder ui;
            if (type is VarType.String or VarType.Char_Array)
            {
                ui = new StringBuilder(value.Length * 40 + 70);

                byte[] rg = Pcom_dat.utf32_encode.GetBytes(value);

                ui.Append("mov rcx, ");
                ui.Append((rg.Length / 4).ToString());
                ui.Append("\ncall Pointer_Alloc\npush rax\npush r15\nlea r15, [rax+16]\n");
                for (int i = 0; i < rg.Length / 4; i++)
                {
                    ui.Append("mov rax, ");
                    ui.Append(Item.BitChanger.ToUInt32(rg, Item.ByteOrder.LittleEndian, i * 4).ToString());
                    ui.Append("\nmov qword [r15+" + (8 * i).ToString() + "], rax\n");
                }
                ui.Append("pop r15\npop rax\n");
            }
            else
            {
                ui = new StringBuilder(25);

                ui.Append("mov rax, ");

                if (type is VarType.Type or VarType.Number or VarType.Decimal) ui.Append(value);
                else if (type is VarType.Boolean) ui.Append(value is "true" ? "1" : "0");
                else if (type is VarType.Decimal)
                {
                    ui.Append(value);
                }
                else if (type is VarType.Char) ui.Append(Item.BitChanger.ToUInt32(Pcom_dat.utf32_encode.GetBytes(value), Item.ByteOrder.LittleEndian).ToString());
                else throw new Exception();

                ui.Append('\n');
            }

            return ui.ToString();
        }
    }

    internal class LiteralArrayNode : ValueNode
    {
        public LiteralArrayNode()
        {
            Kind = NodeType.Value_LArray;
        }

        public List<ValueNode> value = new List<ValueNode>();

        public override string ToString()
        {
            string str = "";
            foreach (var item in value)
            {
                str += item.ToString() + ", ";
            }
            return base.ToString() + ", value:{" + str + "}";
        }

        internal override int Js_al => value.Count * 10;

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            StringBuilder ui = new StringBuilder(value.Count * 10);
            {
                ui.Append('[');
                for (int i = 0; i < value.Count; i++)
                {
                    if (i is not 0) ui.Append(",");
                    ui.Append(value[i].Js(data, new()));
                }
                ui.Append(']');
            }

            return ui.ToString();
        }

        internal override int Asm_al => value.Count * 25 + 70;

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            StringBuilder ui = new StringBuilder(value.Count * 25 + 70);

            ui.Append("mov rcx, ");
            ui.Append(value.Count.ToString());
            ui.Append("\ncall Pointer_Alloc\npush rax\npush r15\nlea r15, [rax+16]\n");
            for (int i = 0; i < value.Count; i++)
            {
                ui.Append(value[i].Asm(data, new()));
                ui.Append("mov qword [r15+" + (8 * i).ToString() + "], rax\n");
            }
            ui.Append("pop r15\npop rax\n");

            return ui.ToString();
        }
    }

    internal class FuncBaseNode : Node
    {
        public FuncBaseNode()
        {
            Kind = NodeType.Base_Func;
        }

        public VarType type = VarType.Unknown;
        public string name = "";
        public List<ParamNode> param = new List<ParamNode>();

        public override string ToString()
        {
            string uu = base.ToString() + ", ret:" + type.ToString() + ", name:" + name + ", param:{";
            foreach (var item in param)
            {
                uu += item.ToString() + ",";
            }
            return uu + "}";
        }
    }

    internal class DefFuncNode : FuncBaseNode
    {
        public DefFuncNode()
        {
            Kind = NodeType.Def_Func;
        }

        public FuncNode? define = null;//define is null ? def only : def and func

        public override string ToString()
        {
            return base.ToString() + ", ";
        }

        internal override int Js_al => 75;

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            if (define is null)
            {
                //statement for ignore and deny errors
                string go = "(function (";

                for (int i = 0; i < param.Count; i++)
                {
                    if (i is not 0) go += ",";
                    go += "igq" + i.ToString() + "_" + new Random().Next(100);
                }

                go += "=>{return ";
                if (type is >= VarType.Char_Array) go += "[]";
                else if (type is VarType.Number or VarType.Decimal or VarType.Type) go += "0";
                else if (type is VarType.Boolean) go += "false";
                else if (type is VarType.Char) go += "\'\\0\'";
                go += ";})";


                data.Funcadd(name, type, go);
            }
            else
            {
                data.Funcadd(name, type, "func_" + define.___uuid___.ToString());
            }

            return "";
        }

        internal override int Asm_al => 75;

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            if (define is null)
            {
                //statement for ignore and deny errors
                string go = "push rbp\npush rbp\nmov rbp, rsp\n";

                for (int i = 0; i < param.Count; i++)
                {
                    if (param[i].type is >= VarType.Char_Array)
                    {
                        go += "mov rcx, qword [rbp+" + (16 * (1 + i)).ToString() + "]\n" + Pcom_dat._CACHE1;
                    }
                }

                if (type is >= VarType.Char_Array) go += "lea rax, [nullarray]\n";
                else if (type is not VarType.Unknown) go += "xor rax, rax\n";

                go += "pop rbp\npop rbp";


                data.Funcadd(name, type, go);
            }
            else
            {
                data.Funcadd(name, type, "call func_" + define.___uuid___.ToString());
            }

            return "";
        }
    }

    internal class StatementNode : Node
    {
        public StatementNode()
        {
            Kind = NodeType.Statement;
        }

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            return base.Js(data, arg);
        }

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            return base.Asm(data, arg);
        }
    }
    
    internal class FuncNode : FuncBaseNode
    {
        public FuncNode()
        {
            Kind = NodeType.Func;
        }

        public List<StatementNode> statements = new List<StatementNode>();

        public DefFuncNode? define = null;//define is null ? only : def and func
        public uint ___uuid___ = 0;

        public override string ToString()
        {
            string uu = base.ToString() + ", statements:{";
            foreach (var item in statements)
            {
                uu += item.ToString() + ",";
            }
            return uu + "}";
        }

        internal override int Js_al
        {
            get
            {
                int yu = 202 + (type >= VarType.Char_Array ? 44 : 0);
                foreach (var item in param)
                {
                    yu += item.Asm_al;
                }
                foreach (var item in statements)
                {
                    yu += item.Asm_al;
                }
                return yu;
            }
        }

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            var uuid = ___uuid___;
            var gip = uuid.ToString();

            var ARG = new Pcom_arg() { _gid = uuid };

            data.OpenFunc();
            string call = "func_" + gip;
            if (define is null) data.Funcadd(name, type, call);

            StringBuilder ui = new StringBuilder(260);

            ui.Append("async function ");
            ui.Append(call);
            ui.Append("(");

            for (int i = 0; i < param.Count; i++)
            {
                if (i is not 0) ui.Append(",");
                var item = param[i];
                var ty = data.Varadd(item.name);
                ui.Append(ty.Item1);
            }

            ui.Append("){\n");

            foreach (var item in statements) { ui.Append(item.Js(data, ARG)); }

            ui.Append("}\n");

            data.InFunc = false;
            return ui.ToString();
        }

        internal override int Asm_al
        {
            get
            {
                int yu = 202 + (type >= VarType.Char_Array ? 44 : 0);
                foreach (var item in param)
                {
                    yu += item.Asm_al;
                }
                foreach (var item in statements)
                {
                    yu += item.Asm_al;
                }
                return yu;
            }
        }

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            var uuid = ___uuid___;
            var gip = uuid.ToString();

            var ARG = new Pcom_arg() { _gid = uuid };

            data.OpenFunc();
            string call = "func_" + gip;
            if (define is null) data.Funcadd(name, type, "call " + call);

            StringBuilder ui = new StringBuilder(260);

            ui.Append(call);
            ui.Append(":\npush rbp\nmov rbp, rsp\n");

            for (int i = 0; i < param.Count; i++)
            {
                var item = param[i];
                VarDecNode vdc = new VarDecNode()
                {
                    name = item.name,
                    type = item.type,
                    value = new ArgNode()
                    {
                        index = i,
                        type = item.type
                    }
                };
                ui.Append(vdc.Asm(data, ARG));
            }

            foreach (var item in statements) { ui.Append(item.Asm(data, ARG)); }

            ui.Append(".func");
            ui.Append(gip);
            ui.Append("o:\nmov r15, rax\n");

            if (type is >= VarType.Char_Array) ui.Append("call Pointer_Return1;\n");

            ui.Append("sub rbp, rsp\ncmp rbp, 0\njna .func");
            ui.Append(gip);
            ui.Append("p\n.func");
            ui.Append(gip);
            ui.Append("w:\nmov rdi, qword [rsp+8]\ncmp rdi, 15\njna .func");
            ui.Append(gip);
            ui.Append("h\n.func");
            ui.Append(gip);
            ui.Append("r:\nmov rcx, qword [rsp]\ncall Pointer_Free\n.func");
            ui.Append(gip);
            ui.Append("h:\nadd rsp, 16\nsub rbp, 16\ncmp rbp, 0\nja .func");
            ui.Append(gip);
            ui.Append("w\n.func");
            ui.Append(gip);
            ui.Append("p:\nmov rax, r15\n");

            if (type is >= VarType.Char_Array) ui.Append("call Pointer_Return2\n");

            ui.Append("pop rbp\nret\n");

            data.InFunc = false;
            return ui.ToString();
        }

        internal override void Pre(Pcom_dat data)
        {
            ___uuid___ = data._uuid;
            data._uuid++;
        }
    }

    internal class ArgNode : ValueNode
    {
        public int index = 0;

        internal override int Asm_al => 24;

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            throw new Exception();
        }

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            return "mov rax, qword [rbp+" + (16 * (1 + index)).ToString() + "]\n";
        }
    }
    
    internal class DefConstNode : Node
    {
        public DefConstNode()
        {
            Kind = NodeType.Def_Const;
        }

        public VarType type = VarType.Unknown;
        public string name = "";
        public ValueNode? value = null;

        public override string ToString()
        {
            return base.ToString() + ", type:" + type.ToString() + ", name:" + name + ", value:<" + value.ToString() + ">";
        }

        internal override int Js_al => 0;

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            var st = data.Constadd(name, type);

            if (value is null) throw new Exception();

            StringBuilder scv = new StringBuilder(16);
            scv.Append("var const");
            scv.Append(st.ToString());
            scv.Append(" = ");

            if (value is LiteralValueNode v1) scv.Append(v1.Js(data, new()));
            else if (value is LiteralArrayNode v2) scv.Append(v2.Js(data, new()));
            else throw new Exception();

            scv.Append(";\n");

            data._idata.Append(scv);
            return "";
        }

        internal override int Asm_al => 0;

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            var st = data.Constadd(name, type);

            if (value is null) throw new Exception();

            StringBuilder scv = new StringBuilder(16);
            scv.Append("    const");
            scv.Append(st.ToString());
            scv.Append(" dq ");

            if (value is LiteralValueNode v1) scv.Append(Pcom_dat.LiteralToData(v1));
            else if (value is LiteralArrayNode v2)
            {
                scv.Append(ulong.MaxValue.ToString());
                scv.Append(',');
                scv.Append(v2.value.Count.ToString());

                for (int i = 0; i < v2.value.Count; i++)
                {
                    scv.Append(',');
                    if (v2.value[i] is LiteralValueNode v3) scv.Append(Pcom_dat.LiteralToData(v3));
                    else throw new Exception();
                }
            }
            else throw new Exception();

            scv.Append("\n");

            data._idata.Append(scv);
            return "";
        }
    }
    
    internal class ReturnNode : StatementNode
    {
        public ReturnNode()
        {
            Kind = NodeType.Return;
        }

        public bool IsVoid = false;
        public ValueNode? value = null;

        public override string ToString()
        {
            return base.ToString() + ", IsVoid:" + IsVoid.ToString() + ", value:<" + (value is null ? "null" : value.ToString()) + ">";
        }

        internal override int Js_al => 11;

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            string ui = "return";
            if (!IsVoid)
            { 
                if (value is null) throw new Exception();
                ui += " ";
                ui += value.Js(data, new() { JS_prefix = false });
            }

            ui += ";\n";

            return ui;
        }

        internal override int Asm_al => 13 + (!IsVoid && value is not null ? value.Asm_al : 0);

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            string ui = "";
            if (!IsVoid)
            {
                if (value is null) throw new Exception();

                ui += value.Asm(data, new());
            }

            ui += "jmp .func" + arg._gid.ToString() + "o\n";

            return ui;
        }
    }

    internal class ValueNode : Node
    {
        public ValueNode()
        {
            Kind = NodeType.Value;
        }

        public VarType type = VarType.Unknown;

        public override string ToString()
        {
            return base.ToString() + ", type:" + type.ToString();
        }

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            throw new Exception();
        }

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            throw new Exception();
        }
    }

    internal class VarNode : StatementNode
    {
        public VarNode()
        {
            Kind = NodeType.Var;
        }

        public VarType type = VarType.Unknown;
        public ValueNode? value = null;
        public string name = "";

        public override string ToString()
        {
            return base.ToString() + ", type:" + type.ToString() + ", name:" + name.ToString() + ", value:<" + (value is null ? "null" : value.ToString()) + ">";
        }

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            throw new Exception();
        }

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            throw new Exception();
        }
    }
    
    internal class VarUseNode : ValueNode
    {
        public VarUseNode()
        {
            Kind = NodeType.Value_Var;
        }

        public string name = "";

        public override string ToString()
        {
            return base.ToString() + ", name:" + name;
        }

        internal override int Js_al => 6;

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            var st = data.GetVar(name);
            if (type is >= VarType.Char_Array && arg.JS_prefix) return $"[...{st.Item2}]";
            return st.Item2;
        }

        internal override int Asm_al => 14;

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            var st = data.GetVar(name);
            return "mov rax, " + st.Item2 + "\n";
        }
    }
    
    internal class CallNode : ValueNode
    {
        public CallNode()
        {
            Kind = NodeType.Value_Call;
        }

        public string name = "";
        public ValueNode[] args = new ValueNode[0];

        public override string ToString()
        {
            string ui = base.ToString() + ", name:" + name + ", args:{";
            foreach (var item in args)
            {
                ui += item.ToString() + ", ";
            }
            return ui + "}";
        }

        internal override int Js_al => (this.args.Length * 6 + 8);

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            var st = data.GetVar(name);
            string ui = "(await " + st.Item2 + "(";
            if (this.args is not null)
            {
                for (int i = 0; i < this.args.Length; i++)
                {
                    if (i is not 0) ui += ",";
                    ui += this.args[i].Js(data, new());
                }
            }

            ui += "))";

            return ui;
        }

        internal override int Asm_al => (this.args.Length * 45 + 10);

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            var st = data.GetVar(name);
            string ui = "";

            if (this.args is not null)
            {
                for (int i = this.args.Length - 1; i >= 0; i--)
                {
                    ui += this.args[i].Asm(data, new()) + "push rax\npush rax\n";
                }
            }

            ui += st.Item2 + "\n";

            if (this.args is not null)
            {
                for (int i = 0; i < this.args.Length; i++)
                {
                    ui += "pop rcx\npop rcx\n";
                }
            }

            return ui;
        }
    }
    
    internal class CaluNode : ValueNode
    {
        public CaluNode()
        {
            Kind = NodeType.Value_Calu;
        }

        //0:+,1:-,2:*,3:/,4:%,5:&,6:|,7:==,8:!=,9:>=,10:>.11<=,12:<,13:!,14:minus掛け
        public byte symbol = 0;
        public ValueNode? value1 = null;
        public ValueNode? value2 = null;

        public override string ToString()
        {
            return base.ToString() + ", symbol:" + symbol.ToString() + ", value1:<" + (value1 is null ? "null" : value1.ToString()) + ">, value2:<" + (value2 is null ? "null" : value2.ToString()) + ">";
        }


        internal override int Js_al => 15;

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            if (value1 is null) throw new Exception();

            string ui;
            if (symbol is 13 or 14)
            {
                if (symbol is 14 && value1.type is VarType.Number) ui = "(-(" + value1.Js(data, new()) + "))";
                else if (symbol is 13 && value1.type is VarType.Boolean) ui = "(!(" + value1.Js(data, new()) + "))";
                else if (symbol is 14 && value1.type is VarType.Decimal) ui = "(-(" + value1.Js(data, new()) + "))";
                else throw new Exception();
            }
            else
            {
                if (value2 is null) throw new Exception();
                ui = "(";
                var a = value1.Js(data, new() { JS_prefix = false });
                var b = value2.Js(data, new() { JS_prefix = false });

                if (value1.type is VarType.Number)
                {
                    if (symbol is 0) ui += $"{a}+{b}";
                    else if (symbol is 1) ui += $"{a}-{b}";
                    else if (symbol is 2) ui += $"{a}*{b}";
                    else if (symbol is 3) ui += $"{a}/{b}";
                    else if (symbol is 4) ui += $"{a}%{b}";

                    else if (symbol is 7) ui += $"{a}=={b}";
                    else if (symbol is 8) ui += $"{a}!={b}";
                    else if (symbol is 9) ui += $"{a}>={b}";
                    else if (symbol is 10) ui += $"{a}>{b}";
                    else if (symbol is 11) ui += $"{a}<={b}";
                    else if (symbol is 12) ui += $"{a}<{b}";

                    else throw new Exception();
                }
                else if (value1.type is VarType.Char)
                {
                    if (symbol is 0) ui += $"String.fromCodePoint({a}.codePointAt(0)+{b}.codePointAt(0))";
                    else if (symbol is 1) ui += $"String.fromCodePoint({a}.codePointAt(0)-{b}.codePointAt(0))";
                    else if (symbol is 2) ui += $"String.fromCodePoint({a}.codePointAt(0)*{b}.codePointAt(0))";
                    else if (symbol is 3) ui += $"String.fromCodePoint({a}.codePointAt(0)/{b}.codePointAt(0))";
                    else if (symbol is 4) ui += $"String.fromCodePoint({a}.codePointAt(0)%{b}.codePointAt(0))";

                    else if (symbol is 7) ui += $"{a}.codePointAt(0)=={b}.codePointAt(0)";
                    else if (symbol is 8) ui += $"{a}.codePointAt(0)!={b}.codePointAt(0)";
                    else if (symbol is 9) ui += $"{a}.codePointAt(0)>={b}.codePointAt(0)";
                    else if (symbol is 10) ui += $"{a}.codePointAt(0)>{b}.codePointAt(0)";
                    else if (symbol is 11) ui += $"{a}.codePointAt(0)<={b}.codePointAt(0)";
                    else if (symbol is 12) ui += $"{a}.codePointAt(0)<{b}.codePointAt(0)";

                    else throw new Exception();
                }
                else if (value1.type is VarType.Decimal)
                {
                    if (symbol is 0) ui += $"{a}+{b}";
                    else if (symbol is 1) ui += $"{a}-{b}";
                    else if (symbol is 2) ui += $"{a}*{b}";
                    else if (symbol is 3) ui += $"{a}/{b}";

                    else if (symbol is 7) ui += $"{a}=={b}";
                    else if (symbol is 8) ui += $"{a}!={b}";
                    else if (symbol is 9) ui += $"{a}>={b}";
                    else if (symbol is 10) ui += $"{a}>{b}";
                    else if (symbol is 11) ui += $"{a}<={b}";
                    else if (symbol is 12) ui += $"{a}<{b}";

                    else throw new Exception();
                }
                else if (value1.type is >= VarType.Char_Array)
                {
                    if (symbol is 0) ui += $"{a}.concat({b})";

                    else if (symbol is 7) ui += $"ArrayCom({a},{b})";
                    else if (symbol is 8) ui += $"(!ArrayCom({a},{b}))";

                    else throw new Exception();
                }
                else if (value1.type is VarType.Boolean)
                {
                    if (symbol is 5) ui += $"{a}&&{b}";
                    else if (symbol is 6) ui += $"{a}||{b}";

                    else if (symbol is 7) ui += $"{a}=={b}";
                    else if (symbol is 8) ui += $"{a}!={b}";
                    else if (symbol is 9) ui += $"{a}>={b}";
                    else if (symbol is 10) ui += $"{a}>{b}";
                    else if (symbol is 11) ui += $"{a}<={b}";
                    else if (symbol is 12) ui += $"{a}<{b}";
                    else throw new Exception();
                }
                else
                {
                    if (symbol is 7) ui += $"{a}=={b}";
                    else if (symbol is 8) ui += $"{a}!={b}";
                    else throw new Exception();
                }

                ui += ")";
            }

            return ui;
        }
        internal override int Asm_al => 80;

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            if (value1 is null) throw new Exception();



            string ui;
            if (symbol is 13 or 14)
            {
                ui = value1.Asm(data, new());
                if (symbol is 14 && value1.type is VarType.Number) ui += "neg rax\n";
                else if (symbol is 13 && value1.type is VarType.Boolean) ui += "xor rax, 1\n";
                else if (symbol is 14 && value1.type is VarType.Decimal) ui += "xor rax, [NegZero]\n";
                else throw new Exception();
            }
            else
            {
                if (value2 is null) throw new Exception();
                ui = value1.Asm(data, new()) + "push rax\npush rax\n" + value2.Asm(data, new()) + "\nmov rdi, rax\npop rax\npop rax\nmov rsi, rax\n";

                //rsi(rax)とrdi
                if (value1.type is VarType.Number or VarType.Char)
                {
                    if (symbol is 0) ui += "add rax, rdi\n";
                    else if (symbol is 1) ui += "sub rax, rdi\n";
                    else if (symbol is 2) ui += "imul rax, rdi\n";
                    else if (symbol is 3) ui += "test rdi, rdi\nmov rsi, 1\ncmovz rdi, rsi\ncqo\nidiv rdi\n";
                    else if (symbol is 4) ui += "test rdi, rdi\nmov rsi, 1\ncmovz rdi, rsi\ncqo\nidiv rdi\nmov rax, rdx\n";

                    else if (symbol is 7) ui += "xor rax, rax\ncmp rsi, rdi\nsetz al\n";
                    else if (symbol is 8) ui += "xor rax, rax\ncmp rsi, rdi\nsetnz al\n";
                    else if (symbol is 9) ui += "xor rax, rax\ncmp rsi, rdi\nsetge al\n";
                    else if (symbol is 10) ui += "xor rax, rax\ncmp rsi, rdi\nsetg al\n";
                    else if (symbol is 11) ui += "xor rax, rax\ncmp rsi, rdi\nsetle al\n";
                    else if (symbol is 12) ui += "xor rax, rax\ncmp rsi, rdi\nsetl al\n";

                    else throw new Exception();
                }
                else if (value1.type is VarType.Decimal)
                {
                    ui += "movq xmm0, rax\nmovq xmm1, rdi\n";

                    if (symbol is 0) ui += "addsd xmm0, xmm1\nmovq rax, xmm0\n";
                    else if (symbol is 1) ui += "subsd xmm0, xmm1\nmovq rax, xmm0\n";
                    else if (symbol is 2) ui += "mulsd xmm0, xmm1\nmovq rax, xmm0\n";
                    else if (symbol is 3) ui += "divsd xmm0, xmm1\nmovq rax, xmm0\n";

                    else if (symbol is 7) ui += "xor rax, rax\nxor rsi, rsi\nucomisd xmm0, xmm1\nsetz al\nsetnp sil\nand al, sil\n";
                    else if (symbol is 8) ui += "xor rax, rax\nxor rsi, rsi\nucomisd xmm0, xmm1\nsetnz al\nsetp sil\nor al, sil\n";
                    else if (symbol is 9) ui += "xor rax, rax\nxor rsi, rsi\nucomisd xmm0, xmm1\nsetae al\nsetnp sil\nand al, sil\n";
                    else if (symbol is 10) ui += "xor rax, rax\nxor rsi, rsi\nucomisd xmm0, xmm1\nseta al\nsetnp sil\nand al, sil\n";
                    else if (symbol is 11) ui += "xor rax, rax\nxor rsi, rsi\nucomisd xmm0, xmm1\nsetbe al\nsetnp sil\nand al, sil\n";
                    else if (symbol is 12) ui += "xor rax, rax\nxor rsi, rsi\nucomisd xmm0, xmm1\nsetb al\nsetnp sil\nand al, sil\n";

                    else throw new Exception();
                }
                else if (value1.type is >= VarType.Char_Array)
                {
                    if (symbol is 0) ui += "call Array_Add\n";

                    else if (symbol is 7) ui += "call Array_Com\n";
                    else if (symbol is 8) ui += "call Array_Com\nxor rax, 1\n";

                    else throw new Exception();

                    ui += "push rax\npush rax\nlea rcx, [rsi]\n" + Pcom_dat._CACHE1 + "lea rcx, [rdi]\n" + Pcom_dat._CACHE1 + "pop rax\npop rax\n";
                }
                else if (value1.type is VarType.Boolean)
                {
                    if (symbol is 5) ui += "and rax, rdi\n";
                    else if (symbol is 6) ui += "or rax, rdi\n";

                    else if (symbol is 7) ui += "xor rax, rax\ncmp rsi, rdi\nsetz al\n";
                    else if (symbol is 8) ui += "xor rax, rax\ncmp rsi, rdi\nsetnz al\n";
                    else if (symbol is 9) ui += "xor rax, rax\ncmp rsi, rdi\nsetge al\n";
                    else if (symbol is 10) ui += "xor rax, rax\ncmp rsi, rdi\nsetg al\n";
                    else if (symbol is 11) ui += "xor rax, rax\ncmp rsi, rdi\nsetle al\n";
                    else if (symbol is 12) ui += "xor rax, rax\ncmp rsi, rdi\nsetl al\n";
                    else throw new Exception();
                }
                else
                {
                    if (symbol is 7) ui += "xor rax, rax\ncmp rsi, rdi\nsetz al\n";
                    else if (symbol is 8) ui += "xor rax, rax\ncmp rsi, rdi\nsetnz al\n";
                    else throw new Exception();
                }
            }

            return ui;
        }
    }
    
    internal class ArrayNode : ValueNode
    {
        public ArrayNode()
        {
            Kind = NodeType.Value_Array;
        }

        public ValueNode? length = null;

        public override string ToString()
        {
            return base.ToString() + ", length:<" + (length is null ? "null" : length.ToString()) + ">";
        }

        internal override int Js_al
        {
            get
            {
                return (length is null ? 0 : length.Asm_al) + 35;
            }
        }

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            if (length is null) throw new Exception();
            return $"[...Array({length.Js(data, new())})].map(x => {Pcom_dat.InivalJs(Pcom_dat.AryToVal(type))})";
        }


        internal override int Asm_al
        {
            get
            {
                return (length is null ? 0 : length.Asm_al) + 35;
            }
        }

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            if (length is null) throw new Exception();

            StringBuilder ui = new StringBuilder(length.Asm_al + 35);

            ui.Append(length.Asm(data, new()));
            ui.Append("mov rcx, rax\ncall Pointer_Alloc\n");

            return ui.ToString();
        }

    }
    
    internal class FuncUseNode : StatementNode
    {
        public FuncUseNode()
        {
            Kind = NodeType.Func_Use;
        }

        public string name = "";
        public List<ValueNode>? args = null;

        public override string ToString()
        {
            string ui = base.ToString() + ", name:" + name + ", args:{";
            if (args is null)
            {
                ui += "null";
            }
            else
            {
                foreach (var item in args)
                {
                    ui += item.ToString() + ", ";
                }
            }
            return ui + "}";
        }

        internal override int Js_al => ((this.args is null ? 0 : this.args.Count) * 40 + 45);

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            var st = data.GetVar(name);
            string ui = "await " + st.Item2 + "(";
            if (this.args is not null)
            {
                for (int i = 0; i < this.args.Count; i++)
                {
                    if (i is not 0) ui += ",";

                    ui += this.args[i].Js(data, new());
                }
            }

            ui += ");\n";

            return ui;
        }

        internal override int Asm_al => ((this.args is null ? 0 : this.args.Count) * 40 + 45);

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            var st = data.GetVar(name);
            string ui = "";

            if (this.args is not null)
            {
                for (int i = this.args.Count - 1; i >= 0; i--)
                {
                    ui += this.args[i].Asm(data, new()) + "push rax\npush rax\n";
                }

            }

            ui += st.Item2 + "\n";

            if (st.Item3 is "array") ui += "mov rcx, rax\ncall Pointer_Free\n";

            if (this.args is not null)
            {
                for (int i = 0; i < this.args.Count; i++)
                {
                    ui += "pop rcx\npop rcx\n";
                }
            }

            return ui;
        }
    }
    
    internal class IndexNode : ValueNode
    {
        public IndexNode()
        {
            Kind = NodeType.Value_Index;
        }

        public ValueNode? value = null;

        public ValueNode? index = null;

        public override string ToString()
        {
            return base.ToString() + ", value:{" + (value is null ? "null" : value.ToString()) + "}, index:{" + (index is null ? "null" : index.ToString()) + "}";
        }

        internal override int Js_al => 160 + (index is null ? 0 : index.Asm_al) + (value is null ? 0 : value.Asm_al);

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            if (value is null || index is null) throw new Exception();
            return $"Indexer_GET({value.Js(data, new() { JS_prefix = false })},{index.Js(data, new())},{Pcom_dat.InivalJs(type)})";
        }

        internal override int Asm_al => 160 + (index is null ? 0 : index.Asm_al) + (value is null ? 0 : value.Asm_al);

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            if (value is null || index is null) throw new Exception();
            return value.Asm(data, new()) + "push rax\npush rax\n" + index.Asm(data, new()) + "mov rsi, rax\npop rax\nlea rdi, [rax]\ncall Indexer\nmov rbx, rax\npop rax\nlea rcx, [rax]\n" + Pcom_dat._CACHE1 + "mov rax, rbx\n";
        }
    }
    
    internal class VarDecNode : VarNode
    {
        public VarDecNode()
        {
            Kind = NodeType.Var_Dec;
        }

        internal override int Js_al => (value is null ? 0 : value.Asm_al + 36) + 64;

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            var ty = data.Varadd(name);
            string ui = "let " + ty.Item1 + " = ";
            if (value is null) ui += Pcom_dat.InivalJs(type);
            else ui += value.Js(data, new());
            ui += ";\n";
            return ui;
        }

        internal override int Asm_al => (value is null ? 0 : value.Asm_al + 36) + 64;

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            var ty = data.Varadd(name);
            string ui = "sub rsp, 16\nlea rcx, " + ty.Item1 + "\nmov rdx, " + Pcom_dat.TypeToStr(type) + "\ncall Var_Alloc\n";
            if (value is not null) ui += value.Asm(data, new()) + "lea rcx, " + ty.Item1 + "\ncall Var_Realloc\n";

            return ui;
        }
    }
    
    internal class VarInNode : VarNode
    {
        public VarInNode()
        {
            Kind = NodeType.Var_In;
        }


        public override string ToString()
        {
            return base.ToString();
        }

        internal override int Js_al => 40 + (value is null ? 0 : value.Asm_al);

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            var ty = data.GetVar(name);
            return (value is null ? "" : ty.Item2 + " = " + value.Js(data, new())) + ";\n";
        }

        internal override int Asm_al => 40 + (value is null ? 0 : value.Asm_al);

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            var ty = data.GetVar(name);
            string ui = (value is null ? "" : value.Asm(data, new())) + "lea rcx, " + ty.Item2 + "\ncall Var_Realloc\n";
            return ui;
        }
    }
    
    internal class VarSetNode : VarInNode
    {
        public VarSetNode()
        {
            Kind = NodeType.Var_Set;
        }

        public ValueNode? index = null;

        public override string ToString()
        {
            return base.ToString() + ", index:{" + (index is null ? "null" : index.ToString()) + "}";
        }

        internal override int Js_al => 40 + (value is null ? 0 : value.Asm_al);

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            var ty = data.GetVar(name);
            return $"Indexer_SET({ty.Item2},{(index is null ? "0" : index.Js(data, new()))},{(value is null ? "" : value.Js(data, new()))});\n";
        }

        internal override int Asm_al => 40 + (value is null ? 0 : value.Asm_al);

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            var ty = data.GetVar(name);
            string ui = (value is null ? "" : value.Asm(data, new())) + "push rax\npush rax\n" + (index is null ? "" : index.Asm(data, new())) + "lea rcx, " + ty.Item2 + "\nmov rdi, rax\npop rax\npop rax\ncall Indeset\n";
            return ui;
        }
    }

    internal class LoopNode : StatementNode
    {
        public LoopNode()
        {
            Kind = NodeType.Loop;
        }

        public bool infinity = true;
        public ValueNode? num = null;
        public List<StatementNode> statements = new List<StatementNode>();

        public override string ToString()
        {
            string uu = base.ToString() + ", infinity:" + infinity.ToString() + ", num:{" + (num is null ? "null" : num.ToString()) + "}, statements:{";
            foreach (var item in statements)
            {
                uu += item.ToString() + ",";
            }
            return uu + "}";
        }

        internal override int Js_al
        {
            get
            {
                int yu = 453;
                if (!infinity && num is not null)
                {
                    yu += 49;
                    yu += num.Js_al;
                }

                foreach (var item in statements)
                {
                    yu += item.Js_al;
                }

                return yu;
            }
        }

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            uint uuid = data._uuid;
            data._uuid++;

            string cid = uuid.ToString();

            StringBuilder code = new StringBuilder(Asm_al);
            
            if (num is not null && !infinity)
            {
                code.Append("let _");
                code.Append(cid);
                code.Append("_i = ");
                code.Append(num.Js(data, new()));
                code.Append(";\nwhile(_");
                code.Append(cid);
                code.Append("_i>0");                
            }
            else code.Append("while(true");

            code.Append("){\n");

            data.Incpas(true);
            foreach (var item in statements)
            {
                code.Append(item.Js(data, new Pcom_arg() { _gid = uuid }));
            }
            data.Decpas();

            if (!infinity)
            {
                code.Append("_");
                code.Append(cid);
                code.Append("_i--;\n");
            }
            code.Append("}\n");

            return code.ToString();
        }

        internal override int Asm_al
        {
            get
            {
                int yu = 453;
                if (!infinity && num is not null)
                {
                    yu += 49;
                    yu += num.Asm_al;
                }

                foreach (var item in statements)
                {
                    yu += item.Asm_al;
                }

                return yu;
            }
        }

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            uint uuid = data._uuid;
            data._uuid++;

            string cid = uuid.ToString();

            StringBuilder code = new StringBuilder(Asm_al);

            code.Append("mov rdi, rsp\nsub rdi, 16\npush r14\nmov r14, rdi\n");

            if (num is not null && !infinity) code.Append(num.Asm(data, new()));
            else code.Append("xor rax, rax\ninc rax\n");

            code.Append("push rax\ncmp rax, 0\njng .loop_");
            code.Append(cid);
            code.Append("_e\n.loop_");
            code.Append(cid);
            code.Append("_s:\n");

            data.Incpas(true);
            foreach (var item in statements)
            {
                code.Append(item.Asm(data, new Pcom_arg() { _gid = uuid }));
            }
            data.Decpas();


            code.Append('\n');

            code.Append("xor r15, r15\n.loop_");
            code.Append(cid);
            code.Append("_o:\nmov rsi, r14\nsub rsi, rsp\ncmp rsi, 0\njna .loop_");
            code.Append(cid);
            code.Append("_p\n.loop_");
            code.Append(cid);
            code.Append("_w:\nmov rdi, qword [rsp+8]\ncmp rdi, 15\njna .loop_");
            code.Append(cid);
            code.Append("_h\n.loop_");
            code.Append(cid);
            code.Append("_r:\nmov rcx, qword [rsp]\ncall Pointer_Free\n.loop_");
            code.Append(cid);
            code.Append("_h:\nadd rsp, 16\nsub rsi, 16\ncmp rsi, 0\nja .loop_");
            code.Append(cid);
            code.Append("_w\n.loop_");
            code.Append(cid);
            code.Append("_p:\ncmp r15, 1\nje .loop_");
            code.Append(cid);
            code.Append("_e\n");

            if (!infinity) code.Append("pop rdi\ndec rdi\npush rdi\ncmp rdi, 0\n");

            code.Append("jne .loop_");
            code.Append(cid);
            code.Append("_s\n.loop_");
            code.Append(cid);
            code.Append("_e:\npop rax\npop r14\n");

            return code.ToString();
        }
    }

    internal class IfNode : StatementNode
    {
        public IfNode()
        {
            Kind = NodeType.If;
        }

        public List<ValueNode?> condition = new List<ValueNode?>(1);//value : if or elif, null : else
        public List<List<StatementNode>> statements = new List<List<StatementNode>>(1);

        public override string ToString()
        {
            string uu = base.ToString() + ", value:{";
            for (int i = 0; i < condition.Count; i++)
            {
                uu += "\n{condition:{" + (condition[i] is null ? "null" : condition[i].ToString()) + "}, \nstatements:{";
                foreach (var item in statements[i])
                {
                    uu += "\n" + item.ToString() + ",";
                }
                uu += "}, ";
            }
            return uu + "}";
        }

        internal override int Js_al
        {
            get
            {
                int yu = 25;

                foreach (var item in condition)
                {
                    if (item is null) yu += 14;
                    else yu += item.Asm_al + 26;
                }

                foreach (var item in statements)
                {
                    yu += 286;
                    foreach (var item2 in item)
                    {
                        yu += item2.Asm_al;
                    }
                }

                return yu;
            }
        }

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            uint uuid = data._uuid;
            data._uuid++;

            string ciod = uuid.ToString();

            StringBuilder code = new StringBuilder(condition.Count * 300 + 20 + 240 * condition.Count);

            for (int i = 0; i < condition.Count; i++)
            {
                string inf = i.ToString();
                if (condition[i] is null) code.Append("else{\n");
                else
                {
                    if (i is not 0) code.Append("else ");
                    code.Append("if (");
                    code.Append(condition[i].Js(data, new()));
                    code.Append("){\n");
                }

                data.Incpas();
                foreach (var item in statements[i])
                {
                    code.Append(item.Js(data, new Pcom_arg() { _gid = arg._gid }));
                }
                data.Decpas();

                code.Append("}\n");
            }

            return code.ToString();
        }

        internal override int Asm_al
        {
            get
            {
                int yu = 25;

                foreach (var item in condition)
                {
                    if (item is null) yu += 14;
                    else yu += item.Asm_al + 26;
                }

                foreach (var item in statements)
                {
                    yu += 286;
                    foreach (var item2 in item)
                    {
                        yu += item2.Asm_al;
                    }
                }

                return yu;
            }
        }

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            uint uuid = data._uuid;
            data._uuid++;

            string ciod = uuid.ToString();

            StringBuilder code = new StringBuilder(condition.Count * 300 + 20);
            StringBuilder code2 = new StringBuilder(240 * condition.Count);

            for (int i = 0; i < condition.Count; i++)
            {
                string inf = i.ToString();
                if (condition[i] is null)
                {
                    code.Append("jmp .if_");
                    code.Append(ciod);
                    code.Append("_s");
                    code.Append(inf);
                }
                else
                {
                    code.Append(condition[i].Asm(data, new()));
                    code.Append("cmp rax, 1\nje .if_");
                    code.Append(ciod);
                    code.Append("_s");
                    code.Append(inf);
                }
                code.Append('\n');

                code2.Append(".if_");
                code2.Append(ciod);
                code2.Append("_s");
                code2.Append(inf);
                code2.Append(":\n");

                int ccc = 0;
                data.Incpas();
                foreach (var item in statements[i])
                {
                    if (item is VarDecNode) ccc++;
                    code2.Append(item.Asm(data, new Pcom_arg() { _gid = arg._gid }));
                }
                data.Decpas();
                ccc *= 16;

                code2.Append("mov rsi, ");
                code2.Append(ccc.ToString());
                code2.Append("\ncmp rsi, 0\njna .if_");
                code2.Append(ciod);
                code2.Append("_p\n.if_");
                code2.Append(ciod);
                code2.Append("_w");
                code2.Append(inf);
                code2.Append(":\nmov rdi, qword [rsp+8]\ncmp rdi, 15\njna .if_");
                code2.Append(ciod);
                code2.Append("_h");
                code2.Append(inf);
                code2.Append("\nmov rcx, qword [rsp]\ncall Pointer_Free\n.if_");
                code2.Append(ciod);
                code2.Append("_h");
                code2.Append(inf);
                code2.Append(":\nadd rsp, 16\nsub rsi, 16\ncmp rsi, 0\nja .if_");
                code2.Append(ciod);
                code2.Append("_w");
                code2.Append(inf);
                code2.Append("\njmp .if_");
                code2.Append(ciod);
                code2.Append("_p\n");
            }

            code.Append("jmp .if_");
            code.Append(ciod);
            code.Append("_p\n");

            code.Append(code2);

            code.Append(".if_");
            code.Append(ciod);
            code.Append("_p:\n");

            return code.ToString();
        }
    }

    internal class TextArrayNode : ValueNode
    {
        public TextArrayNode()
        {
            Kind = NodeType.TextArray;
            type = VarType.Char_Array;
        }

        public List<ValueNode> items = new List<ValueNode>();

        public override string ToString()
        {
            var ba = base.ToString() + ", items:{";
            foreach (var item in items)
            {
                ba += "<" + item.ToString() + ">,";
            }
            ba += "}";
            return ba;
        }

        internal override int Js_al
        {
            get
            {
                return items.Count * 160 + 20;
            }
        }

        internal override string Js(Pcom_dat data, Pcom_arg arg)
        {
            StringBuilder ui = new StringBuilder(items.Count * 160 + 20);

            ui.Append('(');
            foreach (var item in items)
            {
                ui.Append(item.Asm(data, new()));
                ui.Append("+ TextArray_MarkText + ");
            }
            ui.Append("[])");

            return ui.ToString();
        }

        internal override int Asm_al
        {
            get
            {
                return items.Count * 160 + 20;
            }
        }

        internal override string Asm(Pcom_dat data, Pcom_arg arg)
        {
            StringBuilder ui = new StringBuilder(items.Count * 160 + 20);

            ui.Append("lea rax, [nullarray]\n");

            foreach (var item in items)
            {
                ui.Append("push rax\npush rax\n");
                ui.Append(item.Asm(data, new()));
                ui.Append("mov rdi, TextArray_MarkText\nmov rsi, rax\ncall Array_Add\nmov rdi, rax\npop rax\npop rax\nmov rsi, rax\ncall Array_Add\n");
            }

            return ui.ToString();
        }
    }


    internal class Pcom_dat
    {
        public uint _uuid = 0;
        public int _error = 0;
        public byte _type
        {
            get
            {
                return Head.Type;
            }
        }

        public bool InFunc = false;

        public int global_loc = 0;
        //VARI : name ad-valu     ad-type
        //FUNC : name ad-call-str ad-type(return  "void" or "value" or "array")
        public List<(string, string, string)> global_var = new List<(string, string, string)>();

        public int local_pas = 0;
        public int local_loc = 0;
        public List<(List<(string, string, string)>, int)> local_var = new(new[] { (new List<(string, string, string)>(), 0) });

        public int func_pas = 0;
        public int func_loc = 0;
        public List<(List<(string, string, string)>, int)> func_var = new(new[] { (new List<(string, string, string)>(), 0) });

        public string? _cache = null;

        public const string _CACHE1 = "call Pointer_Free2\n";

        public StringBuilder _idata = new StringBuilder(40);

        public static UTF32Encoding utf32_encode = new UTF32Encoding(false, false);

        public (string, string, string) GetVar(string name)
        {
            foreach (var item in global_var)
            {
                if (item.Item1 == name) return item;
            }

            if (InFunc)
            {
                foreach (var item in func_var)
                {
                    foreach (var item2 in item.Item1)
                    {
                        if (item2.Item1 == name) return item2;
                    }
                }
            }
            else
            {
                foreach (var item in local_var)
                {
                    foreach (var item2 in item.Item1)
                    {
                        if (item2.Item1 == name) return item2;
                    }
                }
            }

            return ("", "", "");
        }

        public void Incpas(bool loop = false)
        {
            if (InFunc)
            {
                func_var.Add((new List<(string, string, string)>(), func_loc));
                func_pas++;
                if (loop) func_loc++;
            }
            else
            {
                local_var.Add((new List<(string, string, string)>(), local_loc));
                local_pas++;
                if (loop) local_loc++;
            }
        }
        public void Decpas()
        {
            if (InFunc)
            {
                func_loc = func_var[func_pas].Item2;
                func_var.RemoveAt(func_pas);
                func_pas--;
            }
            else
            {
                local_loc = local_var[local_pas].Item2;
                local_var.RemoveAt(local_pas);
                local_pas--;
            }
        }

        public (string, string) Varadd(string name)
        {
            (string, string) ret;
            if (InFunc)
            {
                int a = ((func_loc + 1) * 16);
                if (_type is 0x00) ret = ("qword [rbp-" + a.ToString() + "]", "qword [rbp-" + (a - 8).ToString() + "]");
                else if (_type is 0x10) ret = ("vf" + func_loc.ToString() + "_" + func_pas.ToString(), "-");
                else ret = ("qword [rbp-" + a.ToString() + "]", "qword [rbp-" + (a - 8).ToString() + "]");
                func_loc++;
                func_var[func_pas].Item1.Add((name, ret.Item1, ret.Item2));
            }
            else
            {
                int a = ((local_loc + 1) * 16);
                if (_type is 0x00) ret = ("qword [rbp-" + a.ToString() + "]", "qword [rbp-" + (a - 8).ToString() + "]");
                else if (_type is 0x10) ret = ("vl" + local_loc.ToString() + "_" + local_pas.ToString(), "-");
                else ret = ("qword [rbp-" + a.ToString() + "]", "qword [rbp-" + (a - 8).ToString() + "]");
                local_loc++;
                local_var[local_pas].Item1.Add((name, ret.Item1, ret.Item2));
            }
            return ret;
        }

        public void OpenFunc()
        {
            func_pas = 0;
            func_loc = 0;
            func_var.Clear();
            func_var.Add((new(), 0));
            this.InFunc = true;
        }
        public void Funcadd(string name, VarType type, string call)
        {
            string tip;
            if (type is VarType.Unknown) tip = "void";
            else if (type is >= VarType.Char_Array) tip = "array";
            else tip = "value";

            global_var.Add((name, call, tip));
        }

        public int Constadd(string name, VarType type)
        {
            int st = global_loc;
            if (_type is 0x00) global_var.Add((name, type is >= VarType.Char_Array ? "const" + global_loc.ToString() : "[const" + global_loc.ToString() + "]", TypeToStr(type)));
            else if (_type is 0x10) global_var.Add((name, "const" + global_loc.ToString(), TypeToStr(type)));
            else global_var.Add((name, type is >= VarType.Char_Array ? "const" + global_loc.ToString() : "[const" + global_loc.ToString() + "]", TypeToStr(type)));
            global_loc++;
            return st;
        }

        public static string TypeToStr(VarType type)
        {
            if (type is VarType.String) type = VarType.Char_Array;
            return ((byte)type).ToString();
        }

        public static string LiteralToData(LiteralValueNode v)
        {
            if (v.type is VarType.Type or VarType.Number) return v.value;
            else if (v.type is VarType.Boolean) return (v.value is "true" ? "1" : "0");
            else if (v.type is VarType.Decimal) return "__float64_(" + v.value + ")";
            else if (v.type is VarType.Char) return Item.BitChanger.ToUInt32(Pcom_dat.utf32_encode.GetBytes(v.value), Item.ByteOrder.LittleEndian).ToString();
            else if (v.type is VarType.String)
            {
                StringBuilder sg = new StringBuilder(20 * (v.value.Length + 1));
                sg.Append(ulong.MaxValue.ToString());
                sg.Append(',');
                var fg = Pcom_dat.utf32_encode.GetBytes(v.value);
                sg.Append((fg.Length / 4).ToString());
                for (int i = 0; i < fg.Length / 4; i++)
                {
                    sg.Append(',');
                    sg.Append(Item.BitChanger.ToUInt32(fg, Item.ByteOrder.LittleEndian, i * 4).ToString());
                }

                return sg.ToString();
            }
            else throw new Exception();
        }


        public static VarType AryToVal(VarType t)
        {
            if (t is VarType.String) return VarType.Char;
            if (t is VarType.Arrable) return VarType.Valable;
            if (t is VarType.Array) return VarType.Value;
            return (VarType)(t - 15);
        }

        public static string EscaJs(string s)
        {
            return s
                .Replace("\\", "\\\\")   // バックスラッシュ
                .Replace("\r", "\\r")    // CR
                .Replace("\n", "\\n")    // LF
                .Replace("\t", "\\t")    // タブ
                .Replace("\b", "\\b")    // バックスペース
                .Replace("\f", "\\f")    // フォームフィード
                .Replace("\"", "\\\"")   // ダブルクォート
                .Replace("'", "\\'");    // シングルクォート
        }

        public static string InivalJs(VarType t)
        {
            return t switch
            {
                VarType.Char => "\"\\0\"",
                VarType.Boolean => "false",
                VarType.Type or VarType.Number or VarType.Decimal => "0",
                >= VarType.Char_Array => "[]",
                _ => ""
            };
        }

        //最適化
        public required bool Optim;

        public required Node[] All;

        public required Header Head;
    }

    internal class Pcom_arg
    {
        public string _text = "";
        public uint _gid = 0;
        public bool JS_prefix = true;
    }
}
