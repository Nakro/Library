using System;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Dynamic;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Library;

namespace Library.Json
{
    public class JsonSerializer
    {
        private struct SearchValue
        {
            public int Index { get; set; }
            public string Value { get; set; }
            public bool IsArrayClass { get; set; }
            public bool IsString { get; set; }

            public static IList<SearchValue> Search(StringBuilder sb)
            {
                var i = 0;
                var l = new List<SearchValue>(10);

                while (i < sb.Length)
                {
                    var c = sb[i];
                    if (c == '"')
                    {
                        var v = new SearchValue { Index = i, Value = getWord(i, sb), IsString = true, IsArrayClass = false };
                        l.Add(v);
                        i = v.Index + v.Value.Length + 1;
                    }
                    else if (c == ':' && sb[i + 1] != '"')
                    {
                        var val = getValue(i, sb);
                        var v = new SearchValue { Index = i, Value = val, IsArrayClass = val[0] == '[' || val[0] == '{' };
                        l.Add(v);
                        i = v.Index + v.Value.Length + 1;
                    }

                    i++;
                }

                return l;
            }

            private static string getWord(int index, StringBuilder b)
            {
                var sb = new StringBuilder();
                var skip = false;
                for (var i = index + 1; i < b.Length; i++)
                {
                    if (skip)
                    {
                        sb.Append(b[i]);
                        skip = false;
                        continue;
                    }

                    if (b[i] == '\\')
                    {
                        sb.Append("\\");
                        skip = true;
                        continue;
                    }

                    if (b[i] == '"')
                        break;

                    sb.Append(b[i]);
                }
                return sb.ToString();
            }

            private static string getValue(int index, StringBuilder b)
            {
                var sb = new StringBuilder();
                var countStr = 0;
                var countArr = 0;
                var countObj = 0;
                var c = b[index + 1];
                var beg = c == '[' || c == '{' ? c : b[index];
                var skip = true;

                index += 1;
                while (index < b.Length - 1)
                {
                    c = b[index++];
                    sb.Append(c);

                    if (skip)
                    {
                        skip = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        skip = true;
                        continue;
                    }

                    if (c == '"' && countStr > 0)
                    {
                        countStr--;
                        continue;
                    }

                    if (c == '"')
                    {
                        countStr++;
                        continue;
                    }

                    if (c != '"' && countStr > 0)
                        continue;

                    if (c == '[')
                    {
                        countArr++;
                        continue;
                    }

                    if (c == ']' && countArr > 0)
                    {
                        countArr--;
                        continue;
                    }

                    if (c == '{')
                    {
                        countObj++;
                        continue;
                    }

                    if (c == '}' && countObj > 0)
                    {
                        countObj--;
                        continue;
                    }

                    if (beg == ':' && (c == ',' || c == '}' || c == ']') && countArr == 0 && countStr == 0 && countObj == 0)
                    {
                        sb.Remove(sb.Length - 1, 1);
                        break;
                    }

                    if ((beg == '[' && c == ']' && countArr == 0 && countStr == 0 && countObj == 0) || (beg == '{' && c == '}' && countArr == 0 && countStr == 0 && countObj == 0))
                        break;
                }
                return sb.ToString();
            }
        }

        private class JsonValue
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public bool IsString { get; set; }
            public bool IsArrayClass { get; set; }
        }

        public class JsonCache
        {
            public PropertyInfo[] Properties { get; set; }
            public Dictionary<PropertyInfo, Tuple<string, bool, bool, bool>> Attributes { get; set; }
        }

        private static System.Collections.Concurrent.ConcurrentDictionary<Type, JsonCache> cacheList = new System.Collections.Concurrent.ConcurrentDictionary<Type, JsonCache>();

        public static Action<Exception, string> OnError = null;

        // vytvoríme si default instanciu
        private static JsonSerializer _serializer = new JsonSerializer();

        public static void ClearJsonCache()
        {
            cacheList.Clear();
        }

