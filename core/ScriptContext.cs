using System;
using System.Collections.Generic;
using System.Text;

namespace vScripting.core
{

    public class ScriptContext
    {
        public Dictionary<string, Action<ScriptContext, string[]>> Api { get; } =
            new(StringComparer.OrdinalIgnoreCase);


        public Dictionary<string, object> Vars { get; } =
            new(StringComparer.OrdinalIgnoreCase);
    }

}
