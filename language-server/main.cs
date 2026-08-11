using Prorigh.Reporter;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

public class Program
{
    public static Stream? input = null;

    public static void Main()
    {
        try
        {
            while (true)
            {
                var msg = ReadMessage();
                if (msg is null) break;

                HandleMessage(msg);
            }
        }
        catch (Exception e)
        {
            throw;
        }
    }

    public static string? ReadMessage()
    {
        int length = 0;

        if (input is null)
        {
            input = Console.OpenStandardInput();
        }

        List<byte> head = new List<byte>(128);

        while (true)
        {
            var a = input.ReadByte();

            if (a is -1) return null;
            head.Add((byte)a);

            int cc = head.Count;
            if (cc is > 4 
                && head[cc - 1] is (byte)'\n'
                && head[cc - 2] is (byte)'\r'
                && head[cc - 3] is (byte)'\n'
                && head[cc - 4] is (byte)'\r')
            {
                break;
            }
        }

        string heads = Encoding.ASCII.GetString(head.ToArray());

        foreach (var line in heads.Split("\r\n"))
        {
            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
            {
                length = int.Parse(line.Substring("Content-Length:".Length).Trim());
            }
        }


        if (length is 0) return null;

        byte[] buffer = new byte[length];
        int rea = 0;

        while (rea < length)
        {
            int r = input.Read(buffer, rea, length - rea);
            if (r is 0) break;
            rea += r;
        }

        return new UTF8Encoding(false).GetString(buffer, 0, rea);
    }

    public static void HandleMessage(string json)
    {
        var m = JsonNode.Parse(json);
        if (m is null) return;
        if (m["method"] is null) return;

        switch ((string?)m["method"])
        {
            case "initialize":
                if ((int?)m["id"] is null) return;

                JsonObject pay = new JsonObject();
                JsonObject pay_p = new JsonObject();
                JsonObject pay_p_c = new JsonObject();
                pay.Add("jsonrpc", "2.0");
                pay.Add("id", (int?)m["id"]);
                pay.Add("result", pay_p);
                pay_p.Add("capabilities", pay_p_c);
                pay_p_c.Add("textDocumentSync", (int)1);

                SendMessage(pay);
                break;

            case "textDocument/didOpen":
                if (m["params"] is null) return;
                var e1 = m["params"];
                if (e1["textDocument"] is null) return;
                if ((string?)e1["textDocument"]["text"] is null) return;

                DidChange(e1, (string)e1["textDocument"]["text"]);
                break;

            case "textDocument/didChange":
                if (m["params"] is null) return;
                var e2 = m["params"];
                if (e2["contentChanges"] is null) return;
                if (e2["contentChanges"][0] is null) return;
                if ((string?)e2["contentChanges"][0]["text"] is null) return;

                DidChange(e2, (string)e2["contentChanges"][0]["text"]);
                break;
        }
    }

    public static void DidChange(JsonNode e, string text)
    {
        if (e["textDocument"] is null) return;
        if (e["textDocument"]["uri"] is null) return;
        if ((string?)e["textDocument"]["uri"] is null) return;

        var rep = new Reporter();
        rep.Report(text);


        JsonObject pay = new JsonObject();
        JsonObject pay_p = new JsonObject();
        JsonArray pay_p_d = new JsonArray();
        pay.Add("jsonrpc", "2.0");
        pay.Add("method", "textDocument/publishDiagnostics");
        pay.Add("params", pay_p);
        pay_p.Add("uri", ((string?)e["textDocument"]["uri"]));
        pay_p.Add("diagnostics", pay_p_d);
        foreach (var item in rep.Error)
        {
            if (item.Item1 is 0x400) continue;

            JsonObject main = new JsonObject();
            JsonObject rang = new JsonObject();
            JsonObject start = new JsonObject();
            JsonObject end = new JsonObject();
            main.Add("range", rang);
            rang.Add("start", start);
            rang.Add("end", end);
            start.Add("line", (item.Item2 is  <= 0 ? 1 : item.Item2) - 1);
            start.Add("character", (item.Item3 is <= 0 ? 1 : item.Item3) - 1);
            end.Add("line", (item.Item2 is <= 0 ? 1 : item.Item2) - 1);
            end.Add("character", (item.Item3 is <= 0 ? 1 : item.Item3) - 1);
            main.Add("severity", 1);
            main.Add("message", Texer.Japanese[item.Item1]);

            pay_p_d.Add(main);

        }
        foreach (var item in rep.Warnning)
        {
            JsonObject main = new JsonObject();
            JsonObject rang = new JsonObject();
            JsonObject start = new JsonObject();
            JsonObject end = new JsonObject();
            main.Add("range", rang);
            rang.Add("start", start);
            rang.Add("end", end);
            start.Add("line", (item.Item2 is <= 0 ? 1 : item.Item2) - 1);
            start.Add("character", (item.Item3 is <= 0 ? 1 : item.Item3) - 1);
            end.Add("line", (item.Item2 is <= 0 ? 1 : item.Item2) - 1);
            end.Add("character", (item.Item3 is <= 0 ? 1 : item.Item3) - 1);
            main.Add("severity", 2);
            main.Add("message", Texer.Japanese[item.Item1]);

            pay_p_d.Add(main);
        }

        SendMessage(pay);
    }

    public static void SendMessage(JsonNode pay)
    {
        var json = pay.ToJsonString();
        int len = Encoding.UTF8.GetByteCount(json);

        Console.Out.Write($"Content-Length: {len}\r\n\r\n" + json);
        Console.Out.Flush();

    }
}