        public static void GetJsonCache(Action<Type, JsonCache> onItem)
        {
            foreach (var k in cacheList)
                onItem(k.Key, k.Value);
        }

        private static void IsException(string source, Exception ex)
        {
            if (OnError != null)
                OnError(ex, source);
        }

        public static string SerializeObject(object o, params string[] withoutProperty)
        {
            try
            {
                return _serializer.Serialize(o, withoutProperty);
            }
            catch (Exception Ex)
            {
                IsException("SerializeObject", Ex);
                return string.Empty;
            }
        }

        public static T DeserializeObject<T>(string json)
        {
            try
            {
                return _serializer.Deserialize<T>(json.Trim());
            }
            catch (Exception Ex)
            {
                IsException("DeSerializeObject<T>", Ex);
                return default(T);
            }
        }

        public static object DeserializeObject(string json, Type type)
        {
            try
            {
                return _serializer.DeserializeType(json, type);
            }
            catch (Exception Ex)
            {
                IsException("DeSerializeObject", Ex);
                return null;
            }
        }

        public static dynamic DeserializeObject(string json)
        {
            try
            {
                return _serializer.Deserialize(json.Trim());
            }
            catch (Exception Ex)
            {
                IsException("DeSerializeObject", Ex);
                return null;
            }
        }

        #region Serialization
        void WriteString(StringBuilder sb, bool divider, string name, object value)
        {
            if (sb.Length > 0 && divider)
                sb.Append(',');

            sb.Append(string.Format("\"{0}\":{1}", name, (value == null ? "null" : "\"" + ToSafeString(value.ToString()) + "\"")));
        }

        void WriteString(StringBuilder sb, bool divider, object value)
        {
            if (sb.Length > 0 && divider)
                sb.Append(',');

            sb.Append(string.Format("{0}", (value == null ? "null" : "\"" + ToSafeString(value.ToString()) + "\"")));
        }

        void WriteNumber(StringBuilder sb, bool divider, string name, object value)
        {
            if (sb.Length > 0 && divider)
                sb.Append(',');

            sb.Append(string.Format("\"{0}\":{1}", name, (value == null ? "null" : value.ToString().Replace(',', '.'))));
        }

        void WriteNumber(StringBuilder sb, bool divider, object value)
        {
            if (sb.Length > 0 && divider)
                sb.Append(',');

            sb.Append(string.Format("{0}", (value == null ? "null" : value.ToString().Replace(',', '.'))));
        }

        void WriteNull(StringBuilder sb, bool divider, string name)
        {
            if (sb.Length > 0 && divider)
                sb.Append(',');

            sb.Append(string.Format("\"{0}\":null", name));
        }

        void WriteNull(StringBuilder sb, bool divider)
        {
            if (sb.Length > 0 && divider)
                sb.Append(',');

            sb.Append("null");
        }

        void WriteBool(StringBuilder sb, bool divider, string name, object value)
        {
            if (sb.Length > 0 && divider)
                sb.Append(',');

            sb.Append(string.Format("\"{0}\":{1}", name, (value == null ? "null" : value.ToString().ToLower())));
        }

        void WriteBool(StringBuilder sb, bool divider, object value)
        {
            if (sb.Length > 0 && divider)
                sb.Append(',');

            sb.Append(value == null ? "null" : value.ToString().ToLower());
        }

