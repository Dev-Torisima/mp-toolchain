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
    // public class Reporter
    // ---
    // 「@reporter」で作成
    // ---

    public class Reporter
    {
        public List<(int, int, int)> Error = new List<(int, int, int)>();
        public List<(int, int, int)> Warnning = new List<(int, int, int)>();

        public const string Version = "-p reporter 2.4";

        public void Report(string code)
        {
            Lexer lex = new Lexer(code);
            Parser par = new Parser(lex, new Header());
            par.Parse();
            Error = par._error;
            Warnning = par._warning;
        }
    }
}