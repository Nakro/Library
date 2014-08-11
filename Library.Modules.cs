using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using System.IO;
using Library;

namespace Library.Modules
{
    /*
       Copyright (c) 2002 Douglas Crockford  (www.crockford.com)

       Permission is hereby granted, free of charge, to any person obtaining a copy of
       this software and associated documentation files (the "Software"), to deal in
       the Software without restriction, including without limitation the rights to
       use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
       of the Software, and to permit persons to whom the Software is furnished to do
       so, subject to the following conditions:
    
       The above copyright notice and this permission notice shall be included in all
       copies or substantial portions of the Software.

       The Software shall be used for Good, not Evil.

       THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
       IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
       FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
       AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
       LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
       OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
       SOFTWARE.
     */

    #region JavaScriptMinifier
    public class JavaScriptMinifier
    {
        const int EOF = -1;

        StreamReader sr = null;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int theA;
        int theB;
        int theLookahead = EOF;

        public string Minify(MemoryStream src)
        {
            using (sr = new StreamReader(src, System.Text.Encoding.UTF8))
            {
                jsmin();
                return sb.ToString();
            }
        }

        void jsmin()
        {
            theA = '\n';
            action(3);
            while (theA != EOF)
            {
                switch (theA)
                {
                    case ' ':
                        {
                            if (isAlphanum(theB))
                            {
                                action(1);
                            }
                            else
                            {
                                action(2);
                            }
                            break;
                        }
                    case '\n':
                        {
                            switch (theB)
                            {
                                case '{':
                                case '[':
                                case '(':
                                case '+':
                                case '-':
                                    {
                                        action(1);
                                        break;
                                    }
                                case ' ':
                                    {
                                        action(3);
                                        break;
                                    }
                                default:
                                    {
                                        if (isAlphanum(theB))
                                        {
                                            action(1);
                                        }
                                        else
                                        {
                                            action(2);
                                        }
                                        break;
                                    }
                            }
                            break;
                        }
                    default:
                        {
                            switch (theB)
                            {
                                case ' ':
                                    {
                                        if (isAlphanum(theA))
                                        {
                                            action(1);
                                            break;
                                        }
                                        action(3);
                                        break;
                                    }
                                case '\n':
                                    {
                                        switch (theA)
                                        {
                                            case '}':
                                            case ']':
                                            case ')':
                                            case '+':
                                            case '-':
                                            case '"':
                                            case '\'':
                                                {
                                                    action(1);
                                                    break;
                                                }
                                            default:
                                                {
                                                    if (isAlphanum(theA))
                                                    {
                                                        action(1);
                                                    }
                                                    else
                                                    {
                                                        action(3);
                                                    }
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        action(1);
                                        break;
                                    }
                            }
                            break;
                        }
                }
            }
        }

        void action(int d)
        {
            if (d <= 1)
            {
                put(theA);
            }
            if (d <= 2)
            {
                theA = theB;
                if (theA == '\'' || theA == '"')
                {
                    for (; ; )
                    {
                        put(theA);
                        theA = get();
                        if (theA == theB)
                        {
                            break;
                        }
                        if (theA <= '\n')
                        {
                            throw new Exception(string.Format("Error: JSMIN unterminated string literal: {0}\n", theA));
                        }
                        if (theA == '\\')
                        {
                            put(theA);
                            theA = get();
                        }
                    }
                }
            }
            if (d <= 3)
            {
                theB = next();
                if (theB == '/' && (theA == '(' || theA == ',' || theA == '=' ||
                                    theA == '[' || theA == '!' || theA == ':' ||
                                    theA == '&' || theA == '|' || theA == '?' ||
                                    theA == '{' || theA == '}' || theA == ';' ||
                                    theA == '\n'))
                {
                    put(theA);
                    put(theB);
                    while (true) {
                        theA = get();
                        if (theA == '/')
                            break;

                        if (theA == '\\') {
                            put(theA);
                            theA = get();
                        } else if (theA <= '\n') {
                            throw new Exception(string.Format("Error: JSMIN unterminated Regular Expression literal : {0}.\n", theA));
                        }
                        put(theA);
                    }
                    theB = next();
                }
            }
        }

        int next()
        {
            int c = get();
            if (c == '/')
            {
                switch (peek())
                {
                    case '/':
                        {
                            for (; ; )
                            {
                                c = get();
                                if (c <= '\n')
                                {
                                    return c;
                                }
                            }
                        }
                    case '*':
                        {
                            get();
                            for (; ; )
                            {
                                switch (get())
                                {
                                    case '*':
                                        {
                                            if (peek() == '/')
                                            {
                                                get();
                                                return ' ';
                                            }
                                            break;
                                        }
                                    case EOF:
                                        {
                                            throw new Exception("Error: JSMIN Unterminated comment.\n");
                                        }
                                }
                            }
                        }
                    default:
                        {
                            return c;
                        }
                }
            }
            return c;
        }

        int peek()
        {
            theLookahead = get();
            return theLookahead;
        }

        int get()
        {
            int c = theLookahead;
            theLookahead = EOF;
            if (c == EOF)
            {
                c = sr.Read();
            }
            if (c >= ' ' || c == '\n' || c == EOF)
            {
                return c;
            }
            if (c == '\r')
            {
                return '\n';
            }
            return ' ';
        }

        void put(int c)
        {
            if (c == 13 || c == 10)
                sb.Append(' ');
            else
                sb.Append((char)c);
        }

        bool isAlphanum(int c)
        {
            return ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || c == '_' || c == '$' || c == '\\' || c > 126);
        }
    }
    #endregion