        void WriteStruct(StringBuilder sb, bool divider, string name, object o)
        {
            if (o == null)
            {
                WriteNull(sb, divider, name);
                return;
            }
                          
            var pt = o.GetType();
            var isEnum = pt.IsEnum;

            if (isEnum)
                pt = pt.GetEnumUnderlyingType();

            if (pt == ConfigurationCache.type_int || pt == ConfigurationCache.type_byte || pt == ConfigurationCache.type_decimal || pt == ConfigurationCache.type_short || pt == ConfigurationCache.type_long || pt == ConfigurationCache.type_float || pt == ConfigurationCache.type_double)
            {
                WriteNumber(sb, divider, name, isEnum ? Convert.ChangeType(o, pt) : o);
                return;
            }

            if (pt == ConfigurationCache.type_string)
            {
                WriteString(sb, divider, name, isEnum ? Convert.ChangeType(o, pt) : o);
                return;
            }

            if (pt == ConfigurationCache.type_bool)
            {
                WriteBool(sb, divider, name, isEnum ? Convert.ChangeType(o, pt) : o);
                return;
            }

            if (pt == ConfigurationCache.type_datetime)
            {
                WriteString(sb, divider, name, ConverDateTimeToString((DateTime)o));
                return;
            }

            if (pt == ConfigurationCache.type_guid)
            {
                WriteString(sb, divider, name, o == null ? null : o.ToString());
                return;
            }

            if (pt == ConfigurationCache.type_datetime_null)
            {
                var tmp_date = o as Nullable<DateTime>;
                if (tmp_date == null)
                {
                    WriteNull(sb, divider, name);
                    return;
                }
                WriteString(sb, divider, name, ConverDateTimeToString(tmp_date.Value));
                return;
            }

            if (pt == ConfigurationCache.type_bool_null)
            {
                WriteNullable(sb, divider, o, 3, name);
                return;
            }

            if (pt == ConfigurationCache.type_byte_null)
            {
                WriteNullable(sb, divider, o, 1, name);
                return;
            }

            if (pt == ConfigurationCache.type_int_null)
            {
                WriteNullable(sb, divider, o, 1, name);
                return;
            }

            if (pt == ConfigurationCache.type_decimal_null)
            {
                WriteNullable(sb, divider, o, 1, name);
                return;
            }

            if (pt == ConfigurationCache.type_short_null)
            {
                WriteNullable(sb, divider, o, 1, name);
                return;
            }

            if (pt == ConfigurationCache.type_long_null)
            {
                WriteNullable(sb, divider, o, 1, name);
                return;
            }

            if (pt == ConfigurationCache.type_float_null)
            {
                WriteNullable(sb, divider, o, 1, name);
                return;
            }

            if (pt == ConfigurationCache.type_double_null)
            {
                WriteNullable(sb, divider, o, 1, name);
                return;
            }

            if (pt == ConfigurationCache.type_guid_null)
            {
                WriteNullable(sb, divider, o, 2, name);
                return;
            }
        }

        void WriteStruct(StringBuilder sb, bool divider, object o)
        {
            if (o == null)
            {
                WriteNull(sb, divider);
                return;
            }

            var pt = o.GetType();

            if (pt == ConfigurationCache.type_int || pt == ConfigurationCache.type_byte || pt == ConfigurationCache.type_decimal || pt == ConfigurationCache.type_short || pt == ConfigurationCache.type_long || pt == ConfigurationCache.type_float || pt == ConfigurationCache.type_double)
            {
                WriteNumber(sb, divider, o);
                return;
            }

            if (pt == ConfigurationCache.type_string)
            {
                WriteString(sb, divider, o);
                return;
            }

            if (pt == ConfigurationCache.type_bool)
            {
                WriteBool(sb, divider, o);
                return;
            }

            if (pt == ConfigurationCache.type_datetime)
            {
                WriteString(sb, divider, ConverDateTimeToString((DateTime)o));
                return;
            }

            if (pt == ConfigurationCache.type_guid)
            {
                WriteString(sb, divider, o == null ? null : o.ToString());
                return;
            }

            if (pt == ConfigurationCache.type_datetime_null)
            {
                var tmp_date = o as Nullable<DateTime>;
                if (tmp_date == null)
                {
                    WriteNull(sb, divider);
                    return;
                }
                WriteString(sb, divider, ConverDateTimeToString(tmp_date.Value));
                return;
            }

            if (pt == ConfigurationCache.type_bool_null)
            {
                WriteNullable(sb, divider, o, 3);
                return;
            }

            if (pt == ConfigurationCache.type_byte_null)
            {
                WriteNullable(sb, divider, o, 1);
                return;
            }

            if (pt == ConfigurationCache.type_int_null)
            {
                WriteNullable(sb, divider, o, 1);
                return;
            }

            if (pt == ConfigurationCache.type_decimal_null)
            {
                WriteNullable(sb, divider, o, 1);
                return;
            }

            if (pt == ConfigurationCache.type_short_null)
            {
                WriteNullable(sb, divider, o, 1);
                return;
            }

            if (pt == ConfigurationCache.type_long_null)
            {
                WriteNullable(sb, divider, o, 1);
                return;
            }

            if (pt == ConfigurationCache.type_float_null)
            {
                WriteNullable(sb, divider, o, 1);
                return;
            }

            if (pt == ConfigurationCache.type_double_null)
            {
                WriteNullable(sb, divider, o, 1);
                return;
            }

            if (pt == ConfigurationCache.type_guid_null)
            {
                WriteNullable(sb, divider, o, 2);
                return;
            }
        }

