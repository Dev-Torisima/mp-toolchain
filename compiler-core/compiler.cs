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
    internal class Compiler
    {
        public const string Version = "-p compiler v2.4";

        public (int, int, int) _error = (0, 0, 0);
        public (int, int, int)[]? _warnning = null;

        public string path_dest = "";
        public string path_midl = "";
        public string path_from = "";
        public string path_excl = "";//fasm2 path
        public string path_exin = "";//fasm2-include path
        public bool path_mido = true;

        public string _error_EX = "";
        public string _output_EX = "";

        internal const string Idc = "format PE64 console\nentry main\ninclude 'win64a.inc'\nsection '.data' data readable writeable\n    stdout      dq 0\n    stdin       dq 0\n    tmp         dd 0\n    appheap        dq 0\n    nullarray   dq 0,0\none dq 0x3ff0000000000000\ntwo dq 0x4000000000000000\ninv5040 dq 0x3f2a01a01a01a01a\ninv362880 dq 0x3efad012f684bda1\ninv39916800 dq 0x3ec6a688e5c7e0d1\ninv6227020800 dq 0x3e8d89e6c1f13c4a\ninv1307674368000 dq 0x3e4a6d3b5a86c8f4\ninv40320 dq 0x3f1a01a01a01a01a\ninv3628800 dq 0x3eead012f684bda1\ninv479001600 dq 0x3ec2a688e5c7e0d1\ninv87178291200 dq 0x3e88d89e6c1f13c4\ninv4503599627370496 dq 0x3cb0000000000000\ninv3 dq 0x3fd5555555555555\ninv5 dq 0x3fc999999999999a\ninv7 dq 0x3fc2492492492492\ninv2 dq 0x3fe0000000000000\ninv6 dq 0x3fc5555555555555\ninv24 dq 0x3fa5555555555555\ninv120 dq 0x3f81111111111111\ninv720 dq 0x3f56c16c16c16c17\n";
        internal const string Moc = "section '.code' code readable executable\n";

        internal const string Upc = "main:\npush rbp\nsub rsp, 32\nmov rcx, -11\ncall [GetStdHandle]\nmov [stdout], rax\nmov rcx, -10\ncall [GetStdHandle]\nmov [stdin], rax\nmov rcx, 65001\ncall [SetConsoleOutputCP]\nmov rcx, 65001\ncall [SetConsoleCP]\ncall [GetProcessHeap]\nmov qword [appheap], rax\nadd rsp, 32\nmov rbp, rsp\n";
        internal const string Upc2 = ".entry_o:\nsub rbp, rsp\ncmp rbp, 0\njna .entry_p\n.entry_w:\nmov rdi, qword [rsp+8]\ncmp rdi, 15\njna .entry_h\n.entry_r:\nmov rcx, qword [rsp]\ncall Pointer_Free\n.entry_h:\nadd rsp, 16\nsub rbp, 16\ncmp rbp, 0\nja .entry_w\n.entry_p:\nsub rsp, 32\nmov rcx, 0\ncall [ExitProcess]\n;add rsp, 32\n;ret\n";

        internal const string Isc = "section '.idata' import data readable writeable\n    library kernel32, 'kernel32.dll',\\\n            advapi32, 'advapi32.dll'\n    import kernel32,\\\n        Sleep, 'Sleep',\\\n        GetStdHandle, 'GetStdHandle',\\\n        SetConsoleOutputCP, 'SetConsoleOutputCP',\\\n        SetConsoleCP, 'SetConsoleCP',\\\n        SetConsoleTitleW, 'SetConsoleTitleW',\\\n        WriteConsoleW, 'WriteConsoleW',\\\n        ReadConsoleW, 'ReadConsoleW',\\\n        MultiByteToWideChar, 'MultiByteToWideChar',\\\n        WideCharToMultiByte, 'WideCharToMultiByte',\\\n        ExitProcess, 'ExitProcess',\\\n        FlushConsoleInputBuffer, 'FlushConsoleInputBuffer',\\\n        GetConsoleScreenBufferInfo, 'GetConsoleScreenBufferInfo',\\\n        FillConsoleOutputCharacterW, 'FillConsoleOutputCharacterW',\\\n        FillConsoleOutputAttribute, 'FillConsoleOutputAttribute',\\\n        SetConsoleCursorPosition, 'SetConsoleCursorPosition',\\\n        GetProcessHeap, 'GetProcessHeap',\\\n        GetFileSizeEx, 'GetFileSizeEx',\\\n        ReadFile, 'ReadFile',\\\n        WriteFile, 'WriteFile',\\\n        CreateFileW, 'CreateFileW',\\\n        CloseHandle, 'CloseHandle',\\\n        HeapAlloc, 'HeapAlloc',\\\n        HeapFree, 'HeapFree',\\\n        GetLocalTime, 'GetLocalTime',\\\n        QueryPerformanceCounter, 'QueryPerformanceCounter',\\\n        QueryPerformanceFrequency, 'QueryPerformanceFrequency'\n    import advapi32,\\\n        SystemFunction036, 'SystemFunction036'\n";

        internal const string Inc =
            "UTF64_To_UTF16_Num:\npush rbx\nxor rax, rax\nxor r8, r8\nxor r9, r9\n.loof:\ncmp r8, rdx\nje .ent\ninc rax\nmov rbx, qword [rcx+r9]\ncmp rbx, 0x10000\njb .next\ninc rax\n.next:\ninc r8\nadd r9, 8\njmp .loof\n.ent:\npop rbx\nret\n" +
            "UTF64_To_UTF16:\npush rbx\npush rdi\npush rsi\nmov rdi, r8\nxor rax, rax\nxor r8, r8\nxor r9, r9\n.loof:\ncmp r8, rdx\nje .ent\nmov rbx, qword [rcx+r9]\ncmp rbx, 0x10000\njb .one\nsub rbx, 0x10000\nmov rsi, rbx\nshr esi, 10\nor  esi, 0xD800\nmov word [rdi+rax], si\nadd rax, 2\nmov rsi, rbx\nand esi, 0x3FF\nor esi, 0xDC00\nmov word [rdi+rax], si\nadd rax, 2\njmp .next\n.one:\nmov word [rdi+rax], bx\nadd rax, 2\n.next:\ninc r8\nadd r9, 8\njmp .loof\n.ent:\npop rsi\npop rdi\npop rbx\nret\n" +
            "UTF16_To_UTF64_Len:\npush rbx\nxor rax, rax\nxor r8, r8\nxor r9, r9\n.loof:\ncmp r8, rdx\njae .return\nxor rbx, rbx\nmov bx, word [rcx+r9]\ncmp rbx, 0xD800\njb .one\ncmp rbx, 0xDC00\njb .two\ncmp rbx, 0xE000\njb .exp\n.one:\ninc rax\n.exp:\ninc r8\nadd r9, 2\njmp .loof\n.two:\nadd r8, 2\nadd r9, 4\ninc rax\njmp .loof\n.return:\npop rbx\nret\n" +
            "UTF16_To_UTF64:\npush rbx\npush rdi\nmov rbx, rcx\nmov rdi, r8\nxor rax, rax\nxor r8, r8\nxor r9, r9\n.loof:\ncmp rax, rdx\njae .return\nxor rcx, rcx\nmov cx, word [rbx+r9]\ncmp rcx, 0xD800\njb .one\ncmp rcx, 0xDC00\njb .two\ncmp rcx, 0xE000\njnb .one\nxor rcx, rcx\n.one:\nmov qword [rdi+r8], rcx\ninc rax\nadd r8, 8\nadd r9, 2\njmp .loof\n.two:\npush rax\nsub rcx, 0xD800\nshl rcx, 10\nadd r9, 2\nxor rax, rax\nmov ax, word [rbx+r9]\nsub rax, 0xDC00\nadd rcx, rax\nadd rcx, 0x10000\nmov qword [rdi+r8], rcx\npop rax\ninc rax\ninc rax\nadd r8, 8\nadd r9, 2\njmp .loof\n.return:\npop rdi\npop rbx\nret\n" +
            "Array_Com:\npush rdi\npush rsi\nmov rcx, rsi\nmov rdx, rdi\nlea rdi, [rcx+8]\nlea rsi, [rdx+8]\nmov r8, qword [rdi]\nmov r9, qword [rsi]\ncmp r8, r9\njne .retn\nlea rdi, [rcx+16]\nlea rsi, [rdx+16]\nxor r8, r8\nxor rcx, rcx\n.loof:\ncmp r8, r9\nje .rete\nmov rax, qword [rdi+rcx]\nmov rdx, qword [rsi+rcx]\ncmp rax, rdx\njne .retn\ninc r8\nadd rcx, 8\njmp .loof\n.rete:\nxor rax, rax\ninc rax\njmp .return\n.retn:\nxor rax, rax\n.return:\npop rsi\npop rdi\nret\n" +
            "Pointer_Alloc:\npush rbx\nmov rbx, rcx\nmov rcx, qword [appheap]\nmov rax, rbx\nmov r8, 8\nmul r8\nmov r8, rax\nadd r8, 16\nxor rdx, rdx\ninc rdx\nshl rdx, 3\nsub rsp, 32\ncall [HeapAlloc]\nadd rsp, 32\nmov qword [rax], 0 ;refcount\nmov qword [rax+8], rbx ;Length\npop rbx\nret\n" +
            "Pointer_Free2:\npush rbx\ncmp rcx, nullarray\nje .t\nmov rbx, rcx\nmov rax, qword [rbx]\ncmp rax, 0\njne .t\n\nmov rcx, qword [appheap]\nmov rdx, 0\nmov r8, rbx\nsub rsp, 32\ncall [HeapFree]\nadd rsp, 32\n.t:\npop rbx\nret\n" +
            "Pointer_Free:\npush rbx\ncmp rcx, nullarray\nje .t\nmov rbx, rcx\nmov rax, qword [rbx]\ncmp rax, 0\nje .q\nmov r8, rax\nnot r8\ncmp r8, 0\nje .t\ndec rax\nmov qword [rbx], rax\ncmp rax, 0\njne .t\n.q:\nmov rcx, qword [appheap]\nmov rdx, 0\nmov r8, rbx\nsub rsp, 32\ncall [HeapFree]\nadd rsp, 32\n.t:\npop rbx\nret\n" +
            "Var_Realloc:\npush rbx\npush rsi\npush rdi\nmov rbx, rax\nmov rsi, rcx\nlea r8, [rcx+8]\nmov r9, qword [r8]\ncmp r9, 15\njna .t\nlea rax, [rbx]\ncall Pointer_Return1\nmov rcx, qword [rsi]\ncall Pointer_Free\n.t:\nmov qword [rsi], rbx\npop rdi\npop rsi\npop rbx\nret\n" +
            "Var_Alloc:\nmov qword [rcx], 0\nlea r8, [rcx+8]\nmov qword [r8], rdx\ncmp rdx, 15\njna .return\nmov qword [rcx], nullarray\n.return:\nret\n" +
            "Array_Add:\npush rsi\npush rdi\npush rbx\nlea rbx, [rsi+8]\nmov r8, qword [rbx]\nlea rbx, [rdi+8]\nmov r9, qword [rbx]\nadd r8, r9\nmov rbx, r8\nmov rcx, qword [appheap]\nxor rdx, rdx\nmov rax, r8\nmov r8, 8\nmul r8\nmov r8, rax\nadd r8, 16\nsub rsp, 32\ncall [HeapAlloc]\nadd rsp, 32\nmov qword [rax], 0\nmov qword [rax+8], rbx\nmov r9, rbx\nlea rdx, [rax+16]\nxor r8, r8\nlea rbx, [rsi+8]\nmov r9, qword [rbx]\ncmp r9, r8\nje .r\nlea rbx, [rsi+16]\n.d:\ncmp r9, r8\nje .r\nmov rcx, qword [rbx]\nmov qword [rdx], rcx\nadd rbx, 8\ninc r8\nadd rdx, 8\njmp .d\n.r:\nxor r8, r8\nlea rbx, [rdi+8]\nmov r9, qword [rbx]\ncmp r9, r8\nje .f\nlea rbx, [rdi+16]\n.q:\ncmp r9, r8\nje .f\nmov rcx, qword [rbx]\nmov qword [rdx], rcx\nadd rbx, 8\ninc r8\nadd rdx, 8\njmp .q\n.f:\npop rbx\npop rdi\npop rsi\nret\n" +
            "Pointer_Return1:\npush rbx\nmov rbx, qword [rax]\nnot rbx\ncmp rbx, 0\nje .skip\nnot rbx\ninc rbx\nmov qword [rax], rbx\n.skip:\npop rbx\nret\n" +
            "Pointer_Return2:\npush rbx\nmov rbx, qword [rax]\nnot rbx\ncmp rbx, 0\nje .skip\nnot rbx\ndec rbx\nmov qword [rax], rbx\n.skip:\npop rbx\nret\n" + 
            "Indexer:\nmov rax, qword [rdi+8]\ncmp rax, 0\nje .q\ncmp rsi, 0\njl .q\ncmp rax, rsi\njbe .q\npush rsi\npush rdi\nshl rsi, 3\nadd rsi, 16\nadd rdi, rsi\nmov rax, [rdi]\npop rdi\npop rsi\nret\n.q:\nxor rax, rax\nret\n" +
            "Pointer_Relocate:\npush rbx\nmov rbx, qword [rcx]\ncmp rbx, 0\nje .nul\nmov rbx, rcx\nmov rcx, qword [appheap]\nmov rax, qword [rbx+8]\nxor r8, r8\ninc r8\nshl r8, 4\nshl rax, 3\nadd r8, rax\nxor rdx, rdx\nsub rsp, 32\ncall [HeapAlloc]\nadd rsp, 32\nxor rcx, rcx\nmov qword [rax], rcx\nmov rdx, qword [rbx+8]\nmov qword [rax+8], rdx\nxor r8, r8\ninc r8\nshl r8, 4\nxor r9, r9\n.loof:\ncmp r9, rdx\njae .retu\nmov rcx, qword [rbx+r8]\nmov qword [rax+r8], rcx\nadd r8, 8\ninc r9\njmp .loof\n.nul:\nlea rax, [nullarray]\n.retu:\npop rbx\nret\n" +
            "Indeset:\npush rbx\npush rsi\nmov rbx, qword [rcx]\nmov r8, qword [rbx+8]\ncmp rdi, r8\njae .retu\ncmp rdi, 0\njb .retu\npush rax\npush rdi\npush rcx\nmov rcx, rbx\ncall Pointer_Relocate\ncmp rax, nullarray\nje .skip\nmov rbx, rax\npop rcx\npush rcx\ncall Var_Realloc\n.skip:\npop rcx\npop rdi\npop rax\nmov rcx, rbx\nshl rdi, 3\nadd rcx, 16\nadd rcx, rdi\nmov qword [rcx], rax\n.retu:\npop rsi\npop rbx\nret\n" + 
            "UTF64_To_UTF8_Num:\npush rbx\nxor rax, rax\nxor r8, r8\nxor r9, r9\n.loof:\ncmp r8, rdx\nje .ent\ninc rax\nmov rbx, qword [rcx+r9]\ncmp rbx, 0x80\njb .next\ncmp rbx, 0x800\njb .pat2\ncmp rbx, 0x10000\njb .pat3\nadd rax, 3\njmp .next\n.pat2:\ninc rax\njmp .next\n.pat3:\nadd rax, 2\n.next:\ninc r8\nadd r9, 8\njmp .loof\n.ent:\npop rbx\nret\nUTF64_To_UTF8:\npush rbx\npush rdi\npush rsi\nmov rdi, r8\nxor rax, rax\nxor r8, r8\nxor r9, r9\n.loof:\ncmp r8, rdx\nje .ent\nmov rbx, qword [rcx+r9]\ncmp rbx, 0x80\njb .pat1\ncmp rbx, 0x800\njb .pat2\ncmp rbx, 0x10000\njb .pat3\nmov rsi, rbx\nshr rsi, 18\nor sil, 0xF0\nmov byte [rdi+rax], sil\ninc rax\nmov rsi, rbx\nshr rsi, 12\nand sil, 0x3F\nor sil, 0x80\nmov byte [rdi+rax], sil\ninc rax\nmov rsi, rbx\nshr rsi, 6\nand sil, 0x3F\nor sil, 0x80\nmov byte [rdi+rax], sil\ninc rax\nmov rsi, rbx\nand sil, 0x3F\nor sil, 0x80\nmov byte [rdi+rax], sil\ninc rax\njmp .next\n.pat2:\nmov rsi, rbx\nshr rsi, 6\nor sil, 0xC0\nmov byte [rdi+rax], sil\ninc rax\nmov rsi, rbx\nand sil, 0x3F\nor sil, 0x80\nmov byte [rdi+rax], sil\ninc rax\njmp .next\n.pat3:\nmov rsi, rbx\nshr rsi, 12\nor sil, 0xE0\nmov byte [rdi+rax], sil\ninc rax\nmov rsi, rbx\nshr rsi, 6\nand sil, 0x3F\nor sil, 0x80\nmov byte [rdi+rax], sil\ninc rax\nmov rsi, rbx\nand sil, 0x3F\nor sil, 0x80\nmov byte [rdi+rax], sil\ninc rax\njmp .next\n.pat1:\nmov byte [rdi+rax], bl\ninc rax\n.next:\ninc r8\nadd r9, 8\njmp .loof\n.ent:\npop rsi\npop rdi\npop rbx\nret\nUTF8_To_UTF64_Len:\npush rbx\nxor rax, rax\nxor r8, r8\nxor r9, r9\n.loof:\ncmp r8, rdx\njae .return\nxor rbx, rbx\nmov bl, byte [rcx+r9]\ncmp bl, 0x80\njb .pat1\ncmp bl, 0xE0\njb .pat2\ncmp bl, 0xF0\njb .pat3\nadd r8, 4\nadd r9, 4\ninc rax\njmp .loof\n.pat1:\ninc rax\ninc r8\ninc r9\njmp .loof\n.pat2:\nadd r8, 2\nadd r9, 2\ninc rax\njmp .loof\n.pat3:\nadd r8, 3\nadd r9, 3\ninc rax\njmp .loof\n.return:\npop rbx\nret\nUTF8_To_UTF64:\npush rbx\npush rdi\npush rsi\nmov rbx, rcx\nmov rdi, r8\nxor rax, rax\nxor r8, r8\nxor r9, r9\n.loof:\ncmp rax, rdx\njae .return\nxor rcx, rcx\nmov cl, byte [rbx+r9]\ncmp cl, 0x80\njb .pat1\ncmp cl, 0xE0\njb .pat2\ncmp cl, 0xF0\njb .pat3\nand rcx, 0x07\nshl rcx, 18\ninc r9\nxor rsi, rsi\nmov sil, byte [rbx+r9]\nand sil, 0x3F\nshl rsi, 12\nor rcx, rsi\ninc r9\nxor rsi, rsi\nmov sil, byte [rbx+r9]\nand sil, 0x3F\nshl rsi, 6\nor rcx, rsi\ninc r9\nxor rsi, rsi\nmov sil, byte [rbx+r9]\nand sil, 0x3F\nor rcx, rsi\nmov qword [rdi+r8], rcx\nadd rax, 4\nadd r8, 8\ninc r9\njmp .loof\n.pat1:\nmov qword [rdi+r8], rcx\ninc rax\nadd r8, 8\ninc r9\njmp .loof\n.pat2:\nand rcx, 0x1F\nshl rcx, 6\ninc r9\nxor rsi, rsi\nmov sil, byte [rbx+r9]\nand sil, 0x3F\nor cl, sil\nmov qword [rdi+r8], rcx\nadd rax, 2\nadd r8, 8\ninc r9\njmp .loof\n.pat3:\nand rcx, 0x0F\nshl rcx, 12\ninc r9\nxor rsi, rsi\nmov sil, byte [rbx+r9]\nand sil, 0x3F\nshl rsi, 6\nor rcx, rsi\ninc r9\nxor rsi, rsi\nmov sil, byte [rbx+r9]\nand sil, 0x3F\nor rcx, rsi\nmov qword [rdi+r8], rcx\nadd rax, 3\nadd r8, 8\ninc r9\njmp .loof\n.return:\npop rsi\npop rdi\npop rbx\nret\n" + 
            "\n";

        internal const string Cop = ";Copyright (c) Imqutive 2026, All Rights Reserved (excluding the generated parts from user code)\n";

        internal const string Jak = "<!DOCTYPE html>\n<html>\n<head>\n<meta charset=\"utf-8\">\n        <meta content=\"width=device-width, initial-scale=1\" name=\"viewport\"/>\n<title>Title</title>\n<meta content=\"\" name=\"description\"/>\n<meta content=\"Generated by html generator of -p compiler for html\" property=\"comment\"/>\n</head>\n<body>\n<script src=\"https://res.tools.imqutive.f5.si/source/FrontConsole.js\"></script>\n<script>\n"
            + "/*This code is written by Torisima, 2026.*/function GetStorage(abc1) {try {const data = localStorage.getItem(abc1);return data ? JSON.parse(data)[\"data\"] : null;} catch (e) {return null;}}function SetStorage(abc1, abc2) {try {localStorage.setItem(abc1, JSON.stringify({\"data\":abc2}));} catch (e) {}}function GetStorageData(abc1){try {return localStorage.getItem(abc1) ?? null;} catch (e) {return null;}}var init_Char = \"\\0\";var init_Bool = false;var init_Number = 0;var init_Decimal = 0;var init_Type = 0;function Indexer_GET(abc1, abc2, abc3){if (abc2 >= abc1.length || abc2 < 0){return abc3;}else{return abc1[abc2];}}function Indexer_SET(abc1, abc2, abc3){if (abc2 < abc1.length && abc2 >= 0){abc1[abc2] = abc3;}}function ArrayCom(a, b){if (a.length != b.length) return false;for (let i = 0; i < a.length; i++) {if(a[i] !== b[i]){return false;}}return true;}\n"
            + "document.addEventListener(\"DOMContentLoaded\", async function () {\n";

        public StringBuilder? _pre = null;

        public bool Optim = false;

        public void PreCompile(Node[] node, Header header, byte outtype)
        {
            _pre = null;
            if (outtype is 0x00)
            {
                int cap1 = 0;
                int cap2 = 0;
                int cap3 = 0;
                foreach (var item in node)
                {
                    if (item is FuncNode) cap1 += item.Asm_al;
                    else cap2 += item.Asm_al;

                    if (item is DefConstNode) cap3++;
                }

                cap2 += Upc.Length + Upc2.Length;

                _pre = new StringBuilder(cap1 + cap2 + 100 + Idc.Length + Moc.Length + 36 * cap3 + Isc.Length + Header.Asm1_al + Inc.Length + Cop.Length + Header.Asm2_al);
                _pre.Append(Cop);
                _pre.Append(Idc);

                StringBuilder ui = new StringBuilder(cap1 + cap2 + 100);
                StringBuilder ui2 = new StringBuilder(cap2);
                var ty = new Pcom_dat() { _uuid = 1, Optim = Optim, All = node, Head = header };
                var gy = new Pcom_arg() { _gid = 0 };

                ui2.Append(Upc);

                foreach (var item in node)
                {
                    item.Pre(ty);
                }

                foreach (var item in node)
                {
                    if (Optim && !item._must) continue;

                    if (item is FuncNode or HeadNode) ui.Append(item.Asm(ty, gy));
                    else if (item is DefFuncNode or DefConstNode) ui2.Append(item.Asm(ty, gy));
                }

                foreach (var item in node)
                {
                    if (Optim && !item._must) continue;

                    if (!(item is FuncNode or HeadNode or DefFuncNode or DefConstNode)) ui2.Append(item.Asm(ty, gy));
                }

                _pre.Append(ty._idata);
                _pre.Append(Isc);
                _pre.Append(Moc);
                _pre.Append(Inc);

                ui2.Append(Upc2);

                ui.Append(ui2);

                _pre.Append(ui);
            }
            else if (outtype is 0x10)
            {
                int cap1 = 0;
                int cap2 = 0;
                int cap3 = 0;
                foreach (var item in node)
                {
                    if (item is FuncNode) cap1 += item.Js_al;
                    else cap2 += item.Js_al;

                    if (item is DefConstNode) cap3++;
                }

                _pre = new StringBuilder(cap1 + cap2 + 100 + Jak.Length + 36 * cap3 + + Header.Js1_al + Header.Js2_al);
                _pre.Append(Jak);

                StringBuilder ui = new StringBuilder(cap1 + cap2 + 100);
                StringBuilder ui2 = new StringBuilder(cap2);
                var ty = new Pcom_dat() { _uuid = 1, Optim = Optim, All = node, Head = header };
                var gy = new Pcom_arg() { _gid = 0 };

                foreach (var item in node)
                {
                    item.Pre(ty);
                }

                foreach (var item in node)
                {
                    if (Optim && !item._must) continue;

                    if (item is FuncNode or HeadNode) ui.Append(item.Js(ty, gy));
                    else if (item is DefFuncNode or DefConstNode) ui2.Append(item.Js(ty, gy));
                }

                foreach (var item in node)
                {
                    if (Optim && !item._must) continue;

                    if (!(item is FuncNode or HeadNode or DefFuncNode or DefConstNode)) ui2.Append(item.Js(ty, gy));
                }

                _pre.Append(ty._idata);
                ui.Append(ui2);

                ui.Append("});</script>\n</body>\n</html>\n");
                _pre.Append(ui);
            }
        }

        public async Task Compile(byte outtype, Encoding encode, byte arch)
        {
            _error = (0, 0, 0);
            _error_EX = "";
            _output_EX = "";
            _warnning = null;

            if (!CanWrite(path_dest))
            {
                _error.Item1 = 0x602;
                return;
            }

            try
            {
                string yhs = "";
                using (FileStream fs = new FileStream(path_from, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] gh = new byte[fs.Length];
                    await fs.ReadAsync(gh, 0, gh.Length);
                    yhs = encode.GetString(gh);
                }

                var header = new Header(outtype);
                var lex = new Lexer(yhs);
                var paser = new Parser(lex, header);

                _warnning = paser.GetWarning();

                var ty = paser.Parse();

                if (ty is null)
                {
                    _error = (paser.GetError(), paser._lexer._line, paser._lexer._row);
                    _warnning = paser.GetWarning();

                    if (_error.Item1 is 0) _error = (0x600, 0, 0);

                    return;
                }

                PreCompile(ty.ToArray(), header, outtype);

                if (outtype is 0x00)
                {
                    using (FileStream fs = new FileStream(path_midl, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        fs.SetLength(0);
                        fs.Position = 0;
                        await fs.WriteAsync(Encoding.UTF8.GetBytes(_pre.ToString()));
                    }

                    ProcessStartInfo psi;
                    if (arch is 0)
                    {
                        psi = new ProcessStartInfo
                        {
                            FileName = path_excl,
                            Arguments = "-iInclude('fasm2.inc') " + path_midl + " " + path_dest,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };

                        if (psi.EnvironmentVariables.ContainsKey("include")) psi.EnvironmentVariables["include"] = path_exin + ";" + psi.EnvironmentVariables["include"];
                        else psi.EnvironmentVariables["include"] = path_exin;
                    }
                    else if (arch is 1)
                    {
                        psi = new ProcessStartInfo
                        {
                            FileName = path_excl,
                            Arguments = path_midl + " " + path_dest,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };

                        File.SetUnixFileMode(path_excl, UnixFileMode.UserExecute | UnixFileMode.UserWrite | UnixFileMode.UserRead);
                        File.SetUnixFileMode(path_excl + ".x64", UnixFileMode.UserExecute | UnixFileMode.UserWrite | UnixFileMode.UserRead);
                    }
                    else
                    {
                        throw new Exception();
                    }


                    
                    var process = new Process { StartInfo = psi };
                    process.Start();

                    _output_EX = await process.StandardOutput.ReadToEndAsync();
                    _error_EX = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync();

                    if (process.ExitCode is not 0)
                    {
                        _error = (0x601, 0, 0);
                    }
                }
                else if (outtype is 0x10)
                {
                    using (FileStream fs = new FileStream(path_dest, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        fs.SetLength(0);
                        fs.Position = 0;
                        await fs.WriteAsync(Encoding.UTF8.GetBytes(_pre.ToString()));
                    }
                }
            }
            catch (Exception e)
            {
                _error_EX = e.Message;
                _error.Item1 = 0x603;
            }

            if (outtype is 0x00)
            {
                try
                {
#if ASM
#else
                    if (File.Exists(path_midl) && path_mido) File.Delete(path_midl);
#endif
                }
                catch (Exception)
                {

                }
            }
        }

        private bool CanWrite(string path)
        {
            try
            {
                using (FileStream fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }



    public class CompilerInput
    {
        public string path_from = "";
        public string path_midl = "";
        public string path_dest = "";
        public bool path_mido = true;
    }

    public class CompilerOutput
    {
        public (int, int, int) ERROR = (0, 0, 0);
        public (int, int, int)[]? WARNNING = null;
        public string OUTPUTEX = "";
        public string ERROREX = "";
    }

    public static class CompilerHelper
    {
        public static string Version => Compiler.Version;

        public static async Task<CompilerOutput> Compile(CompilerInput a, bool opt, byte outtype, Encoding encode, byte env)
        {
            string? hu = AppContext.BaseDirectory;
            hu = Path.GetDirectoryName(hu);
            if (hu is null) hu = "";
            hu = Path.Combine(hu, "exclude");

            var te = new Compiler();
            te.Optim = opt;
            te.path_from = a.path_from;
            te.path_midl = (a.path_mido ? Path.Combine(a.path_midl, "mf" + new Random().Next().ToString() + ".asm") : a.path_midl);
            te.path_dest = a.path_dest;
            te.path_excl = Path.Combine(hu, (env is 0 ? "compiler.exe" : "compiler"));
            te.path_exin = Path.Combine(hu, "include");
            te.path_mido = a.path_mido;

            await te.Compile(outtype, encode, env);

            var _a_ = new CompilerOutput();
            _a_.ERROR = te._error;
            _a_.WARNNING = te._warnning;
            _a_.OUTPUTEX = te._output_EX;
            _a_.ERROREX = te._error_EX;

            return _a_;
        }

    }

}
