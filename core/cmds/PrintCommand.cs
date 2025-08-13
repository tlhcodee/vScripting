using System;
using System.Collections.Generic;
using System.Text;

namespace vScripting.core.cmds
{
    public sealed class PrintCommand : IScriptCommand
    {
        public string Name => "print";

        public void Execute(ScriptContext ctx, string args)
        {
            args = args.Trim();
            if (!args.StartsWith("(") || !args.EndsWith(")"))
                throw new InvalidOperationException($"Geçersiz print/yazdir sözdizimi: {args}");

            var inner = args.Substring(1, args.Length - 2).Trim();

            var parts = SplitPlusRespectingQuotes(inner);

            var sb = new StringBuilder();

            foreach (var raw in parts)
            {
                var piece = raw.Trim();

                if (piece.Length == 0)
                    continue;

                if (piece.StartsWith("\"") && piece.EndsWith("\"") && piece.Length >= 2)
                {
                    sb.Append(piece.Substring(1, piece.Length - 2));
                    continue;
                }

                if (ctx.Vars.TryGetValue(piece, out var val))
                {
                    sb.Append(val?.ToString() ?? "");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[vScripting] Değişken bulunamadı: {piece}");
                }
            }

            ServerSend.SendChatMessage(1, sb.ToString());
        }

        private static List<string> SplitPlusRespectingQuotes(string s)
        {
            var list = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    sb.Append(c);
                    continue;
                }

                if (!inQuotes && c == '+')
                {
                    list.Add(sb.ToString());
                    sb.Clear();
                    continue;
                }

                sb.Append(c);
            }

            if (sb.Length > 0)
                list.Add(sb.ToString());

            return list;
        }
    }
}
