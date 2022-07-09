using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ExpressionEvaluatorExtended
{
    //
    // Résumé :
    //     Evaluates simple math expressions; supports int float and operators: + - * %
    //     ^ ( ).
    public class ExpressionEvaluator
    {
        
        private enum Associativity
        {
            Left,
            Right
        }

        private struct Operator
        {
            public char character;

            public int presedence;

            public Associativity associativity;

            public int inputs;

            public Operator(char character, int presedence, int inputs, Associativity associativity)
            {
                this.character = character;
                this.presedence = presedence; //priority order 
                this.inputs = inputs;
                this.associativity = associativity;
            }
        }

        private static readonly Operator[] s_Operators = new Operator[7]
        {
            new Operator('-', 2, 2, Associativity.Left),
            new Operator('+', 2, 2, Associativity.Left),
            new Operator('/', 3, 2, Associativity.Left),
            new Operator('*', 3, 2, Associativity.Left),
            new Operator('%', 3, 2, Associativity.Left),
            new Operator('^', 4, 2, Associativity.Right),
            new Operator('u', 4, 1, Associativity.Left)
        };

        public static bool Evaluate<T>(string expression, out T value)
        {
            if (TryParse<T>(expression, out value))
            {
                return true;
            }

            expression = PreFormatExpression(expression);
            string[] tokens = ExpressionToTokens(expression);
            tokens = FixUnaryOperators(tokens);
            string[] tokens2 = InfixToRPN(tokens);
            return Evaluate(tokens2, out value);
        }

        public static bool Evaluate<T>(string expression, Dictionary<string, T> dict, out T value)
        {

            if (TryParse<T>(expression, out value))
            {
                return true;
            }

            expression = PreFormatExpression(expression);
            string[] tokens = ExpressionToTokens(expression);
            tokens = FixUnaryOperators(tokens);
            string[] tokens2 = InfixToRPN(tokens);
            string[] tokens3 = ExpressionToUserInput(tokens2,dict);
            return Evaluate(tokens3, out value);
        }

        private static string[] ExpressionToUserInput<T>(string[] tokens, Dictionary<string,T> dict)
        {
            for (int i =0;i<tokens.Length;i++)
            {
                string token = tokens[i];
                if (dict.ContainsKey(token))
                {
                    tokens[i] = dict[token].ToString() ;
                }
            }
            return tokens;
        }
        

        private static bool Evaluate<T>(string[] tokens, out T value)
        {
            Stack<string> stack = new Stack<string>();
            foreach (string text in tokens)
            {
                if (IsOperator(text))
                {
                    Operator @operator = CharToOperator(text[0]);
                    List<T> list = new List<T>();
                    bool flag = true;
                    while (stack.Count > 0 && !IsCommand(stack.Peek()) && list.Count < @operator.inputs)
                    {
                        flag &= TryParse<T>(stack.Pop(), out var result);
                        list.Add(result);
                    }

                    list.Reverse();
                    if (!flag || list.Count != @operator.inputs)
                    {
                        value = default(T);
                        return false;
                    }

                    stack.Push(Evaluate(list.ToArray(), text[0]).ToString());
                }
                else
                {
                    stack.Push(text);
                }
            }

            if (stack.Count == 1 && TryParse<T>(stack.Pop(), out value))
            {
                return true;
            }

            value = default(T);
            return false;
        }

        private static string[] InfixToRPN(string[] tokens)
        {
            Stack<char> stack = new Stack<char>();
            Queue<string> queue = new Queue<string>();
            foreach (string text in tokens)
            {
                if (IsCommand(text))
                {
                    char c = text[0];
                    switch (c)
                    {
                        case '(':
                            stack.Push(c);
                            break;
                        case ')':
                            while (stack.Count > 0 && stack.Peek() != '(')
                            {
                                queue.Enqueue(stack.Pop().ToString());
                            }

                            if (stack.Count > 0)
                            {
                                stack.Pop();
                            }

                            break;
                        default:
                            {
                                Operator newOperator = CharToOperator(c);
                                while (NeedToPop(stack, newOperator))
                                {
                                    queue.Enqueue(stack.Pop().ToString());
                                }

                                stack.Push(c);
                                break;
                            }
                    }
                }
                else
                {
                    queue.Enqueue(text);
                }
            }

            while (stack.Count > 0)
            {
                queue.Enqueue(stack.Pop().ToString());
            }

            return queue.ToArray();
        }

        private static bool NeedToPop(Stack<char> operatorStack, Operator newOperator)
        {
            if (operatorStack.Count > 0)
            {
                Operator @operator = CharToOperator(operatorStack.Peek());
                if (IsOperator(@operator.character) && ((newOperator.associativity == Associativity.Left && newOperator.presedence <= @operator.presedence) || (newOperator.associativity == Associativity.Right && newOperator.presedence < @operator.presedence)))
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] ExpressionToTokens(string expression)
        {
            List<string> list = new List<string>();
            string text = "";
            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];
                if (IsCommand(c))
                {
                    if (text.Length > 0)
                    {
                        list.Add(text);
                    }

                    list.Add(c.ToString());
                    text = "";
                }
                else if (c != ' ')
                {
                    text += c;
                }
            }

            if (text.Length > 0)
            {
                list.Add(text);
            }

            return list.ToArray();
        }

        private static bool IsCommand(string token)
        {
            if (token.Length != 1)
            {
                return false;
            }

            return IsCommand(token[0]);
        }

        private static bool IsCommand(char character)
        {
            if (character == '(' || character == ')')
            {
                return true;
            }

            return IsOperator(character);
        }

        private static bool IsOperator(string token)
        {
            if (token.Length != 1)
            {
                return false;
            }

            return IsOperator(token[0]);
        }

        private static bool IsOperator(char character)
        {
            Operator[] array = s_Operators;
            for (int i = 0; i < array.Length; i++)
            {
                Operator @operator = array[i];
                if (@operator.character == character)
                {
                    return true;
                }
            }

            return false;
        }

        private static Operator CharToOperator(char character)
        {
            Operator[] array = s_Operators;
            for (int i = 0; i < array.Length; i++)
            {
                Operator result = array[i];
                if (result.character == character)
                {
                    return result;
                }
            }

            return default(Operator);
        }

        private static string PreFormatExpression(string expression)
        {
            string text = expression;
            text = text.Trim();
            if (text.Length == 0)
            {
                return text;
            }

            char c = text[text.Length - 1];
            if (IsOperator(c))
            {
                text = text.TrimEnd(c);
            }

            return text;
        }

        private static string[] FixUnaryOperators(string[] tokens)
        {
            if (tokens.Length == 0)
            {
                return tokens;
            }

            if (tokens[0] == "-")
            {
                tokens[0] = "u";
            }

            for (int i = 1; i < tokens.Length - 1; i++)
            {
                string text = tokens[i];
                string token = tokens[i - 1];
                string text2 = tokens[i - 1];
                if (text == "-" && (IsCommand(token) || text2 == "(" || text2 == ")"))
                {
                    tokens[i] = "u";
                }
            }

            return tokens;
        }

        private static T Evaluate<T>(T[] values, char oper)
        {
            if ((object)typeof(T) == typeof(float))
            {
                if (values.Length == 1)
                {
                    char c = oper;
                    char c2 = c;
                    if (c2 == 'u')
                    {
                        return (T)(object)((float)(object)values[0] * -1f);
                    }
                }
                else if (values.Length == 2)
                {
                    switch (oper)
                    {
                        case '+':
                            return (T)(object)((float)(object)values[0] + (float)(object)values[1]);
                        case '-':
                            return (T)(object)((float)(object)values[0] - (float)(object)values[1]);
                        case '*':
                            return (T)(object)((float)(object)values[0] * (float)(object)values[1]);
                        case '/':
                            return (T)(object)((float)(object)values[0] / (float)(object)values[1]);
                        case '%':
                            return (T)(object)((float)(object)values[0] % (float)(object)values[1]);
                        case '^':
                            return (T)(object)Mathf.Pow((float)(object)values[0], (float)(object)values[1]);
                    }
                }
            }
            else if ((object)typeof(T) == typeof(int))
            {
                if (values.Length == 1)
                {
                    char c3 = oper;
                    char c4 = c3;
                    if (c4 == 'u')
                    {
                        return (T)(object)((int)(object)values[0] * -1);
                    }
                }
                else if (values.Length == 2)
                {
                    switch (oper)
                    {
                        case '+':
                            return (T)(object)((int)(object)values[0] + (int)(object)values[1]);
                        case '-':
                            return (T)(object)((int)(object)values[0] - (int)(object)values[1]);
                        case '*':
                            return (T)(object)((int)(object)values[0] * (int)(object)values[1]);
                        case '/':
                            return (T)(object)((int)(object)values[0] / (int)(object)values[1]);
                        case '%':
                            return (T)(object)((int)(object)values[0] % (int)(object)values[1]);
                        case '^':
                            return (T)(object)(int)Math.Pow((int)(object)values[0], (int)(object)values[1]);
                    }
                }
            }

            if ((object)typeof(T) == typeof(double))
            {
                if (values.Length == 1)
                {
                    char c5 = oper;
                    char c6 = c5;
                    if (c6 == 'u')
                    {
                        return (T)(object)((double)(object)values[0] * -1.0);
                    }
                }
                else if (values.Length == 2)
                {
                    switch (oper)
                    {
                        case '+':
                            return (T)(object)((double)(object)values[0] + (double)(object)values[1]);
                        case '-':
                            return (T)(object)((double)(object)values[0] - (double)(object)values[1]);
                        case '*':
                            return (T)(object)((double)(object)values[0] * (double)(object)values[1]);
                        case '/':
                            return (T)(object)((double)(object)values[0] / (double)(object)values[1]);
                        case '%':
                            return (T)(object)((double)(object)values[0] % (double)(object)values[1]);
                        case '^':
                            return (T)(object)Math.Pow((double)(object)values[0], (double)(object)values[1]);
                    }
                }
            }
            else if ((object)typeof(T) == typeof(long))
            {
                if (values.Length == 1)
                {
                    char c7 = oper;
                    char c8 = c7;
                    if (c8 == 'u')
                    {
                        return (T)(object)((long)(object)values[0] * -1);
                    }
                }
                else if (values.Length == 2)
                {
                    switch (oper)
                    {
                        case '+':
                            return (T)(object)((long)(object)values[0] + (long)(object)values[1]);
                        case '-':
                            return (T)(object)((long)(object)values[0] - (long)(object)values[1]);
                        case '*':
                            return (T)(object)((long)(object)values[0] * (long)(object)values[1]);
                        case '/':
                            return (T)(object)((long)(object)values[0] / (long)(object)values[1]);
                        case '%':
                            return (T)(object)((long)(object)values[0] % (long)(object)values[1]);
                        case '^':
                            return (T)(object)(long)Math.Pow((long)(object)values[0], (long)(object)values[1]);
                    }
                }
            }

            return default(T);
        }

        private static bool TryParse<T>(string expression, out T result)
        {
            expression = expression.Replace(',', '.');
            expression = expression.TrimEnd('f');
            bool result2 = false;
            result = default(T);
            if ((object)typeof(T) == typeof(float))
            {
                float result3 = 0f;
                result2 = float.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out result3);
                result = (T)(object)result3;
            }
            else if ((object)typeof(T) == typeof(int))
            {
                int result4 = 0;
                result2 = int.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out result4);
                result = (T)(object)result4;
            }
            else if ((object)typeof(T) == typeof(double))
            {
                double result5 = 0.0;
                result2 = double.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out result5);
                result = (T)(object)result5;
            }
            else if ((object)typeof(T) == typeof(long))
            {
                long result6 = 0L;
                result2 = long.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out result6);
                result = (T)(object)result6;
            }

            return result2;
        }
    }
}


