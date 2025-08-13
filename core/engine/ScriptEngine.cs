using System;
using System.Collections.Generic;
using System.IO;

namespace vScripting.core.engine
{
    public sealed class ScriptEngine
    {
        private static readonly string[] StartKeywords = { "onEvent", "olay", "etkinlik" };
        private static readonly string[] EndKeywords = { "end", "bitir", "son" };

        private readonly Dictionary<string, List<string>> _eventBlocks =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, IScriptCommand> _commands =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _aliases =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly List<(string name, Action<ScriptContext, string[]> action)> _pendingApiAdds = new();

        private readonly Dictionary<string, vScripting.core.IVariable> _variables =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly List<string> _oyuncuTagParams = new();

        public void Register(IScriptCommand command) => _commands[command.Name] = command;
        public void RegisterAlias(string alias, string targetCommandName) => _aliases[alias] = targetCommandName;
        public void RegisterApi(string name, Action<ScriptContext, string[]> action) => _pendingApiAdds.Add((name, action));

        public void LoadScript(string path)
        {
            var lines = File.ReadAllLines(path);
            string? currentEvent = null;
            var buffer = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                var raw = lines[i];
                var line = raw.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                if (currentEvent == null)
                {
                    if (TryParseGlobalInteger(line))
                        continue;

                    if (TryMatchStart(line, out var evtDecl))
                    {
                        if (evtDecl.StartsWith("OyuncuTag", StringComparison.OrdinalIgnoreCase))
                        {
                            _oyuncuTagParams.Clear();
                            int p = evtDecl.IndexOf('(');
                            if (p != -1 && evtDecl.EndsWith(")"))
                            {
                                var inside = evtDecl.Substring(p + 1, evtDecl.Length - p - 2);
                                foreach (var item in inside.Split(',', StringSplitOptions.RemoveEmptyEntries))
                                    _oyuncuTagParams.Add(item.Trim());
                                evtDecl = evtDecl.Substring(0, p).Trim(); 
                            }
                        }

                        currentEvent = evtDecl;
                        buffer.Clear();
                        continue;
                    }

                    throw new InvalidOperationException($"Olay (onEvent/olay) dışında komut var (satır {i + 1}): {raw}");
                }

                if (IsEnd(line))
                {
                    if (currentEvent == null)
                        throw new InvalidOperationException($"'son' / 'end' için açık bir olay yok (satır {i + 1}).");

                    _eventBlocks[currentEvent] = new List<string>(buffer);
                    currentEvent = null;
                    buffer.Clear();
                    continue;
                }

                buffer.Add(raw);
            }

            if (currentEvent != null)
                throw new InvalidOperationException($"'son' / 'end' eksik: {currentEvent}");
        }

        public bool HasEvent(string eventName) => _eventBlocks.ContainsKey(eventName);

        public void Trigger(string eventName, ScriptContext ctx, params object[] args)
        {
            if (!_eventBlocks.TryGetValue(eventName, out var block))
                return;

            foreach (var (name, action) in _pendingApiAdds)
                ctx.Api[name] = action;

            foreach (var kv in _variables)
                ctx.Vars[kv.Key] = kv.Value.value; 

            if (eventName.Equals("OyuncuTag", StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 0; i < _oyuncuTagParams.Count && i < args.Length; i++)
                    ctx.Vars[_oyuncuTagParams[i]] = args[i];
            }

            foreach (var raw in block)
            {
                var line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                if (TryExecuteIntegerStatement(line, ctx))
                    continue;

                var (cmd, cmdArgs) = SplitCommand(line);

                if (!_commands.ContainsKey(cmd) && _aliases.TryGetValue(cmd, out var target))
                    cmd = target;

                if (_commands.TryGetValue(cmd, out var handler))
                {
                    handler.Execute(ctx, cmdArgs);
                }
                else
                {
                    throw new InvalidOperationException($"Bilinmeyen komut: {cmd}");
                }
            }
        }