    // ===============================================================================================
    // 
    // Library.Module.[Less]
    // 
    // ===============================================================================================

    // ===============================================================================================
    // Autor        : Peter Širka
    // Created      : 10. 01. 2012
    // Updated      : 01. 02. 2014
    // Description  : LESS CSS + automated CSS vendor prefixes
    // ===============================================================================================

    #region Less
    public class Less
    {
        private class LessValue
        {
            private class LessParam
            {
                public string Name { get; set; }
                public string Value { get; set; }
            }

            public int Index { get; set; }
            public string Value { get; set; }
            public string Name { get; set; }
            public bool IsVariable { get; set; }
            public bool IsFunction { get; set; }
            public bool IsProblem { get; set; }

            public string GetValue(LessValue less)
            {
                if (less == null)
                    return "";

                if (IsVariable)
                    return "";

                var value = "";

                if (!IsFunction)
                {

                    value = less.Value.Substring(less.Name.Length).Trim();

                    if ((value[0] == '{') && (value[value.Length - 1] == '}'))
                        value = value.Substring(1, value.Length - 2).Trim();

                    return value;
                }

                var param = new List<LessParam>(5);

                var beg = less.Value.IndexOf('(') + 1;
                var end = less.Value.IndexOf(')', beg + 1);

                foreach (var p in less.Value.Substring(beg, end - beg).Split(','))
                    param.Add(new LessParam { Name = p.Trim() });

                beg = Value.IndexOf('(') + 1;
                end = Value.LastIndexOf(')');

                var index = 0;
                foreach (var p in Params(Value.Substring(beg, end - beg)))
                {
                    param[index].Value = p.Trim().Replace('|', ',');
                    index++;
                }

                beg = less.Value.IndexOf('{') + 1;
                end = less.Value.LastIndexOf('}');
                index = 0;

                var sb = new System.Text.StringBuilder();

                foreach (var p in less.Value.Substring(beg, end - beg).Split(';'))
                {
                    value = p.Trim();
                    if (string.IsNullOrEmpty(value))
                        continue;

                    foreach (var j in param)
                    {
                        if (j.Value[0] == '\'' || j.Value[0] == '\"')
                            j.Value = j.Value.Substring(1, j.Value.Length - 2);

                        value = value.Replace("@" + j.Name, j.Value);
                    }

                    if (sb.Length > 0)
                        sb.Append(';');
                    sb.Append(value);
                }
                return sb.ToString();

            }
        }

        private static IList<string> Params(string param)
        {
            var sb = new System.Text.StringBuilder();
            var l = new List<string>(5);
            var index = 0;
            var skip = false;
            var closure = false;

            var prepare = new Func<string, string>(n =>
            {
                var val = n.Replace('|', ',');
                if (val[0] == '\'' || val[0] == '\"')
                    return val.Substring(1, val.Length - 2);
                return val;
            });

            do
            {
                var c = param[index];

                if (c == '(' && !skip)
                {
                    closure = true;
                    skip = true;
                }

                if (!closure)
                {
                    if (c == '\'' || c == '\"')
                        skip = !skip;
                }

                if (c == ')' && !skip && closure)
                {
                    skip = false;
                    closure = false;
                }

                if (c != ',' || skip || closure)
                    sb.Append(c);
                else
                {
                    l.Add(prepare(sb.ToString()));
                    sb.Clear();
                }

                index++;
            } while (index < param.Length);

            if (sb.Length > 0)
                l.Add(sb.ToString());

            return l;
        }

