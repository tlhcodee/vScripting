using System;
using System.Collections.Generic;
using System.Text;

namespace vScripting.core
{
    public interface IScriptCommand
    {
        string Name { get; }
        void Execute(ScriptContext ctx, string args);
    }
}
