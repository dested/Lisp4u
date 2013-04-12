using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace lisp4u
{
    class ParseLisp
    {
        private readonly string _str;

        public ParseLisp(string str)
        {
            _str = str;
        }
        Dictionary<string, Func<object[], object>> SpecialMethods;
        private Dictionary<string, int> specItems;




        public void run(bool trace)
        {
            string lispString = _str.Replace("\r\n", " ");



            var f = tokenize(lispString);



            List<LispExpression> g = lispanize(f);
            SpecialMethods = new Dictionary<string, Func<object[], object>>();
            SpecialMethods.Add("+", (a => { long fr = 0; foreach (var o in a) { fr += (long)o; } return fr; }));
            SpecialMethods.Add("-", (a => { long fr = (long)a[0]; for (int index = 1; index < a.Length; index++) { var o = a[index]; fr -= (long)o; } return fr; }));
            SpecialMethods.Add("<", (a => (long)a[0] < (long)a[1]));
            SpecialMethods.Add(">", (a => (long)a[0] > (long)a[1]));
            SpecialMethods.Add("*", (a => { long fr = 1; foreach (var o in a) { fr *= (long)o; } return fr; }));
            SpecialMethods.Add("if", (a => null));
            SpecialMethods.Add("cond", (a => null));
            SpecialMethods.Add("format", (a =>
                                              {
                                                  string b = a[0].ToString();
                                                  for (int index = 1; index < a.Length; index++)
                                                  {
                                                      b = b.Replace("{" + (index - 1) + "}", a[index].ToString());
                                                  }
                                                  return b;
                                              }));

            SpecialMethods.Add("write-line", (a =>
            {
                Console.WriteLine(a[0].ToString());
                return null;
            }));
            SpecialMethods.Add("write", (a =>
            {
                Console.Write(a[0]);
                return null;
            }));

            var SpecMethods = new Func<object[], object>[SpecialMethods.Count];
            Dictionary<string, int> cd = new Dictionary<string, int>();
            int gf = 0;
            foreach (var specialMethod in SpecialMethods)
            {
                cd.Add(specialMethod.Key, gf++);
                SpecMethods[gf - 1] = specialMethod.Value;
            }

            specItems = cd;

            getMethods(g);



            List<ILispRunnable> vaf = new List<ILispRunnable>();


            ParsedMethods = new Dictionary<string, MT<ILispRunnable[], List<LispMethodRunnable>>>();
            foreach (var lispMethod in Methods)
            {
                ParsedMethods.Add(lispMethod.Key, null);
            }


            foreach (var lispExpression in g)
            {
                vaf.Add(assign(lispExpression));
            }




            foreach (var lispRunnablese in ParsedMethods)
            {
                Dictionary<string, int> vars = new Dictionary<string, int>();
                int curIndex = 0;
                foreach (LispVariableRunnable v in lispRunnablese.Value.Item1.SelectMany(a => getV(a)).ToArray())
                {
                    int ff;
                    if (vars.TryGetValue(v.VariableName, out ff))
                        v.VariableIndex = ff;
                    else
                    {
                        vars.Add(v.VariableName, curIndex++);
                        v.VariableIndex = curIndex - 1;
                    }
                }
                foreach (var lispMethodRunnable in lispRunnablese.Value.Item2)
                {

                    lispMethodRunnable.NumOfVariables = curIndex;
                }
            }

            foreach (var lispRunnable in vaf)
            {
                fixR(lispRunnable);
            }


            RunLisp rl = new RunLisp(SpecMethods, vaf.ToArray(), trace);


            Console.Write(rl.Run());


            Console.WriteLine("Done");


            if (trace)
            {
                File.WriteAllText("c:\\aaaa.txt", rl.tracing.ToString());

            }

            Console.Read();
        }

        private void fixR(ILispRunnable lr)
        {
            if (lr is LispMethodRunnable)
            {
                if (lr.Type != LispType.Empty && lr.Lines == null)
                {
                    (lr).Lines = ParsedMethods[((LispMethodRunnable)lr).MethodName].Item1;
                }
                else
                {
                    foreach (var lispRunnable in (lr).Parameters)
                        fixR(lispRunnable);
                    if (lr.Lines != null)
                        foreach (var line in (lr).Lines)
                            fixR(line);
                }
            }
            else if (lr is LispSpecialMethodRunnable)
            {
                foreach (var lispRunnable in (lr).Parameters)
                {
                    fixR(lispRunnable);
                }
            }
            else if (lr is LispVariableRunnable)
            {

            }
        }




        private List<LispVariableRunnable> getV(ILispRunnable lr)
        {
            if (lr is LispMethodRunnable)
            {
                List<LispVariableRunnable> vf = new List<LispVariableRunnable>();

                if (lr.Lines != null)
                {

                    foreach (var lispRunnable in lr.Parameters)
                    {
                        vf.AddRange(getV(lispRunnable));
                    }
                    foreach (var line in lr.Lines)
                    {
                        vf.AddRange(getV(line));
                    }
                }
                else
                {
                    foreach (var lispRunnable in lr.Parameters)
                    {
                        vf.AddRange(getV(lispRunnable));
                    }
                }
                return vf;
            }
            else if (lr is LispSpecialMethodRunnable)
            {
                List<LispVariableRunnable> vf = new List<LispVariableRunnable>();


                foreach (var lispRunnable in lr.Parameters)
                {
                    vf.AddRange(getV(lispRunnable));
                }
                return vf;
            }
            else if (lr is LispVariableRunnable)
            {
                return new List<LispVariableRunnable>() { (LispVariableRunnable)lr };
            }
            return new List<LispVariableRunnable>();
        }





        private void getMethods(List<LispExpression> g)
        {


            Methods = new Dictionary<string, LispMethod>();

            for (int i = g.Count - 1; i >= 0; i--)
            {
                var lispExpression = g[i];
                if (lispExpression.MethodName.ToLower() == "defun")
                {
                    LispExpression[] par = new LispExpression[lispExpression.Parameters.Count - 2];
                    string methodName = lispExpression.Parameters[0].Value.ToString();
                    var method = new LispMethod();

                    for (int index = 1; index < lispExpression.Parameters.Count; index++)
                    {
                        var expression = lispExpression.Parameters[index];

                        if (index == 1)
                        {
                            List<string> n = new List<string>();
                            n.Add(expression.MethodName);

                            foreach (var parameter in expression.Parameters)
                            {
                                n.Add(parameter.Value.ToString());
                            }
                            method.ParameterNames = n.ToArray();
                        }
                        else
                            par[index - 2] = expression;
                    }
                    method.Lines = par;
                    Methods.Add(methodName, method);

                    g.RemoveAt(i);
                }
            }
        }

        private Dictionary<string, LispMethod> Methods;


        private Dictionary<string, MT<ILispRunnable[], List<LispMethodRunnable>>> ParsedMethods;

        private ILispRunnable assign(LispExpression lispExpression)
        {
            if (lispExpression.Value != null)
            {
                long f;
                if (long.TryParse(lispExpression.Value.ToString(), out f))
                {
                    return new LispIntRunnable() { IntValue = f };
                }
                else if (lispExpression.Value.ToString().StartsWith("\"") && lispExpression.Value.ToString().EndsWith("\""))
                    return new LispStringRunnable() { StringValue = lispExpression.Value.ToString().Trim('\"') };


                return new LispVariableRunnable() { VariableName = lispExpression.Value.ToString() };
            }

            if (lispExpression.Parameters.Count == 0)
            {

                LispMethod fr;
                long f;
                if (Methods.TryGetValue(lispExpression.MethodName, out fr))
                    return new LispMethodRunnable() { MethodName = lispExpression.MethodName, Parameters = new ILispRunnable[0] };
                if (long.TryParse(lispExpression.MethodName, out f))
                    return new LispIntRunnable() { IntValue = f };
                return new LispVariableRunnable() { VariableName = lispExpression.MethodName };
            }


            if (string.IsNullOrEmpty(lispExpression.MethodName))
            {
                LispMethodRunnable method = new LispMethodRunnable();
                method.Type = LispType.Empty;
                method.Parameters = new ILispRunnable[lispExpression.Parameters.Count];

                for (int index = 0; index < lispExpression.Parameters.Count; index++)
                {
                    method.Parameters[index] = assign(lispExpression.Parameters[index]);
                }
                return method;
            }


            LispMethod v;
            if (Methods.TryGetValue(lispExpression.MethodName, out v))
            {
                LispMethodRunnable method = new LispMethodRunnable();
                method.MethodName = lispExpression.MethodName;
                ILispRunnable[] par = new ILispRunnable[lispExpression.Parameters.Count];

                for (int index = 0; index < lispExpression.Parameters.Count; index++)
                {
                    var expression = lispExpression.Parameters[index];
                    par[index] = assign(expression);
                }

                method.Parameters = par;
                List<ILispRunnable> lines = new List<ILispRunnable>();

                if (ParsedMethods[lispExpression.MethodName] == null)
                {
                    ParsedMethods[lispExpression.MethodName] = new MT<ILispRunnable[], List<LispMethodRunnable>>(null, new List<LispMethodRunnable>() { method });
                    foreach (var expression in v.Lines)
                    {
                        lines.Add(assign(expression));
                    }
                    method.Lines = lines.ToArray();
                    ParsedMethods[lispExpression.MethodName].Item1 = lines.ToArray();

                }
                else
                {
                    ParsedMethods[lispExpression.MethodName].Item2.Add(method);
                }
                return method;

            }
            else
            {






                Func<object[], object> r;
                if (SpecialMethods.TryGetValue(lispExpression.MethodName, out r))
                {
                    LispSpecialMethodRunnable method = new LispSpecialMethodRunnable();

                    ILispRunnable[] par = new ILispRunnable[lispExpression.Parameters.Count];
                    for (int index = 0; index < lispExpression.Parameters.Count; index++)
                    {
                        var expression = lispExpression.Parameters[index];
                        par[index] = assign(expression);
                    }
                    method.MethodIndex = specItems[lispExpression.MethodName];
                    if (lispExpression.MethodName.ToLower() == "if")
                        method.Type = LispType.If;
                    if (lispExpression.MethodName.ToLower() == "cond")
                        method.Type = LispType.Cond;
                    method.Parameters = par;
                    return method;
                }

                else throw new AbandonedMutexException("war");
            }
        }
        public class MT<T, T2>
        {
            public T Item1; public T2 Item2;
            public MT(T t, T2 t2)
            {
                Item1 = t;
                Item2 = t2;
            }
        }

        private object evaluate(LispExpression lispExpression, Dictionary<string, object> variables)
        {
            if (lispExpression.Value != null)
            {
                object fr;
                if (variables.TryGetValue(lispExpression.Value.ToString(), out fr))
                {

                    return fr;
                }
                int f;
                if (int.TryParse(lispExpression.Value.ToString(), out f))
                {
                    return f;
                }

                return lispExpression.Value;
            }

            if (lispExpression.Parameters.Count == 0)
            {
                object fr;
                if (variables.TryGetValue(lispExpression.MethodName, out fr))
                {
                    return fr;
                }
                int f;
                if (int.TryParse(lispExpression.MethodName, out f))
                {
                    return f;
                }

            }


            LispMethod v;
            if (Methods.TryGetValue(lispExpression.MethodName, out v))
            {

                object[] par = new object[lispExpression.Parameters.Count];

                for (int index = 0; index < lispExpression.Parameters.Count; index++)
                {
                    var expression = lispExpression.Parameters[index];
                    par[index] = evaluate(expression, variables);
                }

                Dictionary<string, object> newParams = new Dictionary<string, object>();
                int i = 0;
                foreach (var parameterName in v.ParameterNames)
                {
                    newParams.Add(parameterName, par[i++]);
                }
                object last = null;
                foreach (var expression in v.Lines)
                {
                    last = evaluate(expression, newParams);
                }

                return last;

            }
            else
            {



                if (lispExpression.MethodName.ToLower() == "if")
                {

                    bool ok = (bool)evaluate(lispExpression.Parameters[0], variables);

                    return evaluate(lispExpression.Parameters[ok ? 1 : 2], variables);
                }




                Func<object[], object> r;
                if (SpecialMethods.TryGetValue(lispExpression.MethodName, out r))
                {


                    object[] par = new object[lispExpression.Parameters.Count];
                    for (int index = 0; index < lispExpression.Parameters.Count; index++)
                    {
                        var expression = lispExpression.Parameters[index];

                        par[index] = evaluate(expression, variables);
                    }
                    return r(par);
                }

                else throw new AbandonedMutexException("war");
            }
        }



        private List<LispExpression> lispanize(List<LispToken> lispTokens)
        {

            List<LispExpression> expressions = new List<LispExpression>();


            LispExpression[] currentExpression = new LispExpression[100];
            int expIndex = -1;
            foreach (var lispToken in lispTokens)
            {
                switch (lispToken.TokenType)
                {
                    case LispTokenType.OpenParen:
                        expIndex++;
                        currentExpression[expIndex] = new LispExpression();
                        currentExpression[expIndex].Parameters = new List<LispExpression>();

                        break;
                    case LispTokenType.CloseParen:
                        if (expIndex == 0)
                            expressions.Add(currentExpression[0]);
                        else
                        {
                            currentExpression[expIndex - 1].Parameters.Add(currentExpression[expIndex]);
                        }

                        expIndex--;
                        break;
                    case LispTokenType.MethodName:
                        if (expIndex == -1)
                            throw new AbandonedMutexException("wat");
                        currentExpression[expIndex].MethodName = lispToken.TokenInformation.ToString();
                        break;
                    case LispTokenType.Value:
                        if (expIndex == -1)
                            throw new AbandonedMutexException("wat");

                        var lp = new LispExpression(lispToken.TokenInformation);
                        currentExpression[expIndex].Parameters.Add(lp);

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return expressions;
        }

        private List<LispToken> tokenize(string lispString)
        {
            List<LispToken> tokens = new List<LispToken>();

            bool wasSpace = false;
            bool goMethod = false;
            bool inString = false;
            string addToWord = "";

            for (int i = 0; i < lispString.Length; i++)
            {
                var c = lispString[i];
                if (inString && c != '\"')
                {
                    addToWord += c;
                    continue;
                }

                switch (c)
                {
                    case '\"':
                        inString = !inString;
                        addToWord += c;
                        if (!inString)
                        {
                            goMethod = false;

                        }
                        break;
                    case ' ':
                        if (wasSpace)
                            continue;
                        if (addToWord.Length > 0)

                            if (goMethod)
                            {
                                tokens.Add(new LispToken(LispTokenType.MethodName, addToWord));
                                goMethod = false;
                            }
                            else
                                tokens.Add(new LispToken(LispTokenType.Value, addToWord));

                        addToWord = "";

                        wasSpace = true;
                        continue; 
                    case '(':
                        tokens.Add(new LispToken(LispTokenType.OpenParen));
                        goMethod = true;

                        break;
                    case ')':
                        if (!wasSpace && addToWord.Length > 0)
                        {

                            if (goMethod)
                            {
                                tokens.Add(new LispToken(LispTokenType.MethodName, addToWord));
                            }
                            else

                                tokens.Add(new LispToken(LispTokenType.Value, addToWord));

                        }
                        addToWord = "";
                        goMethod = false;

                        tokens.Add(new LispToken(LispTokenType.CloseParen));
                        break;
                    default:
                        addToWord += c;
                        break;
                }
                wasSpace = false;
            }
            return tokens;
        }

    }

    public class LispStringRunnable : ILispRunnable
    { 

        public ILispRunnable[] Lines
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public LispType Type
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public ILispRunnable[] Parameters
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string MethodName
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public object Value
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public long IntValue
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string StringValue
        { get; set; }

        public bool BoolValue
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int VariableIndex
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int NumOfVariables
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int MethodIndex
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    public class LispIntRunnable : ILispRunnable
    { 

        public ILispRunnable[] Lines
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public LispType Type
        {
            get { return LispType.Int; }
            set { throw new NotImplementedException(); }
        }

        public ILispRunnable[] Parameters
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string MethodName
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public object Value
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public long IntValue
        { get; set; }

        public string StringValue
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool BoolValue
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int VariableIndex
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int NumOfVariables
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int MethodIndex
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    public class LispSpecialMethodRunnable : ILispRunnable
    { 
        public ILispRunnable[] Lines
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public LispType Type { get; set; }

        public ILispRunnable[] Parameters
        { get; set; }

        public string MethodName
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public object Value
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public long IntValue
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string StringValue
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool BoolValue
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int VariableIndex
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int NumOfVariables
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int MethodIndex
        { get; set; }
    }

    public class LispVariableRunnable : ILispRunnable
    {
        public string VariableName; 

        public ILispRunnable[] Lines
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public LispType Type
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public ILispRunnable[] Parameters
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string MethodName
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public object Value
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public long IntValue
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string StringValue
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool BoolValue
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int VariableIndex { get; set; }

        public int NumOfVariables
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int MethodIndex
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    public class LispMethodRunnable : ILispRunnable
    {
        public int VariableIndex
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int NumOfVariables { get; set; }

        public int MethodIndex
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public ILispRunnable[] Parameters
        { get; set; }

        public string MethodName { get; set; }

        public object Value
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public long IntValue
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string StringValue
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool BoolValue
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public LispType Type { get; set; }
        public ILispRunnable[] Lines { get; set; }
    }

    public enum LispType
    {
        Empty,
        If,
        Cond,
        Int,
        String,
        Bool,
        Variable,
        Method,
        Special
    }
    public interface ILispRunnable
    {
        ILispRunnable[] Lines { get; set; }
        LispType Type { get; set; }
        ILispRunnable[] Parameters { get; set; }
        string MethodName { get; set; }
        object Value { get; set; }
        long IntValue { get; set; }
        string StringValue { get; set; }
        bool BoolValue { get; set; }
        int VariableIndex { get; set; }
        int NumOfVariables { get; set; }
        int MethodIndex { get; set; }
    }

    public class LispExpression
    {
        public object Value;
        public List<LispExpression> Parameters;
        public string MethodName;

        public LispExpression(object tokenInformation)
        {
            Value = tokenInformation;
        }

        public LispExpression()
        { 
        }
    }

    public enum LispTokenType
    {
        CloseParen,
        Value,
        MethodName,
        OpenParen
    }
    public class LispToken
    {
        public LispTokenType TokenType;
        public object TokenInformation;

        public LispToken(LispTokenType value, string addToWord)
        {
            TokenType = value;
            TokenInformation = addToWord;
        }

        public LispToken(LispTokenType closeParen)
        {
            TokenType = closeParen;
        }
    }

    public class LispMethod
    {
        public string[] ParameterNames;
        public LispExpression[] Lines;
    }
}