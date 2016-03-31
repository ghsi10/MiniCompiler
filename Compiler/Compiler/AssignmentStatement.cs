using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCompiler
{
    public class AssignmentStatement : Statement
    {
        public VariableExpression Variable;
        public Expression Value;

        public override string ToString()
        {
            return "let " + Variable + " = " + Value + ";";
        }
    }
}
