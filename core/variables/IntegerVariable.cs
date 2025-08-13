using System;
using System.Collections.Generic;
using System.Text;

namespace vScripting.core.variables
{
    public sealed class IntegerVariable : IVariable
    {
        public string name { get; }
        public object value { get; private set; }

        public IntegerVariable(string name, int value)
        {
            this.name = name ?? throw new ArgumentNullException(nameof(name));
            this.value = value; 
        }

        public void SetValue(object value)
        {
            if (value is int i)
            {
                this.value = i;
            }
            else if (value is string s && int.TryParse(s, out var j))
            {
                this.value = j;
            }
            else
            {
                throw new InvalidCastException($"IntegerVariable '{name}' int dışı değer alamaz: {value?.GetType().Name}");
            }
        }

        public override string ToString() => value.ToString();
    }
}
