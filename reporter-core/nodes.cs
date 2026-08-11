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
    #region Nodes
    // ---
    // #region Nodes
    // ---
    // 「@compiler」から「value」関連だけを抽出
    // ---

    internal class HeadNode
    {
        public HeadNode((string[], (int, int, VarType, object?), string[]?) data, string code, bool must = true)
        {
            Name = data.Item1; Data = data.Item2; Code = code; Requir = (data.Item3 is null ? new string[0] : data.Item3);
        }

        public string[] Name;

        public (int, int, VarType, object?) Data;

        public string Code;

        public string[] Requir;
    }


    public class ParamNode
    {
        public VarType type = VarType.Unknown;
        public string name = "";
    }

    public class LiteralValueNode : ValueNode
    {
        public string value = "";
    }

    public class LiteralArrayNode : ValueNode
    {
        public List<ValueNode> value = new List<ValueNode>();
    }

    public class ValueNode
    {
        public bool NULL = false;
        public VarType type = VarType.Unknown;
    }

    public class VarUseNode : ValueNode
    {
        public string name = "";
    }

    public class CallNode : ValueNode
    {
        public string name = "";
        public ValueNode[] args = new ValueNode[0];
    }

    public class CaluNode : ValueNode
    {
        //0:+,1:-,2:*,3:/,4:%,5:&,6:|,7:==,8:!=,9:>=,10:>.11<=,12:<,13:!,14:minus掛け
        public byte symbol = 0;
        public ValueNode? value1 = null;
        public ValueNode? value2 = null;
    }

    public class IndexNode : ValueNode
    {
        public ValueNode? value = null;

        public ValueNode? index = null;
    }

    public class ArrayNode : ValueNode
    {
        public ValueNode? length = null;
    }

    public class TextArrayNode : ValueNode
    {
        public TextArrayNode()
        {
            type = VarType.String;
        }

        public List<ValueNode> items = new List<ValueNode>();
    }
    #endregion

}