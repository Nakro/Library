using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Dynamic;
using System.Globalization;
using System.ComponentModel;

using Library;

namespace Library.DatabaseUtils
{

    #region Parameter
    public sealed class Parameter
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public int Size { get; set; }
        public byte Scale { get; set; }
        public byte Precision { get; set; }
        public SqlDbType Type { get; set; }

        internal bool IsAttributDeclared { get; set; }
        public bool IsSizeDeclared { get; set; }
        public bool Json { get; set; }

        public Parameter(string name, System.Type propertyType, object value, int size, SqlDbType? type, byte precision = 10, bool json = false, byte scale = 2)
        {
            this.Name = name;
            this.Value = value;
            this.Size = size;
            this.Scale = scale;
            this.Precision = precision;
            this.Json = json;

            if (Size > 0)
                IsSizeDeclared = true;

            if (type != null)
            {
                this.Type = type.Value;
                return;
            }

            if (propertyType == ConfigurationCache.type_object)
            {
                if (value != null)
                    propertyType = value.GetType();
            }

            this.Type = DbUtils.ToType(propertyType);
        }

        public static IList<Parameter> CreateFromDictionary<T>(IDictionary<string, object> dictionary)
        {
            if (dictionary == null)
                return new List<Parameter>(0);

            var p = typeof(T).GetProperties();
            var l = new List<Parameter>(p.Length);

            foreach (var i in p)
            {
                object value = null;

                if (!dictionary.TryGetValue(i.Name, out value))
                    continue;

                SqlDbType? type = null;
                var h = false;
                var size = 0;
                var name = i.Name;
                byte precision = 0;
                byte scale = 0;
                var json = false;

                foreach (var j in i.GetCustomAttributes(ConfigurationCache.type_dbparameter, false))
                {
                    h = true;

                    foreach (var g in j.GetType().GetProperties())
                    {
                        var v = g.GetValue(j, null);
                        if (v == null)
                            continue;

                        switch (g.Name)
                        {
                            case "Size":
                                size = (int)v;
                                break;
                            case "Precision":
                                precision = (byte)v;
                                break;
                            case "Scale":
                                scale = (byte)v;
                                break;
                            case "Type":
                                type = (SqlDbType)v;
                                if (type == SqlDbType.Variant)
                                    type = null;
                                break;
                            case "Json":
                                json = (bool)v;
                                break;
                        }
                    }

                    break;
                }

                if (!i.PropertyType.IsGenericType || ((i.PropertyType != ConfigurationCache.type_string && i.PropertyType != ConfigurationCache.type_nullable) && i.DeclaringType.IsClass))
                    l.Add(new Parameter(i.Name, i.PropertyType, value, size, h ? type : null, precision, json, scale));
            }

            return l;
        }

        public static IList<Parameter> CreateFromArray(object[] o, params string[] disabledPropertyName)
        {
            if (o == null || o.Length == 0)
                return new List<Parameter>(0);

            var l = new List<Parameter>(o.Length);

            foreach (var p in o)
            {
                foreach (var i in p.GetType().GetProperties())
                {
                    if (disabledPropertyName.Contains(i.Name))
                        continue;

                    SqlDbType? type = null;
                    var h = false;
                    var size = 0;
                    var name = i.Name;
                    var json = false;
                    byte precision = 0;
                    byte scale = 0;

                    foreach (var j in i.GetCustomAttributes(ConfigurationCache.type_dbparameter, false))
                    {
                        h = true;
                        foreach (var g in j.GetType().GetProperties())
                        {
                            var v = g.GetValue(j, null);
                            if (v == null)
                                continue;

                            switch (g.Name)
                            {
                                case "Size":
                                    size = (int)v;
                                    break;
                                case "Precision":
                                    precision = (byte)v;
                                    break;
                                case "Scale":
                                    scale = (byte)v;
                                    break;
                                case "Type":
                                    type = (SqlDbType)v;
                                    if (type == SqlDbType.Variant)
                                        type = null;
                                    break;
                                case "Json":
                                    json = (bool)v;
                                    break;
                            }

                        }
                        break;
                    }

                    if (!i.PropertyType.IsGenericType || ((i.PropertyType != ConfigurationCache.type_string && i.PropertyType != ConfigurationCache.type_nullable) && i.DeclaringType.IsClass))
                        l.Add(new Parameter(name, i.PropertyType, i.GetValue(o, null), size, h ? type : null, precision, json, scale));
                }
            }
            return l;
        }

