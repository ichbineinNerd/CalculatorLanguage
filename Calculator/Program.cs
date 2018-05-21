using Microsoft.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Calculator
{
    public class Program
    {
        #region Printing
        enum PrintType
        {
            All,
            FlatDataToType,
            ScopedDataToType,
            FlatData,
            FlatType,
            ScopedData,
            ScopedType,
            Pretty
        }
        static void Main() {
            string input = "PI*(add(1,1)+2)";
            Token[] Tokenized = Tokenizer(input);
            Console.WriteLine("Parsed \"" + input + "\" into");

            PrintType type = PrintType.ScopedDataToType;
            if ((int)type < 1) {
                Array printTypes = Enum.GetValues(typeof(PrintType));
                for (int i = 0; i < printTypes.Length; i++) {
                    PrintType printType = (PrintType)printTypes.GetValue(i);
                    Console.WriteLine(Regex.Replace(Enum.GetName(typeof(PrintType), printType), "(\\B[A-Z])", " $1") + "\n");
                    int TabCount = 0;
                    foreach (Token t in Tokenized) {
                        PrintToken(t, printType, ref TabCount);
                    }
                    Console.WriteLine("\n");
                }
            } else {
                int TabCount = 0;
                foreach (Token t in Tokenized) {
                    PrintToken(t, type, ref TabCount);
                }
            }
            Console.WriteLine("Implementing functions");
            SetFunctions();
            SetVariables();
            Console.WriteLine("Executing...");
            int Result = Execute(Tokenized.ToList());
            Console.WriteLine("Result: " + Result);
            Console.ReadKey();
        }
        public static int Run(string input) {
            Token[] Tokenized = Tokenizer(input);
            SetFunctions();
            SetVariables();
            int Result = Execute(Tokenized.ToList());
            return Result;
        }
        static void PrintToken(Token t, PrintType printType, ref int TabCount) {
            string name = Enum.GetName(typeof(TokenType), t.type);
            switch (t.type) {
                case TokenType.SCOPEEND:
                    TabCount--;
                    break;
            }
            switch (printType) {
                case PrintType.FlatDataToType:
                    Console.WriteLine(t.data + " => " + name);
                    break;
                case PrintType.FlatData:
                    Console.WriteLine(t.data);
                    break;
                case PrintType.FlatType:
                    Console.WriteLine(name);
                    break;
                case PrintType.ScopedData:
                    for (int i = 0; i < TabCount; i++)
                        Console.Write("\t");
                    Console.WriteLine(t.data);
                    break;
                case PrintType.ScopedType:
                    for (int i = 0; i < TabCount; i++)
                        Console.Write("\t");
                    Console.WriteLine(name);
                    break;
                case PrintType.ScopedDataToType:
                    for (int i = 0; i < TabCount; i++)
                        Console.Write("\t");
                    Console.WriteLine(t.data + " => " + name);
                    break;
                case PrintType.Pretty:
                    if (t.type == TokenType.PARAMETERS)
                        Console.Write("(");
                    Console.Write(t.data);
                    if (t.type == TokenType.PARAMETERS)
                        Console.Write(")");
                    if (t.type != TokenType.IDENTIFIER && t.type != TokenType.SCOPESTART && t.type != TokenType.SCOPEEND && t.type != TokenType.PARAMETERS)
                        Console.Write(" ");
                    break;
                default:
                    Console.WriteLine(t.data + " => " + name);
                    break;
            }
            switch (t.type) {
                case TokenType.SCOPESTART:
                    TabCount++;
                    break;
            }
        }
        #endregion
        #region Types
        enum TokenType
        {
            OP,
            NUM,
            IDENTIFIER,
            SCOPESTART,
            SCOPEEND,
            ERROR,
            WHITESPACE,
            PARAMETERS,
            EOF,
            FUNCTIONCALL,
            VAR,
            ASSIGN
        }
        class Token
        {
            public TokenType type;
            public string data;
            public override string ToString() {
                return Enum.GetName(typeof(Token), this) + " => " + data;
            }
        }
        class ArgumentToken : Token
        {
            public List<Token> Arguments;
        }
        class FunctionCallToken : Token
        {
            public Token Function;
            public ArgumentToken Arguments;
        }
        enum State
        {
            MIDID,
            MIDNUM,
            OK,
            MIDPARAM
        }
        #endregion
        #region Helpers
        static void Between(List<Token> tokens, int start, int end, Action<int> callback) {
            for (int i = start; i <= end; i++) {
                callback(i);
            }
        }
        static List<Token> GetBetween(List<Token> tokens, int start, int end) {
            List<Token> ret = new List<Token>();
            Between(tokens, start, end, (int i) => ret.Add(tokens[i]));
            return ret;
        }
        class UnbalancedParanthesisException : Exception
        {
            public override string Message => "Unbalanced Paranthesises";
        }
        static string CharArrayToString(IEnumerable<object> list) {
            string ret = "";
            foreach (object obj in list) {
                ret += obj.ToString();
            }
            return ret;
        }
        static int GetVar(string name) {
            if (Variables.ContainsKey(name))
                return Variables[name];
            else
                return 0;
        }
        public static void SetVal(string name, int value) {
            Variables[name] = value;
        }
        #endregion
        #region Setup
        static void SetFunctions() {
            AddMethod("add", (int x, int y) => { return x + y; });
        }
        public static void AddFunction(string name, MethodInfo method) {
            Functions[name] = method;
        }
        public static void AddMethod<T>(string name, Func<T> func) {
            AddFunction(name, func.Method);
        }
        public static void AddMethod<T, T2>(string name, Func<T, T2> func) {
            AddFunction(name, func.Method);
        }
        public static void AddMethod<T, T2, T3>(string name, Func<T, T2, T3> func) {
            AddFunction(name, func.Method);
        }
        static void AddVariable(string name, int value) {
            Variables[name] = value;
        }
         static void SetVariables() {
            AddVariable("PI", 3);
        }
        #endregion
        #region Executer
        static Dictionary<string, int> Variables = new Dictionary<string, int>();
        static Dictionary<string, MethodInfo> Functions = new Dictionary<string, MethodInfo>();
        static int Execute(List<Token> tokens) {
            int call;
            while ((call = tokens.FindIndex((Token t) => t.type == TokenType.FUNCTIONCALL)) >= 0) {
                FunctionCallToken functionCall = (FunctionCallToken)tokens[call];
                if (Functions.ContainsKey(functionCall.Function.data)) {
                    MethodInfo method = Functions[functionCall.Function.data];
                    List<object> arguments = new List<object>();
                    foreach (Token token in functionCall.Arguments.Arguments) {
                        object data;
                        switch (token.type) {
                            case TokenType.NUM:
                                data = int.Parse(token.data);
                                break;
                            default:
                                data = null;
                                break;
                        }
                        arguments.Add(data);
                    }
                    object ret = method.Invoke(Activator.CreateInstance(method.DeclaringType), arguments.ToArray());
                    string dat = ret.ToString();
                    List<object> generic = new List<object>();
                    List<Token> list = new List<Token>();
                    State state = State.OK;
                    dat = PreProcessInput(dat);
                    foreach (char c in dat) {
                        ParseChar(c, ref state, ref generic, ref list, ref dat);
                    }
                    RemoveEOF(ref list);
                    tokens[call] = new Token() { type = TokenType.WHITESPACE };
                    foreach (Token tok in list) {
                        tokens.Insert(call, tok);
                    }
                }
            }
            int end;
            while ((end = tokens.FindIndex((Token t) => t.type == TokenType.SCOPEEND)) >= 0) {
                List<Token> reversed = new List<Token>(tokens);
                reversed.Reverse();
                int reversedstart = reversed.FindIndex((Token t) => t.type == TokenType.SCOPESTART);
                if (reversedstart < -1)
                    throw new UnbalancedParanthesisException();
                int start = reversed.Count - 1 - reversedstart;

                List<Token> scope = GetBetween(tokens, start + 1, end - 1);
                int value = Calculate(scope);
                tokens = tokens.Select((Token t, int i) => {
                    if (i == start) {
                        return new Token() { type = TokenType.NUM, data = value.ToString() };
                    }
                    if (i > start && i <= end) {
                        return new Token() { type = TokenType.WHITESPACE };
                    }
                    return t;
                }).ToList();
            }
            int result = Calculate(tokens);
            return result;
        }
        enum Operation
        {
            PLUS,
            MINUS,
            MULTIPLY,
            DIVIDE
        }
        static int DoOP(int num1, Operation op, int num2) {
            switch (op) {
                case Operation.PLUS:
                    return num1 + num2;
                case Operation.MINUS:
                    return num1 - num2;
                case Operation.MULTIPLY:
                    return num1 * num2;
                case Operation.DIVIDE:
                    return num1 / num2;
            }
            return 0;
        }
        static int Calculate(List<Token> tokens) {
            int value = 0;
            Operation op = Operation.MULTIPLY;
            for (int i = 0; i < tokens.Count; i++) {
                if ((i == tokens.Count - 1 || i == 0) && tokens[i].type == TokenType.NUM) {
                    value = DoOP(value, op, int.Parse(tokens[i].data));
                } else {
                    if (tokens[i].type == TokenType.OP) {
                        switch (tokens[i].data) {
                            case "+":
                                op = Operation.PLUS;
                                break;
                            case "-":
                                op = Operation.MINUS;
                                break;
                            case "*":
                                op = Operation.MULTIPLY;
                                break;
                            case "/":
                                op = Operation.DIVIDE;
                                break;
                        }
                    } else if (tokens[i].type == TokenType.NUM) {
                        value = DoOP(value, op, int.Parse(tokens[i].data));
                    } else if (tokens[i].type == TokenType.VAR) {
                        if (tokens[i + 1].type != TokenType.ASSIGN) {
                            value = GetVar(tokens[i].data);
                        } else {
                            int val = int.Parse(tokens[i + 2].data);
                            SetVal(tokens[i].data, val);
                        }
                    }
                }
            }
            return value;
        }
        #endregion
        #region Parser
        static char[] Operators = new char[] {
        '*',
        '+',
        '-',
        '/',
        '%'
    };
        static char[] Scopes = new char[] {
        '(',
        ')'
    };
        static char[] Seperator = new char[] {
        ','
    };
        static char[] EOF = new char[] {
        ':'
    };
        static Token[] Tokenizer(string input) {
            input = PreProcessInput(input);
            if (input.Count((char c) => c == '(') != input.Count((char c) => c == ')'))
                throw new UnbalancedParanthesisException();
            List<object> generic = new List<object>();
            List<Token> list = new List<Token>();
            State state = State.OK;
            foreach (char c in input) {
                ParseChar(c, ref state, ref generic, ref list, ref input);
            }
            // IDENTIFIER + PARAMETERS => FUNCTIONCALL
            int name;
            while ((name = list.FindIndex((Token t) => t.type == TokenType.IDENTIFIER)) >= 0) {
                if (name != list.Count - 1) {
                    Token Parameter = list[name + 1];
                    if (list[name + 1].type == TokenType.PARAMETERS) {
                        Token Identifier = list[name];
                        //Found function call!
                        FunctionCallToken call = new FunctionCallToken() { type = TokenType.FUNCTIONCALL, Function = Identifier, Arguments = (ArgumentToken)Parameter, data = Identifier.data + "(" + Parameter.data + ")" };
                        list[name + 1] = new Token() { type = TokenType.WHITESPACE };
                        list[name] = call;
                    } else {
                        list[name].type = TokenType.VAR;
                    }
                }
            }
            return list.ToArray();
        }
        static string PreProcessInput(string input) {
            return input + ":";
        }
        static void RemoveEOF(ref List<Token> input) {
            input = input.Where((Token t) => t.type != TokenType.EOF).ToList();
        }
        static void ParseChar(char c, ref State state, ref List<object> generic, ref List<Token> list, ref string input) {
start:
            if (state == State.OK) {
                if (int.TryParse(c.ToString(), out int res)) {
                    generic.Add(c);
                    state = State.MIDNUM;
                } else if (Operators.Contains(c)) {
                    list.Add(new Token() { type = TokenType.OP, data = c.ToString() });
                } else if (Scopes.Contains(c)) {
                    TokenType t = TokenType.ERROR;
                    switch (c) {
                        case '(':
                            t = TokenType.SCOPESTART;
                            if (list.Count > 0)
                                if (list.Last().type == TokenType.IDENTIFIER)
                                    state = State.MIDPARAM;
                            break;
                        case ')':
                            t = TokenType.SCOPEEND;
                            break;
                    }
                    list.Add(new Token() { type = t, data = c.ToString() });
                    if (state == State.MIDPARAM) {
                        list.Add(new Token());
                        generic.Add(c);
                    }
                } else if (Seperator.Contains(c)) {
                    for (int i = 2; i > 0; i--) {
                        generic.Add(list[list.Count - i]);
                    }
                    generic.Add(c);
                    state = State.MIDPARAM;
                } else if (EOF.Contains(c)) {
                    list.Add(new Token() { type = TokenType.EOF, data = c.ToString() });
                } else if (c == '=') {
                    list.Add(new Token() { type = TokenType.ASSIGN, data = c.ToString() });
                } else {
                    generic.Add(c);
                    state = State.MIDID;
                }
            } else if (state == State.MIDID) {
                if (char.IsLetter(c) && c != input.Last())
                    generic.Add(c);
                else {
                    list.Add(new Token() { type = TokenType.IDENTIFIER, data = CharArrayToString(generic) });
                    generic.Clear();
                    state = State.OK;
                    goto start;
                }
            } else if (state == State.MIDNUM) {
                if (char.IsNumber(c) && c != input.Last())
                    generic.Add(c);
                else {
                    list.Add(new Token() { type = TokenType.NUM, data = CharArrayToString(generic) });
                    generic.Clear();
                    state = State.OK;
                    goto start;
                }
            } else if (state == State.MIDPARAM) {
                generic.Add(c);
                if (c == ')') {
                    List<Token> li = new List<Token>();
                    string inp = "";
                    foreach (object obj in generic) {
                        if (obj.GetType() == typeof(char))
                            inp += (char)obj;
                        else if (((Token)obj).type != TokenType.EOF) {
                            inp += ((Token)obj).data;
                        }
                    }
                    inp = PreProcessInput(inp);
                    li = ParseParam(inp);
                    string temp = "";
                    foreach (Token t in li) {
                        temp += t.data;
                        temp += ",";
                    }
                    temp = temp.Substring(0, temp.Length - 1);
                    list.RemoveRange(list.Count - 2, 2);
                    list.Add(new ArgumentToken() { type = TokenType.PARAMETERS, data = temp, Arguments = li });
                    generic.Clear();
                    state = State.OK;
                }
            }
        }
        static List<Token> ParseParam(string input) {
            string param = CharArrayToString(input.Where((char c, int i) => i != 0 && i < input.Length - 2).Cast<object>());
            param = PreProcessInput(param);
            List<char> generic = new List<char>();
            List<Token> list = new List<Token>();
            foreach (char c in param) {
                if (c != ',' && c != ':')
                    generic.Add(c);
                else {
                    State st = State.OK;
                    List<object> gen = new List<object>();
                    List<Token> li = new List<Token>();
                    string inp = CharArrayToString(generic.Cast<object>());
                    inp = PreProcessInput(inp);
                    foreach (char ch in inp) {
                        ParseChar(ch, ref st, ref gen, ref li, ref inp);
                    }
                    foreach (Token t in li) {
                        if (t.type != TokenType.EOF)
                            list.Add(t);
                    }
                    generic.Clear();
                }
            }
            return list;
        }
        #endregion
    }
}