using Prorigh.Compiler;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
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

    static async Task<string?> __debug()
    {
        string? error = null;

        string filepath = AppContext.BaseDirectory + "mpcom_file_" + Convert.ToString(new Random().Next(), 16);
        string directory = AppContext.BaseDirectory;

        await File.WriteAllBytesAsync(filepath + ".cmpp", Encoding.UTF8.GetBytes("num a = 0\ndef func A() : void\nfunc A() : void\n\treturn\nend"));
        Directory.CreateDirectory(filepath);

        CompilerInput rt = new CompilerInput();
        rt.path_from = filepath + ".cmpp";
        rt.path_midl = filepath;
        rt.path_dest = filepath + ".bin";

        var ty = await CompilerHelper.Compile(rt, true, 0x10, Encoding.UTF8, 0xff);
        if (ty.ERROR.Item1 is not 0)
        {
            error = $"Faild to exe this program in checking status, due to compiler to html.\nThe error code is {_toString(ty.ERROR)}.";
            goto Finalizer;
        }

#if WINDOWS
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ty = await CompilerHelper.Compile(rt, true, 0x00, Encoding.UTF8, 0x00);
            if (ty.ERROR.Item1 is not 0) 
            {
                error = $"Faild to exe this program in checking status, due to compiler to exe in win-env.\nThe error code is {_toString(ty.ERROR)}.";
                goto Finalizer;
            }

            Console.WriteLine("^^status:^html=ok/exe=win-ok^^");
        }
        else
        {
            error = "Faild to exe this program in checking status, because of pratforms.";
            goto Finalizer;
        }
#elif LINUX_x64
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.OSArchitecture is Architecture.X64)
        {
            ty = await CompilerHelper.Compile(rt, true, 0x00, Encoding.UTF8, 0x01);
            if (ty.ERROR.Item1 is not 0) 
            {
                error = $"Faild to exe this program in checking status, due to compiler to exe in linux-x64.\nThe error code is {_toString(ty.ERROR)}.";
                goto Finalizer;
            }

            Console.WriteLine("^^status:^html=ok/exe=linux-ok^^");
        }
        else 
        {
            error = "Faild to exe this program in checking status, because of pratforms.";
            goto Finalizer;
        }
#else
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            error = "Faild to exe this program in checking status, because of pratforms.";
            goto Finalizer;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.OSArchitecture is Architecture.X64)
        {
            error = "Faild to exe this program in checking status, because of pratforms.";
            goto Finalizer;
        }
        Console.WriteLine("^^status:^html=ok/exe=not-support^^");
#endif

Finalizer:
        if (File.Exists(filepath + ".cmpp")) File.Delete(filepath + ".cmpp");
        if (File.Exists(filepath + ".bin")) File.Delete(filepath + ".bin");
        if (Directory.Exists(filepath)) Directory.Delete(filepath);

        return error;
    }

    public static async Task Main(string[] args)
    {
        if (args.Length is 1)
        {
            if (args[0] is "--version") Console.WriteLine(CompilerHelper.Version);
            else if (args[0] is "help")
            {
                Console.WriteLine("\"--version\"：バージョンを表示");

                Console.WriteLine("\"-no {ファイルパス（入力）｝｛ファイルパス（出力）｝\"：コンパイルする");
                Console.WriteLine("\"｛エンコーディング｝｛ファイルパス（入力）｝｛ファイルパス（出力）｝\"：コンパイルする");
                Console.WriteLine("\"{ファイルパス（入力）｝｛ファイルパス（出力）}\"：コンパイルする");
                Console.WriteLine("\"{ファイルパス（入力）｝｛ディレクトリパス（中間出力）｝｛ファイルパス（出力）｝\"：コンパイルする");

                Console.WriteLine("詳細は https://github.com/Dev-Torisima/mp-toolchain/blob/main/compiler/README.md から確認できます");
            }
            else if (args[0] is "--debug")
            {
                string? output = await __debug();
                if (output is not null) throw new Exception(output);
            }
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
        else Console.WriteLine("構文が正しくありません\n\"help\"と入力してみてください");
    }
}