using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCompiler
{
    public class Compiler
    {
        //Dictionary for the symbol table. For each variable we map its name into its offset (index) from the current LOCAL.
        public Dictionary<string, int> m_dSymbolTable;
        //The number of local variables (including artificial ones)
        public int m_Locals;
        public int m_line;

        public Compiler()
        {
            m_dSymbolTable = new Dictionary<string, int>();
            m_Locals = 0;
            m_line = 0;
        }

        //This method is designed to be the only public method of the class. It launches the compilation process.
        //The rest of the methods are public just to make the debugging easier.
        public List<string> Compile(List<string> lLines)
        {

            List<string> lCompiledCode = new List<string>();
            foreach (string sExpression in lLines)
            {
                List<string> lAssembly = Compile(sExpression);
                lCompiledCode.Add("// " + sExpression);
                lCompiledCode.AddRange(lAssembly);
            }
            return lCompiledCode;
        }

        //Compile a single line containing only an assignment of the form "let <var> = <expression>;"
        public List<string> Compile(string sAssignment)
        {
            //Tokenize the string into a stack of tokens
            Stack<Token> sTokens = Tokenize(sAssignment);
            //Parse the tokens into objects representing the meaning of the statement
            AssignmentStatement s = Parse(sTokens);
            //Simplify complex expressions in order for the code generation to be simpler
            List<AssignmentStatement> lSimpleAssignments = SimplifyExpressions(s);
            //Compute the symbol table here
            ComputeSymbolTable(lSimpleAssignments);
            //Generate the actual code
            List<string> lAssembly = GenerateCode(lSimpleAssignments);
            return lAssembly;
        }

        //Computes the symbol table.
        //For each variable we keep its index (offset from LOCAL).
        //No need to keep type (we use inly int), and kind (we use only local variables).
        private void ComputeSymbolTable(List<AssignmentStatement> lSimpleAssignments)
        {
        }

        //Generates assembly code for a simple assignment statement. Can accept only the following:
        //let <var> = <number>; e.g. let x = 5;
        //let <var> = <var>; e.g. let x = y;
        //let <var> = (<operator> <operand1> <operand2>); where operand1 and operand2 can only by either numbers or variables, but not nested expressions. e.g. let x = (- y 5);
        public List<string> GenerateCode(AssignmentStatement aSimple)
        {
            //your code here
            List<string> generateList = new List<string>();
            //let <var>=<number>
            if (aSimple.Value is NumericExpression)
            {
                generateList.Add("@" + aSimple.Value);
                generateList.Add("D=A");
                generateList.AddRange(Resulting());
            }
            //let <var> = <var>;
            if (aSimple.Value is VariableExpression)
            {
                VariableExpression var = (VariableExpression)aSimple.Value;
                generateList.Add("@LCL");
                generateList.Add("D=M");
                generateList.Add("@" + m_dSymbolTable[var.Name]);
                generateList.Add("A=D+A");
                generateList.Add("D=M");// bringing the value of var
                generateList.AddRange(Resulting());
            }
            //let <var> = (<operator> <operand1> <operand2>);
            if (aSimple.Value is BinaryOperationExpression)
            {
                BinaryOperationExpression bo = (BinaryOperationExpression)aSimple.Value;
                //let x=(+ 5 y)
                if (bo.Operand1 is NumericExpression)
                {
                    generateList.Add("@" + bo.Operand1);
                    generateList.Add("D=A");
                    generateList.Add("@OP1");
                    generateList.Add("M=D");
                }//let x=(+ y 5)
                else if (bo.Operand1 is VariableExpression)
                {
                    VariableExpression var = (VariableExpression)bo.Operand1;
                    generateList.Add("@LCL");
                    generateList.Add("D=M");
                    generateList.Add("@" + m_dSymbolTable[var.Name]);
                    generateList.Add("A=D+A");
                    generateList.Add("D=M");
                    generateList.Add("@OP1");
                    generateList.Add("M=D");
                }
                //let x=(+ y 5)
                if (bo.Operand2 is NumericExpression)
                {
                    generateList.Add("@" + bo.Operand2);
                    generateList.Add("D=A");
                    generateList.Add("@OP2");
                    generateList.Add("M=D");
                }
                //let x=(+ 5 y)
                else if (bo.Operand2 is VariableExpression)
                {
                    VariableExpression var = (VariableExpression)bo.Operand1;
                    generateList.Add("@LCL");
                    generateList.Add("D=M");
                    generateList.Add("@" + m_dSymbolTable[var.Name]);
                    generateList.Add("A=D+A");
                    generateList.Add("D=M");
                    generateList.Add("@OP2");
                    generateList.Add("M=D");
                }
                generateList.AddRange(OperationType(bo));
            }
            generateList.Add("@LCL");
            generateList.Add("D=M");
            generateList.Add("@" + m_dSymbolTable[aSimple.Variable.Name]);
            generateList.Add("D=D+A");
            generateList.Add("@ADDRESS");
            generateList.Add("M=D");
            generateList.Add("@RESULT");
            generateList.Add("D=M");
            generateList.Add("@ADDRESS");
            generateList.Add("A=M");
            generateList.Add("M=D");

            return generateList;
        }

        //privat function which add to the list the assembly commands for result
        private List<string> Resulting()
        {
            List<string> list = new List<string>();
            list.Add("@RESULT");
            list.Add("M=D");
            return list;
        }

        //private function which create assembley for binary operation after op1 and op2 assembley creation
        private List<string> OperationType(BinaryOperationExpression bo)
        {
            List<string> ans = new List<string>();
            ans.Add("@OP1");
            ans.Add("D=M");
            ans.Add("@OP2");
            ans.Add("D=D" + bo.Operator + "M");
            ans.AddRange(Resulting());
            return ans;
        }

        //Generates assembly code for a list of simple assignment statements
        public List<string> GenerateCode(List<AssignmentStatement> lSimpleAssignments)
        {
            List<string> lAssembly = new List<string>();
            foreach (AssignmentStatement aSimple in lSimpleAssignments)
                lAssembly.AddRange(GenerateCode(aSimple));
            return lAssembly;
        }

        //Simplify an expression by creating artificial local variables, and using them for intermidiate computations.
        //For example, let x = (+ (- y 2) (- 5 z)); will be simplified into:
        //let _1 = (- y 2);
        //let _2 = (- 5 z);
        //let x = (+ _1 _2);
        public List<AssignmentStatement> SimplifyExpressions(AssignmentStatement s)
        {
            List<AssignmentStatement> assignList = new List<AssignmentStatement>();
            AssignmentStatement newAssignState;
            VariableExpression newVarForAssign;
            Boolean inIf = false;
            if (s.Value is BinaryOperationExpression)
            {
                BinaryOperationExpression bo = (BinaryOperationExpression)s.Value;
                if (bo.Operand1 is BinaryOperationExpression)
                {
                    inIf = true;
                    newAssignState = new AssignmentStatement();
                    newVarForAssign = new VariableExpression();
                    string newVar = "_" + m_Locals;
                    m_dSymbolTable[newVar] = m_Locals;
                    m_Locals++;
                    newVarForAssign.Name = newVar;
                    newAssignState.Variable = newVarForAssign;
                    newAssignState.Value = bo.Operand1;
                    bo.Operand1 = newAssignState.Variable;
                    assignList.AddRange(SimplifyExpressions(newAssignState));
                }
                if (bo.Operand2 is BinaryOperationExpression)
                {
                    inIf = true;
                    newAssignState = new AssignmentStatement();
                    newVarForAssign = new VariableExpression();
                    string newVar = "_" + m_Locals;
                    m_dSymbolTable[newVar] = m_Locals;
                    m_Locals++;
                    newVarForAssign.Name = newVar;
                    newAssignState.Variable = newVarForAssign;
                    newAssignState.Value = bo.Operand2;
                    bo.Operand2 = newAssignState.Variable;
                    assignList.AddRange(SimplifyExpressions(newAssignState));
                }
                if (inIf)
                {
                    s.Value = bo;
                }
            }
            assignList.Add(s);
            return assignList;
        }

        //Tokenizes a string into tokens. Possible token delimiters are white spaces and also ;()+-
        //Tokens are pushed on the stack in reversed order, that is, the first token is pushed last.
        public Stack<Token> Tokenize(string sExpression)
        {
            Stack<Token> stack = new Stack<Token>();
            int position = sExpression.Length - 1;
            m_line++;
            Token input = new Token();
            Boolean completed = false;
            for (int i = sExpression.Length - 1; i >= 0 && !completed; i--)
            {
                if (sExpression[i] != ' ')
                {
                    if (sExpression[i] == '(' || sExpression[i] == ')' || sExpression[i] == '+' ||
                        sExpression[i] == '-' || sExpression[i] == ';' || sExpression[i] == '=')
                    {
                        input.Name = Convert.ToString(sExpression[i]);
                        input.Position = position;
                        input.Line = m_line;
                        switch (sExpression[i])
                        {
                            case ('+'):
                            case ('-'):
                                {
                                    input.Type = Token.TokenType.Operator;
                                    break;
                                }
                            case ('('):
                            case (')'):
                            case (';'):
                            case ('='):
                                {
                                    input.Type = Token.TokenType.Symbol;
                                    break;
                                }
                        }
                        stack.Push(input);
                    }
                    else
                    {
                        string word = "";
                        while (i >= 0 && sExpression[i] != ' ' && (sExpression[i] != '(') && (sExpression[i] != ')') &&
                                (sExpression[i] != '+') && (sExpression[i] != '-') &&
                                (sExpression[i] != ';') && (sExpression[i] != '='))
                        {
                            word = sExpression[i] + word;
                            i--;
                        }
                        input.Name = word;
                        input.Line = m_line;
                        input.Position = position;
                        int num;
                        Boolean isNumber = int.TryParse(Convert.ToString(word), out num);
                        if (word == "let")
                            input.Type = Token.TokenType.Keyword;
                        else
                        {
                            if (isNumber)
                                input.Type = Token.TokenType.Number;
                            else
                            {
                                int n;
                                Boolean alphaNumeric = false;
                                Boolean valid = false;
                                if (word.Length > 1)
                                {
                                    alphaNumeric = int.TryParse(word[0].ToString(), out n);
                                    if (!alphaNumeric)
                                        valid = true;
                                }
                                if ((word.Length == 1) && ((word[0] >= 'a') && (word[0] <= 'z')) || ((word[0] >= 'A') && (word[0] <= 'Z')))
                                    valid = true;
                                if ((valid) && (!isNumber) && (!alphaNumeric))
                                    input.Type = Token.TokenType.ID;
                                else if ((alphaNumeric) || (!isNumber))
                                    throw new SyntaxErrorException("alpha Numeric statment is invalid in the expression or invalid input in statment", input);
                            }

                        }
                        stack.Push(input);
                    }
                }
                input = new Token();
                position--;
            }
            return stack;
        }

        //Parses a stack of tokens, containing a single assignment statement. 
        //The structure must be "let <var> = <expression>;" where expression can be of an arbitrary complexity, i.e., any complex expression is allowed.
        //Parsing must detect syntax problems (e.g. "let" or "=" are missing, opened parantheses are not closed, sentence does not end with a ;, and so forth).
        //When syntax errors are detected, a SyntaxErrorException must be thrown, with an appropriate message explaining the problem.
        public AssignmentStatement Parse(Stack<Token> sTokens)
        {
            //assignment has variable and expression
            AssignmentStatement assignmentState = new AssignmentStatement();
            //check brackets
            checkBrackets(sTokens);

            //first word should be "let"
            Token s = sTokens.Pop();
            if (s.Name != "let") //start with "let"
                throw new SyntaxErrorException("let is missing in the Expression", s);

            //second should be variable 
            s = sTokens.Pop();//the variable must start with a letter
            if (!(((s.Name[0] >= 'a') && (s.Name[0] <= 'z')) || ((s.Name[0] >= 'A') && (s.Name[0] <= 'Z'))))
                throw new SyntaxErrorException("Variable must start with a letter", s);
            VariableExpression variable = new VariableExpression();
            variable.Name = s.Name;
            assignmentState.Variable = variable; //name of the var
            if (!m_dSymbolTable.ContainsKey(s.Name)) // if the var is not in the table
            {
                m_dSymbolTable[s.Name] = m_Locals;
                m_Locals++;
            }

            //after variable should be "="
            s = sTokens.Pop();
            if (s.Name != "=")
                throw new SyntaxErrorException("missing '=' ", s);

            //the expression after "="
            s = sTokens.Pop();
            int c;
            Boolean isNum = int.TryParse(s.Name, out c);
            if (isNum)
            {
                NumericExpression number = new NumericExpression();
                number.Value = c;
                assignmentState.Value = number;
            }
            else if ((s.Name != "(" && s.Name != "+" && s.Name != "-") && (((s.Name[0] >= 'a') && (s.Name[0] <= 'z')) || ((s.Name[0] >= 'A') && (s.Name[0] <= 'Z'))))
            {
                if (m_dSymbolTable.ContainsKey(s.Name))
                {
                    VariableExpression varExp = new VariableExpression();
                    varExp.Name = s.Name;
                    assignmentState.Value = varExp;
                }
                else
                {
                    m_dSymbolTable[s.Name] = m_Locals;
                    m_Locals++;
                }
            }
            else if (s.Name == "(")
            {
                while (s.Name == "(")
                {
                    s = sTokens.Pop();
                }
                if (s.Name != "+" && s.Name != "-")
                    throw new SyntaxErrorException("wrong expression", s);

                BinaryOperationExpression binaryOpeartion = new BinaryOperationExpression();
                binaryOpeartion = parseBinaryOp(sTokens, s);
                assignmentState.Value = binaryOpeartion;
            }
            else
                throw new SyntaxErrorException("expression error", s);

            s = sTokens.Pop();
            if (s.Name != ";")
                throw new SyntaxErrorException("missing ';'", s);
            return assignmentState;
        }


        //this nethod checks that the brackets are correct
        private void checkBrackets(Stack<Token> stack)
        {
            Stack<Token> tmp = new Stack<Token>();
            Token s = new Token();
            int countLeft = 0, countRight = 0;
            int count = stack.Count();
            while (count > 0)
            {
                s = stack.Pop();
                if (s.Name == "(")
                    countLeft++;
                if (s.Name == ")")
                    countRight++;
                tmp.Push(s);
                count = stack.Count();
            }
            if (countRight != countLeft)
                throw new SyntaxErrorException("Brackets problem", s);
            else
            {
                count = tmp.Count();
                while (count > 0)
                {
                    s = tmp.Pop();
                    stack.Push(s);
                    count = tmp.Count();
                }
            }
        }


        private BinaryOperationExpression parseBinaryOp(Stack<Token> stack, Token s)
        {
            BinaryOperationExpression bo = new BinaryOperationExpression();
            //operator
            if (s.Name == "+" || s.Name == "-")
            {
                bo.Operator = s.Name;
                s = stack.Pop();
            }

            //operand1
            int number;
            Boolean isNum = int.TryParse(s.Name, out number);
            if (s.Name == "(")
            {
                s = stack.Pop();
                if (s.Name == "+" || s.Name == "-")
                    bo.Operand1 = parseBinaryOp(stack, s);
                else
                    throw new SyntaxErrorException("wrong expression in operand1", s);
            }
            else if (isNum)
            {
                NumericExpression numeric = new NumericExpression();
                numeric.Value = number;
                bo.Operand1 = numeric;
            }

            else if (m_dSymbolTable.ContainsKey(s.Name))
            {
                VariableExpression var = new VariableExpression();
                var.Name = s.Name;
                bo.Operand1 = var;
            }
            else
            {
                m_dSymbolTable[s.Name] = m_Locals;
                m_Locals++;
                VariableExpression var = new VariableExpression();
                var.Name = s.Name;
                bo.Operand1 = var;
            }
            s = stack.Pop();

            //op2
            int num2;
            Boolean isNum2 = int.TryParse(s.Name, out num2);
            if (s.Name == "(")
            {
                s = stack.Pop();
                if (s.Name == "+" || s.Name == "-")
                    bo.Operand2 = parseBinaryOp(stack, s);
                else
                    throw new SyntaxErrorException("wrong expression in operand2", s);
            }
            else if (isNum2)
            {
                NumericExpression numeric = new NumericExpression();
                numeric.Value = num2;
                bo.Operand2 = numeric;
            }
            else if (m_dSymbolTable.ContainsKey(s.Name))
            {
                VariableExpression var = new VariableExpression();
                var.Name = s.Name;
                bo.Operand2 = var;
            }
            else if (((s.Name[0] >= 'a') && (s.Name[0] <= 'z')) || ((s.Name[0] >= 'A') && (s.Name[0] <= 'Z')))
            {
                m_dSymbolTable[s.Name] = m_Locals;
                m_Locals++;
                VariableExpression var = new VariableExpression();
                var.Name = s.Name;
                bo.Operand2 = var;
            }
            else
                throw new SyntaxErrorException("wrong expression", s);

            //end of expression
            if (stack.Peek().Name != ";")
                s = stack.Pop();

            return bo;
        }


    }
}
