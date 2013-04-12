using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace lisp4u
{
    public class RunLisp
    {
        private Func<object[], object>[] SpecMethods;
        private readonly ILispRunnable[] _runs;
        private bool trace;
        private int traceIndent;
        public StringBuilder tracing = new StringBuilder();
        public RunLisp(Func<object[], object>[] specMethods, ILispRunnable[] runs, bool _trace)
        {
            SpecMethods = specMethods;
            _runs = runs;
            trace = _trace;
            traceIndent = 0;
        }

        public object Run()
        {
            object r = null;
            var v = new object[0];
            for (int i = 0; i < _runs.Length; i++)
            {
                r = evalRunnable(_runs[i], v);
            }
            return r;
        }


        private int count = 0;
        private object evalRunnable(ILispRunnable v, object[] variables)
        {
            object last;
            count++;
            object[] obj;
            switch (v.Type)
            {
                case LispType.Int:
                    return v.IntValue;
                case LispType.String:
                    return v.StringValue;
                    break;
                case LispType.Bool:
                    return v.BoolValue;

                case LispType.Variable:
                    return variables[v.VariableIndex];
                case LispType.Method:

                    last = null;
                    obj = new object[v.NumOfVariables];
                    for (int index = 0; index < v.Parameters.Length; index++)
                    {
                        obj[index] = evalRunnable(v.Parameters[index], variables);
                    }
                     


                    for (int index = 0; index < v.Lines.Length; index++)
                    {
                        last = evalRunnable(v.Lines[index], obj);
                    }


                    return last;

                case LispType.Special:

                    obj = new object[v.Parameters.Length];

                    for (int index = 0; index < v.Parameters.Length; index++)
                    {
                        obj[index] = evalRunnable(v.Parameters[index], variables);
                    }

                    return SpecMethods[v.MethodIndex](obj);
                case LispType.If:

                    return evalRunnable(v.Parameters[((bool)evalRunnable(v.Parameters[0], variables)) ? 1 : 2], variables);
                case LispType.Cond:

                    for (int i = 0; i < v.Parameters.Length; i++)
                    {
                        var c = v.Parameters[i];
                        if ((bool)evalRunnable(c.Parameters[0], variables))
                        {
                            for (int a = 1; a < c.Parameters.Length; a++)
                            {
                                evalRunnable(c.Parameters[a], variables);
                            }
                        }

                    }
                    return null;
                case LispType.Empty:
                    last = null;
                    for (int i = 0; i < v.Parameters.Length; i++)
                    {
                        last = evalRunnable(v.Parameters[i], variables);
                    }

                    return last;

                default:
                    throw new ArgumentOutOfRangeException();
            }



        }
        public static string Multiply(string s, int b)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < b; i++)
            {
                sb.Append(s);
            }
            return sb.ToString();
        }


    }
}