        void WriteNullable(StringBuilder sb, bool divider, object value, int type)
        {
            if (value == null)
            {
                WriteNull(sb, divider);
                return;
            }

            var val = value.GetType().GetProperty("Value").GetValue(value, null);

            switch (type)
            {
                case 1:
                    WriteNumber(sb, divider, val);
                    return;
                case 2:
                    WriteString(sb, divider, val);
                    return;
                case 3:
                    WriteBool(sb, divider, val);
                    return;
            }
        }

        void WriteNullable(StringBuilder sb, bool divider, object value, int type, string name)
        {
            if (value == null)
            {
                WriteNull(sb, divider, name);
                return;
            }

            var val = value.GetType().GetProperty("Value").GetValue(value, null);

            switch (type)
            {
                case 1:
                    WriteNumber(sb, divider, name, val);
                    return;
                case 2:
                    WriteString(sb, divider, name, val);
                    return;
                case 3:
                    WriteBool(sb, divider, name, val);
                    return;
            }
        }

        void WriteClass(StringBuilder sb, bool divider, string name, object value)
        {
            if (value == null)
            {
                WriteNull(sb, divider, name);
                return;
            }

            var t = value.GetType();
            if (t.GetInterface("IList") != null)
            {
                var arr = value as IList;
                var obj = new object[arr.Count];

                for (var i = 0; i < arr.Count; i++)
                    obj[i] = arr[i];

                WriteArray(sb, divider, name, obj);
                return;
            }

            if (t.GetInterface("IEnumerable") != null)
            {
                var arr = value as IEnumerable;
                var lst = new List<Object>();

                foreach (var obj in arr)
                    lst.Add(obj);

                WriteArray(sb, divider, name, lst.ToArray());
                return;
            }

            if (sb.Length > 0 && divider)
                sb.Append(',');

            if (string.IsNullOrEmpty(name))
                sb.Append('{');
            else
                sb.Append(string.Format("\"{0}\":{1}", name, '{'));

            var div = false;

            JsonCache cache = null;

            if (!cacheList.TryGetValue(t, out cache))
            {
                cache = new JsonCache();
                cache.Properties = t.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public);
                cache.Attributes = new Dictionary<PropertyInfo, Tuple<string, bool, bool, bool>>(2);
                cacheList.TryAdd(t, cache);
            }

