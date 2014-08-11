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

using Library.DatabaseUtils;

namespace Library
{

    #region Enums
    public enum ReaderCursor
    {
        Continue,
        // pokračuje
        Skip,
        // preskočí
        Break,
        // ukončí while
        Close
        // ukončí while aj pri nextResult
    }

    public enum OrderByType : byte
    {
        Asc = 0,
        Desc = 1
    }
    #endregion

    #region Interfaces
    public interface ISql<T>
    {
        string TableName { get; set; }

        ISql<T> Use(Action<ISql<T>> declaration);

        IDatabase DB { get; }

        IList<string> Columns { get; }

        string PrimaryKey { get; }

        bool AutoLocking { get; set; }

        int Execute(string sql);

        int Execute(string sql, object arg);

        object Scalar(string sql);

        object Scalar(string sql, object arg);

        object Scalar(string columnName, SqlBuilder builder);

        IEnumerable<T> Reader(string sql, bool singleRow = false, int skip = 0, int tak = 0);

        IEnumerable<T> Reader(string sql, object arg, bool singleRow = false, int skip = 0, int tak = 0);

        IEnumerable<T> GetAll(params OrderBy[] orderBy);

        IEnumerable<T> GetAll(string[] disabledPropertyName, params OrderBy[] orderBy);

        IEnumerable<T> GetAll(int top, params OrderBy[] orderBy);

        IEnumerable<T> GetAll(int top, string[] disabledPropertyName, params OrderBy[] orderBy);

        IEnumerable<T> GetAll(int skip, int take, params OrderBy[] orderBy);

        IEnumerable<T> GetAll(int skip, int take, string[] disabledPropertyName, params OrderBy[] orderBy);

        IEnumerable<T> FindAll(SqlBuilder builder, params OrderBy[] orderBy);

        IEnumerable<T> FindAll(SqlBuilder builder, int skip, int take, params OrderBy[] orderBy);

        IEnumerable<T> FindAll(SqlBuilder builder, string[] disabledPropertyName, params OrderBy[] orderBy);

        IEnumerable<T> FindAll(SqlBuilder builder, int skip, int take, string[] disabledPropertyName, params OrderBy[] orderBy);

        IEnumerable<T> FindTop(int top, SqlBuilder builder, params OrderBy[] orderBy);

        IEnumerable<T> FindTop(int top, SqlBuilder builder, string[] disabledPropertyName, params OrderBy[] orderBy);

        T FindByPK(object primaryKey, params string[] disabledPropertyName);

        T FindOne(SqlBuilder builder, params OrderBy[] orderBy);

        T FindOne(SqlBuilder builder, string[] disabledPropertyName, params OrderBy[] orderBy);

        int Count();

        int Count(SqlBuilder builder);

        bool Exists();

        bool Exists(SqlBuilder builder);

        object Insert(object arg, params string[] disabledPropertyName);

        int Update(string where, object arg, params string[] disabledPropertyName);

        int Update(string where, object arg, object whereArg, params string[] disabledPropertyName);

        int Update(SqlBuilder builder, object arg, params string[] disabledPropertyName);

        int UpdateOnly(string where, object arg, params string[] propertyName);

        int UpdateOnly(string where, object arg, object whereArg, params string[] propertyName);

        int UpdateOnly(SqlBuilder builder, object arg, params string[] propertyName);

        bool Save(object arg);

        int Delete(object arg);

        int Delete(SqlBuilder builder);

        int Delete(string where, object arg);
    }

    public interface ISqlUnknown
    {
        IDatabase DB { get; }

        int Execute(string sql);

        int Execute(string sql, object arg);

        object Scalar(string sql);

        object Scalar(string sql, object arg);

        IEnumerable<dynamic> Reader(string sql, bool singleRow = false, int skip = 0, int take = 0);

        IEnumerable<dynamic> Reader(string sql, object arg, bool singleRow = false, int skip = 0, int take = 0);

        bool ReaderBinary(string sql, object arg, Action<byte[]> onReadBinary);

        bool ReaderBinary(string sql, Action<byte[]> onReadBinary);

        void ReaderMultiple(string sql, object arg, Func<int, dynamic, ReaderCursor> onRead);

        void ReaderMultiple(string sql, Func<int, dynamic, ReaderCursor> onRead);
    }

    public interface IConnection<T> where T : class, IDisposable
    {
        T Instance { get; }

        bool IsOpened { get; }

        void Open(string connString);

        void Open(string connString, IsolationLevel? isoLevel);

        void Close();
    }

    public interface ICommand
    {
        bool Prepare { get; set; }

        int Execute(string sql, IList<Parameter> parameters);

        object Scalar(string sql, IList<Parameter> parameters);

        IEnumerable<T> ReaderEnumerator<T>(string sql, IList<Parameter> parameters, Func<string, DatabaseColumnMap> onColumnMap, bool singleRow = false);

        IEnumerable<dynamic> ReaderEnumerator(string sql, IList<Parameter> parameters, bool singleRow = false);

        bool ReaderBinary(string sql, IList<Parameter> parameters, Action<byte[]> onReadBinary);

        void ReaderMultiple(string sql, IList<Parameter> parameters, Func<int, dynamic, ReaderCursor> onRead);
    }

    public interface ITransaction
    {
        void TransactionBegin(IsolationLevel isoLevel);

        void TransactionRollback();

        void TransactionCommit();
    }

    public interface IDatabase : ICommand, ITransaction, IDisposable
    {
        int Version { get; }
        bool IsOleDB { get; }
    }
    #endregion

    #region Attributes
    [AttributeUsage(AttributeTargets.Property)]
    public class DbParameterAttribute : Attribute
    {
        private SqlDbType type = SqlDbType.Variant;
        private int update = 2;
        private int insert = 2;
        private int select = 2;
        private byte precision = 0;
        private int size = 0;

        public int Size
        {
            get { return size; }
            set { size = value; }
        }

        public SqlDbType Type
        {
            get { return type; }
            set { type = value; }
        }

        public string Name { get; set; }

        public string Format { get; set; }

        public string Raw { get; set; }

        public byte Precision
        {
            get { return precision; }
            set { precision = value; }
        }

        public bool Update
        {
            get { return update != 0; }
            set { update = value ? 1 : 0; }
        }

        public bool Insert
        {
            get { return insert != 0; }
            set { insert = value ? 1 : 0; }
        }

        public bool Select
        {
            get { return select != 0; }
            set { select = value ? 1 : 0; }
        }

        public bool Json { get; set; }

        public bool IsInsert
        {
            get { return insert != 2; }
        }

        public bool IsUpdate
        {
            get { return update != 2; }
        }

        public bool PrimaryKey { get; set; }

        public DbParameterAttribute()
        {
        }

        public DbParameterAttribute(int size)
        {
            Size = size;
        }

        public DbParameterAttribute(int size, bool isUnicode)
        {
            Size = size;
            if (isUnicode)
                type = SqlDbType.NVarChar;
        }