        public static IList<Parameter> CreateFromObject(object o, params string[] disabledPropertyName)
        {
            if (o == null)
                return new List<Parameter>(0);

            if (o.GetType() == typeof(SqlBuilder))
            {
                var b = (o as SqlBuilder);
                if (b.HasParameter)
                    return b.Parameters.ToArray();
                else
                    return null;
            }

            if (o.GetType() == typeof(List<Parameter>))
            {
                var b = (o as List<Parameter>);
                return b.ToArray();
            }

            var p = o.GetType().GetProperties();
            var l = new List<Parameter>(p.Length);

            foreach (var i in p)
            {
                if (disabledPropertyName.Contains(i.Name))
                    continue;

                SqlDbType? type = null;
                var h = false;
                var size = 0;
                var name = i.Name;
                var json = false;
                byte precision = 0;
                byte scale = 0;

                foreach (var j in i.GetCustomAttributes(ConfigurationCache.type_dbparameter, false))
                {
                    h = true;
                    foreach (var g in j.GetType().GetProperties())
                    {
                        var v = g.GetValue(j, null);
                        if (v == null)
                            continue;

                        switch (g.Name)
                        {
                            case "Size":
                                size = (int)v;
                                break;
                            case "Scale":
                                scale = (byte)v;
                                break;
                            case "Precision":
                                precision = (byte)v;
                                break;
                            case "Type":
                                type = (SqlDbType)v;
                                if (type == SqlDbType.Variant)
                                    type = null;
                                break;
                            case "Json":
                                json = (bool)v;
                                break;
                        }
                    }
                    break;
                }

                if (!i.PropertyType.IsGenericType || ((i.PropertyType != ConfigurationCache.type_string && i.PropertyType != ConfigurationCache.type_nullable) && i.DeclaringType.IsClass))
                    l.Add(new Parameter(name, i.PropertyType, i.GetValue(o, null), size, h ? type : null, precision, json, scale));

            }
            return l;
        }
    }
    #endregion

    #region Utils
    internal class DbUtils
    {
        public static string GetTableName(Type t)
        {
            var name = t.Name;
            var schema = "";

            foreach (var i in t.GetCustomAttributes(ConfigurationCache.type_dbtable, false))
            {
                foreach (var j in i.GetType().GetProperties())
                {
                    var v = j.GetValue(i, null);
                    if (v == null)
                        continue;

                    switch (j.Name)
                    {
                        case "Name":
                            name = v.ToString();
                            break;
                        case "Schema":
                            schema = v.ToString();
                            break;
                    }
                }
            }

            if (string.IsNullOrEmpty(schema))
            {
                foreach (var i in t.GetCustomAttributes(ConfigurationCache.type_dbschema, false))
                {
                    foreach (var j in i.GetType().GetProperties())
                    {
                        var v = j.GetValue(i, null);
                        if (v == null)
                            continue;

                        if (j.Name == "Schema")
                            schema = v.ToString();
                    }
                }
            }

            if (string.IsNullOrEmpty(schema))
                return name;

            return schema + '.' + name;
        }

        public static SqlDbType ToType(Type type)
        {
            if (type == ConfigurationCache.type_int || type == ConfigurationCache.type_int_null)
                return SqlDbType.Int;

            if (type == ConfigurationCache.type_byte || type == ConfigurationCache.type_byte_null)
                return SqlDbType.TinyInt;

            if (type == ConfigurationCache.type_bool || type == ConfigurationCache.type_bool_null)
                return SqlDbType.Bit;

            if (type == ConfigurationCache.type_char || type == ConfigurationCache.type_char_null)
                return SqlDbType.Char;

            if (type == ConfigurationCache.type_string)
                return SqlDbType.VarChar;

            if (type == ConfigurationCache.type_short || type == ConfigurationCache.type_short_null)
                return SqlDbType.SmallInt;

            if (type == ConfigurationCache.type_decimal || type == ConfigurationCache.type_decimal_null)
                return SqlDbType.Decimal;

            if (type == ConfigurationCache.type_datetime || type == ConfigurationCache.type_datetime_null)
                return SqlDbType.DateTime;

            if (type == ConfigurationCache.type_float || type == ConfigurationCache.type_float_null)
                return SqlDbType.Float;

            if (type == ConfigurationCache.type_double || type == ConfigurationCache.type_double_null)
                return SqlDbType.Float;

            if (type == ConfigurationCache.type_long || type == ConfigurationCache.type_long_null)
                return SqlDbType.BigInt;

            if (type == ConfigurationCache.type_uint || type == ConfigurationCache.type_uint_null)
                return SqlDbType.Int;

            if (type == ConfigurationCache.type_ushort || type == ConfigurationCache.type_ushort_null)
                return SqlDbType.SmallInt;

            if (type == ConfigurationCache.type_ulong || type == ConfigurationCache.type_ulong_null)
                return SqlDbType.BigInt;

            if (type == ConfigurationCache.type_byte_array)
                return SqlDbType.VarBinary;

            if (type == ConfigurationCache.type_guid || type == ConfigurationCache.type_guid_null)
                return SqlDbType.UniqueIdentifier;

            return SqlDbType.Variant;
        }

        internal static string Append(string dbName, string raw, string name)
        {
            if (!string.IsNullOrEmpty(raw))
                return string.Format("{0} AS [{0}]", raw, name);

            if (dbName == name)
                return string.Format("[{0}]", name);

            return string.Format("[{0}] AS [{1}]", dbName, name);
        }

        internal static List<Column> GetColumnName(Type t)
        {
            var tl = t.GetProperties();
            var l = new List<Column>(tl.Length);

            foreach (var i in tl)
            {
                var name = i.Name;
                var update = true;
                var insert = true;
                var primary = false;
                var select = true;
                var isUpdate = true;
                var isInsert = true;
                var json = false;

                string raw = null;
                SqlDbType dbType = ToType(i.PropertyType);

                if (i.GetCustomAttributes(ConfigurationCache.type_nodbparameter, false).Length > 0)
                    continue;

                if (i.GetCustomAttributes(ConfigurationCache.type_dbskip, false).Length > 0)
                    continue;

                foreach (var j in i.GetCustomAttributes(ConfigurationCache.type_dbparameter, false))
                {
                    foreach (var g in j.GetType().GetProperties())
                    {
                        var v = g.GetValue(j, null);
                        if (v == null)
                            continue;

                        if (g.Name == "Name")
                        {
                            name = (string)v;
                            continue;
                        }

                        if (g.Name == "Json")
                        {
                            json = (bool)v;
                            continue;
                        }

                        if (g.Name == "Update")
                        {
                            update = (bool)v;
                            continue;
                        }

                        if (g.Name == "Insert")
                        {
                            insert = (bool)v;
                            continue;
                        }

                        if (g.Name == "IsInsert")
                        {
                            isInsert = (bool)v;
                            continue;
                        }

                        if (g.Name == "IsUpdate")
                        {
                            isUpdate = (bool)v;
                            continue;
                        }

                        if (g.Name == "Select")
                        {
                            select = (bool)v;
                            continue;
                        }

                        if (g.Name == "PrimaryKey" && v != null)
                        {
                            primary = (bool)v;
                            continue;
                        }

                        if (g.Name == "Raw")
                        {
                            raw = (string)v;
                            continue;
                        }

                        if (g.Name == "Type" && (SqlDbType)v != SqlDbType.Variant)
                        {
                            dbType = (SqlDbType)v;
                            continue;
                        }
                    }
                }

                if (primary && !isInsert)
                    insert = false;

                if (primary && !isUpdate)
                    update = false;

                l.Add(new Column() { Name = i.Name, DbName = name, Update = update, Insert = insert, PrimaryKey = primary, Raw = raw, Select = select, Type = dbType, Json = json });
            }

            return l;
        }
    }
    #endregion

    #region Column
    public class Column
    {
        public string Name { get; set; }
        public string DbName { get; set; }
        public string Raw { get; set; }

        [DefaultValue(true)]
        public bool Update { get; set; }

        [DefaultValue(true)]
        public bool Insert { get; set; }

        [DefaultValue(true)]
        public bool Select { get; set; }

        public bool Json { get; set; }
        public bool PrimaryKey { get; set; }

        public SqlDbType Type { get; set; }
    }
    #endregion

}
