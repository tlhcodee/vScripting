using System;
using System.Collections.Generic;
using System.Text;

namespace vScripting.core
{
    public interface IVariable
    {
        string name { get; }
        object value { get; }
        void SetValue(object value);
    }
}
