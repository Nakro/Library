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
        Continue,       // pokračuje
        Skip,           // preskočí
        Break,          // ukončí while
        Close           // ukončí while aj pri nextResult
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

        Func<string, object, object> OnBind { get; set; }
        ISql<T> Use(Action<ISql<T>> declaration);
        IDatabase DB { get; }

        IList<string> Columns { get; }

        bool AutoLocking { get; set; }

        int Execute(string sql);
        int Execute(string sql, object arg);

        object Scalar(string sql);
        object Scalar(string sql, object arg);
        object Scalar(string columnName, SqlBuilder builder);

        IEnumerable<T> Reader(string sql, bool singleRow = false);
        IEnumerable<T> Reader(string sql, object arg, bool singleRow = false);

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

        object Insert(object Arg, params string[] disabledPropertyName);

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
        IEnumerable<dynamic> Reader(string sql, bool singleRow = false);
        IEnumerable<dynamic> Reader(string sql, object arg, bool singleRow = false);
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
        IEnumerable<T> ReaderEnumerator<T>(string sql, IList<Parameter> parameters, Func<string, Tuple<string, string, Func<string, object, object>>> onColumnMap, bool singleRow = false);
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

    public interface IDatabase : ICommand, ITransaction { }
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

        public bool IsInsert
        {
            get { return insert != 2; }
        }

        public bool IsUpdate
        {
            get { return update != 2; }
        }

        public bool PrimaryKey { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DbTableAttribute : Attribute
    {
        public string Name { get; set; }
        public string Schema { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class NoDbParameterAttribute : Attribute { }
    #endregion

    #region SqlBuilder
    public class SqlBuilder
    {
        private System.Text.StringBuilder sb = new System.Text.StringBuilder();
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

        public SqlBuilder Append(string column, string sqlOperator, object value)
        {
            return AppendSql(string.Format("{0} {1} {2}", column, sqlOperator, value));
        }

        public SqlBuilder AppendSql(string sql, params object[] values)
        {
            if (values != null && values.Length > 0)
                sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, sql, values));
            else
                sb.Append(sql);
            return this;
        }

        public SqlBuilder AppendParameter(string column, string sqlOperator, object value)
        {
            var pn = "_" + column + (param == null ? "0" : (param.Value.Count + 1).ToString());
            return AppendSqlParameter(string.Format("{0} {1} @{2}", column, sqlOperator, pn), pn, value);
        }

        public SqlBuilder AppendSqlParameter(string sql, string parameterName, object value)
        {
            if (string.IsNullOrEmpty(sql))
                return this;

            param.Value.Add(new Parameter(parameterName, (value == null ? DBNull.Value.GetType() : value.GetType()), value, 0, null));
            sb.Append(sql);

            return this;
        }

        public SqlBuilder AppendParameter(string parameterName, object value)
        {
            param.Value.Add(new Parameter(parameterName, (value == null ? DBNull.Value.GetType() : value.GetType()), value, 0, null));
            return this;
        }

        public SqlBuilder AppendLike(string name, string value, string format = "%{0}%")
        {
            var paramName = "param" + param.Value.Count;
            return AppendSqlParameter(string.Format("{0} LIKE @{1}", name, paramName), paramName, string.Format(format, value.Replace(' ', '%')));
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
                    this.param.Value.Add(i);
            }

            return this.AppendSql(builder.ToString());
        }

        public SqlBuilder AppendBuilder(System.Text.StringBuilder builder)
        {
            return this.AppendSql(builder.ToString());
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
            if (this.HasValue)
                return (appendWhere ? " WHERE " : "") + this.ToString();
            else
                return "";
        }
    }
    #endregion

    #region OrderBy
    public class OrderBy
    {
        public string Name { get; set; }
        public OrderByType Order { get; set; }

        public static OrderBy Asc(string ColumnName)
        {
            return new OrderBy() { Name = ColumnName, Order = OrderByType.Asc };
        }

        public static OrderBy Desc(string ColumnName)
        {
            return new OrderBy() { Name = ColumnName, Order = OrderByType.Desc };
        }

        public static OrderBy Create(string ColumnName, bool Asc)
        {
            if (Asc)
                return OrderBy.Asc(ColumnName);
            else
                return OrderBy.Desc(ColumnName);
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Name, Order);
        }
    }
    #endregion

    #region Database (SqlServer)
    public class Database : IConnection<Database>, IDatabase, IDisposable
    {
        public bool IsOpened { get; private set; }

        private SqlCommand cmd = null;
        private string _sql = "";

        private bool _prepare = false;

        internal SqlConnection connection = null;
        internal SqlTransaction transaction = null;

        public static ConcurrentDictionary<string, Column[]> TableColumnCache = new ConcurrentDictionary<string, Column[]>(3, 15);

        public static Column[] GetTableColumnCache(Type t)
        {
            Column[] columns = null;

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

        public Database(string connectionString, System.Data.IsolationLevel? isoLevel)
        {
            Open(connectionString, isoLevel);
        }

        public Database Instance
        {
            get { return this; }
        }

        internal void _error(Exception ex)
        {
            throw ex;
        }

        public void Open(string connString, System.Data.IsolationLevel? isoLevel)
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
            }
            catch (Exception Ex)
            {
                _error(Ex);
            }
        }

        public void Open(string connString)
        {
            Open(connString, null);
        }

        public void TransactionBegin(System.Data.IsolationLevel isoLevel)
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
            }
            catch (Exception Ex)
            {
                Instance._error(Ex);
            }
        }

        public void TransactionCommit()
        {
            try
            {
                transaction.Commit();
            }
            catch (Exception Ex)
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
            }
            catch (Exception Ex)
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
                    ParamToParam(cmd, parameters, false);
                    _sql = sql;

                    if (_prepare)
                        cmd.Prepare();
                }
                else
                    ParamToParam(cmd, parameters, true);

                v = cmd.ExecuteNonQuery();
            }
            catch (Exception Ex)
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
                    ParamToParam(cmd, parameters, false);
                    _sql = sql;

                    if (_prepare)
                        cmd.Prepare();
                }
                else
                    ParamToParam(cmd, parameters, true);

                v = cmd.ExecuteScalar();
            }
            catch (Exception ex)
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
                ParamToParam(cmd, parameters, false);
                _sql = sql;

                if (_prepare)
                    cmd.Prepare();
            }
            else
                ParamToParam(cmd, parameters, true);

            using (var reader = cmd.ExecuteReader(singleRow ? CommandBehavior.SingleRow : CommandBehavior.SingleResult))
            {
                if (reader.HasRows)
                {
                    // dátum vynecháme ako ValueType, pretože pri default Activator jebe dátum od počiatku dátumu 01.01.0001
                    var date = typeof(DateTime);
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
                            }
                            else
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
                ParamToParam(cmd, parameters, false);
                _sql = sql;

                if (_prepare)
                    cmd.Prepare();
            }
            else
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
                        var date = typeof(DateTime);
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
                                }
                                else
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
                ParamToParam(cmd, parameters, false);
                _sql = sql;

                if (_prepare)
                    cmd.Prepare();
            }
            else
                ParamToParam(cmd, parameters, true);

            using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
            {
                var has = reader.HasRows;
                if (has)
                {
                    while (reader.Read())
                    {
                        var bufferSize = 1024;
                        var buffer = new byte[bufferSize];
                        long readBytes = 0;
                        long readIndex = 0;
                        readBytes = reader.GetBytes(0, readIndex, buffer, 0, bufferSize);

                        while (readBytes == bufferSize)
                        {
                            onReadBinary(buffer);
                            readIndex += bufferSize;
                            readBytes = reader.GetBytes(0, readIndex, buffer, 0, bufferSize);
                        }

                        onReadBinary(buffer);
                        break;
                    }
                }

                reader.Close();
                return has;
            }
        }

        public IEnumerable<T> ReaderEnumerator<T>(string sql, IList<Parameter> parameters, Func<string, Tuple<string, string, Func<string, object, object>>> onColumnMap, bool singleRow = false)
        {
            if (sql != _sql)
            {
                cmd.CommandText = sql;
                ParamToParam(cmd, parameters, false);
                _sql = sql;

                if (_prepare)
                    cmd.Prepare();
            }
            else
                ParamToParam(cmd, parameters, true);

            using (var reader = cmd.ExecuteReader(singleRow ? CommandBehavior.SingleRow : CommandBehavior.SingleResult))
            {
                var l = new List<Tuple<string, string, Func<string, object, object>>>(reader.FieldCount);

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

                if (reader.HasRows)
                {
                    var typ = typeof(T);
                    while (reader.Read())
                    {
                        var obj = Activator.CreateInstance(typ);
                        foreach (var i in l)
                        {
                            var value = reader[i.Item1];
                            typ.GetProperty(i.Item2).SetValue(obj, (i.Item3 == null ? value == DBNull.Value ? null : value : i.Item3(i.Item1, value == DBNull.Value ? null : value)), null);
                        }
                        yield return (T)obj;
                    }
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

                    v.Value = (p.Value == null ? DBNull.Value : p.Value);
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

                    if (p.Type == SqlDbType.NVarChar || p.Type == SqlDbType.VarChar)
                    {
                        v.Value = p.Value;
                        SetupString(v, p.IsSizeDeclared);
                        continue;
                    }

                    v.Value = (p.Value == null ? DBNull.Value : p.Value);
                    continue;
                }

                if (p.Type == SqlDbType.NVarChar || p.Type == SqlDbType.VarChar)
                {
                    param.Value = p.Value;
                    SetupString(param, p.IsSizeDeclared);
                }
                else
                {
                    // neviem na čo tu je?
                    //param.Size = p.Size;
                    param.Value = (p.Value == null ? DBNull.Value : p.Value);
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
                param.Value = str.Max(param.Size, "...");
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

        public IEnumerable<dynamic> Reader(string sql, bool singleRow = false)
        {
            return Reader(sql, null, singleRow);
        }

        public IEnumerable<dynamic> Reader(string sql, object arg, bool singleRow = false)
        {
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

        public Func<string, object, object> OnBind { get; set; }

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

        public IDatabase DB
        {
            get { return db; }
        }

        public Sql(IDatabase db)
        {
            this.db = db;
            this.TableName = DbUtils.GetTableName(typeof(T));
            this.AutoLocking = true;
            this.columns = Database.GetTableColumnCache(typeof(T));
        }

        public Sql(IDatabase db, bool autoLocking)
        {
            this.db = db;
            this.TableName = DbUtils.GetTableName(typeof(T));
            this.AutoLocking = autoLocking;
            this.columns = Database.GetTableColumnCache(typeof(T));
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

        public IEnumerable<T> Reader(string sql, bool singleRow = false)
        {
            return Reader(sql, null, singleRow);
        }

        public IEnumerable<T> Reader(string sql, object arg, bool singleRow = false)
        {
            return ReaderParam(sql, Parameter.CreateFromObject(arg), singleRow);
        }

        public IEnumerable<T> ReaderParam(string sql, IList<Parameter> arg, bool singleRow = false)
        {
            Func<string, Tuple<string, string, Func<string, object, object>>> OnColumnMap = delegate(string n)
            {
                var c = columns.FirstOrDefault(m => m.DbName == n);

                if (c == null)
                    return null;

                return new Tuple<string, string, Func<string, object, object>>(c.DbName, c.Name, null);
            };
            return db.ReaderEnumerator<T>(sql, arg, OnColumnMap, singleRow);
        }

        public object Scalar(string columnName, SqlBuilder builder)
        {
            return db.Scalar(string.Format("SELECT {0} FROM {1}{2}", columnName, GetTableNameSelect, builder.HasValue ? " WHERE " + builder.ToString() : ""), builder.Parameters);
        }

        public R Scalar<R>(string columnName, SqlBuilder builder)
        {
            return Utils.To<R>(db.Scalar(string.Format("SELECT {0} FROM {1}{2}", columnName, GetTableNameSelect, builder.HasValue ? " WHERE " + builder.ToString() : ""), builder.Parameters));
        }

        public T FindByPK(object primaryKey, params string[] disablePropertyName)
        {
            var sb = new System.Text.StringBuilder();
            var p = "";
            foreach (var s in columns)
            {
                if (!s.Select && !s.PrimaryKey)
                    continue;

                if (disablePropertyName.Contains(s.DbName) || disablePropertyName.Contains(s.Name))
                    continue;

                sb.Append((sb.Length > 0 ? "," : "") + (string.IsNullOrEmpty(s.Raw) ? s.DbName : s.Raw + " As " + s.DbName));
                if (s.PrimaryKey)
                    p = s.DbName;
            }

            if (string.IsNullOrEmpty(p))
                throw new Exception("Primary key not implement.");

            return Reader(string.Format("SELECT TOP 1 {0} FROM {1} WHERE {2}=@Id", sb.ToString(), GetTableNameSelect, p), new { Id = primaryKey }).FirstOrDefault();
        }

        public IEnumerable<T> GetAll(params OrderBy[] orderBy)
        {
            return Reader(string.Format("SELECT {0} FROM {1} {2}", GetColumns(), GetTableNameSelect, OrderByCreate(orderBy)));
        }

        public IEnumerable<T> GetAll(string[] disablePropertyName, params OrderBy[] orderBy)
        {
            return Reader(string.Format("SELECT {0} FROM {1} {2}", GetColumns(disablePropertyName), GetTableNameSelect, OrderByCreate(orderBy)));
        }

        public IEnumerable<T> GetAll(int top, params OrderBy[] orderBy)
        {
            return Reader(string.Format("SELECT TOP {3} {0} FROM {1} {2}", GetColumns(), GetTableNameSelect, OrderByCreate(orderBy), top), top == 1);
        }

        public IEnumerable<T> GetAll(int top, string[] disablePropertyName, params OrderBy[] orderBy)
        {
            return Reader(string.Format("SELECT TOP {3} {0} FROM {1} {2}", GetColumns(disablePropertyName), GetTableNameSelect, OrderByCreate(orderBy), top), top == 1);
        }

        public IEnumerable<T> GetAll(int skip, int take, params OrderBy[] orderBy)
        {
            var r = GetColumns(OrderByCreate(orderBy, true));
            return Reader(string.Format("SELECT TOP {0} {5} FROM (SELECT ROW_NUMBER() OVER (ORDER BY {3}) As rowindex, {1} FROM {2}) As _query WHERE _query.rowindex>{4} ORDER BY _query.rowindex", take, r.Item1.ToString(), GetTableNameSelect, r.Item3, skip, r.Item2.ToString()));
        }

        public IEnumerable<T> GetAll(int skip, int take, string[] disablePropertyName, params OrderBy[] orderBy)
        {
            var r = GetColumns(OrderByCreate(orderBy, true), disablePropertyName);
            return Reader(string.Format("SELECT TOP {0} {5} FROM (SELECT ROW_NUMBER() OVER (ORDER BY {3}) As rowindex, {1} FROM {2}) As _query WHERE _query.rowindex>{4} ORDER BY _query.rowindex", take, r.Item1.ToString(), GetTableNameSelect, r.Item3, skip, r.Item2.ToString()));
        }

        public IEnumerable<T> FindAll(SqlBuilder builder, params OrderBy[] orderBy)
        {
            return ReaderParam(string.Format("SELECT {0} FROM {1}{2} {3}", GetColumns(), GetTableNameSelect, builder.HasValue ? " WHERE " + builder.ToString() : "", OrderByCreate(orderBy)), builder.Parameters);
        }

        public IEnumerable<T> FindAll(SqlBuilder builder, int skip, int take, params OrderBy[] orderBy)
        {
            var r = GetColumns(OrderByCreate(orderBy, true));
            var sql = string.Format("SELECT TOP {0} {6} FROM (SELECT ROW_NUMBER() OVER (ORDER BY {2}) As rowindex, {1} FROM {3}{4}) As _query WHERE _query.rowindex>{5} ORDER BY _query.rowindex", take, r.Item1.ToString(), r.Item3, GetTableNameSelect, builder.HasValue ? " WHERE " + builder.ToString() : "", skip, r.Item2.ToString());
            return ReaderParam(sql, builder.Parameters);
        }

        public IEnumerable<T> FindAll(SqlBuilder builder, string[] disablePropertyName, params OrderBy[] orderBy)
        {
            return ReaderParam(string.Format("SELECT {0} FROM {1}{2} {3}", GetColumns(disablePropertyName), GetTableNameSelect, builder.HasValue ? " WHERE " + builder.ToString() : "", OrderByCreate(orderBy)), builder.Parameters);
        }

        public IEnumerable<T> FindAll(SqlBuilder builder, int skip, int take, string[] disablePropertyName, params OrderBy[] orderBy)
        {
            var r = GetColumns(OrderByCreate(orderBy, true), disablePropertyName);
            var sql = string.Format("SELECT TOP {0} {6} FROM (SELECT ROW_NUMBER() OVER (ORDER BY {2}) As rowindex, {1} FROM {3}{4}) As _query WHERE _query.rowindex>{5} ORDER BY _query.rowindex", take, r.Item1.ToString(), r.Item3, GetTableNameSelect, builder.HasValue ? " WHERE " + builder.ToString() : "", skip, r.Item2.ToString());
            return ReaderParam(sql, builder.Parameters);
        }

        public IEnumerable<T> FindTop(int top, SqlBuilder builder, params OrderBy[] orderBy)
        {
            var sql = string.Format("SELECT TOP {3} {0} FROM {1}{2} {4}", GetColumns(), GetTableNameSelect, builder.HasValue ? " WHERE " + builder.ToString() : "", top, OrderByCreate(orderBy));
            return ReaderParam(sql, builder.Parameters, top == 1);
        }

        public IEnumerable<T> FindTop(int top, SqlBuilder builder, string[] disablePropertyName, params OrderBy[] orderBy)
        {
            var sql = string.Format("SELECT TOP {3} {0} FROM {1}{2} {4}", GetColumns(disablePropertyName), GetTableNameSelect, builder.HasValue ? " WHERE " + builder.ToString() : "", top, OrderByCreate(orderBy));
            return ReaderParam(sql, builder.Parameters, top == 1);
        }

        public T FindOne(SqlBuilder builder, params OrderBy[] orderBy)
        {
            var sql = string.Format("SELECT TOP 1 {0} FROM {1}{2} {3}", GetColumns(), GetTableNameSelect, builder.HasValue ? " WHERE " + builder.ToString() : "", OrderByCreate(orderBy));
            return ReaderParam(sql, builder.Parameters, true).FirstOrDefault();
        }

        public T FindOne(SqlBuilder builder, string[] disablePropertyName, params OrderBy[] orderBy)
        {
            var sql = string.Format("SELECT TOP 1 {0} FROM {1}{2} {3}", GetColumns(disablePropertyName), GetTableNameSelect, builder.HasValue ? " WHERE " + builder.ToString() : "", OrderByCreate(orderBy));
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
            return Convert.ToInt32(db.Scalar(string.Format("SELECT COUNT(*) FROM {0}{1}", GetTableNameSelect, builder.HasValue ? " WHERE " + builder.ToString() : ""), builder.Parameters));
        }

        public bool Exists()
        {
            return Count(null, null) > 0;
        }

        public bool Exists(SqlBuilder builder)
        {
            return Convert.ToInt32(db.Scalar(string.Format("SELECT COUNT(*) FROM {0}{1}", GetTableNameSelect, builder.HasValue ? " WHERE " + builder.ToString() : ""), builder.Parameters)) > 0;
        }

        public int Update(object arg, params string[] disablePropertyName)
        {
            var hodnoty = new System.Text.StringBuilder();
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
                throw new Exception("Primary key not implemented.");

            return db.Execute(string.Format("UPDATE {0} SET {1} WHERE {2}", GetTableNameUpdate, hodnoty.ToString(), pk.DbName + "=@" + pk.DbName), disabled.IsValueCreated ? Parameter.CreateFromObject(arg, disabled.Value.ToArray()) : Parameter.CreateFromObject(arg));
        }

        public int Update(string where, object arg, params string[] disablePropertyName)
        {
            return Update(where, arg, null, disablePropertyName);
        }

        public int Update(string where, object arg, object whereArg, params string[] disablePropertyName)
        {
            var hodnoty = new System.Text.StringBuilder();
            var disabled = new Lazy<List<string>>();

            foreach (var s in columns)
            {
                if (!disablePropertyName.Contains(s.Name) && s.Update)
                    hodnoty.Append((hodnoty.Length > 0 ? "," : "") + s.DbName + "=@" + s.DbName);
                else if (!s.PrimaryKey)
                    disabled.Value.Add(s.Name);
            }

            var p = new List<Parameter>(10);

            if (arg != null && whereArg != null)
            {
                p.AddRange(disabled.IsValueCreated ? Parameter.CreateFromObject(arg, disabled.Value.ToArray()) : Parameter.CreateFromObject(arg));
                p.AddRange(Parameter.CreateFromObject(whereArg));
            }
            else if (arg != null)
                p.AddRange(disabled.IsValueCreated ? Parameter.CreateFromObject(arg, disabled.Value.ToArray()) : Parameter.CreateFromObject(arg));

            var sql = string.Format("UPDATE {0} SET {1} WHERE {2}", GetTableNameUpdate, hodnoty.ToString(), where);
            return db.Execute(sql, p);
        }

        public int Update(SqlBuilder builder, object arg, params string[] disablePropertyName)
        {
            var hodnoty = new System.Text.StringBuilder();
            var disabled = new Lazy<List<string>>();

            foreach (var s in columns)
            {
                if (!disablePropertyName.Contains(s.Name) && s.Update)
                    hodnoty.Append((hodnoty.Length > 0 ? "," : "") + s.DbName + "=@" + s.DbName);
                else if (!s.PrimaryKey)
                    disabled.Value.Add(s.Name);
            }

            var p = new List<Parameter>(10);
            p.AddRange(disabled.IsValueCreated ? Parameter.CreateFromObject(arg, disabled.Value.ToArray()) : Parameter.CreateFromObject(arg));
            p.AddRange(builder.Parameters);

            var sql = string.Format("UPDATE {0} SET {1}{2}", GetTableNameUpdate, hodnoty.ToString(), builder.HasValue ? " WHERE " + builder.ToString() : "");
            return db.Execute(sql, p);
        }

        public int UpdateOnly(object arg, params string[] propertyName)
        {

            if (propertyName == null || propertyName.Length == 0)
                throw new Exception("You must define a property name");

            var hodnoty = new System.Text.StringBuilder();
            Column pk = null;

            var disabled = new Lazy<List<string>>();

            foreach (var s in columns)
            {
                if (pk == null && s.PrimaryKey)
                    pk = s;

                if (propertyName.Contains(s.Name) && s.Update)
                    hodnoty.Append((hodnoty.Length > 0 ? "," : "") + s.DbName + "=@" + s.DbName);
                else if (!s.PrimaryKey)
                    disabled.Value.Add(s.Name);
            }

            if (hodnoty.Length == 0)
                throw new Exception("You must define a property name");

            if (pk == null)
                throw new Exception("Primary key not implemented.");

            return db.Execute(string.Format("UPDATE {0} SET {1} WHERE {2}", GetTableNameUpdate, hodnoty.ToString(), pk.DbName + "=@" + pk.DbName), disabled.IsValueCreated ? Parameter.CreateFromObject(arg, disabled.Value.ToArray()) : Parameter.CreateFromObject(arg));
        }

        public int UpdateOnly(string where, object arg, params string[] propertyName)
        {
            return UpdateOnly(where, arg, null, propertyName);
        }

        public int UpdateOnly(string where, object arg, object whereArg, params string[] propertyName)
        {
            if (propertyName == null || propertyName.Length == 0)
                throw new Exception("You must define a property name");

            var hodnoty = new System.Text.StringBuilder();
            var disabled = new Lazy<List<string>>();

            foreach (var s in columns)
            {
                if (propertyName.Contains(s.Name) && s.Update)
                    hodnoty.Append((hodnoty.Length > 0 ? "," : "") + s.DbName + "=@" + s.DbName);
                else if (!s.PrimaryKey)
                    disabled.Value.Add(s.Name);
            }

            if (hodnoty.Length == 0)
                throw new Exception("You must define a property name");

            var p = new List<Parameter>(10);

            if (arg != null && whereArg != null)
            {
                p.AddRange(disabled.IsValueCreated ? Parameter.CreateFromObject(arg, disabled.Value.ToArray()) : Parameter.CreateFromObject(arg));
                p.AddRange(Parameter.CreateFromObject(whereArg));
            }
            else if (arg != null)
                p.AddRange(disabled.IsValueCreated ? Parameter.CreateFromObject(arg, disabled.Value.ToArray()) : Parameter.CreateFromObject(arg));

            var sql = string.Format("UPDATE {0} SET {1} WHERE {2}", GetTableNameUpdate, hodnoty.ToString(), where);
            return db.Execute(sql, p);
        }

        public int UpdateOnly(SqlBuilder builder, object arg, params string[] propertyName)
        {
            if (propertyName == null || propertyName.Length == 0)
                throw new Exception("You must define a property name");

            var hodnoty = new System.Text.StringBuilder();
            var disabled = new Lazy<List<string>>();

            foreach (var s in columns)
            {
                if (propertyName.Contains(s.Name) && s.Update)
                    hodnoty.Append((hodnoty.Length > 0 ? "," : "") + s.DbName + "=@" + s.DbName);
                else if (!s.PrimaryKey)
                    disabled.Value.Add(s.Name);
            }

            if (hodnoty.Length == 0)
                throw new Exception("You must define a property name");

            var p = new List<Parameter>(10);
            p.AddRange(disabled.IsValueCreated ? Parameter.CreateFromObject(arg, disabled.Value.ToArray()) : Parameter.CreateFromObject(arg));
            p.AddRange(builder.Parameters);

            var sql = string.Format("UPDATE {0} SET {1}{2}", GetTableNameUpdate, hodnoty.ToString(), builder.HasValue ? " WHERE " + builder.ToString() : "");
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
            var stlpce = new System.Text.StringBuilder();
            var hodnoty = new System.Text.StringBuilder();

            Column pk = null;

            foreach (var s in columns)
            {
                if (pk == null && s.PrimaryKey)
                    pk = s;

                if (!disablePropertyName.Contains(s.Name) && s.Insert)
                {
                    stlpce.Append((stlpce.Length > 0 ? "," : "") + s.DbName);
                    hodnoty.Append((hodnoty.Length > 0 ? "," : "") + "@" + s.DbName);
                }
            }

            var sql = string.Format("INSERT INTO {0} ({1}) VALUES({2}); SELECT @@IDENTITY", GetTableNameUpdate, stlpce.ToString(), hodnoty.ToString());

            var v = Scalar(sql, arg);

            if (pk != null)
            {
                var prop = arg.GetType().GetProperty(pk.Name);
                var t = prop.PropertyType;
                if (v == DBNull.Value && (pk.Insert || pk.Update))
                    v = prop.GetValue(arg, null);
                if (t == typeof(int))
                    prop.SetValue(arg, Convert.ToInt32(v), null);
                else if (t == typeof(byte))
                    prop.SetValue(arg, Convert.ToByte(v), null);
                else if (t == typeof(Guid))
                    prop.SetValue(arg, (Guid)v, null);
                else if (t == typeof(Int16))
                    prop.SetValue(arg, Convert.ToInt16(v), null);
                else if (t == typeof(Int64))
                    prop.SetValue(arg, Convert.ToInt64(v), null);
                else if (t == typeof(double))
                    prop.SetValue(arg, Convert.ToDouble(v), null);
                else if (t == typeof(float))
                    prop.SetValue(arg, Convert.ToSingle(v), null);
                else if (t == typeof(decimal))
                    prop.SetValue(arg, Convert.ToDecimal(v), null);
                else if (t == typeof(uint))
                    prop.SetValue(arg, Convert.ToUInt32(v), null);
                else if (t == typeof(UInt16))
                    prop.SetValue(arg, Convert.ToUInt16(v), null);
                else if (t == typeof(UInt64))
                    prop.SetValue(arg, Convert.ToUInt64(v), null);
                else if (t == typeof(string))
                {
                    if (v != DBNull.Value)
                        prop.SetValue(arg, (string)v, null);
                }
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
                throw new Exception("Primary key not implemented.");

            return db.Execute(string.Format("DELETE FROM {0} WHERE {1}", GetTableNameUpdate, pk.DbName + "=@" + pk.DbName), Parameter.CreateFromObject(arg));
        }

        public int Delete(string where, object arg)
        {
            var sql = string.Format("DELETE FROM {0} WHERE {1}", GetTableNameUpdate, where);
            return Execute(sql, arg);
        }

        public int Delete(SqlBuilder builder)
        {
            var sql = string.Format("DELETE FROM {0}{1}", GetTableNameUpdate, builder.HasValue ? " WHERE " + builder.ToString() : "");
            return db.Execute(sql, builder.Parameters);
        }

        private Tuple<StringBuilder, StringBuilder, string> GetColumns(string orderBy, params string[] disablePropertyName)
        {
            var sb = new System.Text.StringBuilder();
            var sb2 = new System.Text.StringBuilder();
            var cn = orderBy;

            foreach (var s in columns)
            {
                if (!s.Select)
                    continue;

                if (disablePropertyName.Contains(s.DbName) || disablePropertyName.Contains(s.Name))
                    continue;

                if (cn == "")
                    cn = s.DbName;

                sb2.Append((sb2.Length > 0 ? "," : "") + s.DbName);
                sb.Append((sb.Length > 0 ? "," : "") + (string.IsNullOrEmpty(s.Raw) ? s.DbName : s.Raw + " AS " + s.DbName));
            }

            return new Tuple<StringBuilder, StringBuilder, string>(sb, sb2, cn);
        }

        private StringBuilder GetColumns(params string[] disablePropertyName)
        {
            var sb = new System.Text.StringBuilder();

            foreach (var s in columns)
            {
                if (disablePropertyName.Contains(s.DbName) || disablePropertyName.Contains(s.Name))
                    continue;

                if (s.Select)
                    sb.Append((sb.Length > 0 ? "," : "") + (string.IsNullOrEmpty(s.Raw) ? s.DbName : s.Raw + " AS " + s.DbName));
            }
            return sb;
        }

        private string OrderByCreate(OrderBy[] o)
        {
            return OrderByCreate(o, false);
        }

        private string OrderByCreate(OrderBy[] o, bool withoutOrderBy)
        {
            if (o == null)
                return "";

            var sb = new System.Text.StringBuilder();
            foreach (var i in o)
            {
                if (i == null)
                    continue;
                sb.Append((sb.Length > 0 ? "," : "") + string.Format("{0} {1}", i.Name, (i.Order == OrderByType.Asc ? "ASC" : "DESC")));
            }
            return sb.Length > 0 ? (withoutOrderBy ? "" : "ORDER BY ") + sb.ToString() : "";
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

        public bool Update(IDatabase db, params string[] disablePropertyName)
        {
            _isnew = false;
            return new Sql<T>(db).Update(this, disablePropertyName) > 0;
        }

        public bool UpdateOnly(IDatabase db, params string[] propertyName)
        {
            _isnew = false;
            return new Sql<T>(db).UpdateOnly(this, propertyName) > 0;
        }

        public bool Insert(IDatabase db, params string[] disablePropertyName)
        {
            return _isnew = (new Sql<T>(db).Insert(this, disablePropertyName) != null);
        }

        public bool Save(IDatabase db, params string[] disablePropertyName)
        {
            if (!Update(db, disablePropertyName))
                return Insert(db, disablePropertyName);
            return true;
        }

        public bool Delete(IDatabase db)
        {
            return new Sql<T>(db).Delete(this) > 0;
        }

        public bool IsNew()
        {
            return _isnew;
        }
    }
    #endregion

}