        private static LessValue getValue(LessValue prev, string value)
        {
            var index = 0;
            if (prev != null)
                index = prev.Index + prev.Value.Length;

            var beg = false;
            var copy = false;

            var param = 0;
            var val = 0;

            var sb = new System.Text.StringBuilder();
            var less = new LessValue();

            while (index < value.Length)
            {
                var c = value[index];

                if (c == '@' && !less.IsFunction)
                {
                    beg = true;
                    copy = true;
                    less.Index = index;
                }
                else if (beg)
                {
                    var charindex = Convert.ToInt32(c);

                    if (charindex == 40)
                        param++;
                    else if (charindex == 41)
                        param--;

                    var next = val != 0;
                    if (charindex == 123)
                    {
                        if (val == 0)
                            less.IsVariable = true;
                        val++;
                        next = true;
                    }
                    else if (charindex == 125)
                    {
                        if (val == 0)
                        {
                            index++;
                            continue;
                        }
                        val--;
                        next = true;
                    }

                    if (charindex == 32 || charindex == 41)
                    {
                        next = true;
                    }
                    else if (param == 0 && val == 0 && !next)
                    {
                        next = (charindex >= 65 && charindex <= 90) || (charindex >= 97 && charindex <= 122) || charindex == 45;
                    }
                    else if (param > 0 && val == 0)
                    {
                        next = charindex != 41;
                        less.IsFunction = true;
                    }
                    else if (val > 0 && param == 0)
                    {
                        next = true;
                    }

                    copy = next;
                }

                if (beg && copy)
                {
                    sb.Append(c);
                }
                else if (beg)
                {
                    if (copy)
                        sb.Append(c);

                    less.Value = sb.ToString().Trim();

                    if (less.IsFunction)
                        less.Name = less.Value.Substring(0, less.Value.IndexOf('(')).Trim();
                    else if (less.IsVariable)
                        less.Name = less.Value.Substring(0, less.Value.IndexOf('{')).Trim();
                    else
                        less.Name = less.Value.Trim();

                    if (less.Name.Contains(false, "@import", "@font-face", "@keyframes", "@-moz-keyframes", "@-webkit-keyframes", "@-o-keyframes", "@-ms-keyframes", "@media", "@charset"))
                        less.IsProblem = true;

                    return less;
                }
                index++;
            }
            return null;
        }

        public static string Compile(string css)
        {
            css = Autoprefixer(css);

            var l = new List<LessValue>(10);
            var less = getValue(null, css);

            while (less != null)
            {
                l.Add(less);
                less = getValue(less, css);
            }

            if (l.Count > 0)
            {
                var variables = l.Where(n => n.IsVariable && !n.IsProblem).ToList();

                foreach (var m in variables)
                    css = css.Replace(m.Value, "");

                foreach (var m in l.Where(n => !n.IsVariable && !n.IsProblem))
                {
                    var value = m.GetValue(variables.FirstOrDefault(n => n.Name == m.Name));
                    css = css.Replace(m.Value, value);
                }
            }

            if (Configuration.OnVersion != null)
            {
                var reg = new Regex("url\\(.*?\\)", RegexOptions.Compiled);
                foreach (Match m in reg.Matches(css))
                {
                    if (!m.Success)
                        continue;

                    var url = m.Value.Substring(4);
                    url = url.Substring(0, url.Length - 1);
                    css = css.Replace(m.Value, string.Format("url({0})", Configuration.OnVersion(url)));
                }
            }

            var reg1 = new Regex(@"\n|\s{2,}", RegexOptions.Compiled);
            var reg2 = new Regex(@"\s?\{\s{1,}");
            var reg3 = new Regex(@"\s?\}\s{1,}");
            var reg4 = new Regex(@"\s?\:\s{1,}");
            var reg5 = new Regex(@"\s?\;\s{1,}");
            return reg5.Replace(reg4.Replace(reg3.Replace(reg2.Replace(reg1.Replace(css, ""), "{"), "}"), ":"), ";").Replace(" }", "}").Replace(" {", "{");
        }

        private static string Autoprefixer_keyframes(string value)
        {
            var builder = new List<KeyValue>(10);
            var output = new List<string>(10);
            var index = 0;

            while (index != -1)
            {

                index = value.IndexOf("@keyframes", index + 1, StringComparison.InvariantCultureIgnoreCase);
                if (index == -1)
                    continue;

                var counter = 0;
                var end = -1;

                for (var indexer = index + 10; indexer < value.Length; indexer++)
                {

                    if (value[indexer] == '{')
                        counter++;

                    if (value[indexer] != '}')
                        continue;

                    if (counter > 1)
                    {
                        counter--;
                        continue;
                    }

                    end = indexer;
                    break;
                }

                if (end == -1)
                    continue;

                var css = value.Substring(index, (end + 1) - index);
                builder.Add(new KeyValue("keyframes", css));
            }

            for (var i = 0; i < builder.Count; i++)
            {
                var property = builder[i].Value;
                var plus = property.Substring(1);
                var delimiter = "\n";
                var updated = plus + delimiter;

                updated += "@-webkit-" + plus + delimiter;
                updated += "@-moz-" + plus + delimiter;
                updated += "@-o-" + plus;
                value = value.Replace(property, "[[{0}]]".format(output.Count));
                output.Add(updated);
            }

            index = 0;
            foreach (var m in output)
                value = value.Replace("[[{0}]]".format(index++), m);

            return value;
        }