            foreach (var p in cache.Properties)
            {
                Tuple<string, bool, bool, bool> attr;

                if (!cache.Attributes.TryGetValue(p, out attr))
                {
                    attr = GetAttribute(p);
                    cache.Attributes.Add(p, attr);
                }

                if (!attr.Item4 || !attr.Item3)
                    continue;

                var nazov = attr.Item1;

                if (p.PropertyType.IsClass)
                {
                    if (p.PropertyType == ConfigurationCache.type_string)
                        WriteString(sb, div, nazov, p.GetValue(value, null));
                    else
                        WriteClass(sb, div, nazov, p.GetValue(value, null));
                }
                else
                {
                    if (p.PropertyType.GetInterface("IEnumerable") != null)
                        WriteClass(sb, div, nazov, p.GetValue(value, null));
                    else
                        WriteStruct(sb, div, nazov, p.GetValue(value, null));
                }

                if (!div)
                    div = true;
            }
            sb.Append('}');
        }

        void WriteArray(StringBuilder sb, bool divider, string name, object value)
        {

            if (value == null)
            {
                WriteNull(sb, divider, name);
                return;
            }

            if (sb.Length > 0 && divider)
                sb.Append(',');

            var arr = value as Array;
            if (arr == null)
                return;

            if (!string.IsNullOrEmpty(name))
                sb.Append(string.Format("\"{0}\":[", name));

            var div = false;

            foreach (var i in arr)
            {
                if (i == null)
                {
                    WriteNull(sb, div);
                    div = true;
                    continue;
                }

                var t = i.GetType();

                if (t.IsClass && t != ConfigurationCache.type_string)
                    WriteClass(sb, div, null, i);
                else
                    WriteStruct(sb, div, i);

                div = true;
            }

            if (!string.IsNullOrEmpty(name))
                sb.Append(']');
        }

        public string Serialize(object o, params string[] withoutProperty)
        {
            var t = o.GetType();
            var sb = new StringBuilder();

            var isList = t.GetInterface("IList") != null;
            var isEnum = !isList && t.GetInterface("IEnumerable") != null;

            if (t.IsArray || isList || isEnum)
            {
                sb.Append('[');

                if (!t.IsGenericType)
                {
                    WriteArray(sb, false, null, o);
                    return sb.Append(']').ToString();
                }

                if (isList)
                {
                    // ilist
                    var arr = o as IList;
                    object[] obj = null;

                    if (arr != null)
                    {
                        obj = new object[arr.Count];
                        for (var i = 0; i < arr.Count; i++)
                            obj[i] = arr[i];
                    }

                    WriteArray(sb, false, null, obj);
                    return sb.Append(']').ToString();
                }

                if (isEnum)
                {
                    // enumereable
                    var arr = o as IEnumerable;
                    var l = new List<object>(10);

                    foreach (var obj in arr)
                        l.Add(obj);

                    WriteArray(sb, false, null, l.ToArray());
                    return sb.Append(']').ToString();
                }

                return sb.ToString();
            }

            if (!t.IsClass || t == ConfigurationCache.type_string)
            {
                WriteStruct(sb, false, o);
                return sb.ToString();
            }

            sb.Append('{');

            var divider = false;
            foreach (var p in t.GetProperties())
            {
                var pt = p.PropertyType;

                if (withoutProperty != null && withoutProperty.Length > 0 && withoutProperty.Contains(pt.Name))
                    continue;

                var attr = GetAttribute(p);

                if (!attr.Item4 || !attr.Item3)
                    continue;

                if ((!pt.IsClass && pt.GetInterface("IEnumerable") == null) || pt == ConfigurationCache.type_string)
                    WriteStruct(sb, divider, attr.Item1, p.GetValue(o, null));
                else
                {
                    if (pt.IsArray)
                        WriteArray(sb, divider, p.Name, p.GetValue(o, null));
                    else if (t.GetInterface("IList") != null)
                    {
                        var arr = p.GetValue(o, null) as IList;
                        object[] obj = null;

                        if (arr != null)
                        {
                            obj = new object[arr.Count];
                            for (var i = 0; i < arr.Count; i++)
                                obj[i] = arr[i];
                        }
                        WriteArray(sb, divider, attr.Item1, obj);

                    }
                    else if (t.GetInterface("IEnumerable") != null)
                    {
                        var arr = p.GetValue(o, null) as IEnumerable;

                        var l = new List<Object>();
                        foreach (var obj in arr)
                            l.Add(obj);

                        WriteArray(sb, divider, attr.Item1, l.ToArray());
                    }
                    else
                        WriteClass(sb, divider, attr.Item1, p.GetValue(o, null));
                }

                if (!divider)
                    divider = true;
            }

            sb.Append('}');
            return sb.ToString();
        }
        #endregion

        #region DeSerialization
        List<JsonValue> Parser(string value)
        {
            var v = new List<JsonValue>(10);
            var n = "";
            var i = 0;
            foreach (var j in SearchValue.Search(new StringBuilder(value)))
            {
                if (i++ % 2 == 0)
                {
                    n = j.Value;
                    continue;
                }

                v.Add(new JsonValue { Name = n, Value = j.Value, IsArrayClass = j.IsArrayClass, IsString = j.IsString });
            }
            return v;
        }

        List<string> ParserArray(string value)
        {
            var l = new List<string>(10);

            if (value == "[]")
                return l;

            var JeVnorene = false;
            var indexBeg = 0;
            var counter = 0;
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c == '{' && !JeVnorene)
                {
                    JeVnorene = true;
                    indexBeg = i;
                    counter = 0;
                    continue;
                }

                if (c == '{' && JeVnorene)
                {
                    counter++;
                    continue;
                }

                if (c == '}')
                {
                    counter--;
                    if (counter == -1)
                    {
                        JeVnorene = false;
                        l.Add(value.Substring(indexBeg, ((i + 1) - indexBeg)));
                    }
                    continue;
                }
            }
            return l;
        }

        List<string> ParserArrayValue(string value)
        {
            var l = new List<string>(10);

            if (value == "[]")
                return l;

            var index = 0;
            var beg = 1;
            var end = 0;
            var skip = false;
            char c;

            if (value.IndexOf('"') <= 0)
            {
                l.AddRange(value.Replace("[", "").Replace("]", "").Split(','));
                return l;
            }

            var countStr = 0;
            while (index < value.Length)
            {
                c = value[index++];

                if (skip)
                {
                    skip = false;
                    continue;
                }

                if (c == '\\')
                {
                    skip = true;
                    continue;
                }

                if (c == '\"')
                {
                    if (countStr == 0)
                    {
                        countStr++;
                        beg = index;
                        continue;
                    }

                    end = index - 1;
                    l.Add(value.Substring(beg, end - beg));
                    beg = end;
                    countStr = 0;
                }
            }

            return l;
        }

        object GetValue(string v, Type t, bool isArray = true)
        {
            if (v == "null")
                return null;

            if (t == ConfigurationCache.type_int || t == ConfigurationCache.type_int_null)
                return Utils.To<int>(v);

            if (t == ConfigurationCache.type_bool || t == ConfigurationCache.type_bool_null)
                return Utils.To<bool>(v);

            if (t == ConfigurationCache.type_string)
                return isArray ? FromSafeString(v) : FromSafeString(Utils.To<string>(v.Substring(0, v.Length)));

            if (t == ConfigurationCache.type_datetime || t == ConfigurationCache.type_datetime_null)
                return ConvertStringToDateTime(v);

            if (t == ConfigurationCache.type_byte || t == ConfigurationCache.type_byte_null)
                return Utils.To<byte>(v);

            if (t == ConfigurationCache.type_decimal || t == ConfigurationCache.type_decimal_null)
                return Utils.To<decimal>(v);

            if (t == ConfigurationCache.type_float || t == ConfigurationCache.type_float_null)
                return Utils.To<float>(v);

            if (t == ConfigurationCache.type_short || t == ConfigurationCache.type_short_null)
                return Utils.To<short>(v);

            if (t == ConfigurationCache.type_long || t == ConfigurationCache.type_long_null)
                return Utils.To<long>(v);

            if (t == ConfigurationCache.type_double || t == ConfigurationCache.type_double_null)
                return Utils.To<double>(v);

            if (t == ConfigurationCache.type_float || t == ConfigurationCache.type_float_null)
                return Utils.To<float>(v);

            if (t == ConfigurationCache.type_guid || t == ConfigurationCache.type_guid_null) {
                Guid g;
                if (Guid.TryParse(v, out g))
                    return g;

                if (t == ConfigurationCache.type_guid_null)
                    return null;
                return Guid.Empty;
            }

            return null;
        }

        JsonValue Find(string name, IEnumerable<JsonValue> values)
        {
            return values.FirstOrDefault(n => n.Name == name);
        }

        object ParseArray(Type t, string jsonValue)
        {


            if (jsonValue.StartsWith("[{", StringComparison.InvariantCulture) && !t.IsGenericType)
            {
                // objekt array
                var values = ParserArray(jsonValue);
                var obj = Array.CreateInstance(t.GetElementType(), values.Count);

                for (var i = 0; i < values.Count; i++)
                    obj.SetValue(ParseObject(t.GetElementType(), values[i]), i);

                return obj;
            }

            if (jsonValue[0] == '[' && !t.IsGenericType)
            {

                // classic array
                var values = ParserArrayValue(jsonValue);
                var obj = Array.CreateInstance(t.GetElementType(), values.Count);

                for (var i = 0; i < values.Count; i++)
                    obj.SetValue(GetValue(values[i], t.GetElementType()), i);

                return obj;

            }

            if (jsonValue.StartsWith("[{", StringComparison.InvariantCulture) && t.IsGenericType)
            {
                var obj = Activator.CreateInstance(t) as IList;
                var values = ParserArray(jsonValue);

                foreach (var v in values)
                    obj.Add(ParseObject(t.GetGenericArguments()[0], v));

                return obj;
            }

            if (jsonValue[0] == '[' && t.IsGenericType)
            {
                // list
                var obj = Activator.CreateInstance(t) as IList;
                foreach (var v in ParserArrayValue(jsonValue))
                    obj.Add(GetValue(v, t.GetGenericArguments()[0]));

                return obj;
            }

            return null;
        }

        object ParseObject(Type t, string JV)
        {
            var o = Activator.CreateInstance(t);
            var values = Parser(JV);

            JsonValue item = null;
            JsonCache cache;

            if (!cacheList.TryGetValue(t, out cache))
            {
                cache = new JsonCache();
                cache.Properties = t.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public);
                cache.Attributes = new Dictionary<PropertyInfo, Tuple<string, bool, bool, bool>>(2);
                cacheList.TryAdd(t, cache);
            }

            foreach (var p in cache.Properties)
            {
                Tuple<string, bool, bool, bool> attr;

                if (!cache.Attributes.TryGetValue(p, out attr))
                {
                    attr = GetAttribute(p);
                    cache.Attributes.Add(p, attr);
                }

                var name = attr.Item1;
                if (!attr.Item4 || !attr.Item2)
                    continue;

                if (!(p.PropertyType.IsArray || p.PropertyType.IsClass) || p.PropertyType == ConfigurationCache.type_string)
                {
                    item = Find(name, values);
                    if (item != null)
                        p.SetValue(o, GetValue(item.Value, p.PropertyType, false), null);
                    continue;
                }

                if (p.PropertyType.IsArray || p.PropertyType.IsGenericType)
                {
                    item = Find(name, values);
                    if (item != null && item.IsArrayClass)
                        p.SetValue(o, item.Value == "null" ? null : ParseArray(p.PropertyType, item.Value), null);
                }
                else
                {
                    item = Find(name, values);
                    if (item != null)
                    {
                        if (item.Value != "null")
                            p.SetValue(o, ParseObject(p.PropertyType, item.Value), null);
                    }
                }
            }

            return o;
        }

        public dynamic Deserialize(string value)
        {
            if (value != null && !value.StartsWith("[{", StringComparison.InvariantCulture) && value.StartsWith("[", StringComparison.InvariantCulture))
                return ParserArrayValue(value);

            var values = Parser(value);

            var obj = new ExpandoObject();
            var dic = obj as IDictionary<string, object>;

            foreach (var j in values)
            {
                if (j.IsString)
                {
                    dic.Add(j.Name, FromSafeString(j.Value));
                    continue;
                }

                if (j.Value == "false" || j.Value == "true")
                {
                    dic.Add(j.Name, j.Value == "true");
                    continue;
                }

                if (j.Value == "null" || j.Value == "{}")
                {
                    dic.Add(j.Name, null);
                    continue;
                }

                if (j.Value == "[]")
                {
                    dic.Add(j.Name, new string[0]);
                    continue;
                }

                if (j.Value.StartsWith("[{", StringComparison.InvariantCulture))
                {
                    var arr = ParserArray(j.Value);
                    var arrDynamic = new List<dynamic>(arr.Count);
                    foreach (var a in arr)
                        arrDynamic.Add(Deserialize(a));
                    dic.Add(j.Name, arrDynamic);
                    continue;
                }

                if (j.Value[0] == '[')
                {
                    dic.Add(j.Name, ParserArrayValue(j.Value));
                    continue;
                }

                if (j.Value[0] == '{')
                {
                    dic.Add(j.Name, Deserialize(j.Value));
                    continue;
                }

                if (j.Value.IndexOf('.') > 0)
                    dic.Add(j.Name, j.Value.To<float>());
                else
                    dic.Add(j.Name, j.Value.To<int>());
            }

            return obj;
        }

        public T Deserialize<T>(string value)
        {
            var t = typeof(T);

            if (value[0] == '[' && (t.IsGenericType || t.IsArray))
                return (T)ParseArray(t, value);

            return (T)ParseObject(t, value);
        }

        public object DeserializeType(string value, Type type)
        {
            var t = type;

            if (value[0] == '[' && (t.IsGenericType || t.IsArray))
                return ParseArray(t, value);

            return ParseObject(t, value);
        }
        #endregion

        #region Utils
        private Tuple<string, bool, bool, bool> GetAttribute(PropertyInfo t)
        {
            var name = t.Name;
            var use = true;
            var read = true;
            var write = true;

            foreach (var p in t.GetCustomAttributes(false))
            {
                var type = p.GetType();
                if (type == typeof(NoJsonParameterAttribute) || type == typeof(NonSerializedAttribute))
                {
                    use = false;
                    break;
                }
            }

            if (!use)
                return new Tuple<string, bool, bool, bool>(name, read, write, use);

            foreach (var p in t.GetCustomAttributes(typeof(JsonParameterAttribute), false))
            {
                foreach (var a in p.GetType().GetProperties())
                {
                    var n = a.Name.ToLower();
                    object obj = a.GetValue(p, null);

                    if (obj == null)
                        continue;

                    switch (n)
                    {
                        case "name":
                            name = obj.ToString();
                            break;
                        case "read":
                            read = (bool)obj;
                            break;
                        case "write":
                            write = (bool)obj;
                            break;
                    }
                }
            }

            return new Tuple<string, bool, bool, bool>(name, read, write, use);
        }

        private string ConverDateTimeToString(DateTime d)
        {
            var uni = d.ToUniversalTime();
            return string.Format("{0}T{1}Z", uni.ToString("yyyy-MM-dd"), uni.ToString("HH:mm:ss.ms"));
        }

        private string ToSafeString(string s)
        {
            var sb = new StringBuilder();
            foreach (var c in s)
            {
                var code = Convert.ToInt32(c);

                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        continue;
                    case '/':
                        sb.Append("\\/");
                        continue;
                    case '\n':
                        sb.Append("\\n");
                        continue;
                    case '\r':
                        continue;
                    case '\t':
                        sb.Append("\\t");
                        continue;
                    case '"':
                        sb.Append("\\\"");
                        continue;
                }

                if (!Configuration.JsonUnicode)
                {
                    sb.Append(c);
                    continue;
                }

                if (code > 192 || code == 64)
                {
                    sb.Append("\\u" + code.ToString("X").PadLeft(4, '0'));
                    continue;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        private string FromSafeString(string s)
        {
            if (s.IndexOf("\\u", StringComparison.Ordinal) > -1)
                s = Utils.UnicodeDecode(s);
            return s.Replace("\\/", "/").Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"");
        }

        private DateTime ConvertStringToDateTime(string s)
        {
            DateTime d;
            if (DateTime.TryParse(s, out d))
                return d.ToLocalTime();
            return DateTime.Now;
        }
        #endregion

    }

}
