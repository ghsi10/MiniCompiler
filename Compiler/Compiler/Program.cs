using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCompiler
{
    class Program
    {
        static void Test1()
        {
            Compiler c = new Compiler();
            string s = "let x = 5;";
            Stack<Token> sTokens = c.Tokenize(s);
            string[] aTokens = new string[] { "let", "x", "=", "5", ";" };
            for (int i = 0; i < aTokens.Length; i++)
            {
                Token sToken = sTokens.Pop();
                if (sToken.ToString() != aTokens[i])
                    Console.WriteLine("BUGBUG");
            }
            sTokens = c.Tokenize(s);
            AssignmentStatement assignment = c.Parse(sTokens);
            if (assignment.ToString() != s)
                Console.WriteLine("BUGBUG");
            List<AssignmentStatement> lSimple = c.SimplifyExpressions(assignment);
            if (lSimple.Count != 1 || lSimple[0].ToString() != assignment.ToString())
                Console.WriteLine("BUGBUG");
            List<string> lAssembly = c.GenerateCode(lSimple);
        }

        static void Test2()
        {
            Compiler c = new Compiler();
            string s = "let x = (+ (+ x 5) (- y z));";
            Stack<Token> sTokens = c.Tokenize(s);
            string[] aTokens = new string[] { "let", "x", "=", "(", "+", "(", "+", "x", "5", ")", "(", "-", "y", "z", ")", ")", ";" };
            for (int i = 0; i < aTokens.Length; i++)
            {
                Token sToken = sTokens.Pop();
                if (sToken.ToString() != aTokens[i])
                    Console.WriteLine("BUGBUG");
            }
            sTokens = c.Tokenize(s);
            AssignmentStatement assignment = c.Parse(sTokens);
            if (assignment.ToString() != s)
                Console.WriteLine("BUGBUG");
            List<AssignmentStatement> lSimple = c.SimplifyExpressions(assignment);
            string[] aSimple = new string[] { "let _3 = (+ x 5);", "let _4 = (- y z);", "let x = (+ _3 _4);" };
            for (int i = 0; i < aSimple.Length; i++)
                if (lSimple[i].ToString() != aSimple[i])
                    Console.WriteLine("BUGBUG");
            List<string> lAssembly = c.GenerateCode(lSimple);

        }

        static void Test3()
        {
            Compiler c = new Compiler();
            List<string> lExpressions = new List<string>();
            lExpressions.Add("let x = (+ (- 53 12) (- 467 3));");
            lExpressions.Add("let y = 3;");
            lExpressions.Add("let z = (+ (- x 12) (+ y 3));");
            c.Compile(lExpressions);
        }

        static void Main(string[] args)
        {
            Test1();
            Test2();
            Test3();
        }
    }
}