        private static string Autoprefixer(string value)
        {
            var prefix = new [] { "appearance", "column-count", "column-gap", "column-rule", "display", "transform", "transform-style", "transform-origin", "transition", "user-select", "animation", "perspective", "animation-name", "animation-duration", "animation-timing-function", "animation-delay", "animation-iteration-count", "animation-direction", "animation-play-state", "opacity", "background", "background-image", "font-smoothing", "text-size-adjust", "backface-visibility" };
            var id = "@#auto-vendor-prefix#@";

            if (value.IndexOf(id, StringComparison.InvariantCultureIgnoreCase) == -1)
            {
                id = "/*auto*/";
                if (value.IndexOf(id, StringComparison.InvariantCultureIgnoreCase) == -1)
                    return value;
            }

            value = Autoprefixer_keyframes(value.Replace(id, ""));

            var builder = new List<KeyValue>(10);
            var output = new List<string>(10);
            var index = 0;

            for (var i = 0; i < prefix.Length; i++)
            {

                var property = prefix[i];
                index = 0;

                while (index != -1)
                {

                    index = value.IndexOf(property, index + 1, StringComparison.InvariantCultureIgnoreCase);

                    if (index == -1)
                        continue;

                    var a = value.IndexOf(';', index);
                    var b = value.IndexOf('}', index);

                    var end = Math.Min(a, b);

                    if (end == -1)
                        end = Math.Max(a, b);

                    if (end == -1)
                        continue;

                    var beg = index - 1;
                    if (beg > -1)
                    {
                        var prev = value.Substring(beg, 1);
                        if (property == "transform" && prev[0] == '-')
                            continue;
                    }

                    var css = value.Substring(index, (end - index) + 1);

                    end = css.IndexOf(':');

                    if (end == -1)
                        continue;

                    if (css.Substring(0, end + 1).RegExReplace(@"\s", "") != property + ':')
                        continue;

                    builder.Add(new KeyValue(property, css));
                }
            }

            for (var i = 0; i < builder.Count; i++)
            {
                var name = builder[i].Key;
                var property = builder[i].Value;
                var plus = property.Trim();
                var delimiter = ";";
                var last = plus.Substring(plus.Length - 1);

                if (last == "}")
                    plus = plus.Substring(0, plus.Length - 1).Trim();

                if (last == ";")
                {
                    delimiter = "";
                    last = "";
                }

                var updated = plus + delimiter;

                if (name == "opacity")
                {
                    var opacity = plus.Replace("opacity", "").Replace(":", "").RemoveOperator('.').To<decimal>();
                    updated += "filter:alpha(opacity=" + Math.Floor(opacity * 100) + ");";
                    value = value.Replace(property, "[[{0}]]".format(output.Count));
                    output.Add(updated);
                    continue;
                }

                if (name == "background" || name == "background-image")
                {

                    if (property.IndexOf("linear-gradient", StringComparison.InvariantCultureIgnoreCase) == -1)
                        continue;

                    updated = plus.Replace("linear-", "-webkit-linear-") + delimiter;
                    updated += plus.Replace("linear-", "-moz-linear-") + delimiter;
                    updated += plus.Replace("linear-", "-o-linear-") + delimiter;
                    updated += plus.Replace("linear-", "-ms-linear-") + delimiter;
                    updated += plus + delimiter;

                    value = value.Replace(property, "[[{0}]]".format(output.Count));
                    output.Add(updated);
                    continue;
                }

                if (name == "text-overflow")
                {
                    updated = plus + delimiter;
                    updated += plus.Replace("text-overflow", "-ms-text-overflow") + delimiter;
                    value = value.Replace(property, "[[{0}]]".format(output.Count));
                    output.Add(updated);
                    continue;
                }

                if (name == "display")
                {

                    if (property.IndexOf("box", StringComparison.InvariantCultureIgnoreCase) == -1)
                        continue;

                    updated = plus + delimiter;
                    updated += plus.Replace("box", "-webkit-box") + delimiter;
                    updated += plus.Replace("box", "-moz-box") + delimiter;
                    value = value.Replace(property, "[[{0}]]".format(output.Count));
                    output.Add(updated);
                    continue;
                }

                updated += "-webkit-" + plus + delimiter;
                updated += "-moz-" + plus;

                if (name.IndexOf("animation", StringComparison.InvariantCultureIgnoreCase) == -1)
                    updated += delimiter + "-ms-" + plus;

                updated += delimiter + "-o-" + plus;

                value = value.Replace(property, "[[{0}]]".format(output.Count));
                output.Add(updated + last);
            }

            index = 0;
            foreach (var m in output)
                value = value.Replace("[[{0}]]".format(index++), m);

            return value;
        }
    }
    #endregion

}
