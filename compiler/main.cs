using Prorigh.Compiler;
using System.Text;
using System.Threading.Tasks;

public static class Program
{
    static Program()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    static string _toString((int, int, int) rt)
    {
        return Texer.Japanese[rt.Item1] + "｜" + Convert.ToString(rt.Item1, 16) + "（行：" + rt.Item2.ToString() + ", 列：" + rt.Item3.ToString() + "）";
    }

    public static async Task Main(string[] args)
    {
        if (args.Length is 1)
        {
            if (args[0] is "version" or "バージョン") Console.WriteLine(CompilerHelper.Version);
            else if (args[0] is "help" or "ヘルプ")
            {
                Console.WriteLine("\"バージョン\"：バージョンを表示");
                Console.WriteLine("\"せつめい\"：手順を表示");

                Console.WriteLine("\"{ファイルパス（入力）} {ファイルパス（出力）}\"：コンパイルする");
                Console.WriteLine("\"{ファイルパス（入力）} {ディレクトリパス（中間出力）} {ファイルパス（出力）}\"：コンパイルする");
            }
            else if (args[0] is "explain" or "せつめい") Console.WriteLine("コードを書くファイルをUTF-8で作成します\nコンパイルします\nx64向けのEXEファイル（アプリケーション）が作成されます");
        }
        else if (args.Length is >= 2)
        {
            var rt = new CompilerInput();
            int io = 0;
            bool pp = true;
            byte outtype = 0x10;
            byte env = 0xff;
            Encoding encode = Encoding.UTF8;
            if (args[io] is "-no")
            {
                pp = !pp;
                io++;
            }

            if (args[io] is "utf8") io++;
            else if (args[io] is "ascii")
            {
                encode = Encoding.ASCII;
                io++;
            }
            else if (args[io] is "utf32")
            {
                encode = Encoding.UTF32;
                io++;
            }
            else if (args[io] is "utf16_b")
            {
                encode = Encoding.BigEndianUnicode;
                io++;
            }
            else if (args[io] is "utf16" or "utf16_l")
            {
                encode = Encoding.Unicode;
                io++;
            }
            else if (args[io] is "shift_jis")
            {
                encode = Encoding.GetEncoding(932);
                io++;
            }
            else if (args[io] is "big5")
            {
                encode = Encoding.GetEncoding(950);
                io++;
            }
            else if (args[io] is "gbk")
            {
                encode = Encoding.GetEncoding(936);
                io++;
            }
            else if (args[io] is "cp1251")
            {
                encode = Encoding.GetEncoding(1251);
                io++;
            }
            else if (args[io] is "cp1252")
            {
                encode = Encoding.GetEncoding(1252);
                io++;
            }
            else if (args[io] is "cp949")
            {
                encode = Encoding.GetEncoding(949);
                io++;
            }

            if (args[io] is "html") io++;
            else if (args[io] is "exe")
            {
                outtype = 0x00;
                io++;
#if WINDOWS
                env = 0;
#elif LINUX_x64
                env = 1;
#else
                Console.WriteLine("ご利用の環境では利用できません");
                return;
#endif
            }

            if (args.Length - io is 2)
            {
                rt.path_from = args[io];
                rt.path_dest = args[io + 1];
                String? fg = null;
                try
                {
                    fg = Path.GetDirectoryName(args[1 + io]);
                }
                catch (Exception) { fg = null; }
                if (fg is null)
                {
                    Console.WriteLine("失敗：出力（アプリ）のファイルパスが正しくありません");
                    return;
                }
                rt.path_midl = fg;
            }
            else
            {
                    rt.path_from = args[0 + io];
                    rt.path_midl = args[1 + io];
                    rt.path_dest = args[2 + io];
            }
            if (!Path.Exists(rt.path_from))
            {
                Console.WriteLine("失敗：入力（コード）のファイルがありません");
                return;
            }
            if (!Directory.Exists(rt.path_midl))
            {
                Console.WriteLine("失敗：中間出力のディレクトリがありません");
                return;
            }

            var ty = await CompilerHelper.Compile(rt, pp, outtype, encode, env);

            if (ty.ERROR.Item1 is 0) Console.WriteLine("正常に終了しました");
            else Console.WriteLine("エラー：" + _toString(ty.ERROR));
            if (ty.WARNNING is not null)
            {
                foreach (var item in ty.WARNNING)
                {
                    Console.WriteLine("警告：" + _toString(item));
                }
            }
            if (ty.ERROREX is not "") Console.WriteLine("詳細：" + ty.ERROREX);
        }
        else Console.WriteLine("構文が正しくありません\n\"ヘルプ\"と入力してみてください");
    }
}