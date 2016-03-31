using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCompiler
{
    public class BinaryOperationExpression : Expression
    {
        public string Operator;
        public Expression Operand1, Operand2;

        public override string ToString()
        {
            return "(" + Operator + " " + Operand1 + " " + Operand2 + ")";
        }
    }
}
