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
        public byte Precision { get; set; }
        public SqlDbType Type { get; set; }

        internal bool IsAttributDeclared { get; set; }
        public bool IsSizeDeclared { get; set; }

        public Parameter(string name, System.Type propertyType, object value, int size, SqlDbType? type, byte precision = 10)
        {
            this.Name = name;
            this.Value = value;
            this.Size = size;
            this.Precision = precision;

            if (Size > 0)
                IsSizeDeclared = true;

            if (type != null)
            {
                this.Type = type.Value;
                return;
            }

            if (propertyType == typeof(object))
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

                foreach (var j in i.GetCustomAttributes(typeof(Library.DbParameterAttribute), false))
                {
                    h = true;
                    foreach (var g in j.GetType().GetProperties())
                    {
                        var v = g.GetValue(j, null);
                        if (v != null)
                        {
                            if (g.Name == "Size")
                                size = (int)v;
                            else if (g.Name == "Precision")
                                precision = (byte)v;
                            else if (g.Name == "Type")
                                type = (SqlDbType)v;
                        }
                    }
                    break;
                }

                if (!i.PropertyType.IsGenericType || ((i.PropertyType != typeof(string) && i.PropertyType != typeof(Nullable)) && i.DeclaringType.IsClass))
                    l.Add(new Parameter(i.Name, i.PropertyType, value, size, h ? type : null, precision));
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
                    byte precision = 0;

                    foreach (var j in i.GetCustomAttributes(typeof(Library.DbParameterAttribute), false))
                    {
                        h = true;
                        foreach (var g in j.GetType().GetProperties())
                        {
                            var v = g.GetValue(j, null);
                            if (v != null)
                            {
                                if (g.Name == "Size")
                                    size = (int)v;
                                else if (g.Name == "Precision")
                                    precision = (byte)v;
                                else if (g.Name == "Type")
                                    type = (SqlDbType)v;
                            }
                        }
                        break;
                    }

                    if (!i.PropertyType.IsGenericType || ((i.PropertyType != typeof(string) && i.PropertyType != typeof(Nullable)) && i.DeclaringType.IsClass))
                        l.Add(new Parameter(name, i.PropertyType, i.GetValue(o, null), size, h ? type : null, precision));
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
                byte precision = 0;

                foreach (var j in i.GetCustomAttributes(typeof(Library.DbParameterAttribute), false))
                {
                    h = true;
                    foreach (var g in j.GetType().GetProperties())
                    {
                        var v = g.GetValue(j, null);
                        if (v != null)
                        {
                            if (g.Name == "Size")
                                size = (int)v;
                            else if (g.Name == "Precision")
                                precision = (byte)v;
                            else if (g.Name == "Type")
                            {                                
                                type = (SqlDbType)v;
                                if (type == SqlDbType.Variant)
                                    type = null;
                            }
                        }
                    }
                    break;
                }

                if (!i.PropertyType.IsGenericType || ((i.PropertyType != typeof(string) && i.PropertyType != typeof(Nullable)) && i.DeclaringType.IsClass))
                    l.Add(new Parameter(name, i.PropertyType, i.GetValue(o, null), size, h ? type : null, precision));

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
            var n = "";
            foreach (var i in t.GetCustomAttributes(typeof(DbTableAttribute), false))
            {
                foreach (var j in i.GetType().GetProperties())
                {
                    var v = j.GetValue(i, null);
                    if (v != null && j.Name == "Name")
                    {
                        n = (n != "" ? n + '.' + v.ToString() : v.ToString());
                        continue;
                    }

                    if (v != null && j.Name == "Schema")
                    {
                        n = (n != "" ? v.ToString() + '.' + n : v.ToString());
                        continue;
                    }
                }
            }
            return (n == "" ? t.Name : n);
        }

        public static SqlDbType ToType(Type type)
        {
            if (type == typeof(int) || type == typeof(int?))
                return SqlDbType.Int;

            if (type == typeof(byte) || type == typeof(byte?))
                return SqlDbType.TinyInt;

            if (type == typeof(bool) || type == typeof(bool?))
                return SqlDbType.Bit;

            if (type == typeof(char) || type == typeof(char?))
                return SqlDbType.Char;

            if (type == typeof(string))
                return SqlDbType.VarChar;

            if (type == typeof(Int16) || type == typeof(Int16?))
                return SqlDbType.SmallInt;

            if (type == typeof(decimal) || type == typeof(decimal?))
                return SqlDbType.Decimal;

            if (type == typeof(DateTime) || type == typeof(DateTime?))
                return SqlDbType.DateTime;

            if (type == typeof(float) || type == typeof(Single) || type == typeof(float?) || type == typeof(Single?))
                return SqlDbType.Float;

            if (type == typeof(double) || type == typeof(Double?))
                return SqlDbType.Float;

            if (type == typeof(Int64) || type == typeof(Int64?))
                return SqlDbType.BigInt;

            if (type == typeof(UInt32) || type == typeof(UInt32?))
                return SqlDbType.Int;

            if (type == typeof(UInt16) || type == typeof(UInt16?))
                return SqlDbType.SmallInt;

            if (type == typeof(UInt64) || type == typeof(UInt64?))
                return SqlDbType.BigInt;

            if (type == typeof(byte[]))
                return SqlDbType.VarBinary;

            if (type == typeof(Guid) || type == typeof(Guid?))
                return SqlDbType.UniqueIdentifier;

            return SqlDbType.Variant;
        }

        internal static string Append(string dbName, string raw, string name)
        {
            if (string.IsNullOrEmpty(raw))
            {
                if (dbName == name)
                    return string.Format("[{0}]", name);
                else
                    return string.Format("[{0}] AS {1}", dbName, name);
            }
            else
                return raw + " AS " + name;
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

                string raw = null;
                SqlDbType dbType = ToType(i.PropertyType);

                if (i.GetCustomAttributes(typeof(Library.NoDbParameterAttribute), false).Length > 0)
                    continue;

                foreach (var j in i.GetCustomAttributes(typeof(Library.DbParameterAttribute), false))
                {
                    foreach (var g in j.GetType().GetProperties())
                    {
                        var v = g.GetValue(j, null);
                        if (g.Name == "Name")
                        {
                            if (v == null)
                                continue;

                            name = (string)v;
                            continue;
                        }

                        if (g.Name == "Update")
                        {
                            if (v == null)
                                continue;

                            update = (bool)v;
                            continue;
                        }

                        if (g.Name == "Insert")
                        {
                            if (v == null)
                                continue;

                            insert = (bool)v;
                            continue;
                        }

                        if (g.Name == "IsInsert")
                        {
                            if (v == null)
                                continue;

                            isInsert = (bool)v;
                            continue;
                        }

                        if (g.Name == "IsUpdate")
                        {
                            if (v == null)
                                continue;

                            isUpdate = (bool)v;
                            continue;
                        }

                        if (g.Name == "Select")
                        {
                            if (v == null)
                                continue;

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
                            if (v == null)
                                continue;

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

                l.Add(new Column() { Name = i.Name, DbName = name, Update = update, Insert = insert, PrimaryKey = primary, Raw = raw, Select = select, Type = dbType });
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

        public bool PrimaryKey { get; set; }
        public SqlDbType Type { get; set; }
    }
    #endregion

}