        public DbParameterAttribute(byte precision, int size)
        {
            Precision = precision;
            Size = size;
        }

        public DbParameterAttribute(SqlDbType type)
        {
            Type = type;
        }

        public DbParameterAttribute(int size, SqlDbType type)
        {
            Size = size;
            Type = type;
        }

        public DbParameterAttribute(bool isPrimary)
        {
            PrimaryKey = isPrimary;
        }

        public DbParameterAttribute(bool isPrimary, bool insert)
        {
            PrimaryKey = isPrimary;
            Insert = insert;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DbTableAttribute : Attribute
    {
        public string Name { get; set; }

        public string Schema { get; set; }

        public DbTableAttribute(string name)
        {
            Name = name;
        }

        public DbTableAttribute(string name, string schema)
        {
            Name = name;
            Schema = schema;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DbSchemaAttribute : Attribute
    {
        public string Schema { get; set; }

        public DbSchemaAttribute(string schema)
        {
            Schema = schema;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class NoDbParameterAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DbSkipAttribute : Attribute
    {

    }
    #endregion

    #region SqlBuilder
    public class SqlBuilder
    {
        private StringBuilder sb = new StringBuilder();
        private Lazy<List<Parameter>> param = new Lazy<List<Parameter>>();

        public List<Parameter> Parameters
        {
            get { return param.IsValueCreated ? param.Value : null; }
        }

        public bool HasValue
        {
            get { return sb.Length > 0; }
        }

        public bool HasParameter
        {
            get { return !param.IsValueCreated ? false : param.Value.Count > 0; }
        }

        public SqlBuilder Clear()
        {
            sb.Length = 0;
            if (param.IsValueCreated)
                param.Value.Clear();
            return this;
        }

        public SqlBuilder AppendOperator(string sqlOperator)
        {
            if (string.IsNullOrEmpty(sqlOperator))
                return this;

            if (sb.Length > 0)
                sb.Append(" " + sqlOperator + " ");

            return this;
        }

        public SqlBuilder Append(string column, string sqlOperator, object value, string schema = "")
        {
            var safe = value == null ? string.Empty : value.ToString();

            if (value.GetType() == ConfigurationCache.type_string)
                safe = safe.Replace("'", "''");

            return AppendSql(string.Format("{0}[{1}] {2} {3}", schema != "" ? "[" + schema + "]." : "", column, sqlOperator, safe));
        }

        public SqlBuilder AppendSql(string sql, params object[] values)
        {
            if (values != null && values.Length > 0)
                sb.Append(string.Format(Configuration.InvariantCulture, sql, values));
            else
                sb.Append(sql);
            return this;
        }

        public SqlBuilder AppendParameter(string column, string sqlOperator, object value)
        {
            var pn = "_" + column + (param == null ? "0" : (param.Value.Count + 1).ToString());
            return AppendSqlParameter(string.Format("[{0}] {1} @{2}", column, sqlOperator, pn), pn, value);
        }

        public SqlBuilder And()
        {
            return AppendOperator("AND");
        }

        public SqlBuilder Or()
        {
            return AppendOperator("OR");
        }

        public SqlBuilder AppendSqlParameter(string sql, string parameterName, object value)
        {
            if (string.IsNullOrEmpty(sql))
                return this;

            param.Value.Add(new Parameter(parameterName, (value == null ? ConfigurationCache.type_dbnull : value.GetType()), value, 0, null));
            sb.Append(sql);

            return this;
        }

        public SqlBuilder AppendParameter(string parameterName, object value)
        {
            param.Value.Add(new Parameter(parameterName, (value == null ? ConfigurationCache.type_dbnull : value.GetType()), value, 0, null));
            return this;
        }

        public SqlBuilder AppendLike(string name, string value, string schema = "", string format = "%{0}%")
        {
            var paramName = "param" + param.Value.Count;
            return AppendSqlParameter(string.Format("{0}[{1}] LIKE @{2}", schema != "" ? "[" + schema + "]." : "", name, paramName), paramName, string.Format(format, value.Replace(' ', '%')));
        }

        public override string ToString()
        {
            return sb.ToString();
        }

        public SqlBuilder AppendBuilder(SqlBuilder builder)
        {
            if (!builder.HasValue)
                return this;

            if (builder.Parameters != null)
            {
                foreach (var i in builder.Parameters)
                    param.Value.Add(i);
            }

            return AppendSql(builder.ToString());
        }

        public SqlBuilder AppendBuilder(StringBuilder builder)
        {
            return AppendSql(builder.ToString());
        }

        public SqlBuilder ParameterAdd(string name, object value)
        {
            return ParameterAdd(name, value, 0);
        }

        public SqlBuilder ParameterAdd(string name, object value, int size)
        {
            param.Value.Add(new Parameter(name, value.GetType(), value, size, null));
            return this;
        }

        public string ToString(bool appendWhere)
        {
            if (HasValue)
                return (appendWhere ? " WHERE " : "") + this;
            return "";
        }
    }
    #endregion

    #region OrderBy
    public class OrderBy
    {
        public string Name { get; set; }

        public OrderByType Order { get; set; }

        public static OrderBy Asc(string columnName)
        {
            return new OrderBy { Name = columnName, Order = OrderByType.Asc };
        }

        public static OrderBy Desc(string columnName)
        {
            return new OrderBy {
                Name = columnName,
                Order = OrderByType.Desc
            };
        }

        public static OrderBy Create(string columnName, bool asc)
        {
            if (asc)
                return OrderBy.Asc(columnName);
            return OrderBy.Desc(columnName);
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Name, Order);
        }
    }
    #endregion

    #region Database (SqlServer)
    public class DatabaseColumnMap
    {
        public string DbName { get; set; }

        public string Name { get; set; }

        public bool Json { get; set; }

        public DatabaseColumnMap(string dbName, string name, bool json = false)
        {
            DbName = dbName;
            Name = name;
            Json = json;
        }
    }

    public class DatabaseColumns
    {
        public StringBuilder Clean { get; set; }

        public StringBuilder Raw { get; set; }

        public string OrderBy { get; set; }
    }

    public class Database : IConnection<Database>, IDatabase, IDisposable
    {
        private const int BUFFER_SIZE = 1024;

        public bool IsOpened { get; private set; }
        public bool IsOleDB
        {
            get { return false; }
        }

        public int Version
        {
            get
            {
                return connection.ServerVersion.Substring(0, connection.ServerVersion.IndexOf('.')).To<int>();
            }
        }

        private SqlCommand cmd = null;
        private string _sql = "";

        private bool _prepare = false;

        internal SqlConnection connection = null;
        internal SqlTransaction transaction = null;

        public static ConcurrentDictionary<string, Column[]> TableColumnCache = new ConcurrentDictionary<string, Column[]>(3, 15);

        public static Column[] GetTableColumnCache(Type t)
        {
            Column[] columns;

            if (TableColumnCache.TryGetValue(t.FullName, out columns))
                return columns;

            columns = DbUtils.GetColumnName(t).ToArray();
            TableColumnCache.TryAdd(t.FullName, columns);

            return columns;
        }

        public bool Prepare
        {
            get { return _prepare; }
            set { _prepare = value; }
        }

        public static void ClearCache()
        {
            TableColumnCache.Clear();
        }

        public Database(string connectionString)
        {
            Open(connectionString, null);
        }

        public Database(string connectionString, IsolationLevel? isoLevel)
        {
            Open(connectionString, isoLevel);
        }

        public Database Instance
        {
            get { return this; }
        }

        internal void _error(Exception ex)
        {
            throw new Exception(_sql, ex);
        }

        public void Open(string connString, IsolationLevel? isoLevel)
        {
            try
            {
                var connstring = connString;

                if (connString.Length > 30)
                    connstring = connString;
                else
                {
                    var conn = System.Configuration.ConfigurationManager.ConnectionStrings[connString];
                    if (conn == null)
                        connstring = connString;
                    else
                        connstring = conn.ConnectionString;
                }

                connection = new SqlConnection(connstring);
                connection.Open();
                connection.StatisticsEnabled = false;

                cmd = new SqlCommand();
                cmd.Connection = connection;
                cmd.NotificationAutoEnlist = false;

                if (isoLevel != null)
                    TransactionBegin(isoLevel.Value);

                IsOpened = true;
            } catch (Exception Ex)
            {
                _error(Ex);
            }
        }

        public void Open(string connString)
        {
            Open(connString, null);
        }

        public void TransactionBegin(IsolationLevel isoLevel)
        {
            if (transaction != null)
            {
                transaction.Dispose();
                transaction = null;
            }
            transaction = connection.BeginTransaction(isoLevel);
            cmd.Transaction = transaction;
        }

        public void TransactionRollback()
        {
            try
            {
                transaction.Rollback();
            } catch (Exception Ex)
            {
                Instance._error(Ex);
            }
        }

        public void TransactionCommit()
        {
            try
            {
                transaction.Commit();
            } catch (Exception Ex)
            {
                Instance._error(Ex);
            }
        }

        public void Close()
        {
            if (!IsOpened)
                return;
            try
            {
                cmd.Dispose();
                Instance.connection.Close();
            } catch (Exception Ex)
            {
                Instance._error(Ex);
            }
        }

        public void Dispose()
        {
            Close();
        }

        public int Execute(string sql, IList<Parameter> parameters)
        {
            int v = 0;

            try
            {

                if (sql != _sql)
                {
                    cmd.CommandText = sql;
                    ParamToParam(cmd, parameters);
                    _sql = sql;

                    if (_prepare)
                        cmd.Prepare();
                } else
                    ParamToParam(cmd, parameters, true);

                v = cmd.ExecuteNonQuery();
            } catch (Exception Ex)
            {
                _error(Ex);
            }

            return v;
        }

        public object Scalar(string sql, IList<Parameter> parameters)
        {
            object v;

            try
            {
                if (sql != _sql)
                {
                    cmd.CommandText = sql;
                    ParamToParam(cmd, parameters);
                    _sql = sql;

                    if (_prepare)
                        cmd.Prepare();
                } else
                    ParamToParam(cmd, parameters, true);

                v = cmd.ExecuteScalar();
            } catch (Exception ex)
            {
                _error(ex);
                v = null;
            }

            return v;
        }

        public IEnumerable<dynamic> ReaderEnumerator(string sql, IList<Parameter> parameters, bool singleRow = false)
        {
            if (sql != _sql)
            {
                cmd.CommandText = sql;
                ParamToParam(cmd, parameters);
                _sql = sql;

                if (_prepare)
                    cmd.Prepare();
            } else
                ParamToParam(cmd, parameters, true);

            using (var reader = cmd.ExecuteReader(singleRow ? CommandBehavior.SingleRow : CommandBehavior.SingleResult))
            {
                if (reader.HasRows)
                {
                    // dátum vynecháme ako ValueType, pretože pri default Activator jebe dátum od počiatku dátumu 01.01.0001
                    var date = ConfigurationCache.type_datetime;
                    while (reader.Read())
                    {
                        var obj = new ExpandoObject();
                        var dic = obj as IDictionary<string, object>;
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var value = reader.GetValue(i);
                            if (value == DBNull.Value || value == null)
                            {
                                var t = reader.GetFieldType(i);
                                dic.Add(reader.GetName(i), t.IsValueType && t != date ? Activator.CreateInstance(t) : null);
                                continue;
                            }

                            dic.Add(reader.GetName(i), value);
                        }
                        yield return obj;
                    }
                }
                reader.Close();
            }
        }

        public void ReaderMultiple(string sql, IList<Parameter> parameters, Func<int, dynamic, ReaderCursor> onRead)
        {
            if (sql != _sql)
            {
                cmd.CommandText = sql;
                ParamToParam(cmd, parameters);
                _sql = sql;

                if (_prepare)
                    cmd.Prepare();
            } else
                ParamToParam(cmd, parameters, true);

            using (var reader = cmd.ExecuteReader(CommandBehavior.Default))
            {
                var index = 0;
                var skip = false;

                do
                {
                    if (reader.HasRows)
                    {

                        // dátum vynecháme ako ValueType, pretože pri default Activator jebe dátum od počiatku dátumu 01.01.0001
                        var date = ConfigurationCache.type_datetime;
                        while (reader.Read())
                        {

                            if (skip)
                            {
                                skip = false;
                                continue;
                            }

                            var obj = new ExpandoObject();
                            var dic = obj as IDictionary<string, object>;
                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                var value = reader.GetValue(i);
                                if (value == DBNull.Value || value == null)
                                {
                                    var t = reader.GetFieldType(i);
                                    dic.Add(reader.GetName(i), t.IsValueType && t != date ? Activator.CreateInstance(t) : null);
                                    continue;
                                }

                                dic.Add(reader.GetName(i), value);
                            }

                            var result = onRead(index, obj);
                            if (result == ReaderCursor.Break)
                                break;

                            if (result == ReaderCursor.Close)
                                return;

                            if (result == ReaderCursor.Skip)
                                skip = true;
                        }
                    }

                    index++;
                } while (reader.NextResult());
                reader.Close();
            }
        }

        public bool ReaderBinary(string sql, IList<Parameter> parameters, Action<byte[]> onReadBinary)
        {
            if (sql != _sql)
            {
                cmd.CommandText = sql;
                ParamToParam(cmd, parameters);
                _sql = sql;

                if (_prepare)
                    cmd.Prepare();
            } else
                ParamToParam(cmd, parameters, true);

            using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
            {
                var has = reader.HasRows;
                if (!has)
                {
                    reader.Close();
                    return has;
                }

                while (reader.Read())
                {
                    var buffer = new byte[BUFFER_SIZE];
                    long readBytes;
                    long readIndex = 0;
                    readBytes = reader.GetBytes(0, readIndex, buffer, 0, BUFFER_SIZE);

                    while (readBytes == BUFFER_SIZE)
                    {
                        onReadBinary(buffer);
                        readIndex += BUFFER_SIZE;
                        readBytes = reader.GetBytes(0, readIndex, buffer, 0, BUFFER_SIZE);
                    }

                    onReadBinary(buffer);
                    break;
                }

                reader.Close();
                return has;
            }
        }

        public IEnumerable<T> ReaderEnumerator<T>(string sql, IList<Parameter> parameters, Func<string, DatabaseColumnMap> onColumnMap, bool singleRow = false)
        {
            if (sql != _sql)
            {
                cmd.CommandText = sql;
                ParamToParam(cmd, parameters);
                _sql = sql;

                if (_prepare)
                    cmd.Prepare();
            } else
                ParamToParam(cmd, parameters, true);

            using (var reader = cmd.ExecuteReader(singleRow ? CommandBehavior.SingleRow : CommandBehavior.SingleResult))
            {
                var l = new List<DatabaseColumnMap>(reader.FieldCount);

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var item = onColumnMap(reader.GetName(i));
                    if (item != null)
                        l.Add(item);
                }

                if (l.Count == 0)
                {
                    reader.Close();
                    yield break;
                }

                if (!reader.HasRows)
                {
                    reader.Close();
                    yield break;
                }

                var typ = typeof(T);
                while (reader.Read())
                {
                    var obj = Activator.CreateInstance(typ);

                    foreach (var i in l)
                    {
                        var value = reader[i.DbName];
                        object bindValue = null;

                        if (i.Json)
                        {
                            if (value != DBNull.Value)
                                bindValue = Configuration.JsonProvider.DeserializeObject(value as string, typ);
                        } else if (value != DBNull.Value)
                            bindValue = value;

                        typ.GetProperty(i.Name).SetValue(obj, bindValue, null);
                    }

                    yield return (T)obj;
                }

                reader.Close();
            }
        }

        private void ParamToParam(SqlCommand cmd, IList<Parameter> parameters, bool updateValues = false)
        {
            if (parameters == null)
                return;

            if (!updateValues)
            {
                cmd.Parameters.Clear();
                foreach (var p in parameters)
                {
                    var v = cmd.Parameters.Add(new SqlParameter(p.Name, p.Type, p.Size));

                    if (p.Type == SqlDbType.Decimal)
                    {
                        if (p.Precision > 0)
                            v.Precision = p.Precision;

                        if (p.Size > 0)
                            v.Scale = (byte)p.Size;
                    }

                    if (p.Json)
                    {
                        if (p.Type == SqlDbType.Variant)
                            p.Type = SqlDbType.VarChar;

                        v.Value = p.Value == null ? "null" : Configuration.JsonProvider.Serialize(p.Value);
                        continue;
                    }

                    v.Value = p.Value ?? DBNull.Value;
                }

                return;
            }

            foreach (var p in parameters)
            {
                var param = cmd.Parameters[p.Name];
                if (param == null)
                {
                    var v = cmd.Parameters.Add(new SqlParameter(p.Name, p.Type, p.Size));

                    if (p.Type == SqlDbType.Decimal)
                    {
                        if (p.Precision > 0)
                            v.Precision = p.Precision;
                        if (p.Size > 0)
                            v.Scale = (byte)p.Size;
                        continue;
                    }

                    if (p.Json)
                    {
                        if (p.Type == SqlDbType.Variant)
                            v.SqlDbType = SqlDbType.VarChar;

                        v.Value = p.Value == null ? "null" : Configuration.JsonProvider.Serialize(p.Value);
                        SetupString(v, p.IsSizeDeclared);
                        continue;
                    }

                    if (p.Type == SqlDbType.NVarChar || p.Type == SqlDbType.VarChar)
                    {
                        v.Value = p.Value;
                        SetupString(v, p.IsSizeDeclared);
                        continue;
                    }

                    v.Value = (p.Value ?? DBNull.Value);
                    continue;
                }

                if (p.Json)
                {
                    param.Value = p.Value == null ? "null" : Configuration.JsonProvider.Serialize(p.Value);
                    SetupString(param, p.IsSizeDeclared);
                    continue;
                }

                if (p.Type == SqlDbType.NVarChar || p.Type == SqlDbType.VarChar)
                {
                    param.Value = p.Value;
                    SetupString(param, p.IsSizeDeclared);
                } else
                {
                    // TODO: neviem na čo to tu je
                    // param.Size = p.Size;
                    param.Value = p.Value ?? DBNull.Value;
                }
            }
        }

        private void SetupString(SqlParameter param, bool sizeDeclared)
        {
            var str = param.Value as string;

            if (string.IsNullOrEmpty(str))
            {
                param.Value = DBNull.Value;
                return;
            }

            if (sizeDeclared)
            {
                param.Value = str.Max(param.Size);
                return;
            }

            param.Size = str.Length;
        }
    }
    #endregion

    #region SqlUnknown
    public class SqlUnknown : ISqlUnknown
    {
        private IDatabase db = null;

        public IDatabase DB
        {
            get { return db; }
        }

        public SqlUnknown(IDatabase db)
        {
            this.db = db;
        }

        public int Execute(string sql)
        {
            return db.Execute(sql, null);
        }

        public int Execute(string sql, object arg)
        {
            return db.Execute(sql, Parameter.CreateFromObject(arg));
        }

        public object Scalar(string sql)
        {
            return db.Scalar(sql, null);
        }

        public object Scalar(string sql, object arg)
        {
            return db.Scalar(sql, Parameter.CreateFromObject(arg));
        }

        public T Scalar<T>(string sql)
        {
            return Utils.To<T>(db.Scalar(sql, null));
        }

        public T Scalar<T>(string sql, object arg)
        {
            return Utils.To<T>(db.Scalar(sql, Parameter.CreateFromObject(arg)));
        }

        public bool ReaderBinary(string sql, object arg, Action<byte[]> onReadBinary)
        {
            return db.ReaderBinary(sql, Parameter.CreateFromObject(arg), onReadBinary);
        }

        public bool ReaderBinary(string sql, Action<byte[]> onReadBinary)
        {
            return db.ReaderBinary(sql, null, onReadBinary);
        }

        public IEnumerable<dynamic> Reader(string sql, bool singleRow = false, int skip = 0, int take = 0)
        {
            return Reader(sql, null, singleRow, skip, take);
        }

        public IEnumerable<dynamic> Reader(string sql, object arg, bool singleRow = false, int skip = 0, int take = 0)
        {
            if (skip > 0 || take > 0)
            {
                if (DB.Version < 11)
                    throw new Exception("SQL Server doesn't support effective pagination.");
                sql += string.Format(" OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY", skip, take);
            }
            return db.ReaderEnumerator(sql, Parameter.CreateFromObject(arg), singleRow);
        }

        public void ReaderMultiple(string sql, object arg, Func<int, dynamic, ReaderCursor> onRead)
        {
            db.ReaderMultiple(sql, Parameter.CreateFromObject(arg), onRead);
        }

        public void ReaderMultiple(string sql, Func<int, dynamic, ReaderCursor> onRead)
        {
            db.ReaderMultiple(sql, null, onRead);
        }
    }
    #endregion

    #region Sql
    public class Sql<T> : ISql<T>
    {
        public string TableName { get; set; }

        public bool AutoLocking { get; set; }

        private IDatabase db = null;
        private Column[] columns;

        public IList<string> Columns
        {
            get
            {
                var l = new List<string>(Columns.Count);
                foreach (var m in columns)
                    l.Add(m.Name);
                return l;
            }
        }

        public string PrimaryKey
        {
            get
            {
                foreach (var m in columns)
                    if (m.PrimaryKey)
                        return m.Name;
                return null;
            }
        }

        public IDatabase DB
        {
            get { return db; }
        }

        public Sql(IDatabase db)
        {
            this.db = db;
            TableName = DbUtils.GetTableName(typeof(T));
            AutoLocking = true;
            columns = Database.GetTableColumnCache(typeof(T));
        }

        public Sql(IDatabase db, bool autoLocking)
        {
            this.db = db;
            TableName = DbUtils.GetTableName(typeof(T));
            AutoLocking = autoLocking;
            columns = Database.GetTableColumnCache(typeof(T));
        }

        public ISql<T> Use(Action<ISql<T>> declaration)
        {
            declaration(this);
            return this;
        }

        private string GetTableNameSelect
        {
            get { return TableName + (AutoLocking ? " WITH (NOLOCK)" : ""); }
        }

        private string GetTableNameUpdate
        {
            get { return TableName + (AutoLocking ? " WITH (ROWLOCK)" : ""); }
        }

        public int Execute(string sql)
        {
            return db.Execute(sql, null);
        }

        public int Execute(string sql, object arg)
        {
            return db.Execute(sql, Parameter.CreateFromObject(arg));
        }

        public object Scalar(string sql)
        {
            return db.Scalar(sql, null);
        }

        public object Scalar(string sql, object arg)
        {
            return db.Scalar(sql, Parameter.CreateFromObject(arg));
        }

        public IEnumerable<T> Reader(string sql, bool singleRow = false, int skip = 0, int take = 0)
        {
            return Reader(sql, null, singleRow, skip, take);
        }

        public IEnumerable<T> Reader(string sql, object arg, bool singleRow = false, int skip = 0, int take = 0)
        {
            if (skip > 0 || take > 0)
            {
                if (DB.Version < 11)
                    throw new Exception("SQL Server doesn't support effective pagination.");
                sql += string.Format(" OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY", skip, take);
            }

            return ReaderParam(sql, Parameter.CreateFromObject(arg), singleRow);
        }

        public IEnumerable<T> ReaderParam(string sql, IList<Parameter> arg, bool singleRow = false)
        {
            return db.ReaderEnumerator<T>(sql, arg, Mapping, singleRow);
        }

        private DatabaseColumnMap Mapping(string name)
        {
            var c = columns.FirstOrDefault(m => m.DbName == name);

            if (c == null)
                return null;

            return new DatabaseColumnMap(c.DbName, c.Name, c.Json);
        }

        public object Scalar(string columnName, SqlBuilder builder)
        {
            return db.Scalar(string.Format("SELECT {0} FROM {1}{2}", columnName, GetTableNameSelect, builder.ToString(true)), builder.Parameters);
        }

        public R Scalar<R>(string columnName, SqlBuilder builder)
        {
            return Utils.To<R>(db.Scalar(string.Format("SELECT {0} FROM {1}{2}", columnName, GetTableNameSelect, builder.ToString(true)), builder.Parameters));
        }

        public T FindByPK(object primaryKey, params string[] disablePropertyName)
        {
            var sb = new StringBuilder();
            string p = null;

            foreach (var s in columns)
            {
                if (!s.Select && !s.PrimaryKey)
                    continue;

                if (disablePropertyName.Contains(s.DbName) || disablePropertyName.Contains(s.Name))
                    continue;

                sb.Append((sb.Length > 0 ? "," : "") + (string.IsNullOrEmpty(s.Raw) ? s.DbName : s.Raw + " AS " + s.DbName));
                if (s.PrimaryKey)
                    p = s.DbName;
            }

            if (string.IsNullOrEmpty(p))
                throw new Exception("Primary key is not implemented.");

            return Reader(string.Format("SELECT TOP 1 {0} FROM {1} WHERE {2}=@Id", sb, GetTableNameSelect, p), new { Id = primaryKey }, true).FirstOrDefault();
        }

        public IEnumerable<T> GetAll(params OrderBy[] orderBy)
        {
            return GetAll(null, orderBy);
        }

        public IEnumerable<T> GetAll(string[] disablePropertyName, params OrderBy[] orderBy)
        {
            return Reader(string.Format("SELECT {0} FROM {1} {2}", GetColumns(disablePropertyName), GetTableNameSelect, OrderByCreate(orderBy)));
        }

        public IEnumerable<T> GetAll(int top, params OrderBy[] orderBy)
        {
            return GetAll(top, null, orderBy);
        }

        public IEnumerable<T> GetAll(int top, string[] disablePropertyName, params OrderBy[] orderBy)
        {
            return Reader(string.Format("SELECT TOP {3} {0} FROM {1} {2}", GetColumns(disablePropertyName), GetTableNameSelect, OrderByCreate(orderBy), top), top == 1);
        }

        public IEnumerable<T> GetAll(int skip, int take, params OrderBy[] orderBy)
        {
            return GetAll(skip, take, null, orderBy);
        }

        public IEnumerable<T> GetAll(int skip, int take, string[] disablePropertyName, params OrderBy[] orderBy)
        {
            var r = GetColumns(OrderByCreate(orderBy, true), disablePropertyName);

            if (db.Version > 10)
                return Reader(string.Format("SELECT {0} FROM {1} ORDER BY {2} OFFSET {3} ROWS FETCH NEXT {4} ROWS ONLY", r.Raw, GetTableNameSelect, r.OrderBy, skip, take));

            return Reader(string.Format("SELECT TOP {0} {5} FROM (SELECT ROW_NUMBER() OVER (ORDER BY {3}) As rowindex, {1} FROM {2}) As _query WHERE _query.rowindex>{4} ORDER BY _query.rowindex", take, r.Clean, GetTableNameSelect, r.OrderBy, skip, r.Raw));
        }

        public IEnumerable<T> FindAll(SqlBuilder builder, params OrderBy[] orderBy)
        {
            return FindAll(builder, null, orderBy);
        }

        public IEnumerable<T> FindAll(SqlBuilder builder, string[] disablePropertyName, params OrderBy[] orderBy)
        {
            return ReaderParam(string.Format("SELECT {0} FROM {1}{2} {3}", GetColumns(disablePropertyName), GetTableNameSelect, builder.ToString(true), OrderByCreate(orderBy)), builder.Parameters);
        }

        public IEnumerable<T> FindAll(SqlBuilder builder, int skip, int take, params OrderBy[] orderBy)
        {
            return FindAll(builder, skip, take, null, orderBy);
        }

        public IEnumerable<T> FindAll(SqlBuilder builder, int skip, int take, string[] disablePropertyName, params OrderBy[] orderBy)
        {
            var r = GetColumns(OrderByCreate(orderBy, true), disablePropertyName);
            string sql;

            // SQL Server 2012
            if (db.Version > 10)
                sql = string.Format("SELECT {0} FROM {1}{2} ORDER BY {3} OFFSET {4} ROWS FETCH NEXT {5} ROWS ONLY", r.Raw, GetTableNameSelect, builder.ToString(true), r.OrderBy, skip, take);
            else
                sql = string.Format("SELECT TOP {0} {6} FROM (SELECT ROW_NUMBER() OVER (ORDER BY {2}) As rowindex, {1} FROM {3}{4}) As _query WHERE _query.rowindex>{5} ORDER BY _query.rowindex", take, r.Clean, r.OrderBy, GetTableNameSelect, builder.ToString(true), skip, r.Raw);

            return ReaderParam(sql, builder.Parameters);
        }

        public IEnumerable<T> FindTop(int top, SqlBuilder builder, params OrderBy[] orderBy)
        {
            var sql = string.Format("SELECT TOP {3} {0} FROM {1}{2} {4}", GetColumns(), GetTableNameSelect, builder.ToString(true), top, OrderByCreate(orderBy));
            return ReaderParam(sql, builder.Parameters, top == 1);
        }

        public IEnumerable<T> FindTop(int top, SqlBuilder builder, string[] disablePropertyName, params OrderBy[] orderBy)
        {
            var sql = string.Format("SELECT TOP {3} {0} FROM {1}{2} {4}", GetColumns(disablePropertyName), GetTableNameSelect, builder.ToString(true), top, OrderByCreate(orderBy));
            return ReaderParam(sql, builder.Parameters, top == 1);
        }

        public T FindOne(SqlBuilder builder, params OrderBy[] orderBy)
        {
            return FindOne(builder, null, orderBy);
        }

        public T FindOne(SqlBuilder builder, string[] disablePropertyName, params OrderBy[] orderBy)
        {
            var sql = string.Format("SELECT TOP 1 {0} FROM {1}{2} {3}", GetColumns(disablePropertyName), GetTableNameSelect, builder.ToString(true), OrderByCreate(orderBy));
            return ReaderParam(sql, builder.Parameters, true).FirstOrDefault();
        }

        public int Count()
        {
            return Count(null, null);
        }

        private int Count(string where, object arg)
        {
            return Convert.ToInt32(Scalar(string.Format("SELECT COUNT(*) FROM {0}{1}", GetTableNameSelect, (string.IsNullOrEmpty(where) ? "" : " WHERE " + where)), arg));
        }

        public int Count(SqlBuilder builder)
        {
            return Convert.ToInt32(db.Scalar(string.Format("SELECT COUNT(*) FROM {0}{1}", GetTableNameSelect, builder.ToString(true)), builder.Parameters));
        }

        public bool Exists()
        {
            return Count(null, null) > 0;
        }

        public bool Exists(SqlBuilder builder)
        {
            return Convert.ToInt32(db.Scalar(string.Format("SELECT COUNT(*) FROM {0}{1}", GetTableNameSelect, builder.ToString(true)), builder.Parameters)) > 0;
        }

        public int Update(object arg, params string[] disablePropertyName)
        {
            var hodnoty = new StringBuilder();
            Column pk = null;

            var disabled = new Lazy<List<string>>();

            foreach (var s in columns)
            {
                if (pk == null && s.PrimaryKey)
                    pk = s;

                if (!disablePropertyName.Contains(s.Name) && s.Update)
                    hodnoty.Append((hodnoty.Length > 0 ? "," : "") + s.DbName + "=@" + s.DbName);
                else if (!s.PrimaryKey)
                    disabled.Value.Add(s.Name);
            }

            if (pk == null)
                throw new Exception("Primary key is not implemented.");

            return db.Execute(string.Format("UPDATE {0} SET {1} WHERE {2}", GetTableNameUpdate, hodnoty, pk.DbName + "=@" + pk.DbName), disabled.IsValueCreated ? Parameter.CreateFromObject(arg, disabled.Value.ToArray()) : Parameter.CreateFromObject(arg));
        }

        public int Update(string where, object arg, params string[] disablePropertyName)
        {
            return Update(where, arg, null, disablePropertyName);
        }

        public int Update(string where, object arg, object whereArg, params string[] disablePropertyName)
        {
            var values = new StringBuilder();
            var disabled = new Lazy<List<string>>();
            var isFilled = false;

            foreach (var s in columns)
            {
                if (!disablePropertyName.Contains(s.Name) && s.Update)
                {
                    if (isFilled)
                        values.Append(',');
                    isFilled = true;
                    values.Append('[' + s.DbName + "]=@" + s.DbName);
                    continue;
                }

                if (!s.PrimaryKey)
                    disabled.Value.Add(s.Name);
            }

            var p = new List<Parameter>(10);

            if (arg != null && whereArg != null)
            {
                p.AddRange(disabled.IsValueCreated ? Parameter.CreateFromObject(arg, disabled.Value.ToArray()) : Parameter.CreateFromObject(arg));
                p.AddRange(Parameter.CreateFromObject(whereArg));
            } else if (arg != null)
                p.AddRange(disabled.IsValueCreated ? Parameter.CreateFromObject(arg, disabled.Value.ToArray()) : Parameter.CreateFromObject(arg));

            var sql = string.Format("UPDATE {0} SET {1} WHERE {2}", GetTableNameUpdate, values, where);
            return db.Execute(sql, p);
        }

        public int Update(SqlBuilder builder, object arg, params string[] disablePropertyName)
        {
            var values = new StringBuilder();
            var disabled = new Lazy<List<string>>();
            var isFilled = false;

            foreach (var s in columns)
            {
                if (!disablePropertyName.Contains(s.Name) && s.Update)
                {
                    if (isFilled)
                        values.Append(',');
                    isFilled = true;
                    values.Append('[' + s.DbName + "]=@" + s.DbName);
                    continue;
                }

                if (!s.PrimaryKey)
                    disabled.Value.Add(s.Name);
            }

            var p = new List<Parameter>(10);
            p.AddRange(disabled.IsValueCreated ? Parameter.CreateFromObject(arg, disabled.Value.ToArray()) : Parameter.CreateFromObject(arg));
            p.AddRange(builder.Parameters);

            var sql = string.Format("UPDATE {0} SET {1}{2}", GetTableNameUpdate, values, builder.ToString(true));
            return db.Execute(sql, p);
        }

        public int UpdateOnly(object arg, params string[] propertyName)
        {

            if (propertyName == null || propertyName.Length == 0)
                throw new Exception("You must define names of property.");

            var values = new StringBuilder();
            Column pk = null;

            var disabled = new Lazy<List<string>>();
            var isFilled = false;

            foreach (var s in columns)
            {
                if (pk == null && s.PrimaryKey)
                    pk = s;

                if (propertyName.Contains(s.Name) && s.Update)
                {
                    if (isFilled)
                        values.Append(',');

                    isFilled = true;
                    values.Append('[' + s.DbName + "]=@" + s.DbName);
                    continue;
                }

                if (!s.PrimaryKey)
                    disabled.Value.Add(s.Name);
            }

            if (values.Length == 0)
                throw new Exception("You must define names of property.");

            if (pk == null)
                throw new Exception("Primary key is not implemented.");

            return db.Execute(string.Format("UPDATE {0} SET {1} WHERE {2}", GetTableNameUpdate, values, '[' + pk.DbName + "]=@" + pk.DbName), disabled.IsValueCreated ? Parameter.CreateFromObject(arg, disabled.Value.ToArray()) : Parameter.CreateFromObject(arg));
        }

        public int UpdateOnly(string where, object arg, params string[] propertyName)
        {
            return UpdateOnly(where, arg, null, propertyName);
        }

        public int UpdateOnly(string where, object arg, object whereArg, params string[] propertyName)
        {
            if (propertyName == null || propertyName.Length == 0)
                throw new Exception("You must define names of property.");

            var values = new StringBuilder();
            var disabled = new Lazy<List<string>>();
            var isFilled = false;

            foreach (var s in columns)
            {
                if (propertyName.Contains(s.Name) && s.Update)
                {
                    if (isFilled)
                        values.Append(',');

                    isFilled = true;
                    values.Append('[' + s.DbName + "]=@" + s.DbName);
                    continue;
                }

                if (!s.PrimaryKey)
                    disabled.Value.Add(s.Name);
            }

            if (values.Length == 0)
                throw new Exception("You must define names of property.");

            var p = new List<Parameter>(10);

            if (arg != null && whereArg != null)
            {
                p.AddRange(disabled.IsValueCreated ? Parameter.CreateFromObject(arg, disabled.Value.ToArray()) : Parameter.CreateFromObject(arg));
                p.AddRange(Parameter.CreateFromObject(whereArg));
            } else if (arg != null)
                p.AddRange(disabled.IsValueCreated ? Parameter.CreateFromObject(arg, disabled.Value.ToArray()) : Parameter.CreateFromObject(arg));

            var sql = string.Format("UPDATE {0} SET {1} WHERE {2}", GetTableNameUpdate, values, where);
            return db.Execute(sql, p);
        }

        public int UpdateOnly(SqlBuilder builder, object arg, params string[] propertyName)
        {
            if (propertyName == null || propertyName.Length == 0)
                throw new Exception("You must define names of property.");

            var values = new StringBuilder();
            var disabled = new Lazy<List<string>>();
            var isFilled = false;

            foreach (var s in columns)
            {
                if (propertyName.Contains(s.Name) && s.Update)
                {
                    if (isFilled)
                        values.Append(',');

                    isFilled = true;
                    values.Append('[' + s.DbName + "]=@" + s.DbName);
                    continue;
                }

                if (!s.PrimaryKey)
                    disabled.Value.Add(s.Name);
            }

            if (values.Length == 0)
                throw new Exception("You must define names of property.");

            var p = new List<Parameter>(10);
            p.AddRange(disabled.IsValueCreated ? Parameter.CreateFromObject(arg, disabled.Value.ToArray()) : Parameter.CreateFromObject(arg));
            p.AddRange(builder.Parameters);

            var sql = string.Format("UPDATE {0} SET {1}{2}", GetTableNameUpdate, values, builder.ToString(true));
            return db.Execute(sql, p);
        }

        public bool Save(object arg)
        {
            if (Update(arg) == 0)
                return Insert(arg) != null;
            return true;
        }

        public object Insert(object arg, params string[] disablePropertyName)
        {
            var column = new StringBuilder();
            var values = new StringBuilder();
            var isFilled = false;

            Column pk = null;

            foreach (var s in columns)
            {
                if (pk == null && s.PrimaryKey)
                    pk = s;

                if (!disablePropertyName.Contains(s.Name) && s.Insert)
                {
                    if (isFilled)
                    {
                        column.Append(',');
                        values.Append(',');
                    }

                    column.Append(s.DbName);
                    values.Append("@" + s.DbName);
                    isFilled = true;
                }
            }

            var sql = string.Format("INSERT INTO {0} ({1}) VALUES({2}); SELECT @@IDENTITY", GetTableNameUpdate, column, values);

            var v = Scalar(sql, arg);

            if (pk == null)
                return v;

            var prop = arg.GetType().GetProperty(pk.Name);
            var t = prop.PropertyType;

            if (v == DBNull.Value && (pk.Insert || pk.Update))
                v = prop.GetValue(arg, null);

            if (t == ConfigurationCache.type_int)
            {
                prop.SetValue(arg, Convert.ToInt32(v), null);
                return v;
            }

            if (t == ConfigurationCache.type_byte)
            {
                prop.SetValue(arg, Convert.ToByte(v), null);
                return v;
            }

            if (t == ConfigurationCache.type_decimal)
            {
                prop.SetValue(arg, Convert.ToDecimal(v), null);
                return v;
            }

            if (t == ConfigurationCache.type_guid)
            {
                prop.SetValue(arg, v, null);
                return v;
            }

            if (t == ConfigurationCache.type_short)
            {
                prop.SetValue(arg, Convert.ToInt16(v), null);
                return v;
            }

            if (t == ConfigurationCache.type_long)
            {
                prop.SetValue(arg, Convert.ToInt64(v), null);
                return v;
            }

            if (t == ConfigurationCache.type_double)
            {
                prop.SetValue(arg, Convert.ToDouble(v), null);
                return v;
            }

            if (t == ConfigurationCache.type_float)
            {
                prop.SetValue(arg, Convert.ToSingle(v), null);
                return v;
            }

            if (t == ConfigurationCache.type_uint)
            {
                prop.SetValue(arg, Convert.ToUInt32(v), null);
                return v;
            }

            if (t == ConfigurationCache.type_ushort)
            {
                prop.SetValue(arg, Convert.ToUInt16(v), null);
                return v;
            }

            if (t == ConfigurationCache.type_long)
            {
                prop.SetValue(arg, Convert.ToUInt64(v), null);
                return v;
            }

            if (t == ConfigurationCache.type_string)
            {
                if (v != DBNull.Value)
                    prop.SetValue(arg, v, null);
            }

            return v;
        }

        public int Delete(object arg)
        {
            Column pk = null;

            foreach (var s in columns)
            {
                if (pk == null && s.PrimaryKey)
                {
                    pk = s;
                    break;
                }
            }

            if (pk == null)
                throw new Exception("Primary key is not implemented.");

            return db.Execute(string.Format("DELETE FROM {0} WHERE {1}", GetTableNameUpdate, pk.DbName + "=@" + pk.DbName), Parameter.CreateFromObject(arg));
        }

        public int Delete(string where, object arg)
        {
            var sql = string.Format("DELETE FROM {0} WHERE {1}", GetTableNameUpdate, where);
            return Execute(sql, arg);
        }

        public int Delete(SqlBuilder builder)
        {
            var sql = string.Format("DELETE FROM {0}{1}", GetTableNameUpdate, builder.ToString(true));
            return db.Execute(sql, builder.Parameters);
        }

        private DatabaseColumns GetColumns(string orderBy, params string[] disablePropertyName)
        {
            var r = new DatabaseColumns();
            r.Clean = new StringBuilder();
            r.Raw = new StringBuilder();
            r.OrderBy = orderBy;

            var table = TableName + '.';

            foreach (var s in columns)
            {
                if (!s.Select)
                    continue;

                if (disablePropertyName != null && (disablePropertyName.Contains(s.DbName) || disablePropertyName.Contains(s.Name)))
                    continue;

                if (string.IsNullOrEmpty(r.OrderBy))
                    r.OrderBy = table + '[' + s.DbName + ']';

                if (r.Raw.Length > 0)
                    r.Raw.Append(',');

                if (r.Clean.Length > 0)
                    r.Clean.Append(',');

                r.Clean.Append(table + '[' + s.DbName + ']');
                r.Raw.Append(string.IsNullOrEmpty(s.Raw) ? '[' + s.DbName + ']' : s.Raw + " AS " + table + '[' + s.DbName + ']');
            }

            return r;
        }

        private StringBuilder GetColumns(params string[] disablePropertyName)
        {
            var sb = new StringBuilder();
            var isFilled = false;

            foreach (var s in columns)
            {
                if (disablePropertyName != null && (disablePropertyName.Contains(s.DbName) || disablePropertyName.Contains(s.Name)))
                    continue;

                if (!s.Select)
                    continue;

                if (isFilled)
                    sb.Append(',');

                isFilled = true;
                sb.Append(string.IsNullOrEmpty(s.Raw) ? '[' + s.DbName + ']' : s.Raw + " AS [" + s.DbName + ']');
            }
            return sb;
        }

        private string OrderByCreate(OrderBy[] o)
        {
            return OrderByCreate(o, false);
        }

        private string OrderByCreate(OrderBy[] o, bool withoutDeclaration)
        {
            if (o == null)
                return string.Empty;

            var isFilled = false;
            var sb = new StringBuilder();

            foreach (var i in o)
            {
                if (i == null)
                    continue;

                if (isFilled)
                    sb.Append(',');

                isFilled = true;
                sb.Append(string.Format("{0} {1}", i.Name, (i.Order == OrderByType.Asc ? "ASC" : "DESC")));
            }

            if (sb.Length == 0)
                return string.Empty;

            if (withoutDeclaration)
                return sb.ToString();

            return "ORDER BY " + sb;
        }
    }
    #endregion

    #region DbSchema
    public class DbSchema<T>
    {
        private bool _isnew = false;

        public static Sql<T> Sql(IDatabase db)
        {
            return new Sql<T>(db);
        }

        public virtual bool Update(IDatabase db, params string[] disablePropertyName)
        {
            _isnew = false;
            return new Sql<T>(db).Update(this, disablePropertyName) > 0;
        }

        public virtual bool UpdateOnly(IDatabase db, params string[] propertyName)
        {
            _isnew = false;
            return new Sql<T>(db).UpdateOnly(this, propertyName) > 0;
        }

        public virtual bool Insert(IDatabase db, params string[] disablePropertyName)
        {
            return _isnew = (new Sql<T>(db).Insert(this, disablePropertyName) != null);
        }

        public virtual bool Save(IDatabase db, params string[] disablePropertyName)
        {
            var sql = new Sql<T>(db);
            var name = sql.PrimaryKey;

            if (string.IsNullOrEmpty(name))
                throw new Exception("Primary key is not implemented.");

            var type = typeof(T);
            var prop = type.GetProperty(name);
            var value = prop.GetValue(this, null);
            var propType = prop.PropertyType;
            var insert = false;

            if (propType == ConfigurationCache.type_string)
                insert = (value == null || value.ToString().Length == 0);
            else if (propType == ConfigurationCache.type_int)
                insert = (value == null || (int)value == 0);
            else if (propType == ConfigurationCache.type_guid)
                insert = (value == null || (Guid)value == Guid.Empty);
            else if (propType == ConfigurationCache.type_byte)
                insert = (value == null || (byte)value == 0);
            else if (propType == ConfigurationCache.type_short)
                insert = (value == null || (short)value == 0);
            else if (propType == ConfigurationCache.type_long)
                insert = (value == null || (short)value == 0);
            else if (propType == ConfigurationCache.type_uint)
                insert = (value == null || (uint)value == 0);
            else if (propType == ConfigurationCache.type_ulong)
                insert = (value == null || (uint)value == 0);
            else if (propType == ConfigurationCache.type_ushort)
                insert = (value == null || (uint)value == 0);

            if (insert)
                return Insert(db, disablePropertyName);

            var r = Update(db, disablePropertyName);

            if (!r)
                r = Insert(db, disablePropertyName);

            return r;
        }

        public virtual bool Delete(IDatabase db)
        {
            return new Sql<T>(db).Delete(this) > 0;
        }

        public static T Load(IDatabase db, object primaryKey)
        {
            return new Sql<T>(db).FindByPK(primaryKey);
        }

        public static T Load(IDatabase db, SqlBuilder builder)
        {
            return new Sql<T>(db).FindOne(builder);
        }

        public virtual bool Refresh(IDatabase db)
        {
            var sql = new Sql<T>(db);
            var name = sql.PrimaryKey;

            if (string.IsNullOrEmpty(name))
                throw new Exception("Primary key is not implemented.");

            var type = typeof(T);
            var value = type.GetProperty(name).GetValue(this, null);
            var self = sql.FindByPK(value);

            if (self == null)
                return false;

            foreach (var m in sql.Columns)
                type.GetProperty(m).SetValue(this, type.GetProperty(m).GetValue(self, null), null);

            return true;
        }

        public virtual T Duplicate(IDatabase db)
        {
            var sql = new Sql<T>(db);
            var name = sql.PrimaryKey;

            if (string.IsNullOrEmpty(name))
                throw new Exception("Primary key is not implemented.");

            var value = typeof(T).GetProperty(name).GetValue(this, null);
            return sql.FindByPK(value);
        }

        public virtual bool IsNew()
        {
            return _isnew;
        }
    }
    #endregion

}