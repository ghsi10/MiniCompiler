using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCompiler
{
    public class VariableExpression : Expression
    {
        public string Name;

        public override string ToString()
        {
            return Name;
        }
    }
}
