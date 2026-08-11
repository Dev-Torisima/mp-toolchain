using Prorigh.Reporter;
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
        if (args.Length is 0 or > 2)
        {
            Console.WriteLine("構文が正しくありません\n\"ヘルプ\"と入力してみてください");
        }
        else if (args[0] is "version" or "バージョン") Console.WriteLine(Reporter.Version);
        else if (args[0] is "help" or "ヘルプ")
        {
            Console.WriteLine("\"バージョン\"：バージョンを表示");
            Console.WriteLine("\"｛ファイルパス（入力）｝\"：レポートする");
            Console.WriteLine("\"｛エンコード｝｛ファイルパス（入力）｝\"：レポートする");
        }
        else
        {
            int io = 0;
            byte outtype = 0x10;
            byte env = 0xff;
            Encoding encode = Encoding.UTF8;

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

            if (args.Length - io is not 1)
            {
                Console.WriteLine("構文が正しくありません\n\"ヘルプ\"と入力してみてください");
                return;
            }
            
            if (!Path.Exists(args[io]))
            {
                Console.WriteLine("失敗：ファイルが存在しません");
                return;
            }
            
            var ty = new Reporter();
            var code = File.ReadAllText(args[io], encode);
            ty.Report(code);

            foreach (var item in ty.Error)
            {
                Console.WriteLine("エラー：" + _toString(item));
            }
            foreach (var item in ty.Warnning)
            {
                Console.WriteLine("警告：" + _toString(item));
            }

            if (ty.Error.Count() + ty.Warnning.Count() is 0) Console.WriteLine("正常に終了しました");
        }
    }
}