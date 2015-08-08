using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KPU.Processor
{
    public class Instruction
    {
        public class ErrorMessage : System.Exception
        {
            public string mErrText;
            public ErrorMessage(string errText)
            {
                mErrText = errText;
            }
            public override string ToString()
            {
                return mErrText;
            }
        }
        public class ParseError : ErrorMessage
        {
            public ParseError(string errText)
               : base(errText)
            {
}
        }
        public class EvalError : ErrorMessage
        {
            public EvalError(string errText)
               : base(errText)
            {
            }
        }
        public class ExecError : ErrorMessage
        {
            public ExecError(string errText)
               : base(errText)
            {
            }
        }
        public string mText;

        public bool skip = false;
        public bool lastValue = false;
        private bool mGlitched = false;

        public void glitch()
        {
            mGlitched = true;
        }

        public enum Tokens { TOK_COMMENT, TOK_KEYWORD, TOK_LOG_OP, TOK_COMP_OP, TOK_ARITH_OP, TOK_UN_OP, TOK_AT, TOK_COMMA, TOK_SEMI, TOK_LITERAL, TOK_IDENT, TOK_WHITESPACE };
        private List<KeyValuePair<string, Tokens>> Tokenise(string text)
        {
            //Logging.Log("Attempting to tokenise " + text);
            List<KeyValuePair<string, Tokens>> result = new List<KeyValuePair<string, Tokens>>();
            Dictionary<string, Tokens> TokenDict = new Dictionary<string, Tokens>() {
                {"#.*", Tokens.TOK_COMMENT},
                {"ON", Tokens.TOK_KEYWORD},
                {"DO", Tokens.TOK_KEYWORD},
                {"HIBERNATE", Tokens.TOK_KEYWORD},
                {"IF", Tokens.TOK_KEYWORD},
                {"THEN", Tokens.TOK_KEYWORD},
                {"AND", Tokens.TOK_LOG_OP},
                {"OR", Tokens.TOK_LOG_OP},
                {"[<>]", Tokens.TOK_COMP_OP},
                {"[+\\-*/]", Tokens.TOK_ARITH_OP},
                {"!", Tokens.TOK_UN_OP},
                {"@", Tokens.TOK_AT},
                {",", Tokens.TOK_COMMA},
                {";", Tokens.TOK_SEMI},
                {"-?[0-9]+(\\.[0-9]+)?~?", Tokens.TOK_LITERAL},
                {"[a-z][a-zA-Z0-9_.]*", Tokens.TOK_IDENT},
                {"\\s", Tokens.TOK_WHITESPACE},
            };
            int index = 0;
            while (index < text.Length)
            {
                Dictionary<Tokens, string> Matches = new Dictionary<Tokens, string>();
                foreach (KeyValuePair<string, Tokens> kvp in TokenDict)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(text.Substring(index), "^" + kvp.Key);
                    if (match.Success)
                    {
                        Matches.Add(kvp.Value, match.Value);
                    }
                }
                if (Matches.Count == 0)
                {
                    throw new ParseError(string.Format("No matches for {0}", text.Substring(index)));
                }
                int max_len = Matches.Max(kvp => kvp.Value.Length);
                IEnumerable<KeyValuePair<Tokens, string>> MaxMatches = Matches.Where(kvp => kvp.Value.Length == max_len);
                if (MaxMatches.Count() != 1)
                {
                    foreach (KeyValuePair<Tokens, string> kvp in MaxMatches)
                        Logging.Message(kvp.Key.ToString() + ": " + kvp.Value);
                    throw new ParseError(string.Format("{0:D} matches for {1}", MaxMatches.Count(), text.Substring(index)));
                }
                string matchedText = MaxMatches.First().Value;
                Tokens matchedToken = MaxMatches.First().Key;
                if (matchedToken != Tokens.TOK_WHITESPACE)
                {
                    result.Add(new KeyValuePair<string, Tokens>(matchedText, matchedToken));
                    //Logging.Log(string.Format("Accepted token {0} as {1:G}", matchedText, matchedToken));
                }
                if (matchedText.Length < 1)
                {
                    throw new ParseError(string.Format("Match {0} is too short", matchedText));
                }
                index += matchedText.Length;
            }
            //Logging.Log("Tokenisation complete!");
            return result;
        }

        public class ASTNode
        {
            public KeyValuePair<string, Tokens> mToken;
            public List<ASTNode> mChildren;
            public ASTNode(KeyValuePair<string, Tokens> token)
            {
                mToken = token;
                mChildren = new List<ASTNode>();
            }
            public void Add(ASTNode child)
            {
                mChildren.Add(child);
            }
            public override string ToString()
            {
                string s = "";
                foreach (ASTNode n in mChildren)
                    s += " " + n.ToString();
                return string.Format("({0}:{1}{2})", mToken.Value.ToString(), mToken.Key, s);
            }
            public List<ASTNode> flat
            {
                get
                {
                    List<ASTNode> ret = new List<ASTNode>();
                    ret.Add(this);
                    foreach (ASTNode n in mChildren)
                        ret.AddRange(n.flat);
                    return ret;
                }
            }
        }

        public ASTNode mAST;
        private ASTNode AssembleRecursive(IEnumerator<KeyValuePair<string, Tokens>> tokens)
        {
            if (!tokens.MoveNext())
                throw new ParseError("Out of tokens");
            KeyValuePair<string, Tokens> token = tokens.Current;
            ASTNode n = new ASTNode(token);
            ASTNode left, right, child, cond, actn;
            switch(token.Value)
            {
            case Tokens.TOK_COMMENT:
                break;
            case Tokens.TOK_KEYWORD:
                if (token.Key.Equals("ON"))
                {
                    cond = AssembleRecursive(tokens);
                    if (cond.mToken.Value == Tokens.TOK_KEYWORD)
                        throw new ParseError("ON cond was bad: " + cond.ToString());
                    n.Add(cond);
                    actn = AssembleRecursive(tokens);
                    if (actn.mToken.Value != Tokens.TOK_KEYWORD || !(actn.mToken.Key.Equals("DO") || actn.mToken.Key.Equals("HIBERNATE")))
                        throw new ParseError("ON DO was bad: " + actn.ToString());
                    n.Add(actn);
                    break;
                }
                if (token.Key.Equals("IF"))
                {
                    cond = AssembleRecursive(tokens);
                    if (cond.mToken.Value == Tokens.TOK_KEYWORD)
                        throw new ParseError("IF cond was bad: " + cond.ToString());
                    n.Add(cond);
                    actn = AssembleRecursive(tokens);
                    if (actn.mToken.Value != Tokens.TOK_KEYWORD || !actn.mToken.Key.Equals("THEN"))
                        throw new ParseError("IF THEN was bad: " + actn.ToString());
                    n.Add(actn);
                    break;
                }
                if (token.Key.Equals("DO"))
                {
                    actn = AssembleRecursive(tokens);
                    if (actn.mToken.Value != Tokens.TOK_SEMI &&
                        actn.mToken.Value != Tokens.TOK_AT)
                        throw new ParseError("DO actn was bad: " + actn.ToString());
                    n.Add(actn);
                    break;
                }
                if (token.Key.Equals("THEN"))
                {
                    actn = AssembleRecursive(tokens);
                    if (actn.mToken.Value != Tokens.TOK_SEMI &&
                        actn.mToken.Value != Tokens.TOK_AT)
                        throw new ParseError("THEN actn was bad: " + actn.ToString());
                    n.Add(actn);
                    break;
                }
                if (token.Key.Equals("HIBERNATE"))
                {
                    cond = AssembleRecursive(tokens);
                    if (cond.mToken.Value == Tokens.TOK_KEYWORD ||
                        cond.mToken.Value == Tokens.TOK_AT ||
                        cond.mToken.Value == Tokens.TOK_COMMA ||
                        cond.mToken.Value == Tokens.TOK_SEMI)
                    throw new ParseError("HIBERNATE wake-up condition was bad: " + cond.ToString());
                    n.Add(cond);
                    break;
                }
                throw new ParseError(token.Key); // can't happen
            case Tokens.TOK_LOG_OP:
            case Tokens.TOK_ARITH_OP:
            case Tokens.TOK_COMP_OP:
                left = AssembleRecursive(tokens);
                if (left.mToken.Value == Tokens.TOK_KEYWORD ||
                    left.mToken.Value == Tokens.TOK_AT ||
                    left.mToken.Value == Tokens.TOK_COMMA ||
                    left.mToken.Value == Tokens.TOK_SEMI)
                    throw new ParseError(token.Key + " left expr was bad: " + left.ToString());
                n.Add(left);
                right = AssembleRecursive(tokens);
                if (right.mToken.Value == Tokens.TOK_KEYWORD ||
                    right.mToken.Value == Tokens.TOK_AT ||
                    right.mToken.Value == Tokens.TOK_COMMA ||
                    right.mToken.Value == Tokens.TOK_SEMI)
                    throw new ParseError(token.Key + " right expr was bad: " + right.ToString());
                n.Add(right);
                break;
            case Tokens.TOK_UN_OP:
                child = AssembleRecursive(tokens);
                if (child.mToken.Value == Tokens.TOK_KEYWORD ||
                    child.mToken.Value == Tokens.TOK_AT ||
                    child.mToken.Value == Tokens.TOK_COMMA ||
                    child.mToken.Value == Tokens.TOK_SEMI)
                    throw new ParseError(token.Key + " child expr was bad: " + child.ToString());
                n.Add(child);
                break;
            case Tokens.TOK_IDENT:
            case Tokens.TOK_LITERAL:
                break;
            case Tokens.TOK_AT:
                left = AssembleRecursive(tokens);
                if (left.mToken.Value != Tokens.TOK_IDENT)
                    throw new ParseError(token.Key + " left expr was bad: " + left.ToString());
                n.Add(left);
                right = AssembleRecursive(tokens);
                if (right.mToken.Value == Tokens.TOK_KEYWORD ||
                    right.mToken.Value == Tokens.TOK_AT ||
                    right.mToken.Value == Tokens.TOK_SEMI)
                    throw new ParseError(token.Key + " right expr was bad: " + right.ToString());
                n.Add(right);
                break;
            case Tokens.TOK_COMMA:
                left = AssembleRecursive(tokens);
                if (left.mToken.Value == Tokens.TOK_KEYWORD ||
                    left.mToken.Value == Tokens.TOK_AT ||
                    left.mToken.Value == Tokens.TOK_SEMI)
                    throw new ParseError(token.Key + " left expr was bad: " + left.ToString());
                n.Add(left);
                right = AssembleRecursive(tokens);
                if (right.mToken.Value == Tokens.TOK_KEYWORD ||
                    right.mToken.Value == Tokens.TOK_AT ||
                    right.mToken.Value == Tokens.TOK_SEMI ||
                    right.mToken.Value == Tokens.TOK_COMMA)
                    throw new ParseError(token.Key + " right expr was bad: " + right.ToString());
                n.Add(right);
                break;
            case Tokens.TOK_SEMI:
                left = AssembleRecursive(tokens);
                if (left.mToken.Value != Tokens.TOK_IDENT &&
                    left.mToken.Value != Tokens.TOK_AT &&
                    left.mToken.Value != Tokens.TOK_SEMI)
                    throw new ParseError("; left expr was bad: " + left.ToString());
                n.Add(left);
                right = AssembleRecursive(tokens);
                if (right.mToken.Value != Tokens.TOK_IDENT &&
                    right.mToken.Value != Tokens.TOK_AT)
                    throw new ParseError("; right expr was bad: " + right.ToString());
                n.Add(right);
                break;
            default:
                throw new ParseError(token.Value.ToString() + ": " + token.Key); // can't happen
            }
            return n;
        }

        private ASTNode Assemble(List<KeyValuePair<string, Tokens>> tokens)
        {
            IEnumerator<KeyValuePair<string, Tokens>> iTokens = tokens.GetEnumerator();
            ASTNode n = AssembleRecursive(iTokens);
            if (iTokens.MoveNext())
                throw new ParseError("Leftover token " + iTokens.Current.Value.ToString() + ": " + iTokens.Current.Key);
            if ((n.mToken.Value != Tokens.TOK_KEYWORD ||
                 (n.mToken.Key != "ON" && n.mToken.Key != "IF")) &&
                n.mToken.Value != Tokens.TOK_COMMENT)
                throw new ParseError("Bad root token " + iTokens.Current.Value.ToString() + ": " + iTokens.Current.Key);
            return n;
        }

        public override string ToString ()
        {
            StringBuilder sb = new StringBuilder();
            if (skip)
                sb.Append("# skip: "); 
            foreach (ASTNode n in mAST.flat)
            {
                sb.Append(n.mToken.Key);
                if (n.mToken.Value != Tokens.TOK_UN_OP &&
                    n.mToken.Value != Tokens.TOK_AT)
                {
                    sb.Append(" ");
                }
            }
            return sb.ToString().TrimEnd(' ');
        }

        public Instruction (string text)
        {
            mText = text;
            List<KeyValuePair<string, Tokens>> tokens = Tokenise(text);
            if (tokens[0].Value == Tokens.TOK_COMMENT)
                mImemWords = 0;
            else
                mImemWords = tokens.Count;
            //Logging.Log(string.Format("imemWords = {0:D}", mImemWords));
            requiresLevelTrigger = tokens.Any(kvp => kvp.Value == Tokens.TOK_KEYWORD && kvp.Key == "IF");
            requiresLogicOps = tokens.Any(kvp => kvp.Value == Tokens.TOK_LOG_OP);
            requiresArithOps = tokens.Any(kvp => kvp.Value == Tokens.TOK_ARITH_OP);
            mAST = Assemble(tokens);
            //Logging.Log(mAST.ToString());
            mText = ToString();
        }

        private int mImemWords = 0;
        public int imemWords { get { return mImemWords; } }
        public bool requiresLevelTrigger = false, requiresLogicOps = false, requiresArithOps = false;

        public enum Type { BOOLEAN, DOUBLE, ANGLE, NAME, TUPLE, VOID };

        public class Value
        {
            public Type typ;
            private bool mBool = false;
            private double mDouble = 0f;
            private string mName = null;
            private List<Value> mTuple = null;
            public bool b { get { return mBool; }}
            public double d { get { return mDouble; }}
            public string n { get { return mName; }}
            public List<Value> t { get { return mTuple; }}
            public Value(Value v)
            {
                typ = v.typ;
                switch(typ)
                {
                case Type.ANGLE:
                case Type.DOUBLE:
                    mDouble = v.d;
                    break;
                case Type.BOOLEAN:
                    mBool = v.b;
                    break;
                case Type.NAME:
                    mName = v.n;
                    break;
                case Type.TUPLE:
                    mTuple = new List<Value>(v.t);
                    break;
                case Type.VOID:
                default:
                    break;
                }
            }
            public Value(bool b) { typ = Type.BOOLEAN; mBool = b; }
            public Value(double d, bool angle=false)
            {
                if (angle)
                {
                    typ = Type.ANGLE;
                    mDouble = d % 360.0;
                }
                else
                {
                    typ = Type.DOUBLE;
                    mDouble = d;
                }
            }
            public Value(string n) { typ = Type.NAME; mName = n; }
            public Value(Value car, Value cdr) { typ = Type.TUPLE; mTuple = new List<Value>(2){car, cdr}; }
            public Value() { typ = Type.VOID; }
            public List<Value> l { get {
                if (typ == Type.TUPLE)
                    return new List<Value>(t[0].l.Concat(t[1].l));
                return new List<Value>() {this};
            }}
            public override string ToString()
            {
                switch(typ)
                {
                case Type.BOOLEAN:
                    return mBool ? "True" : "False";
                case Type.DOUBLE:
                case Type.ANGLE:
                    return mDouble.ToString("g4");
                case Type.NAME:
                    return "[" + mName + "]";
                case Type.TUPLE:
                    string s = "(";
                    foreach (Value v in mTuple)
                    {
                        s += v.ToString();
                        s += ",";
                    }
                    s.TrimEnd(',');
                    s += ")";
                    return s;
                case Type.VOID:
                    return "void";
                default:
                    return "{" + typ.ToString() + "}";
                }
            }
        }

        public void assertType(ASTNode n, string s, Type t, Value v)
        {
            assertType(n.mToken.Key, s, t, v);
        }

        public void assertType(string n, string s, Type t, Value v)
        {
            if (v.typ != t)
                throw new EvalError(n + ": expected " + t.ToString() + ", " + s + " is " + v.typ.ToString() + " " + v.ToString());
        }

        public Value exec(string name, Value arglist, Processor p)
        {
            List<Value> args = arglist.l;
            var match = System.Text.RegularExpressions.Regex.Match(name, "^(.+)\\.([^.]+)");
            string error;
            if (match.Success)
            {
                string mainName = match.Groups[1].Value;
                string method = match.Groups[2].Value;
                if (p.outputs.ContainsKey(mainName))
                {
                    IOutputData output = p.outputs[mainName];
                    if (method.Equals("set"))
                    {
                        output.setValue(arglist);
                        return new Value();
                    }
                    if (method.Equals("incr") && arglist.typ == Type.DOUBLE)
                    {
                        output.slewValue(arglist);
                        return new Value();
                    }
                    if (method.Equals("decr") && arglist.typ == Type.DOUBLE)
                    {
                        output.slewValue(new Value(-arglist.d));
                        return new Value();
                    }
                    error = "no such method";
                }
                else
                {
                    error = "no such output";
                }
            }
            else
            {
                error = "method expected";
            }
            string s = error + ": " + name + ":";
            foreach (Value arg in args)
                s += " " + arg.ToString();
            throw new ExecError(s);
        }

        public Value evalRecursive(ASTNode n, Processor p)
        {
            Value left, right;
            switch(n.mToken.Value)
            {
            case Tokens.TOK_ARITH_OP: // + - * /
                left = evalRecursive(n.mChildren[0], p);
                right = evalRecursive(n.mChildren[1], p);
                if ((left.typ == Type.ANGLE) && (right.typ == Type.ANGLE))
                {
                    if (n.mToken.Key.Equals("+"))
                        return new Value((left.d + right.d) % 360.0, true);
                    else if (n.mToken.Key.Equals("-"))
                        return new Value((left.d + 360.0 - right.d) % 360.0, true);
                    else if (n.mToken.Key.Equals("*"))
                        throw new EvalError("'*' not valid on angles");
                    else if (n.mToken.Key.Equals("/"))
                        throw new EvalError("'/' not valid on angles");
                    else // can't happen
                        throw new EvalError(n.mToken.Key);
                }
                else
                {
                    if (left.typ == Type.ANGLE)
                        left = new Value(left.d);
                    else if (right.typ == Type.ANGLE)
                        right = new Value(right.d);
                    assertType(n, "left", Type.DOUBLE, left);
                    assertType(n, "right", Type.DOUBLE, right);
                    if (n.mToken.Key.Equals("+"))
                        return new Value(left.d + right.d);
                    else if (n.mToken.Key.Equals("-"))
                        return new Value(left.d - right.d);
                    else if (n.mToken.Key.Equals("*"))
                        return new Value(left.d * right.d);
                    else if (n.mToken.Key.Equals("/"))
                        return new Value(left.d / right.d);
                    else // can't happen
                        throw new EvalError(n.mToken.Key);
                 }
            case Tokens.TOK_AT:
                left = evalRecursive(n.mChildren[0], p);
                right = evalRecursive(n.mChildren[1], p);
                assertType(n, "left", Type.NAME, left);
                return exec(left.n, right, p);
            case Tokens.TOK_COMMA:
                left = evalRecursive(n.mChildren[0], p);
                right = evalRecursive(n.mChildren[1], p);
                return new Value(left, right);
            case Tokens.TOK_COMP_OP: // < >
                left = evalRecursive(n.mChildren[0], p);
                right = evalRecursive(n.mChildren[1], p);
                if (left.typ == Type.ANGLE && right.typ == Type.ANGLE)
                {
                    double diff = left.d - right.d;
                    if (diff > 180.0)
                        diff -= 360.0;
                    else if (diff < -180.0)
                        diff += 360.0;
                    if (n.mToken.Key.Equals("<"))
                        return new Value(diff < 0);
                    else if (n.mToken.Key.Equals(">"))
                        return new Value(diff > 0);
                    else // can't happen
                        throw new EvalError(n.mToken.Key);
                }
                else
                {
                    if (left.typ == Type.ANGLE)
                        left = new Value(left.d);
                    else if (right.typ == Type.ANGLE)
                        right = new Value(right.d);
                    assertType(n, "left", Type.DOUBLE, left);
                    assertType(n, "right", Type.DOUBLE, right);
                    if (n.mToken.Key.Equals("<"))
                        return new Value(left.d < right.d);
                    else if (n.mToken.Key.Equals(">"))
                        return new Value(left.d > right.d);
                    else // can't happen
                        throw new EvalError(n.mToken.Key);
                }
            case Tokens.TOK_IDENT:
                if (n.mToken.Key.Equals("true"))
                    return new Value(true);
                if (n.mToken.Key.Equals("false"))
                    return new Value(false);
                if (n.mToken.Key.Equals("error"))
                {
                    bool b = p.error;
                    p.error = false;
                    return new Value(b);
                }
                if (p.inputValues.ContainsKey(n.mToken.Key))
                {
                    return new Value(p.inputValues[n.mToken.Key]);
                }
                return new Value(n.mToken.Key);
            case Tokens.TOK_KEYWORD: // ON DO IF THEN
                if (n.mToken.Key.Equals("ON"))
                {
                    Value cond = evalRecursive(n.mChildren[0], p);
                    assertType(n, "cond", Type.BOOLEAN, cond);
                    if (mGlitched)
                        cond = new Value(!cond.b);
                    if (cond.b && !lastValue)
                    {
                        Logging.Log("edge fired! " + mText);
                        if (TimeWarp.CurrentRateIndex > 0)
                            TimeWarp.SetRate(0, true); // force 1x speed
                        try
                        {
                            evalRecursive(n.mChildren[1], p);
                        }
                        catch(Exception ex)
                        {
                            Logging.Message(ex.ToString());
                        }
                    }
                    lastValue = cond.b;
                    return new Value();
                }
                if (n.mToken.Key.Equals("IF"))
                {
                    Value cond = evalRecursive(n.mChildren[0], p);
                    assertType(n, "cond", Type.BOOLEAN, cond);
                    if (mGlitched)
                        cond = new Value(!cond.b);
                    if (cond.b)
                    {
                        evalRecursive(n.mChildren[1], p);
                    }
                    return new Value();
                }
                if (n.mToken.Key.Equals("DO"))
                    return evalRecursive(n.mChildren[0], p);
                if (n.mToken.Key.Equals("THEN"))
                    return evalRecursive(n.mChildren[0], p);
                if (n.mToken.Key.Equals("HIBERNATE"))
                {
                    p.hibernate(this);
                    return new Value();
                }
                // can't happen
                throw new EvalError(n.mToken.Key);
            case Tokens.TOK_LITERAL:
                try
                {
                    if (n.mToken.Key.EndsWith("~"))
                        return new Value(Double.Parse(n.mToken.Key.TrimEnd('~')), true);
                    else
                        return new Value(Double.Parse(n.mToken.Key));
                }
                catch (Exception) // can't happen?
                {
                    throw new EvalError(n.mToken.Key);
                }
            case Tokens.TOK_LOG_OP: // AND OR
                left = evalRecursive(n.mChildren[0], p);
                assertType(n, "left", Type.BOOLEAN, left);
                if (n.mToken.Key.Equals("AND") && !left.b)
                    return new Value(false);
                else if (n.mToken.Key.Equals("OR") && left.b)
                    return new Value(true);
                right = evalRecursive(n.mChildren[1], p);
                assertType(n, "right", Type.BOOLEAN, right);
                if (n.mToken.Key.Equals("AND"))
                    return new Value(left.b && right.b);
                else if (n.mToken.Key.Equals("OR"))
                    return new Value(left.b || right.b);
                else // can't happen
                    throw new EvalError(n.mToken.Key);
            case Tokens.TOK_SEMI:
                evalRecursive(n.mChildren[0], p);
                evalRecursive(n.mChildren[1], p);
                return new Value();
            case Tokens.TOK_UN_OP: // !
                if (!n.mToken.Key.Equals("!")) // can't happen
                    throw new EvalError(n.mToken.Key);
                left = evalRecursive(n.mChildren[0], p);
                assertType(n, "child", Type.BOOLEAN, left);
                return new Value(!left.b);
            case Tokens.TOK_COMMENT:
                return new Value();
            default: // can't happen
                throw new EvalError(n.mToken.Value.ToString());
            }
        }

        public void eval(Processor p)
        {
            if (!skip)
            {
                try
                {
                    evalRecursive(mAST, p);
                }
                catch (EvalError exc)
                {
                    Logging.Message("EvalError: " + exc.ToString());
                    p.error = true;
                    skip = true;
                }
                catch (ExecError exc)
                {
                    Logging.Message("ExecError: " + exc.ToString());
                    p.error = true;
                    skip = true;
                }
            }
            mGlitched = false;
        }

        public void considerWakeup(Processor p)
        {
            try
            {
                if (mAST.mToken.Value != Tokens.TOK_KEYWORD || !mAST.mToken.Key.Equals("ON"))
                    throw new ExecError("expected ON, saw " + mAST.mToken.Key);
                if (mAST.mChildren.Count < 2)
                    throw new ExecError("ON missing HIBERNATE");
                ASTNode h = mAST.mChildren[1];
                if (h.mToken.Value != Tokens.TOK_KEYWORD || !h.mToken.Key.Equals("HIBERNATE"))
                    throw new ExecError("expected HIBERNATE, saw " + h.mToken.Key);
                if (h.mChildren.Count < 1)
                    throw new ExecError("HIBERNATE missing wakeup condition");
                Value v = evalRecursive(h.mChildren[0], p);
                assertType(h, "wakeup", Type.BOOLEAN, v);
                if (v.b)
                    p.wakeup();
            }
            catch (EvalError exc)
            {
                Logging.Message("EvalError: " + exc.ToString());
                p.error = true;
                p.wakeup(); // leave hibernation
                skip = true; // and don't let this line put us back in
            }
            catch (ExecError exc)
            {
                Logging.Message("ExecError: " + exc.ToString());
                p.error = true;
                p.wakeup(); // leave hibernation
                skip = true; // and don't let this line put us back in
            }
        }
    }

    public interface IInputData
    {
        string name { get; }
        string unit { get; }
        bool available { get; }
        bool useSI { get; }
        Instruction.Type typ { get; }
        Instruction.Value value { get; }
    }

    public class Batteries : IInputData
    {
        public string name { get { return "batteries"; } }
        public string unit { get { return "%"; } }
        public bool available { get { return TotalElectricChargeCapacity > 0.1f; }}
        public bool useSI { get { return false; }}
        public Instruction.Type typ {get { return Instruction.Type.DOUBLE; } }
        public Instruction.Value value { get { return new Instruction.Value(Math.Round(ElectricChargeFillLevel * 10000.0f) / 100.0f); } }
        private Processor mProc = null;

        private Vessel parentVessel { get { return mProc.parentVessel; } }

        public Batteries (Processor p)
        {
            mProc = p;
        }

        private IEnumerable<PartResource> ElectricChargeResources
        {
            get
            {
                if (this.parentVessel != null && this.parentVessel.rootPart != null) {
                    int ecid = PartResourceLibrary.Instance.GetDefinition ("ElectricCharge").id;
                    List<PartResource> resources = new List<PartResource> ();
                    this.parentVessel.rootPart.GetConnectedResources (ecid, ResourceFlowMode.ALL_VESSEL, resources);
                    return resources;
                }
                return new List<PartResource>();
            }
        }

        private double TotalElectricCharge
        {
            get { return this.ElectricChargeResources.Sum(x => x.amount); }
        }

        private double TotalElectricChargeCapacity
        {
            get { return this.ElectricChargeResources.Sum(x => x.maxAmount); }
        }

        private double ElectricChargeFillLevel
        {
            get { return this.TotalElectricChargeCapacity > 0.1f ? this.TotalElectricCharge / this.TotalElectricChargeCapacity : 0f; }
        }

    }

    public class VesselTMR : IInputData
    {
        public string name { get { return "vesselTmr"; } }
        public string unit { get { return "m/s²"; } }
        public bool available { get { return parentVessel != null && TotalMass > 0.1f; }}
        public bool useSI { get { return true; }}
        public Instruction.Type typ {get { return Instruction.Type.DOUBLE; } }
        public Instruction.Value value { get { return new Instruction.Value(TMR); } }
        private Processor mProc = null;

        private Vessel parentVessel { get { return mProc.parentVessel; } }

        public VesselTMR (Processor p)
        {
            mProc = p;
        }

        private double TotalThrust
        {
            get { return FlightCore.GetTotalThrust(parentVessel); }
        }

        private double TotalMass
        {
            get { return parentVessel.GetTotalMass(); }
        }

        private double TMR
        {
            get { return parentVessel != null && TotalMass > 0.1f ? TotalThrust / TotalMass : Double.PositiveInfinity; }
        }

    }

    public class SensorDriven
    {
        public virtual string name { get { return "abstract"; } }
        public virtual string unit { get { return ""; } }
        private Processor mProc = null;
        public Vessel parentVessel { get { return mProc.parentVessel; } }
        public SensorDriven (Processor p)
        {
            mProc = p;
        }
        public bool available
        {
            get
            {
                return parentVessel != null && parentVessel.Parts.Any(p => p.FindModulesImplementing<KPU.Modules.ModuleKpuSensor>().Any(m => m.sensorType.Equals(name) && m.isWorking));
            }
        }
        private KPU.Modules.ModuleKpuSensor chooseSensor { get {
            KPU.Modules.ModuleKpuSensor chosen = null;
            foreach (Part p in parentVessel.Parts)
                foreach(KPU.Modules.ModuleKpuSensor m in p.FindModulesImplementing<KPU.Modules.ModuleKpuSensor>())
                    if (m.sensorType.Equals(name) && m.isWorking)
                        if (chosen == null || m.sensorRes < chosen.sensorRes)
                            chosen = m;
            return chosen;
        }}
        public double err { get {
            KPU.Modules.ModuleKpuSensor s = chooseSensor;
            if (s != null) return (mProc.mRandom.NextDouble() - 0.5) * s.errorBar * 2.0;
            return 0;
        }}
        public double res { get {
            KPU.Modules.ModuleKpuSensor s = chooseSensor;
            if (s != null) return s.sensorRes;
            return Double.PositiveInfinity;
        }}
    }

    public class SensorDouble : SensorDriven
    {
        public virtual double raw { get { return Double.PositiveInfinity; } }
        public virtual Instruction.Type typ { get { return Instruction.Type.DOUBLE; } }
        public Instruction.Value value
        {
            get
            {
                bool angle = (typ == Instruction.Type.ANGLE);
                if (!available) return new Instruction.Value(Double.PositiveInfinity, angle);
                double e = err * res;
                return new Instruction.Value(Math.Round((raw + e) / res) * res, angle);
            }
        }
        public SensorDouble(Processor p) : base(p)
        {
        }
    }

    public class Gear : SensorDriven, IInputData
    {
        public override string name { get { return "gear"; } }
        public Instruction.Type typ { get { return Instruction.Type.BOOLEAN; } }
        public bool useSI { get { return false; }}
        public Instruction.Value value
        {
            get
            {
                Vessel.Situations situation = parentVessel.situation;
                return new Instruction.Value(situation == Vessel.Situations.LANDED || situation == Vessel.Situations.PRELAUNCH);
            }
        }

        public Gear (Processor p) : base(p)
        {
        }
    }

    public class SrfHeight : SensorDouble, IInputData
    {
        public override string name { get { return "srfHeight"; } }
        public override string unit { get { return "m"; } }
        public bool useSI { get { return true; }}
        public override double raw
        {
            get
            {
                return parentVessel.altitude - Math.Max(parentVessel.terrainAltitude, 0.0);
            }
        }

        public SrfHeight (Processor p) : base(p)
        {
        }
    }

    public class SrfSpeed : SensorDouble, IInputData
    {
        public override string name { get { return "srfSpeed"; } }
        public override string unit { get { return "m/s"; } }
        public bool useSI { get { return true; }}
        public override double raw
        {
            get
            {
                return parentVessel.GetSrfVelocity().magnitude;
            }
        }

        public SrfSpeed (Processor p) : base(p)
        {
        }
    }

    public class SrfVerticalSpeed : SensorDouble, IInputData
    {
        public override string name { get { return "srfVerticalSpeed"; } }
        public override string unit { get { return "m/s"; } }
        public bool useSI { get { return true; }}
        public override double raw
        {
            get
            {
                Vector3 v = parentVessel.GetSrfVelocity();
                Vector3 up = Vector3.Normalize(parentVessel.CoM - parentVessel.mainBody.position);
                return Vector3.Dot(v, up);
            }
        }

        public SrfVerticalSpeed (Processor p) : base(p)
        {
        }
    }

    public class LocalG : SensorDouble, IInputData
    {
        public override string name { get { return "localGravity"; } }
        public override string unit { get { return "m/s²"; } }
        public bool useSI { get { return true; }}
        public override double raw { get { return FlightGlobals.getGeeForceAtPosition(FlightGlobals.ship_position).magnitude; } }

        public LocalG (Processor p) : base(p)
        {
        }
    }

    public class Altitude : SensorDouble, IInputData
    {
        public override string name { get { return "altitude"; } }
        public override string unit { get { return "m"; } }
        public bool useSI { get { return true; }}
        public override double raw { get { return parentVessel.altitude; } }

        public Altitude (Processor p) : base(p)
        {
        }
    }

    public class Latitude : SensorDouble, IInputData
    {
        public override string name { get { return "latitude"; } }
        public override string unit { get { return "°"; } }
        public override Instruction.Type typ {get { return Instruction.Type.ANGLE; } }
        public bool useSI { get { return false; }}
        public override double raw { get { return parentVessel.latitude; } }

        public Latitude (Processor p) : base(p)
        {
        }
    }

    public class Longitude : SensorDouble, IInputData
    {
        public override string name { get { return "longitude"; } }
        public override string unit { get { return "°"; } }
        public override Instruction.Type typ {get { return Instruction.Type.ANGLE; } }
        public bool useSI { get { return false; }}
        public override double raw { get { return (parentVessel.longitude + 720.0) % 360.0; } }

        public Longitude (Processor p) : base(p)
        {
        }
    }

    public class OrbSpeed : SensorDouble, IInputData
    {
        public override string name { get { return "orbSpeed"; } }
        public override string unit { get { return "m/s"; } }
        public bool useSI { get { return true; }}
        public override double raw { get { return parentVessel.GetObtVelocity().magnitude; } }

        public OrbSpeed (Processor p) : base(p)
        {
        }
    }

    public class Periapsis : SensorDouble, IInputData
    {
        public override string name { get { return "orbPeriapsis"; } }
        public override string unit { get { return "m"; } }
        public bool useSI { get { return true; }}
        public override double raw { get { return parentVessel.orbit.PeA; } }

        public Periapsis (Processor p) : base(p)
        {
        }
    }

    public class Apoapsis : SensorDouble, IInputData
    {
        public override string name { get { return "orbApoapsis"; } }
        public override string unit { get { return "m"; } }
        public bool useSI { get { return true; }}
        public override double raw { get { return parentVessel.orbit.ApA; } }

        public Apoapsis (Processor p) : base(p)
        {
        }
    }

    public class Inclination : SensorDouble, IInputData
    {
        public override string name { get { return "orbInclination"; } }
        public override string unit { get { return "°"; } }
        public override Instruction.Type typ {get { return Instruction.Type.ANGLE; } }
        public bool useSI { get { return false; }}
        public override double raw { get { return parentVessel.orbit.inclination; } }

        public Inclination (Processor p) : base(p)
        {
        }
    }

    public class ANLongitude : SensorDouble, IInputData
    {
        public override string name { get { return "orbANLongitude"; } }
        public override string unit { get { return "°"; } }
        public override Instruction.Type typ {get { return Instruction.Type.ANGLE; } }
        public bool useSI { get { return false; }}
        public override double raw { get { return (parentVessel.orbit.LAN - parentVessel.orbit.referenceBody.rotationAngle + 360) % 360; } }

        public ANLongitude (Processor p) : base(p)
        {
        }
    }

    public class PeriLongitude : SensorDouble, IInputData
    {
        // we give the longitude of periapsis, because that seems more useful than the argument
        // especially as the language has no easy way to work with angles
        public override string name { get { return "orbPeriapsisLongitude"; } }
        public override string unit { get { return "°"; } }
        public override Instruction.Type typ {get { return Instruction.Type.ANGLE; } }
        public bool useSI { get { return false; }}
        public override double raw { get {
            double l = parentVessel.orbit.argumentOfPeriapsis + parentVessel.orbit.LAN - parentVessel.orbit.referenceBody.rotationAngle;
            return (l + 360) % 360;
        } }

        public PeriLongitude (Processor p) : base(p)
        {
        }
    }

    public class Heading : IInputData
    {
        private Processor mProc = null;

        private Vessel parentVessel { get { return mProc.parentVessel; } }

        public string name { get { return "srfHeading"; } }
        public string unit { get { return "°"; } }
        public bool available { get { return true; } } // TODO not always available...
        public Instruction.Type typ {get { return Instruction.Type.ANGLE; } }
        public bool useSI { get { return false; } }
        public double res { get { return 0.01; } }
        public Instruction.Value value { get {
            return new Instruction.Value(Math.Round(raw / res) * res, true);
        }}
        private double raw { get {
            Vector3 up = (parentVessel.mainBody.position - parentVessel.CoM).normalized;
            Vector3 fwd = Vector3.ProjectOnPlane(parentVessel.GetTransform().transform.rotation * Vector3.forward, up).normalized;
            Vector3 north = Vector3.ProjectOnPlane(parentVessel.mainBody.transform.up, up).normalized;
            double angle = Math.Atan2(Vector3.Dot(Vector3.Cross(fwd, north), up), Vector3.Dot(fwd, north)) * 180.0 / Math.PI;
            return (angle + 360) % 360;
        } }

        public Heading (Processor p)
        {
            mProc = p;
        }
    }

    public interface IOutputData
    {
        string name { get; }
        Instruction.Type typ { get; }
        Instruction.Value value { get; }
        void Invoke(FlightCtrlState fcs, Processor p);
        void clean();
        void setValue(Instruction.Value value);
        void slewValue(Instruction.Value rate);
    }

    public class Throttle : IOutputData
    {
        public string name { get { return "throttle"; } }
        public Instruction.Type typ {get { return Instruction.Type.DOUBLE; } }
        private double mValue = 0;
        private double mSlewRate = 0;
        public Instruction.Value value { get { return new Instruction.Value(mValue); } }

        public void Invoke(FlightCtrlState fcs, Processor p)
        {
            setTo(mValue + mSlewRate * TimeWarp.deltaTime);
            fcs.mainThrottle = (float)mValue / 100.0f;
            mSlewRate = 0;
        }

        public void clean()
        {
            mValue = 0;
            mSlewRate = 0;
        }

        private void setTo(double value)
        {
            mValue = Math.Min(Math.Max(value, 0), 100.0f);
        }

        public void setValue(Instruction.Value value)
        {
            if (value.typ == Instruction.Type.DOUBLE)
            {
                setTo(value.d);
            }
        }

        public void slewValue(Instruction.Value rate)
        {
            if (rate.typ == Instruction.Type.DOUBLE)
            {
                mSlewRate += rate.d;
            }
        }
    }

    public class Orient : IOutputData
    {
        public string name { get { return "orient"; } }
        public Instruction.Type typ {get { return Instruction.Type.NAME; } }
        private string mValue = "none";
        private double mHdg = 0, mPitch = 0, mRoll = 0;
        public Instruction.Value value { get { return new Instruction.Value(mValue); } }

        private List<Modules.ModuleKpuOrientation> oriSrc(Processor p)
        {
            return p.parentVessel.FindPartModulesImplementing<Modules.ModuleKpuOrientation>();
        }

        public void Invoke(FlightCtrlState fcs, Processor p)
        {
            
            if (mValue.StartsWith("customHP"))
            {
                if (oriSrc(p).Any(m => m.customHP > 0 && m.isWorking))
                    FlightCore.HoldAttitude(fcs, p, new FlightAttitude(mHdg, mPitch));
            }
            else if (mValue.StartsWith("customHPR")) /* not plumbed in yet */
            {
                /* if (oriSrc(p).Any(m => m.customHPR > 0 && m.isWorking)) */
                FlightCore.HoldAttitude(fcs, p, new FlightAttitude(mHdg, mPitch, mRoll));
            }
            else if (mValue.Equals("srfPrograde") || mValue.Equals("srfRetrograde"))
            {
                if (oriSrc(p).Any(m => m.srfPrograde > 0 && m.isWorking))
                    FlightCore.HoldAttitude(fcs, p, new FlightAttitude(mValue));
            }
            else if (mValue.Equals("orbPrograde") || mValue.Equals("orbRetrograde"))
            {
                if (oriSrc(p).Any(m => m.orbPrograde > 0 && m.isWorking))
                    FlightCore.HoldAttitude(fcs, p, new FlightAttitude(mValue));
            }
            else if (mValue.Equals("srfVertical"))
            {
                if (oriSrc(p).Any(m => m.srfVertical > 0 && m.isWorking))
                    FlightCore.HoldAttitude(fcs, p, new FlightAttitude(mValue));
            }
            else if (mValue.Equals("orbVertical"))
            {
                if (oriSrc(p).Any(m => m.orbVertical > 0 && m.isWorking))
                    FlightCore.HoldAttitude(fcs, p, new FlightAttitude(mValue));
            }
        }

        public void clean()
        {
            mValue = "none";
        }

        public void setValue(Instruction.Value value)
        {
            if (value.typ == Instruction.Type.NAME)
            {
                mValue = value.n;
            }
            else if (value.typ == Instruction.Type.TUPLE && value.l.Count == 2)
            {
                Instruction.Value hdg = value.l[0], pitch = value.l[1];
                if (hdg.typ == Instruction.Type.DOUBLE && pitch.typ == Instruction.Type.DOUBLE)
                {
                    mValue = string.Format("customHP({0},{1})", hdg.d, pitch.d);
                    mHdg = hdg.d;
                    mPitch = pitch.d;
                }
            }
            /*else if (value.typ == Instruction.Type.TUPLE && value.l.Count == 3)
            {
                Instruction.Value hdg = value.l[0], pitch = value.l[1], roll = value.l[2];
                if (hdg.typ == Instruction.Type.DOUBLE && pitch.typ == Instruction.Type.DOUBLE && roll.typ == Instruction.Type.DOUBLE)
                {
                    mValue = string.Format("customHPR({0},{1},{2})", hdg.d, pitch.d, roll.d);
                    mHdg = hdg.d;
                    mPitch = pitch.d;
                    mRoll = roll.d;
                }
            }*/
        }

        public void slewValue(Instruction.Value rate)
        {
        }
    }

    public class Stage : IOutputData
    {
        public string name { get { return "stage"; } }
        public Instruction.Type typ {get { return Instruction.Type.BOOLEAN; } }
        public Instruction.Value value { get { return new Instruction.Value(mQueued > 0); } }
        private int mQueued;

        public void Invoke(FlightCtrlState fcs, Processor p)
        {
            if (mQueued > 0)
            {
                Staging.ActivateNextStage();
                mQueued--;
            }
        }

        public void clean()
        {
            mQueued = 0;
        }

        public void setValue(Instruction.Value value)
        {
            if (value.typ == Instruction.Type.BOOLEAN && value.b)
            {
                mQueued++;
            }
        }

        public void slewValue(Instruction.Value rate)
        {
        }
    }

    public class BooleanTrigger
    {
        public Instruction.Type typ {get { return Instruction.Type.BOOLEAN; } }
        private bool mValue = false;
        private bool mChange = false;
        public Instruction.Value value { get { return new Instruction.Value(mValue); } }

        public virtual void rawInvoke(FlightCtrlState fcs, Processor p, bool b) {}
        public virtual bool rawGet(FlightCtrlState fcs, Processor p) { return false; }

        public void Invoke(FlightCtrlState fcs, Processor p)
        {
            if (mChange)
                rawInvoke(fcs, p, mValue);
            mChange = false;
        }

        public void clean()
        {
            mChange = false;
        }

        public void setValue(Instruction.Value value)
        {
            if (value.typ == Instruction.Type.BOOLEAN)
            {
                mValue = value.b;
                mChange = true;
            }
        }

        public void slewValue(Instruction.Value rate) {}
    }

    public class GearOutput : BooleanTrigger, IOutputData
    {
        public string name { get { return "gear"; } }
        public override void rawInvoke(FlightCtrlState fcs, Processor p, bool b)
        {
            p.parentVessel.ActionGroups.SetGroup(KSPActionGroup.Gear, b);
        }
    }

    public class Brakes : BooleanTrigger, IOutputData
    {
        public string name { get { return "brakes"; } }
        public override void rawInvoke(FlightCtrlState fcs, Processor p, bool b)
        {
            p.parentVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, b);
        }
    }

    public class Lights : BooleanTrigger, IOutputData
    {
        public string name { get { return "lights"; } }
        public override void rawInvoke(FlightCtrlState fcs, Processor p, bool b)
        {
            p.parentVessel.ActionGroups.SetGroup(KSPActionGroup.Light, b);
        }
    }

    public class Abort : BooleanTrigger, IOutputData
    {
        public string name { get { return "abort"; } }
        public override void rawInvoke(FlightCtrlState fcs, Processor p, bool b)
        {
            p.parentVessel.ActionGroups.SetGroup(KSPActionGroup.Abort, b);
        }
    }

    public class SolarPanels : BooleanTrigger, IOutputData
    {
        public string name { get { return "solarPanels"; } }
        public override void rawInvoke(FlightCtrlState fcs, Processor p, bool b)
        {
            foreach (Part part in p.parentVessel.Parts)
            {
                foreach (ModuleDeployableSolarPanel sp in part.FindModulesImplementing<ModuleDeployableSolarPanel>())
                {
                    if (b)
                        sp.Extend();
                    else
                        sp.Retract();
                }
            }
        }
    }

    public class RTAntennas : BooleanTrigger, IOutputData
    {
        public string name { get { return "rtAntennas"; } }
        public override void rawInvoke(FlightCtrlState fcs, Processor p, bool b)
        {
            foreach (Part part in p.parentVessel.Parts)
            {
                foreach (RemoteTech.Modules.ModuleRTAntenna ant in part.FindModulesImplementing<RemoteTech.Modules.ModuleRTAntenna>())
                {
                    if (b)
                        ant.ActionOpen(new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate));
                    else
                        ant.ActionClose(new KSPActionParam(KSPActionGroup.None, KSPActionType.Deactivate));
                }
            }
        }
    }

    public class RoverMotors : IOutputData
    {
        public string name { get { return "wheelMotors"; } }
        public Instruction.Type typ {get { return Instruction.Type.DOUBLE; } }
        private double mValue = 0;
        private double mSlewRate = 0;
        public Instruction.Value value { get { return new Instruction.Value(mValue); } }

        public void Invoke(FlightCtrlState fcs, Processor p)
        {
            setTo(mValue + mSlewRate * TimeWarp.deltaTime);
            fcs.wheelThrottle = (float)mValue / 100.0f;
            mSlewRate = 0;
        }

        public void clean()
        {
            mValue = 0;
            mSlewRate = 0;
        }

        private void setTo(double value)
        {
            mValue = Math.Min(Math.Max(value, -100.0f), 100.0f);
        }

        public void setValue(Instruction.Value value)
        {
            if (value.typ == Instruction.Type.DOUBLE)
            {
                setTo(value.d);
            }
        }

        public void slewValue(Instruction.Value rate)
        {
            if (rate.typ == Instruction.Type.DOUBLE)
            {
                mSlewRate += rate.d;
            }
        }
    }

    public class RoverSteer : IOutputData
    {
        public string name { get { return "wheelSteering"; } }
        public Instruction.Type typ {get { return Instruction.Type.DOUBLE; } }
        private double mValue = 0;
        private double mSlewRate = 0;
        public Instruction.Value value { get { return new Instruction.Value(mValue); } }

        public void Invoke(FlightCtrlState fcs, Processor p)
        {
            setTo(mValue + mSlewRate * TimeWarp.deltaTime);
            fcs.wheelSteer = (float)mValue / 100.0f;
            mSlewRate = 0;
        }

        public void clean()
        {
            mValue = 0;
            mSlewRate = 0;
        }

        private void setTo(double value)
        {
            mValue = Math.Min(Math.Max(value, -100.0f), 100.0f);
        }

        public void setValue(Instruction.Value value)
        {
            if (value.typ == Instruction.Type.DOUBLE)
            {
                setTo(value.d);
            }
        }

        public void slewValue(Instruction.Value rate)
        {
            if (rate.typ == Instruction.Type.DOUBLE)
            {
                mSlewRate += rate.d;
            }
        }
    }

    public class LatchIO : IInputData, IOutputData
    {
        private int mIndex;
        private Processor mProcessor;
        public string name { get { return string.Format("latch{0:D}", mIndex); } }
        public string unit { get { return ""; } }
        public bool available { get { return true; } }
        public bool useSI { get { return false; } }
        public Instruction.Type typ { get { return Instruction.Type.BOOLEAN; } }
        public Instruction.Value value { get { return new Instruction.Value(mProcessor.latchState[mIndex]); } }
        public void clean() { mProcessor.latchState[mIndex] = false; }
        public void Invoke(FlightCtrlState fcs, Processor p)
        {
        }
        public void setValue(Instruction.Value value)
        {
            if (value.typ == Instruction.Type.BOOLEAN)
            {
                mProcessor.latchState[mIndex] = value.b;
            }
        }
        public void slewValue(Instruction.Value rate)
        {
        }
        public LatchIO(Processor p, int index)
        {
            mProcessor = p;
            mIndex = index;
        }
    }

    public class TimerIO : IInputData, IOutputData
    {
        private int mIndex;
        public string name { get { return string.Format("timer{0:D}", mIndex); } }
        public string unit { get { return "s"; } }
        public double res = 0.2;
        public double startTime = -1.0;
        public bool available { get { return true; } }
        public bool useSI { get { return false; } }
        public Instruction.Type typ { get { return Instruction.Type.DOUBLE; } }
        public double runTime
        {
            get
            {
                if (startTime < 0.0)
                    return -1.0;
                return Planetarium.GetUniversalTime() - startTime;
            }
        }
        public Instruction.Value value
        {
            get
            {
                return new Instruction.Value(Math.Round(runTime / res) * res);
            }
        }
        public void clean() { startTime = -1.0; }
        public void Invoke(FlightCtrlState fcs, Processor p)
        {
        }
        public void setValue(Instruction.Value value)
        {
            if (value.typ == Instruction.Type.BOOLEAN)
            {
                if (value.b)
                    startTime = Planetarium.GetUniversalTime();
                else
                    startTime = -1.0;
            }
        }
        public void slewValue(Instruction.Value rate)
        {
        }
        public TimerIO(Processor p, int index)
        {
            mIndex = index;
        }
    }

    public class Processor
    {
        public bool hasLevelTrigger, hasLogicOps, hasArithOps;
        public int imemWords, latches, timers;
        public List<Instruction> instructions;
        public List<bool> latchState = null;
        public List<TimerIO> timerState = null;

        public System.Random mRandom;

        private Part mPart;
        public bool hasPower = false;
        private bool mIsRunning;
        public bool isRunning
        {
            get { return mIsRunning; }
            set {
                mIsRunning = value;
                foreach (IOutputData o in outputs.Values)
                {
                    o.clean();
                }
            }
        }
        public bool isHibernating = false;
        private int hibernationLine;

        public void hibernate(Instruction src)
        {
            if (isHibernating) return;
            if (!instructions.Contains(src)) // can't happen
                throw new Instruction.ExecError("hibernated instruction not in program");
            hibernationLine = instructions.IndexOf(src);
            isHibernating = true;
            Logging.Log(string.Format("Hibernated from line {0:D}: {1}", hibernationLine, src.ToString()));
        }

        public void wakeup()
        {
            isHibernating = false;
            Logging.Log("Awoke from hibernation");
        }

        // Warning, may be null
        public Vessel parentVessel { get { return mPart.vessel; } }

        public bool error = false;

        public Dictionary<string, IInputData> inputs = new Dictionary<string, IInputData>();
        public Dictionary<string, IOutputData> outputs = new Dictionary<string, IOutputData>();

        private void addInput(IInputData i)
        {
            inputs.Add(i.name, i);
        }

        private void addOutput(IOutputData o)
        {
            outputs.Add(o.name, o);
        }

        private void addLatch(LatchIO l)
        {
            addInput(l);
            addOutput(l);
        }

        private void addTimer(TimerIO t)
        {
            addInput(t);
            addOutput(t);
        }

        public Processor (Part part, Modules.ModuleKpuProcessor module)
        {
            mRandom = new System.Random();
            mPart = part;
            hasLevelTrigger = module.hasLevelTrigger;
            hasLogicOps = module.hasLogicOps;
            hasArithOps = module.hasArithOps;
            imemWords = module.imemWords;
            latches = module.latches;
            timers = module.timers;
            isRunning = module.isRunning;
            instructions = new List<Instruction>();
            if (latches > 0)
            {
                latchState = new List<bool>(latches);
                for (int i = 0; i < latches; i++)
                {
                    latchState.Add(false);
                    addLatch(new LatchIO(this, i));
                }
            }
            if (timers > 0)
            {
                timerState = new List<TimerIO>(timers);
                for (int i = 0; i < timers; i++)
                {
                    TimerIO t = new TimerIO(this, i);
                    timerState.Add(t);
                    addTimer(t);
                }
            }
            addInput(new Batteries(this));
            addInput(new Gear(this));
            addInput(new SrfHeight(this));
            addInput(new SrfSpeed(this));
            addInput(new SrfVerticalSpeed(this));
            addInput(new VesselTMR(this));
            addInput(new LocalG(this));
            addInput(new Altitude(this));
            addInput(new Latitude(this));
            addInput(new Longitude(this));
            addInput(new OrbSpeed(this));
            addInput(new Periapsis(this));
            addInput(new Apoapsis(this));
            addInput(new Inclination(this));
            addInput(new ANLongitude(this));
            addInput(new PeriLongitude(this));
            addInput(new Heading(this));
            addOutput(new Throttle());
            addOutput(new Orient());
            addOutput(new Stage());
            addOutput(new GearOutput());
            addOutput(new Brakes());
            addOutput(new Lights());
            addOutput(new Abort());
            addOutput(new SolarPanels());
            addOutput(new RTAntennas());
            addOutput(new RoverMotors());
            addOutput(new RoverSteer());

            initPIDParameters();
        }

        public void ClearInstructions()
        {
            foreach (Instruction i in instructions)
            {
                imemWords += i.imemWords;
            }
            instructions.Clear();
        }

        public bool AddInstruction(string text)
        {
            return AddInstruction(text, false, false);
        }

        public bool AddInstruction(string text, bool lastValue, bool skip)
        {
            Instruction i;
            try
            {
                i = new Instruction(text);
            }
            catch (Instruction.ParseError exc)
            {
                Logging.Message(exc.ToString());
                return false;
            }
            if (i.imemWords > imemWords)
            {
                Logging.Message("Insufficient IMEM for " + i.mText);
                return false;
            }
            if (i.requiresLevelTrigger && !hasLevelTrigger)
            {
                Logging.Message("Processor does not support level triggers");
                Logging.Message("  " + i.mText);
                return false;
            }
            if (i.requiresLogicOps && !hasLogicOps)
            {
                Logging.Message("Processor does not support logical operators");
                Logging.Message("  " + i.mText);
                return false;
            }
            if (i.requiresArithOps && !hasArithOps)
            {
                Logging.Message("Processor does not support arithmetic operators");
                Logging.Message("  " + i.mText);
                return false;
            }
            instructions.Add(i);
            imemWords -= i.imemWords;
            //Logging.Log("Added instruction: " + i.mText);
            return true;
        }

        public Dictionary<string, Instruction.Value> inputValues;

        // For kapparay.  Radiation can trigger various kinds of glitches
        public int OnRadiation(double energy, int count)
        {
            /* Chance to flip latches */
            for (int i = 0; i < latches; i++)
            {
                if (kapparay.Core.Instance.mRandom.NextDouble() < count * Math.Log10(energy) / 4e3)
                {
                    Logging.Log(String.Format("SEU flipped latch {0}", i));
                    latchState[i] = !latchState[i];
                    count -= 1;
                    if (count == 0) return 0;
                }
            }
            /* Chance to produce spurious edges */
            foreach (Instruction i in instructions)
            {
                if (kapparay.Core.Instance.mRandom.NextDouble() < count * Math.Log10(energy) / 4e3)
                {
                    Logging.Log("SEU glitched " + i.ToString());
                    i.glitch();
                    count -= 1;
                    if (count == 0) return 0;
                }
            }
            return count;
        }

        public void OnUpdate ()
        {
            inputValues = new Dictionary<string, Instruction.Value>();
            foreach (IInputData i in inputs.Values)
            {
                try
                {
                    if (i.available)
                    {
                        inputValues.Add(i.name, i.value);
                    }
                }
                catch (Exception exc)
                {
                    Logging.Message("Problem in input " + i.name);
                    Logging.Log(exc.ToString());
                    Logging.Log(exc.StackTrace);
                }
            }
            if (hasPower && isRunning)
            {
                if (isHibernating)
                {
                    Instruction hi = instructions[hibernationLine];
                    hi.considerWakeup(this);
                }
                else
                {
                    foreach (Instruction i in instructions)
                    {
                        i.eval(this);
                    }
                }
                error = false;
            }
        }

        public void OnFlyByWire (FlightCtrlState fcs)
        {
            if (hasPower && isRunning)
            {
                OnUpdate(); // ensure outputs are up-to-date
                foreach (IOutputData o in outputs.Values)
                {
                    o.Invoke(fcs, this);
                }
            }
        }

        public void OnFixedUpdate()
        {
            updatePIDParameters();
        }

        public PIDController pid { get; private set; }
        public Vector3d lastAct { get; set; }
        public double Tf = 0.3;
        public double TfMin = 0.1;
        public double TfMax = 0.5;
        public double kpFactor = 3;
        public double kiFactor = 6;
        public double kdFactor = 0.5;

        public void initPIDParameters()
        {
            pid = new PIDController(0, 0, 0, 1, -1);
            pid.Kd = kdFactor / Tf;
            pid.Kp = pid.Kd / (kpFactor * Math.Sqrt(2) * Tf);
            pid.Ki = pid.Kp / (kiFactor * Math.Sqrt(2) * Tf);
            pid.intAccum = Vector3.ClampMagnitude(pid.intAccum, 5);
            lastAct = Vector3d.zero;
        }

        // Calculations of Tf are not safe during Processor constructor
        // Probably because the ship is only half-initialized...
        public void updatePIDParameters()
        {
            if (parentVessel != null)
            {
                Vector3d torque = SteeringHelper.GetTorque(parentVessel,
                    parentVessel.ctrlState != null ? parentVessel.ctrlState.mainThrottle : 0.0f);
                var CoM = parentVessel.findWorldCenterOfMass();
                var MoI = parentVessel.findLocalMOI(CoM);

                Vector3d ratio = new Vector3d(
                                 torque.x != 0 ? MoI.x / torque.x : 0,
                                 torque.y != 0 ? MoI.y / torque.y : 0,
                                 torque.z != 0 ? MoI.z / torque.z : 0
                             );

                Tf = Mathf.Clamp((float)ratio.magnitude / 20f, 2 * TimeWarp.fixedDeltaTime, 1f);
                Tf = Mathf.Clamp((float)Tf, (float)TfMin, (float)TfMax);
            }
        }

        public void Save (ConfigNode node)
        {
            if (node.HasNode("Processor"))
                node.RemoveNode("Processor");
            
            ConfigNode Proc = new ConfigNode("Processor");

            Proc.AddValue("isHibernating", isHibernating);
            if (isHibernating)
                Proc.AddValue("hibernationLine", hibernationLine);

            if (pid != null)
                pid.Save(Proc);

            ConfigNode InstList = new ConfigNode("Instructions");
            foreach (Instruction i in instructions)
            {
                ConfigNode Inst = new ConfigNode("Instruction");
                Inst.AddValue("code", i.mText);
                Inst.AddValue("lastValue", i.lastValue);
                Inst.AddValue("skip", i.skip);
                InstList.AddNode(Inst);
            }
            Proc.AddNode(InstList);

            if (latches > 0)
            {
                ConfigNode LatchList = new ConfigNode("Latches");
                for (int i = 0; i < latches; i++)
                    LatchList.AddValue(string.Format("latch{0:D}", i), latchState[i].ToString());
                Proc.AddNode(LatchList);
            }

            if (timers > 0)
            {
                ConfigNode TimerList = new ConfigNode("Timers");
                for (int i = 0; i < timers; i++)
                    TimerList.AddValue(timerState[i].name, timerState[i].startTime);
                Proc.AddNode(TimerList);
            }

            node.AddNode(Proc);
        }

        public void Load (ConfigNode node)
        {
            if (!node.HasNode("Processor"))
                return;
            
            ConfigNode Proc = node.GetNode("Processor");

            if (Proc.HasValue("isHibernating"))
                bool.TryParse(Proc.GetValue("isHibernating"), out isHibernating);
            if (isHibernating && Proc.HasValue("hibernationLine"))
                int.TryParse(Proc.GetValue("hibernationLine"), out hibernationLine);

            if (pid != null)
                pid.Load(Proc);

            ConfigNode InstList = Proc.GetNode("Instructions");
            if (InstList != null)
            {
                ClearInstructions();
                foreach (ConfigNode Inst in InstList.GetNodes("Instruction"))
                {
                    string code = Inst.GetValue("code");
                    if (code != null)
                    {
                        bool lastValue = false, skip = false;
                        bool.TryParse(Inst.GetValue("lastValue"), out lastValue);
                        bool.TryParse(Inst.GetValue("skip"), out skip);
                        AddInstruction(code, lastValue, skip);
                    }
                }
            }

            ConfigNode LatchList = Proc.GetNode("Latches");
            if (LatchList != null)
            {
                for (int i = 0; i < latches; i++)
                {
                    string state = LatchList.GetValue(string.Format("latch{0:D}", i));
                    if (state != null)
                    {
                        bool stateBit = false;
                        bool.TryParse(state, out stateBit);
                        latchState[i] = stateBit;
                    }
                    else
                    {
                        latchState[i] = false;
                    }
                }
            }

            ConfigNode TimerList = Proc.GetNode("Timers");
            if (TimerList != null)
            {
                for (int i = 0; i < timers; i++)
                {
                    string state = TimerList.GetValue(timerState[i].name);
                    if (state != null)
                    {
                        double startTime = -1;
                        double.TryParse(state, out startTime);
                        timerState[i].startTime = startTime;
                    }
                    else
                    {
                        timerState[i].startTime = -1.0;
                    }
                }
            }
        }
    }
}

