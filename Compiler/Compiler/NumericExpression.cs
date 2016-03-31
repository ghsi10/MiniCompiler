using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCompiler
{
    public class NumericExpression : Expression
    {
        public int Value;

        public override string ToString()
        {
            return Value + "";
        }
    }
}
