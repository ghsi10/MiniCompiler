using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleCompiler
{
    public class Token
    {
        public enum TokenType { Number, ID, Operator, Symbol, Keyword }
        public string Name { get; set; }
        public TokenType Type { get; set; }
        public int Line { get; set; }
        public int Position { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