        private bool TryMatchStart(string line, out string eventDecl)
        {
            foreach (var kw in StartKeywords)
            {
                var prefix = kw + " ";
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    eventDecl = line.Substring(prefix.Length).Trim();
                    return true;
                }
            }
            eventDecl = null!;
            return false;
        }

        private bool IsEnd(string line)
        {
            foreach (var kw in EndKeywords)
                if (line.Equals(kw, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static (string cmd, string args) SplitCommand(string line)
        {
            line = line.Trim();
            int p = line.IndexOf('(');
            if (p > 0)
            {
                string cmd = line.Substring(0, p).Trim();
                string args = line.Substring(p).Trim(); 
                return (cmd, args);
            }

            int s = line.IndexOf(' ');
            string c = s >= 0 ? line[..s].Trim() : line;
            string a = s >= 0 ? line[(s + 1)..].Trim() : string.Empty;
            return (c, a);
        }

        private bool TryParseGlobalInteger(string line)
        {
            bool isDeclaration = false;

            if (line.StartsWith("tam ", StringComparison.OrdinalIgnoreCase))
            {
                isDeclaration = true;
                line = line.Substring(3).Trim();
            }

            if (line.EndsWith("++", StringComparison.Ordinal))
            {
                var name = line.Substring(0, line.Length - 2).Trim();
                var ivar = RequireIntVar(name, mustExist: !isDeclaration);
                ivar.SetValue((int)ivar.value + 1);
                return true;
            }

            if (line.EndsWith("--", StringComparison.Ordinal))
            {
                var name = line.Substring(0, line.Length - 2).Trim();
                var ivar = RequireIntVar(name, mustExist: !isDeclaration);
                ivar.SetValue((int)ivar.value - 1);
                return true;
            }

            int idxPlusEq = line.IndexOf("+=", StringComparison.Ordinal);
            int idxMinusEq = line.IndexOf("-=", StringComparison.Ordinal);
            if (idxPlusEq > 0 || idxMinusEq > 0)
            {
                bool plus = idxPlusEq > 0;
                int idx = plus ? idxPlusEq : idxMinusEq;
                var name = line.Substring(0, idx).Trim();
                var rhs = line.Substring(idx + 2).Trim();

                if (!int.TryParse(rhs, out int delta))
                    throw new InvalidOperationException($"Geçersiz sayı: {rhs}");

                var ivar = RequireIntVar(name, mustExist: !isDeclaration);
                ivar.SetValue((int)ivar.value + (plus ? delta : -delta));
                return true;
            }

            int eq = line.IndexOf('=');
            if (eq >= 0)
            {
                var name = line.Substring(0, eq).Trim();
                var valueStr = line.Substring(eq + 1).Trim();

                if (!int.TryParse(valueStr, out int value))
                    throw new InvalidOperationException($"Geçersiz tam sayı değeri: {valueStr}");

                if (_variables.TryGetValue(name, out var existing))
                {
                    ((vScripting.core.variables.IntegerVariable)existing).SetValue(value);
                }
                else
                {
                    if (!isDeclaration)
                        throw new InvalidOperationException($"Değişken bulunamadı: {name} (ilk tanım için 'tam {name} = ...' yaz)");
                    _variables[name] = new vScripting.core.variables.IntegerVariable(name, value);
                }

                return true;
            }

            return false;
        }


        private bool TryExecuteIntegerStatement(string line, ScriptContext ctx)
        {
            var s = line.Trim();
            if (s.Length == 0 || s.StartsWith("#")) return true; 

            if (s.EndsWith("++", StringComparison.Ordinal))
            {
                var name = s.Substring(0, s.Length - 2).Trim();
                var ivar = RequireIntVar(name, mustExist: true);
                ivar.SetValue((int)ivar.value + 1);
                ctx.Vars[name] = ivar.value;
                return true;
            }

            if (s.EndsWith("--", StringComparison.Ordinal))
            {
                var name = s.Substring(0, s.Length - 2).Trim();
                var ivar = RequireIntVar(name, mustExist: true);
                ivar.SetValue((int)ivar.value - 1);
                ctx.Vars[name] = ivar.value;
                return true;
            }

            int idxPlusEq = s.IndexOf("+=", StringComparison.Ordinal);
            int idxMinusEq = s.IndexOf("-=", StringComparison.Ordinal);
            if (idxPlusEq > 0 || idxMinusEq > 0)
            {
                bool plus = idxPlusEq > 0;
                int idx = plus ? idxPlusEq : idxMinusEq;

                var name = s.Substring(0, idx).Trim();
                var rhs = s.Substring(idx + 2).Trim();

                if (!int.TryParse(rhs, out int delta))
                    throw new InvalidOperationException($"Geçersiz sayı: {rhs}");

                var ivar = RequireIntVar(name, mustExist: true);
                ivar.SetValue((int)ivar.value + (plus ? delta : -delta));
                ctx.Vars[name] = ivar.value;
                return true;
            }

            int eq = s.IndexOf('=');
            if (eq > 0)
            {
                var name = s.Substring(0, eq).Trim();
                var valueStr = s.Substring(eq + 1).Trim();

                if (!int.TryParse(valueStr, out int value))
                    throw new InvalidOperationException($"Geçersiz tam sayı değeri: {valueStr}");

                var ivar = RequireIntVar(name, mustExist: true);
                ivar.SetValue(value);
                ctx.Vars[name] = ivar.value;
                return true;
            }

            return false; 
        }


        private vScripting.core.variables.IntegerVariable RequireIntVar(string name, bool mustExist)
        {
            if (_variables.TryGetValue(name, out var varObj))
                return (vScripting.core.variables.IntegerVariable)varObj;

            if (mustExist)
                throw new InvalidOperationException($"Değişken bulunamadı: {name} (ilk tanım için 'tam {name} = ...' yaz)");

            var created = new vScripting.core.variables.IntegerVariable(name, 0);
            _variables[name] = created;
            return created;
        }
    }
}
