using System;
using System.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Resources;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Reflection;

using Library.Json;
using System.Drawing.Printing;

namespace Library
{

    #region Enums & Attributes & Classes & Interfaces
    public enum XmlSitemapFrequency
    {
        Always,
        Hourly,
        Daily,
        Weekly,
        Monthly,
        Yearly,
        Never
    }

    public enum XmlSitemapPriority
    {
        Random,
        Low,
        Medium,
        High,
        Important
    }

    public enum Authorization : byte
    {
        Logged = 0,
        Unlogged = 1,
        Forbidden = 2
    }

    [Flags]
    public enum ConfigurationDefaults : byte
    {
        Web = 0,
        Analytics = 1,
        Cmd = 2
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class JsonParameterAttribute : Attribute
    {
        private bool read = true;
        private bool write = true;

        public string Name { get; set; }

        public bool Read
        {
            get { return read; }
            set { read = value; }
        }

        public bool Write
        {
            get { return write; }
            set { write = value; }
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class NoJsonParameterAttribute : Attribute
    { }

    public class KeyValue
    {
        [JsonParameter(Name = "K")]
        public string Key { get; set; }

        [JsonParameter(Name = "V")]
        public string Value { get; set; }

        public KeyValue() { }

        public KeyValue(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public KeyValue(string key, int value, bool autoFormat = true)
        {
            Key = key;
            Value = autoFormat ? value.Format() : value.ToString();
        }

        public KeyValue(string key, decimal value, bool autoFormat = true)
        {
            Key = key;
            Value = autoFormat ? value.Format(2) : value.ToString();
        }

        public KeyValue(string key, byte value)
        {
            Key = key;
            Value = value.ToString();
        }

        public KeyValue(string key, string format, params object[] param)
        {
            Key = key;
            Value = string.Format(Configuration.InvariantCulture, format, param);
        }

        public static KeyValue Create(string key, string value)
        {
            return new KeyValue(key, value);
        }

        public static KeyValue Create(string key, decimal value, bool autoFormat = true)
        {
            return new KeyValue(key, value, autoFormat);
        }

        public static KeyValue Create(string key, int value, bool autoFormat = true)
        {
            return new KeyValue(key, value, autoFormat);
        }

        public static KeyValue Create(string key, byte value)
        {
            return new KeyValue(key, value);
        }

        public static KeyValue Create(string key, string format, params object[] param)
        {
            return new KeyValue(key, string.Format(Configuration.InvariantCulture, format, param));
        }
    }

    public class ControllerCache
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
        public string Image { get; set; }
        public HtmlString Body { get; set; }
    }

    public interface ILibraryJsonProvider
    {
        string Serialize(object value, params string[] withoutProperty);
        T DeserializeObject<T>(string value);
        dynamic DeserializeObject(string value);
        object DeserializeObject(string value, Type type);
    }

    public interface ILibraryCacheProvider
    {
        T Write<T>(string key, T value, DateTime expire);
        T Read<T>(string key, Func<string, T> onEmpty = null);
        void Remove(string key);
        void Remove(Func<string, bool> predicate);
    }

    public interface ILibraryAnalyticsProvider
    {
        void Write(Library.Others.Analytics.Statistics stats);
        void WriteState(Library.Others.Analytics.Statistics stats);
        Library.Others.Analytics.Statistics LoadState();

        IList<Library.Others.Analytics.Statistics> Yearly();
        IList<Library.Others.Analytics.Statistics> Monthly(int year);
        IList<Library.Others.Analytics.Statistics> Daily(int year, int month);
    }
    #endregion

    // ===============================================================================================
    // 
    // Library.[Configuration]
    // 
    // ===============================================================================================

    // ===============================================================================================
    // Autor        : Peter Širka
    // Created      : 04. 05. 2008
    // Updated      : 23. 02. 2014
    // Description  : App configuration
    // ===============================================================================================

    #region Configuration
    public class ConfigurationUrl
    {
        public string JS { get; set; }
        public string CSS { get; set; }
        public string Image { get; set; }
        public string Download { get; set; }
        public string Static { get; set; }
    }

    public static class ConfigurationCache
    {
        public static Type type_nodbparameter = typeof(NoDbParameterAttribute);
        public static Type type_dbskip = typeof(DbSkipAttribute);
        public static Type type_dbschema = typeof(DbSchemaAttribute);
        public static Type type_dbtable = typeof(DbTableAttribute);
        public static Type type_dbparameter = typeof(DbParameterAttribute);
        public static Type type_bool_null = typeof(bool?);
        public static Type type_bool = typeof(bool);
        public static Type type_int = typeof(int);
        public static Type type_int_null = typeof(int?);
        public static Type type_byte = typeof(byte);
        public static Type type_byte_null = typeof(byte?);
        public static Type type_short = typeof(short);
        public static Type type_short_null = typeof(short?);
        public static Type type_long = typeof(long);
        public static Type type_long_null = typeof(long?);
        public static Type type_string = typeof(string);
        public static Type type_datetime = typeof(DateTime);
        public static Type type_datetime_null = typeof(DateTime?);
        public static Type type_decimal = typeof(decimal);
        public static Type type_decimal_null = typeof(decimal?);
        public static Type type_float = typeof(float);
        public static Type type_float_null = typeof(float?);
        public static Type type_double = typeof(double);
        public static Type type_double_null = typeof(double?);
        public static Type type_guid = typeof(Guid);
        public static Type type_guid_null = typeof(Guid?);
        public static Type type_char = typeof(Char);
        public static Type type_char_null = typeof(Char?);
        public static Type type_byte_array = typeof(byte[]);
        public static Type type_uint = typeof(uint);
        public static Type type_uint_null = typeof(uint?);
        public static Type type_ushort = typeof(ushort);
        public static Type type_ushort_null = typeof(ushort?);
        public static Type type_ulong = typeof(ulong);
        public static Type type_ulong_null = typeof(ulong?);
        public static Type type_object = typeof(object);
        public static Type type_nullable = typeof(Nullable);
        public static Type type_dbnull = DBNull.Value.GetType();
    }

    public static class Configuration
    {
        public delegate void ProblemEventHandler(string source, string message, Uri uri, string ip);
        public delegate void ErrorEventHandler(string source, Exception ex, Uri uri, string ip);
        public delegate void ChangeEventHandler(string source, string message, Uri uri, string ip);
        public delegate void LogEventHandler(string source, string message, Uri uri, string ip);
        public delegate void StopwatchEventHandler(string source, TimeSpan time, HttpRequestBase request);
        public delegate Authorization AuthorizationDelegate(HttpContextBase context, string roles, string users);
        public delegate ActionResult AuthorizationErrorDelegate(HttpRequestBase request, Authorization authorization);
        public delegate string VersionDelegate(string name);
        public delegate string DateConjugationDelegate(DateTime current, string language);
        public delegate string ResourceDelegate(string key, string language);
        public delegate string EncryptDelegate(int id, string token);
        public delegate int DecryptDelegate(string hash, string token);
        public delegate Size ImageParserDimensionDelegate(string dimension);
        public delegate string ImageParserUrlDelegate(string dimension, ImageParser parser);
        public delegate string JsonSerializeDelegate(object obj);
        public delegate object JsonDeserializeDelegate(string value);
        public delegate void MailHandler(string type, System.Net.Mail.MailMessage message);

        // Events
        public static event ProblemEventHandler Problem;
        public static event ErrorEventHandler Error;
        public static event ChangeEventHandler Change;
        public static event LogEventHandler Log;
        public static event StopwatchEventHandler Stopwatch;

        public static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
        public static ConfigurationUrl Url { get; set; }

        public static string Name { get; set; }
        public static string Author { get; set; }
        public static string Version { get; set; }

        public static string PathTemporary { get; set; }

        public static bool AllowLogs { get; set; }
        public static bool AllowProblems { get; set; }
        public static bool AllowChanges { get; set; }
        public static bool AllowErrors { get; set; }
        public static bool AllowStopwatch { get; set; }

        // Delegates
        public static DateConjugationDelegate OnDateConjugation { get; set; }
        public static VersionDelegate OnVersion { get; set; }
        public static ResourceDelegate OnResource { get; set; }
        public static EncryptDelegate OnEncrypt { get; set; }
        public static DecryptDelegate OnDecrypt { get; set; }
        public static ImageParserDimensionDelegate OnImageParserDimension { get; set; }
        public static ImageParserUrlDelegate OnImageParserUrl { get; set; }
        public static AuthorizationDelegate OnAuthorization { get; set; }
        public static AuthorizationErrorDelegate OnAuthorizationError { get; set; }
        public static MailHandler OnMail { get; set; }

        public static ILibraryJsonProvider JsonProvider { get; set; }
        public static ILibraryCacheProvider CacheProvider { get; set; }
        public static ILibraryAnalyticsProvider AnalyticsProvider { get; set; }

        public static Library.Others.Analytics Analytics { get; set; }
        public static bool IsWindows { get; private set; }

        public static bool JsonUnicode { get; set; }

        public static bool IsDebug
        {
            get { return HttpContext.Current.IsDebuggingEnabled; }
        }

        public static bool IsRelease
        {
            get { return !HttpContext.Current.IsDebuggingEnabled; }
        }

        public static void Defaults(ConfigurationDefaults defaults = ConfigurationDefaults.Web)
        {
            if ((defaults & ConfigurationDefaults.Cmd) != ConfigurationDefaults.Cmd)
            {
                AreaRegistration.RegisterAllAreas();
                GlobalFilters.Filters.Add(new HandleErrorAttribute());
                MvcHandler.DisableMvcResponseHeader = true;
            }

            Configuration.JsonProvider = new Providers.DefaultJsonSerializerDeserializer();
            Configuration.CacheProvider = new Providers.DefaultCacheProvider();
            Configuration.Url = new ConfigurationUrl();

            Configuration.AllowLogs = true;
            Configuration.AllowProblems = true;
            Configuration.AllowErrors = true;
            Configuration.AllowChanges = true;
            Configuration.AllowStopwatch = true;

            Configuration.Url.CSS = "/css/";
            Configuration.Url.JS = "/js/";
            Configuration.Url.Image = "/img/";
            Configuration.Url.Download = "/download/";
            Configuration.Url.Static = "";

            Configuration.PathTemporary = "~/App_Data/tmp/";
            Configuration.JsonUnicode = false;
            Configuration.Name = Utils.Config("name");
            Configuration.Author = Utils.Config("author");
            Configuration.Version = Utils.Config("version");

            Configuration.IsWindows = System.Environment.OSVersion.Platform.ToString().ToLower() == "windows";
            Configuration.OnImageParserUrl = (dimension, image) => Configuration.Url.Static + Configuration.OnVersion(string.Format("/images/{0}/{1}.jpg", dimension, image.Id));

            Configuration.OnDecrypt = (hash, token) =>
            {
                if (string.IsNullOrEmpty(token))
                    token = "Library.110";

                var value = EncryptDecrypt.Decrypt(token, hash);
                if (value.IsEmpty())
                    return 0;

                var index = value.IndexOf('X');
                if (index == -1)
                    return 0;

                var id = value.Substring(0, index).To<int>();

                if (value != Utils.Token(id, token, false))
                    return 0;

                return id;
            };

            Configuration.OnEncrypt = (id, token) =>
            {
                if (string.IsNullOrEmpty(token))
                    token = "Library.110";

                return EncryptDecrypt.Encrypt(token, Utils.Token(id, token, false));
            };

            Configuration.OnDateConjugation = (source, language) =>
            {
                var date = DateTime.Now;
                var ts = date - source;

                if (ts.TotalMinutes < 0)
                    return source.ToString("dd.MM.yyyy");

                if (ts.TotalMinutes < 1)
                    return "teraz pridané";

                if (ts.TotalMinutes < 2)
                    return "pred 1 minútou";

                if (ts.TotalMinutes < 60)
                    return "pred " + ts.Minutes + " minútami";

                if (ts.TotalHours < 2)
                    return "pred hodinou";

                if (ts.TotalHours < 11)
                    return "pred " + ts.Hours + " hodinami";

                if (date.Date == source.Date)
                    return "dnes";

                if (date.Date == source.AddDays(1).Date)
                    return "včera";

                if (date.Date == source.AddDays(2).Date)
                    return "pred 2 dňami";

                if (ts.Days < 7)
                    return "pred " + ts.Days + " dňami";

                if (ts.Days < 14)
                    return "pred týždňom";

                if (ts.Days < 21)
                    return "pred 2 týždňami";

                if (ts.Days < 28)
                    return "pred 3 týždňami";

                if (ts.Days < 42)
                    return "pred mesiacom";

                if (ts.Days < 160)
                    return "pred " + Math.Round(ts.Days / 28D) + " mesiacmi";

                if (ts.Days < 183)
                    return "pred pol rokom";

                if (ts.Days < 365)
                    return "tento rok";

                if (ts.Days < 451)
                    return "pred rokom";

                if (ts.Days < 548)
                    return "pred rokom a pol";

                return "pred " + Math.Round(ts.Days / 365D) + " rokmi";
            };

            if (Configuration.OnVersion == null)
                Configuration.OnVersion = n => n;

            if ((defaults & ConfigurationDefaults.Analytics) == ConfigurationDefaults.Analytics)
            {
                Configuration.AnalyticsProvider = new Providers.DefaultAnalyticsProvider();
                Analytics = new Library.Others.Analytics();
            }

            ValueProviderFactories.Factories.Remove(ValueProviderFactories.Factories.OfType<JsonValueProviderFactory>().Single());
            //ValueProviderFactories.Factories.Add(new JsonDeserializeProviderFactory());
        }

        public static void InvokeProblem(string source, string message, Uri uri, string ip = "")
        {
            if (!AllowProblems)
                return;

            if (Problem != null)
                Problem(source, message, uri, ip);
        }

        public static void InvokeError(string source, Exception error, Uri uri, string ip = "")
        {
            if (!AllowErrors)
                return;

            if (Error != null)
                Error(source, error, uri, ip);
        }

        public static void InvokeChange(string source, string message, Uri uri, string ip = "")
        {
            if (!AllowChanges)
                return;

            if (Change != null)
                Change(source, message, uri, ip);
        }

        public static void InvokeLog(string source, string message, Uri uri, string ip = "")
        {
            if (!AllowLogs)
                return;

            if (Log != null)
                Log(source, message, uri, ip);
        }

        internal static void InvokeStopwatch(string source, TimeSpan time, HttpRequestBase request)
        {
            if (!AllowStopwatch)
                return;

            if (Stopwatch != null)
                Stopwatch(source, time, request);
        }
    }
    #endregion

    // ===============================================================================================
    // 
    // Library.[Utils]
    // 
    // ===============================================================================================

    // ===============================================================================================
    // Autor        : Peter Širka
    // Created      : 04. 05. 2008
    // Updated      : 13. 08. 2013
    // Description  : Pomocné funkcie a metódy
    // ===============================================================================================

    #region Utils
    public static class Utils
    {
        public static double GpsDistanceKm(double latitudeA, double longitudeA, double latitudeB, double longitudeB)
        {
            var toRadian = new Func<double, double>(val => (Math.PI / 180) * val);

            if (latitudeA <= -90 || latitudeA >= 90 || longitudeA <= -180 || longitudeA >= 180 || latitudeB <= -90 && latitudeB >= 90 || longitudeB <= -180 || longitudeB >= 180)
                return 0D;

            double R = 6371;
            double dLat = toRadian(latitudeB - latitudeA);
            double dLon = toRadian(longitudeB - longitudeA);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(toRadian(latitudeA)) * Math.Cos(toRadian(latitudeB)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            double d = R * c;

            return d;
        }

        public static void XmlSitemap(HttpResponse response, Action<Action<string, DateTime, XmlSitemapFrequency, XmlSitemapPriority>> fn)
        {
            var priorita = new [] { "0.1", "0.3", "0.5" };
            var prioritaIndex = 0;

            response.ContentType = "text/xml";
            response.HeaderEncoding = Encoding.UTF8;
            response.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine + "<urlset xmlns=\"http://www.google.com/schemas/sitemap/0.84\">" + Environment.NewLine);

            prioritaIndex++;

            if (prioritaIndex == 3)
                prioritaIndex = 0;

            var action = new Action<string, DateTime, XmlSitemapFrequency, XmlSitemapPriority>((url, date, freq, priority) =>
            {
                response.Write("  <url>" + Environment.NewLine);
                response.Write("    <loc>" + url.Replace("&", "&amp;") + "</loc>" + Environment.NewLine);
                response.Write("    <lastmod>" + date.ToString("yyyy-MM-dd") + "</lastmod>" + Environment.NewLine);
                response.Write("    <changefreq>" + freq + "</changefreq>" + Environment.NewLine);

                var p = "";

                switch (priority)
                {
                    case XmlSitemapPriority.Random:
                        p = priorita[prioritaIndex];
                        break;
                    case XmlSitemapPriority.Low:
                        p = "0.1";
                        break;
                    case XmlSitemapPriority.Medium:
                        p = "0.4";
                        break;
                    case XmlSitemapPriority.High:
                        p = "0.7";
                        break;
                    case XmlSitemapPriority.Important:
                        p = "1.0";
                        break;
                }

                response.Write("    <priority>" + p + "</priority>" + Environment.NewLine);
                response.Write("  </url>" + Environment.NewLine);
            });

            fn(action);

            response.Write("</urlset>");
        }

        public static void Problem(string source, string message, Uri uri, string ip = "")
        {
            Configuration.InvokeProblem(source, message, uri, ip);
        }

        public static void Error(string source, Exception error, Uri uri, string ip = "")
        {
            Configuration.InvokeError(source, error, uri, ip);
        }

        public static void Change(string source, string message, Uri uri, string ip = "")
        {
            Configuration.InvokeChange(source, message, uri, ip);
        }

        public static void Log(string source, string message, Uri uri, string ip = "")
        {
            Configuration.InvokeLog(source, message, uri, ip);
        }

        public static void Compare<T>(IList<T> sourceForm, IList<T> sourceDB, Func<T, T, bool> predicate, Action<T> onAdd, Action<T, T> onUpdate, Action<T> onRemove, bool isNew = false)
        {
            if (isNew)
            {
                foreach (var m in sourceForm)
                    onAdd(m);

                return;
            }

            if (sourceDB == null)
                sourceDB = new List<T>(0);

            if (sourceForm == null)
                sourceForm = new List<T>(0);

            var remove = new List<T>(2);
            foreach (var m in sourceDB)
            {
                var existuje = false;

                foreach (var n in sourceForm)
                {
                    // napr. n.Id == m.Id
                    // update
                    if (!predicate(n, m))
                        continue;

                    existuje = true;

                    if (onUpdate != null)
                        onUpdate(n, m);

                    break;
                }

                if (!existuje)
                    remove.Add(m);
            }

            foreach (var n in sourceForm)
            {
                var existuje = false;
                foreach (var m in sourceDB)
                {
                    if (!predicate(n, m))
                    {
                        existuje = true;
                        break;
                    }
                }

                // inak insert
                if (!existuje)
                {
                    if (onAdd != null)
                        onAdd(n);
                }
            }

            if (onRemove != null)
            {
                foreach (var m in remove)
                    onRemove(m);
            }
        }

        public static void CompareDB<T>(IDatabase db, SqlBuilder builder, IList<T> sourceForm, Func<T, T, bool> predicate, Action<T> onAdd, Action<T, T> onUpdate, Action<T> onRemove, bool isNew = false)
        {
            if (isNew)
            {
                foreach (var m in sourceForm)
                    onAdd(m);

                return;
            }

            if (sourceForm == null)
                sourceForm = new List<T>(0);

            var sourceDB = new Sql<T>(db).FindAll(builder).ToList();
            var remove = new List<T>(2);

            foreach (var m in sourceDB)
            {
                var exists = false;

                foreach (var n in sourceForm)
                {
                    // napr. n.Id == m.Id
                    // update
                    if (!predicate(n, m))
                        continue;

                    exists = true;
                    if (onUpdate != null)
                        onUpdate(n, m);

                    break;
                }

                if (!exists)
                    remove.Add(m);
            }

            foreach (var n in sourceForm)
            {
                var exists = false;
                foreach (var m in sourceDB)
                {
                    if (predicate(n, m))
                    {
                        exists = true;
                        break;
                    }
                }

                // inak insert
                if (!exists)
                {
                    if (onAdd != null)
                        onAdd(n);
                }
            }

            if (onRemove != null)
            {
                foreach (var m in remove)
                    onRemove(m);
            }
        }

        public static string Version(string name)
        {
            return Configuration.OnVersion(name);
        }

        public static string Resource(string name, string language = "")
        {
            return Configuration.OnResource(name, language);
        }

        public static IList<KeyValue> Validate(object source, bool allProperties = false)
        {
            var results = new List<ValidationResult>();
            var r = Validator.TryValidateObject(source, new ValidationContext(source, null, null), results, allProperties);
            var l = new List<KeyValue>(2);

            if (r)
                return l;

            foreach (var o in results)
            {
                foreach (var m in o.MemberNames)
                    l.Add(KeyValue.Create(m, o.ErrorMessage));
            }

            return l;
        }

        public static T To<T>(object value)
        {
            Type t = typeof(T);
            Type tv = value == null ? null : value.GetType();
            object o = null;

            if (t == ConfigurationCache.type_string)
            {
                if (tv == null)
                    o = "";
                else
                    o = value.ToString();

                return o == null ? default(T) : (T)o;
            }

            if (t == ConfigurationCache.type_char)
            {
                if (tv != null)
                {
                    try
                    {
                        o = Convert.ToChar(value);
                    }
                    catch
                    {
                        o = '\0';
                    }
                }
                return o == null ? default(T) : (T)o;
            }

            if (t == ConfigurationCache.type_int)
            {
                if (tv != null)
                {
                    int intVal;
                    if (tv == ConfigurationCache.type_string)
                    {
                        if (Int32.TryParse(value.ToString().Replace(" ", ""), out intVal))
                            o = intVal;
                        else
                            o = 0;
                    }
                    else
                    {
                        try
                        {
                            o = Convert.ToInt32(value);
                        }
                        catch
                        {
                            o = 0;
                        }
                    }
                }

                return o == null ? default(T) : (T)o;
            }

            if (t == ConfigurationCache.type_bool)
            {
                if (tv != null)
                {
                    if (tv == ConfigurationCache.type_string)
                    {
                        var boolVal = value.ToString().ToLower();
                        o = boolVal == "on" || boolVal == "1" || boolVal == "true";
                    }
                    else
                    {
                        try
                        {
                            o = Convert.ToBoolean(value);
                        }
                        catch
                        {
                            o = false;
                        }
                    }
                }

                return o == null ? default(T) : (T)o;
            }

            if (t == ConfigurationCache.type_byte)
            {
                if (tv != null)
                {
                    byte byteVal;
                    if (tv == ConfigurationCache.type_string)
                    {
                        if (Byte.TryParse(value.ToString().Replace(" ", ""), out byteVal))
                            o = byteVal;
                        else
                            o = (byte)0;
                    }
                    else
                    {
                        try
                        {
                            o = Convert.ToByte(value);
                        }
                        catch
                        {
                            o = (byte)0;
                        }
                    }
                }

                return o == null ? default(T) : (T)o;
            }

            if (t == ConfigurationCache.type_decimal)
            {
                if (tv != null)
                {
                    if (tv == ConfigurationCache.type_string)
                    {
                        try
                        {
                            o = Decimal.Parse(value.ToString().Replace(',', '.').Replace(" ", ""), Configuration.InvariantCulture);
                        }
                        catch { o = 0M; }
                    }
                    else
                    {
                        try
                        {
                            o = Convert.ToDecimal(value);
                        }
                        catch
                        {
                            o = 0M;
                        }
                    }
                }
                return o == null ? default(T) : (T)o;
            }

            if (t == ConfigurationCache.type_long)
            {
                if (tv != null)
                {
                    Int64 int64Val;
                    if (tv == ConfigurationCache.type_string)
                    {
                        if (Int64.TryParse(value.ToString().Replace(" ", ""), out int64Val))
                            o = int64Val;
                        else
                            o = (Int64)0;
                    }
                    else
                    {
                        try
                        {
                            o = Convert.ToInt64(value);
                        }
                        catch
                        {
                            o = (Int64)0;
                        }
                    }
                }
                return o == null ? default(T) : (T)o;
            }

            if (t == ConfigurationCache.type_short)
            {
                if (tv != null)
                {
                    Int16 int16Val;
                    if (tv == ConfigurationCache.type_string)
                    {
                        if (Int16.TryParse(value.ToString().Replace(" ", ""), out int16Val))
                            o = int16Val;
                        else
                            o = (Int16)0;
                    }
                    else
                    {
                        try
                        {
                            o = Convert.ToInt16(value);
                        }
                        catch
                        {
                            o = (Int16)0;
                        }
                    }
                }
                return o == null ? default(T) : (T)o;
            }

            if (t == ConfigurationCache.type_float)
            {
                if (tv != null)
                {
                    if (tv == ConfigurationCache.type_string)
                    {
                        try
                        {
                            o = float.Parse(value.ToString().Replace(',', '.').Replace(" ", ""), Configuration.InvariantCulture);
                        }
                        catch { o = 0F; }
                    }
                    else
                    {
                        try
                        {
                            o = Convert.ToSingle(value);
                        }
                        catch
                        {
                            o = 0F;
                        }
                    }
                }
                return o == null ? default(T) : (T)o;
            }

            if (t == ConfigurationCache.type_double)
            {
                if (tv != null)
                {
                    if (tv == ConfigurationCache.type_string)
                    {
                        try
                        {
                            o = double.Parse(value.ToString().Replace(',', '.').Replace(" ", ""), Configuration.InvariantCulture);
                        }
                        catch { o = 0D; }
                    }
                    else
                    {
                        try
                        {
                            o = Convert.ToDouble(value);
                        }
                        catch
                        {
                            o = 0D;
                        }
                    }
                }
                return o == null ? default(T) : (T)o;
            }

            if (t == ConfigurationCache.type_guid)
            {
                try
                {
                    if (o == null)
                        o = default(Guid);
                    else
                        o = new Guid(value.ToString());
                }
                catch
                {
                    o = default(Guid);
                }
                return o == null ? default(T) : (T)o;
            }

            if (t == ConfigurationCache.type_datetime)
            {
                try
                {
                    if (tv == ConfigurationCache.type_string)
                    {
                        DateTime dt;
                        if (DateTime.TryParse(value.ToString(), out dt))
                            o = dt;
                        else
                            o = null;
                    }
                    else
                        o = Convert.ToDateTime(value);
                }
                catch
                {
                    o = default(DateTime);
                }

                return o == null ? default(T) : (T)o;
            }

            if (t == ConfigurationCache.type_byte_array)
            {
                try
                {
                    return o == null ? default(T) : (T)o;
                }
                catch
                {
                    return default(T);
                }
            }

            return default(T);
        }

        public static T Config<T>(string name)
        {
            return To<T>(ConfigurationManager.AppSettings[name]);
        }

        public static string Config(string name)
        {
            return ConfigurationManager.AppSettings[name];
        }

        public static int Asc(char c)
        {
            return (int)c;
        }

        public static char Chr(int i)
        {
            return (char)i;
        }

        public static void Take<T>(IList<T> list, int count, Action<IList<T>> onTake)
        {
            if (list == null || list.Count == 0)
                return;

            var next = true;
            var index = 0;

            while (next)
            {
                var l = (index > 0 ? list.Skip(index).Take(count) : list.Take(count)).ToList();

                index += l.Count;
                next = l.Count == count;

                if (l.Count > 0)
                    onTake(l);
            }
        }

        public static T Proxy<T>(string key, string url, StringBuilder data = null, string method = "POST")
        {
            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);

            if (method != "GET" || method != "DELETE" || method != "OPTION")
                request.ContentType = "application/x-www-form-urlencoded";

            request.Method = method;
            request.Headers.Add("X-Proxy", "Library." + key.Hash("sha256"));

            if (data != null && data.Length > 0)
            {
                System.IO.StreamWriter writer = new System.IO.StreamWriter(request.GetRequestStream(), Encoding.UTF8);

                if (data != null && data.Length > 0)
                    writer.Write(data);

                writer.Flush();
                writer.Close();
            }

            var response = request.GetResponse();

            using (System.IO.StreamReader Reader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.UTF8))
                return Reader.ReadToEnd().JsonDeserialize<T>();
        }

        public static string POST(string url, Action<Dictionary<string, string>> fnData, Action<System.Net.HttpWebRequest> options = null, Encoding encoding = null)
        {
            System.Net.HttpWebRequest r = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            r.ContentType = "application/x-www-form-urlencoded";
            r.Method = "POST";

            if (encoding == null)
                encoding = Encoding.Default;

            if (options != null)
                options(r);

            var sb = new StringBuilder();
            var data = new Dictionary<string, string>(10);

            fnData(data);

            foreach (var item in data)
                sb.Append(string.Format("{0}={1}", item.Key, HttpUtility.UrlEncode(item.Value)), "&");

            System.IO.StreamWriter w = new System.IO.StreamWriter(r.GetRequestStream(), encoding);
            w.Write(sb);
            w.Flush();
            w.Close();

            var response = r.GetResponse();

            using (System.IO.StreamReader Reader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.UTF8))
                return Reader.ReadToEnd();
        }

        internal static string Token(int id, string token, bool session)
        {
            var current = HttpContext.Current;

            if (session)
                return string.Format("{0}X|{1}|{2}|{3}|{4}", id, current.Request.Browser.Browser, string.IsNullOrEmpty(Configuration.Name) ? current.Request.Url.Host.Replace("www.", "") : Configuration.Name, current.Request.UserHostAddress, token);

            return string.Format("{0}X{1}#A0Z9#{0}#{2}", id, string.IsNullOrEmpty(Configuration.Name) ? current.Request.Url.Host.Replace("www.", "") : Configuration.Name, token);
        }

        public static string CookieEncode(int id, string token = "")
        {
            return EncryptDecrypt.Encrypt(string.IsNullOrEmpty(token) ? Configuration.Name : token, Token(id, token, true));
        }

        public static int CookieDecode(string value, string token = "")
        {
            if (value.IsEmpty())
                return 0;

            var hash = EncryptDecrypt.Decrypt(string.IsNullOrEmpty(token) ? Configuration.Name : token, value);
            if (hash.IsEmpty())
                return 0;

            var index = hash.IndexOf('X');
            if (index == -1)
                return 0;

            var id = hash.Substring(0, index).To<int>();

            if (hash != Token(id, token, true))
                return 0;

            return id;
        }

        public static T CacheWrite<T>(string key, T value, DateTime expire)
        {
            return Configuration.CacheProvider.Write<T>(key, value, expire);
        }

        public static T CacheRead<T>(string key, Func<string, T> onEmpty = null)
        {
            return Configuration.CacheProvider.Read<T>(key, onEmpty);
        }

        public static void CacheRemove(string key)
        {
            Configuration.CacheProvider.Remove(key);
        }

        public static void CacheRemove(Func<string, bool> predicate)
        {
            Configuration.CacheProvider.Remove(predicate);
        }

        public static decimal Discount(int discount, decimal price, int decimals = 2)
        {
            if (discount == 0 || price == 0M)
                return price;

            return Math.Round(price - (price / 100) * discount, decimals);
        }

        public static decimal VAT(int vat, decimal price, int decimals = 2, bool includedVAT = true)
        {
            if (vat == 0 || price == 0M)
                return price;

            return includedVAT ? Math.Round(price / ((vat / 100M) + 1M), decimals) : Math.Round(price * ((vat / 100M) + 1M), decimals);
        }

        public static T[] ArrToArr<T>(string[] arr, Func<string, bool> onParse = null)
        {
            if (arr == null)
                return new T[0];

            var l = new List<T>(arr.Length);

            for (var i = 0; i < arr.Length; i++)
            {
                if (onParse != null && onParse(arr[i]))
                    l.Add(arr[i].To<T>());
                else if (onParse == null)
                    l.Add(arr[i].To<T>());
            }

            return l.ToArray();
        }

        public static void LogFile(params object[] value)
        {
            var now = DateTime.Now;
            var filename = now.ToString("yyyy-MM-dd") + ".log";
            var format = now.ToString("HH:mm:ss") + " - ";
            var index = 0;

            foreach (var m in value)
                format += (format.Length > 0 ? " " : "") + "{" + (index++) + "}";

            System.IO.File.AppendAllText(filename.PathData(), string.Format(Configuration.InvariantCulture, format, value) + Environment.NewLine);
        }

        public static void LogResponse(params object[] value)
        {
            var index = 0;
            var format = "LOG-> ";

            foreach (var m in value)
                format += (format.Length > 0 ? " " : "") + "{" + (index++) + "}";

            var response = HttpContext.Current.Response;
            var end = response.ContentType == "text/html" ? "<br />" : Environment.NewLine;

            response.Write(string.Format(Configuration.InvariantCulture, format, value) + end);
            response.Flush();
        }

        public static void Mail(string[] to, string body, Func<string, System.Net.Mail.MailMessage, string> preSend, Action<System.Net.Mail.MailMessage> onSend)
        {
            var pictures = new Dictionary<string, System.Net.Mail.LinkedResource>(10);

            foreach (var address in to)
            {
                var mail = new System.Net.Mail.MailMessage();
                mail.SubjectEncoding = Encoding.UTF8;
                mail.HeadersEncoding = Encoding.UTF8;
                mail.BodyEncoding = Encoding.UTF8;

                try
                {
                    mail.To.Add(address);
                }
                catch
                {
                    // preskočíme
                    continue;
                }

                var msg = preSend(body, mail);
                if (msg == null)
                    continue;

                var view = System.Net.Mail.AlternateView.CreateAlternateViewFromString(msg, null, "text/html");

                foreach (var i in pictures)
                    view.LinkedResources.Add(i.Value);

                mail.AlternateViews.Add(view);
                onSend(mail);
            }
        }

        public static string Get(string name)
        {
            var value = HttpContext.Current.Request.QueryString[name];

            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value;
        }

        public static string Post(string name)
        {
            var value = HttpContext.Current.Request.Form[name];

            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value;
        }

        public static int IdDecrypt(string hash, string token = "")
        {
            return Configuration.OnDecrypt(hash, token);
        }

        public static string IdEncrypt(int id, string token = "")
        {
            return Configuration.OnEncrypt(id, token);
        }

        public static string UnicodeDecode(string s)
        {
            var repeat = true;
            var index = 0;
            while (repeat)
            {
                index = s.IndexOf("\\u", index, StringComparison.InvariantCulture);

                if (index == -1)
                {
                    repeat = false;
                    continue;
                }

                repeat = true;
                var num = s.Substring(index + 2, 4);
                try
                {
                    s = s.Insert(index, Convert.ToChar(Convert.ToInt32(num, 16)).ToString());
                    s = s.Remove(index + 1, 6);
                }
                catch
                {
                    index++;
                }

            }
            return s;
        }

        public static string UnicodeEncode(string s)
        {
            var sb = new StringBuilder();
            foreach (var c in s)
            {
                var code = Convert.ToInt32(c);
                if (code > 192 || code == 64)
                    sb.Append("\\u" + code.ToString("X").PadLeft(4, '0'));
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

    }
    #endregion

    // ===============================================================================================
    // 
    // Library.[Exension]
    // 
    // ===============================================================================================

    // ===============================================================================================
    // Autor        : Peter Širka
    // Created      : 04. 05. 2008
    // Updated      : 02. 04. 2014
    // Description  : Extension methods
    // ===============================================================================================

    #region Extension
    public static class Extension
    {
        public static Others.Analytics Analytics(this Controller source)
        {
            return Configuration.Analytics;
        }

        public static T Deserialize<T>(this Controller source)
        {
            source.Request.InputStream.Seek(0, System.IO.SeekOrigin.Begin);
            var data = new System.IO.StreamReader(source.Request.InputStream).ReadToEnd();
            return Configuration.JsonProvider.DeserializeObject<T>(data);
        }

        public static void Mail(this Controller source, string type, string name, object model, Action<System.Net.Mail.MailMessage> message)
        {
            using (var mail = new System.Net.Mail.MailMessage())
            {
                var utf8 = Encoding.UTF8;
                var body = System.Net.Mail.AlternateView.CreateAlternateViewFromString(source.RenderToString(name, model), utf8, "text/html");
                mail.SubjectEncoding = utf8;
                mail.HeadersEncoding = utf8;
                mail.BodyEncoding = utf8;
                mail.AlternateViews.Add(body);
                message(mail);
                Configuration.OnMail(type, mail);
                using (var smtp = new System.Net.Mail.SmtpClient())
                    smtp.Send(mail);
            }
        }

        public static void Each<T>(this T[] source, Action<T> on)
        {
            if (source == null || source.Length == 0)
                return;

            for (var i = 0; i < source.Length; i++)
                on(source[i]);
        }

        public static bool ContainsKeys<K, V>(this Dictionary<K, V> source, bool allMustExist, params K[] key)
        {
            foreach (var m in key)
            {
                if (allMustExist)
                {
                    if (!source.ContainsKey(m))
                        return false;

                    continue;
                }

                if (source.ContainsKey(m))
                    return true;
            }

            return allMustExist;
        }

        public static void CopyTo(this object source, object target, params string[] withoutProperty)
        {
            var type = target.GetType();
            foreach (var propSource in source.GetType().GetProperties())
            {
                if (withoutProperty != null && withoutProperty.Contains(propSource.Name))
                    continue;

                if (!propSource.CanRead)
                    continue;

                var property = type.GetProperty(propSource.Name);
                if (property == null || !property.CanWrite)
                    continue;

                var value = propSource.GetValue(source, null);

                if (propSource.PropertyType == property.PropertyType)
                {
                    property.SetValue(target, value, null);
                    continue;
                }

                if (propSource.PropertyType == ConfigurationCache.type_string)
                {
                    if (property.PropertyType == ConfigurationCache.type_int)
                    {
                        property.SetValue(target, value.To<int>(), null);
                        continue;
                    }

                    if (property.PropertyType == ConfigurationCache.type_decimal)
                    {
                        property.SetValue(target, value.To<decimal>(), null);
                        continue;
                    }

                    if (property.PropertyType == ConfigurationCache.type_byte)
                    {
                        property.SetValue(target, value.To<byte>(), null);
                        continue;
                    }

                    if (property.PropertyType == ConfigurationCache.type_short)
                    {
                        property.SetValue(target, value.To<short>(), null);
                        continue;
                    }

                    if (property.PropertyType == ConfigurationCache.type_float)
                    {
                        property.SetValue(target, value.To<float>(), null);
                        continue;
                    }

                    if (property.PropertyType == ConfigurationCache.type_bool)
                    {
                        property.SetValue(target, value.To<bool>(), null);
                        continue;
                    }

                    if (property.PropertyType == ConfigurationCache.type_long)
                    {
                        property.SetValue(target, value.To<long>(), null);
                        continue;
                    }

                    if (property.PropertyType == ConfigurationCache.type_double)
                    {
                        property.SetValue(target, value.To<double>(), null);
                        continue;
                    }

                    continue;
                }

                if (property.PropertyType != ConfigurationCache.type_string)
                    continue;

                if (propSource.PropertyType == ConfigurationCache.type_int || propSource.PropertyType == ConfigurationCache.type_byte || propSource.PropertyType == ConfigurationCache.type_short || propSource.PropertyType == ConfigurationCache.type_long || propSource.PropertyType == ConfigurationCache.type_bool)
                {
                    property.SetValue(target, value.ToString(), null);
                    continue;
                }

                if (propSource.PropertyType == ConfigurationCache.type_decimal)
                {
                    property.SetValue(target, ((decimal)value).ToString(Configuration.InvariantCulture), null);
                    continue;
                }

                if (propSource.PropertyType == ConfigurationCache.type_float)
                {
                    property.SetValue(target, ((float)value).ToString(Configuration.InvariantCulture), null);
                    continue;
                }

                if (propSource.PropertyType == ConfigurationCache.type_double)
                {
                    property.SetValue(target, ((double)value).ToString(Configuration.InvariantCulture), null);
                    continue;
                }
            }
        }

        public static string PadLeft(this int source, int width, char padding = '0')
        {
            return source.ToString().PadLeft(width, padding);
        }

        public static string PadRight(this int source, int width, char padding = '0')
        {
            return source.ToString().PadRight(width, padding);
        }

        public static ActionResult ToJsonSuccess(this Controller source, string param = "")
        {
            if (string.IsNullOrEmpty(param))
                return new { r = true }.ToJson();
            return new { r = true, param }.ToJson();
        }

        public static ActionResult ToJsonError(this Controller source, string error, string language = "")
        {
            return error.ToJsonError(language);
        }

        public static IList<int> ToNumbers(this string source, bool allowZero = false)
        {
            var l = new List<int>();
            foreach (var m in source.Empty("").Split(','))
            {
                var number = m.Trim().To<int>();

                if (number <= 0 && !allowZero)
                    continue;

                l.Add(number);
            }

            return l;
        }

        public static IList<int> ToNumbers(this IList<string> source, bool allowZero = false)
        {
            var l = new List<int>();
            foreach (var m in source)
            {
                var number = m.Trim().To<int>();

                if (number <= 0 && !allowZero)
                    continue;

                l.Add(number);
            }

            return l;
        }

        public static bool IsXHR(this Controller source)
        {
            return source.Request.IsAjaxRequest();
        }

        public static IList<KeyValue> Validate(this Controller source, object model, bool allProperties = true)
        {
            return Utils.Validate(model, allProperties);
        }

        public static string Remove(this string source, string text)
        {
            return source.Replace(text, "");
        }

        public static bool IsImage(this HttpPostedFile source)
        {
            return source.ContentType.StartsWith("image/", StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsAudio(this HttpPostedFile source)
        {
            return source.ContentType.StartsWith("audio/", StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsVideo(this HttpPostedFile source)
        {
            return source.ContentType.StartsWith("video/", StringComparison.InvariantCultureIgnoreCase);
        }

        public static string JsCompress(this string source, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            if (encoding == null)
                encoding = Encoding.Default;

            var hash = "javascript." + EncryptDecrypt.Hash(source);

            return Utils.CacheRead<string>(hash, key =>
            {
                var js = new Modules.JavaScriptMinifier();
                string output;

                using (var src = new System.IO.MemoryStream(encoding.GetBytes(source)))
                    output = js.Minify(src).Replace('\n', ' ');

                if (Configuration.IsRelease)
                    return Utils.CacheWrite<string>(key, output, DateTime.Now.AddMinutes(5));

                return output;
            });
        }

        public static string CssCompress(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            var hash = "css." + EncryptDecrypt.Hash(source);

            return Utils.CacheRead<string>(hash, key => {
                var output = Modules.Less.Compile(source);

                if (Configuration.IsRelease)
                    return Utils.CacheWrite<string>(key, output, DateTime.Now.AddMinutes(5));

                return output;
            });
        }

        public static string Empty(this string source, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(source))
                return defaultValue;

            return source;
        }

        public static bool IsJson(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return false;

            if (source.Length <= 1)
                return false;

            var a = source[0];
            var b = source[source.Length - 1];

            return (a == '"' && b == '"') || (a == '[' && b == ']') || (a == '{' && b == '}');
        }

        public static bool NotModified(this HttpRequest source, string eTag)
        {
            if (string.IsNullOrEmpty(eTag))
                return false;

            var match = source.Headers["If-None-Match"];
            if (string.IsNullOrEmpty(match))
                return false;

            return match == eTag;
        }

        public static HttpResponse SetETag(this HttpResponse source, string eTag, bool addExpires = true)
        {
            source.Cache.SetCacheability(HttpCacheability.Public);
            source.Cache.SetETag(eTag);

            if (addExpires)
                source.Cache.SetExpires(DateTime.Now.AddDays(20));

            return source;
        }

        public static bool IsInvalid(this ModelStateDictionary source)
        {
            return !source.IsValid;
        }

        public static string ETag(this string source)
        {
            var sum = 0;
            foreach (var m in source)
                sum += Utils.Asc(m);
            return sum.ToString() + ":" + Configuration.Version;
        }

        public static string Template(this string source, params KeyValue[] param)
        {
            foreach (var p in param)
                source = source.Replace("@" + p.Key, p.Value.IsEmpty() ? "" : p.Value);
            return source;
        }

        public static byte[] ToByteArray(this System.IO.Stream source)
        {
            var data = new byte[source.Length];
            source.Read(data, 0, Convert.ToInt32(source.Length));
            return data;
        }

        public static string Pluralize(this int source, string zero, string one = "", string many = "", string other = "")
        {
            if (source == 0)
                return zero;

            if (source == 1)
                return one;

            if (source > 1 && source < 5)
                return other;

            return many;
        }

        public static string RenderToString(this Controller source, string name, object model)
        {
            source.ViewData.Model = model;
            using (var sw = new System.IO.StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(source.ControllerContext, name);
                var viewContext = new ViewContext(source.ControllerContext, viewResult.View, source.ViewData, source.TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(source.ControllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }

        public static void Problem(this Controller controller, string message, string source = null)
        {
            if (source == null)
                source = controller.GetType().Name;
            Configuration.InvokeProblem(source, message, controller.Request.Url, controller.Request.UserHostAddress);
        }

        public static void Change(this Controller controller, string message, string source = null)
        {
            if (source == null)
                source = controller.GetType().Name;
            Configuration.InvokeChange(source, message, controller.Request.Url, controller.Request.UserHostAddress);
        }

        public static void Log(this Controller controller, string message, string source = null)
        {
            if (source == null)
                source = controller.GetType().Name;
            Configuration.InvokeLog(source, message, controller.Request.Url, controller.Request.UserHostAddress);
        }

        public static void Error(this Controller controller, Exception error, string source = null)
        {
            if (source == null)
                source = controller.GetType().Name;
            Configuration.InvokeError(source, error, controller.Request.Url, controller.Request.UserHostAddress);
        }

        public static HtmlString RenderToHtmlString(this Controller source, string name, object model)
        {
            return new HtmlString(source.RenderToString(name, model));
        }

        public static ControllerCache Cache(this Controller source, string key, Action<Action<ControllerCache, DateTime?>> onEmpty)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return Utils.CacheRead<ControllerCache>(key, k =>
            {
                ControllerCache cache = null;
                DateTime expiration = DateTime.Now.AddMinutes(3);

                var output = new Action<ControllerCache, DateTime?>((render, expire) =>
                {
                    cache = render;

                    if (expire != null && expire.HasValue)
                        expiration = expire.Value;
                });

                onEmpty(output);

                if (cache != null)
                    return Utils.CacheWrite<ControllerCache>(k, cache, expiration);

                return cache;
            });
        }

        public static KeyValue ToError(this string source)
        {
            return KeyValue.Create(source, "@");
        }

        public static ActionResult ToJsonError(this string source, string language = "", Func<string, string, string> onReplace = null)
        {
            return KeyValue.Create(source, "@").ToJsonError(language, onReplace);
        }

        public static Dictionary<string, string> Errors(this ModelStateDictionary source)
        {
            var error = new Dictionary<string, string>(5);
            foreach (var k in source.Keys)
            {
                var el = source[k];
                if (el.Errors != null && el.Errors.Count > 0)
                {
                    if (!error.ContainsKey(k))
                        error.Add(k, el.Errors[0].ErrorMessage);
                }
            }
            return error;
        }

        public static ActionResult ToJsonError(this KeyValue source, string language = "", Func<string, string, string> onReplace = null)
        {
            if (Configuration.OnResource != null && source.Value.IsNotEmpty() && source.Value[0] == '@')
                source.Value = Configuration.OnResource(source.Value == "@" ? source.Key : source.Value.Substring(1), language);

            if (onReplace != null)
                source.Value = onReplace(source.Key, source.Value);

            return new [] { source }.ToJson();
        }

        public static ActionResult ToJsonError(this IEnumerable<KeyValue> source, string language = "", Func<string, string, string> onReplace = null)
        {
            // Analysis disable once PossibleMultipleEnumeration
            foreach (var m in source)
            {
                if (Configuration.OnResource != null && m.Value.IsNotEmpty() && m.Value[0] == '@')
                    m.Value = Configuration.OnResource(m.Value == "@" ? m.Key : m.Value.Substring(1), language);

                if (onReplace != null)
                    m.Value = onReplace(m.Key, m.Value);
            }

            return source.ToJson();
        }

        public static IList<KeyValue> ToError(this IList<KeyValue> source, string language = "", Func<string, string, string> onResource = null)
        {
            foreach (var m in source)
            {
                if (Configuration.OnResource != null && m.Value.IsNotEmpty() && m.Value[0] == '@')
                    m.Value = Configuration.OnResource(m.Value == "@" ? m.Key : m.Value.Substring(1), language);

                if (onResource != null)
                    m.Value = onResource(m.Key, m.Value);
            }

            return source;
        }

        public static ActionResult ToJsonError(this ModelStateDictionary source, string language = "", bool allowKeyDuplicate = true, Func<string, string, string> onReplace = null)
        {
            var error = new List<KeyValue>(source.Keys.Count);
            foreach (var k in source.Keys)
            {
                var el = source[k];
                if (el.Errors != null && el.Errors.Count > 0)
                {
                    if (allowKeyDuplicate)
                    {
                        foreach (var err in el.Errors)
                        {
                            var message = err.ErrorMessage;

                            if (string.IsNullOrEmpty(message))
                                message = "@";

                            if (Configuration.OnResource != null && message[0] == '@')
                                message = Configuration.OnResource(message == "@" ? k : message.Substring(1), language);

                            if (onReplace != null)
                                message = onReplace(k, message);

                            error.Add(KeyValue.Create(k, message));
                        }
                    }
                    else
                    {
                        var message = el.Errors[0].ErrorMessage;

                        if (string.IsNullOrEmpty(message))
                            message = "@";

                        if (Configuration.OnResource != null && message[0] == '@')
                            message = Configuration.OnResource(message == "@" ? k : message.Substring(1), language);

                        if (onReplace != null)
                            message = onReplace(k, message);

                        error.Add(KeyValue.Create(k, message));
                    }
                }
            }
            return error.ToJson();
        }

        public static bool IsEmail(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return false;

            var a = source.LastIndexOf('.');
            var b = source.IndexOf('@');
            var c = source[source.Length - 1];

            return source[0] != '.' && c != '.' && c != '_' && source.IndexOf(' ') == -1 && source.Length > 5 && a > 0 && b > 0 && a > b;
        }

        public static T To<T>(this string source)
        {
            return Utils.To<T>(source);
        }

        public static T To<T>(this bool source)
        {
            return Utils.To<T>(source);
        }

        public static T To<T>(this int source)
        {
            return Utils.To<T>(source);
        }

        public static T To<T>(this byte source)
        {
            return Utils.To<T>(source);
        }

        public static T To<T>(this float source)
        {
            return Utils.To<T>(source);
        }

        public static T To<T>(this double source)
        {
            return Utils.To<T>(source);
        }

        public static T To<T>(this object source)
        {
            return Utils.To<T>(source);
        }

        public static string Format(this int source)
        {
            string s = string.Empty;
            string n = source.ToString();

            if (n.Length < 4)
                return n;

            for (int i = 0; i < n.Length; i++)
                s = (n[(n.Length - 1) - i]) + (i % 3 == 0 ? " " : string.Empty) + s;
            return s.Trim();
        }

        public static string Format(this short source)
        {
            return Format(source);
        }

        public static string Format(this decimal source)
        {
            string s = source.ToString().Replace(',', '.');

            var index = s.IndexOf('.');
            if (index > 0)
            {
                var cislo = s.Substring(0, index);
                if (cislo.Length < 3)
                    return s;
                return cislo.To<int>().Format() + s.Substring(index).Replace(',', '.');
            }

            if (s.Length < 4)
                return s;

            return s.To<int>().Format();
        }

        public static string Format(this double source)
        {
            string s = source.ToString().Replace(',', '.');

            var index = s.IndexOf('.');
            if (index > 0)
            {
                var cislo = s.Substring(0, index);
                if (cislo.Length < 3)
                    return s;
                return cislo.To<int>().Format() + s.Substring(index).Replace(',', '.');
            }

            if (s.Length < 4)
                return s;

            return s.To<int>().Format();
        }

        public static string Format(this decimal source, int round)
        {
            var output = Math.Round(source, round).Format();
            var dot = output.IndexOf('.');

            if (dot == -1)
            {

                if (round == 0)
                    return output;

                output += '.';
                dot = output.Length;
            }
            else
                round++;

            var len = output.Substring(dot).Length;

            if (len < round)
                return output.PadRight(output.Length + (round - len), '0');

            return output;
        }

        public static string Format(this double source, int round)
        {
            var output = Math.Round(source, round).Format();
            var dot = output.IndexOf('.');

            if (dot == -1)
            {

                if (round == 0)
                    return output;

                output += '.';
                dot = output.Length;
            }
            else
                round++;

            var len = output.Substring(dot).Length;

            if (len < round)
                return output.PadRight(output.Length + (round - len), '0');

            return output;
        }

        public static string Format(this float source, int round)
        {
            return ((double)source).Format(round);
        }

        public static decimal FindNumber(this string source)
        {
            return source.RegExMatch("[\\d\\s]+(\\.|\\,)?\\d+").Value.Replace(" ", "").To<decimal>();
        }

        public static string Trim(this decimal source, int scale = 2, bool allowFormat = true, bool allowZero = false, bool fillZero = false)
        {
            var value = allowFormat ? source.Format(2) : source.ToString(Configuration.InvariantCulture);

            var index = value.IndexOf('.');
            if (index == 0 || index == -1)
                return value;

            var beg = value.Substring(0, index);
            var end = value.Substring(index + 1);

            if (end.Length > scale)
                end = end.Substring(0, scale);

            index = 0;
            foreach (var m in end)
            {
                if (m != '0')
                    break;

                index++;
            }

            if (allowZero == false && index == end.Length)
                return beg;

            if (fillZero && end.Length < scale)
            {
                for (var i = end.Length; i < scale; i++)
                    end += '0';
            }

            return beg + '.' + end;
        }

        public static string Max(this string source, int size, string endChars = "...")
        {
            if (string.IsNullOrEmpty(source))
                return source;

            if (source.Length > size)
                return source.Substring(0, size - endChars.Length) + endChars;

            return source;
        }

        public static string Search(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            return source.RemoveDiakritics().ToLower().Replace('y', 'i').Replace('w', 'v');
        }

        public static string Space(this string source, int maxChar, char delimiter)
        {
            if (source.Length > maxChar)
                return Max(source, maxChar);

            int i = maxChar - source.Length;
            if (i > 0)
            {
                for (int j = 0; j < i; j++)
                    source += delimiter;
            }

            return source;
        }

        public static string Link(this string source, int maxLength = 60)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            var sb = new StringBuilder();
            foreach (Match m in Regex.Matches(source.Trim(), "\\w+"))
                sb.Append((sb.Length > 0 ? "-" : string.Empty) + RemoveDiakritics(m.Value.ToLower()).Trim());

            var url = sb.ToString().Max(maxLength, "");

            if (url[url.Length - 1] == '-')
                return url.Substring(0, url.Length - 1);

            return url;
        }

        public static string UpperLower(this string source, int index, bool upper = true)
        {
            if (string.IsNullOrEmpty(source) || index > source.Length || index < 0)
                return source;

            var n = source.ToCharArray();
            n[index] = upper ? Char.ToUpper(n[index]) : Char.ToLower(n[index]);
            return new string(n);
        }

        public static string RemoveDiakritics(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            string ns = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            for (int i = 0; i < ns.Length; i++)
            {
                char c = ns[i];
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString();
        }

        public static string RemoveOperator(this string value, params char[] withoutOperator)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            char c;
            var sb = new StringBuilder();

            var ca = Utils.Asc('a');
            var cz = Utils.Asc('z');
            var cA = Utils.Asc('A');
            var cZ = Utils.Asc('Z');
            var c0 = Utils.Asc('0');
            var c9 = Utils.Asc('9');
            var caa = Utils.Asc('á');
            var czz = Utils.Asc('ž');
            var cAA = Utils.Asc('Á');
            var cZZ = Utils.Asc('Ž');

            for (int i = 0; i < value.Length; i++)
            {
                c = value[i];
                var n = Utils.Asc(c);
                if (((n >= ca) && (n <= cz)) || ((n >= cA) && (n <= cZ)) || ((n >= c0) && (n <= c9)) || ((n >= cAA) && (n <= cZZ)) || ((n >= caa) && (n <= czz)))
                    sb.Append(c);

                else if (withoutOperator != null)
                {
                    foreach (var o in withoutOperator)
                        if (n == Utils.Asc(o))
                            sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public static bool IsEmpty(this string source, int length = 0)
        {
            return (source == null || source.Length <= length);
        }

        public static bool IsNotEmpty(this string source, int length = 0)
        {
            return !IsEmpty(source, length);
        }

        public static bool IsNotEmpty(this Array source, int length = 0)
        {
            return !IsEmpty(source, length);
        }

        public static bool IsEmpty(this Array source, int length = 0)
        {
            return (source == null || source.Length <= length);
        }

        public static int Ticks(this DateTime source, bool useMinutes = false)
        {
            if (useMinutes)
                return Convert.ToInt32(string.Format("{0}{1}{2}", source.Month, source.Day, (source.Hour * 60) + source.Minute));
            return Convert.ToInt32(source.ToString("yyyyMdH"));
        }

        public static T Read<K, T>(this Dictionary<K, T> source, K key, T defaultValue = default(T))
        {
            if (source == null)
                return defaultValue;

            if (source.ContainsKey(key))
                return source[key];

            return defaultValue;
        }

        public static T Read<T>(this List<T> source, int index, T defaultValue = default(T))
        {
            if (source == null)
                return defaultValue;

            if (index >= source.Count)
                return defaultValue;

            if (index < 0)
                return defaultValue;

            return source[index];
        }

        public static T ReadProperty<T>(this Object source, string propertyName)
        {

            if (source == null)
                return default(T);

            var property = source.GetType().GetProperty(propertyName);
            if (property == null)
                return default(T);

            return property.GetValue(source, null).To<T>();
        }

        public static T Read<T>(this XElement source, T defaultValue = default(T))
        {
            if (source == null)
                return defaultValue;

            return Utils.To<T>(source.Value.To<T>());
        }

        public static T Read<T>(this XAttribute source, T defaultValue = default(T))
        {
            if (source == null)
                return defaultValue;

            return Utils.To<T>(source.Value.To<T>());
        }

        public static T Read<T>(this Array source, int index, T defaultValue = default(T))
        {
            if (source == null)
                return defaultValue;

            if (index >= source.Length)
                return defaultValue;

            if (index < 0)
                return defaultValue;

            return Utils.To<T>(source.GetValue(index));
        }

        public static void Route(this RouteCollection source, string url, string controller, string action)
        {

            if (url.Length > 1)
            {
                if (url[0] == '/')
                    url = url.Substring(1);

                if (url[url.Length - 1] == '/')
                    url = url.Substring(0, url.Length - 1);
            }

            if (url == "/")
                url = "";

            source.MapRoute(string.Format("{0}.{1}.{2}", url, controller, action).Hash("md5"), url, new { controller, action });
        }

        public static void Route(this RouteCollection source, string url, object defaults)
        {
            var t = defaults.GetType();
            var controller = t.GetProperty("controller").GetValue(defaults, null).ToString();
            var action = t.GetProperty("action").GetValue(defaults, null).ToString();
            source.MapRoute(string.Format("{0}.{1}.{2}", url, controller, action).Hash("md5"), url, defaults);
        }

        public static ContentResult ToJson(this object source, string contentType = "application/json")
        {
            return source.AsJson(contentType);
        }

        public static ContentResult AsJson(this object source, string contentType = "application/json")
        {
            var content = new ContentResult();
            content.Content = source.JsonSerialize();
            content.ContentType = contentType;
            content.ContentEncoding = Encoding.UTF8;

            // Solves problem with Safari
            HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.NoCache);

            return content;
        }

        public static string Url(this HttpRequestBase source, bool withHost = false)
        {
            var i = source.RawUrl.IndexOf('?');
            var u = i > -1 ? source.RawUrl.Substring(0, i) : source.RawUrl;
            return withHost ? string.Format("{0}://{1}{2}", source.Url.Scheme, source.Url.Host, u) : u;
        }

        public static string Url(this HttpRequest source, bool withHost = false)
        {
            var i = source.RawUrl.IndexOf('?');
            var u = i > -1 ? source.RawUrl.Substring(0, i) : source.RawUrl;
            return withHost ? string.Format("{0}://{1}{2}", source.Url.Scheme, source.Url.Host, u) : u;
        }

        public static string Host(this HttpRequest source, string url = "")
        {
            return string.Format("{0}://{1}{2}", source.Url.Scheme, source.Url.Host, url);
        }

        public static string Host(this HttpRequestBase source, string url = "")
        {
            return string.Format("{0}://{1}{2}", source.Url.Scheme, source.Url.Host, url);
        }

        public static string Hash(this string source, string type, string salt = "", Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;

            if (type.Equals("sha1", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var crypto = new System.Security.Cryptography.SHA1CryptoServiceProvider())
                {
                    if (encoding == null)
                        encoding = Encoding.UTF8;

                    var data = encoding.GetBytes(source + salt);
                    return BitConverter.ToString(crypto.ComputeHash(data)).Replace("-", string.Empty);
                }
            }

            if (type.Equals("sha256", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var crypto = new System.Security.Cryptography.SHA256CryptoServiceProvider())
                {
                    if (encoding == null)
                        encoding = Encoding.UTF8;

                    var data = encoding.GetBytes(source + salt);
                    return BitConverter.ToString(crypto.ComputeHash(data)).Replace("-", string.Empty);
                }
            }

            if (type.Equals("sha512", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var crypto = new System.Security.Cryptography.SHA512CryptoServiceProvider())
                {
                    if (encoding == null)
                        encoding = Encoding.UTF8;

                    var data = encoding.GetBytes(source + salt);
                    return BitConverter.ToString(crypto.ComputeHash(data)).Replace("-", string.Empty);
                }
            }

            if (type.Equals("md5", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var crypto = new System.Security.Cryptography.MD5CryptoServiceProvider())
                {
                    if (encoding == null)
                        encoding = Encoding.UTF8;

                    var data = encoding.GetBytes(source + salt);
                    return BitConverter.ToString(crypto.ComputeHash(data)).Replace("-", string.Empty);
                }
            }

            return EncryptDecrypt.Hash(source + salt);
        }

        public static string Hash(this System.IO.Stream source, string type = "md5")
        {
            source.Seek(0, System.IO.SeekOrigin.Begin);

            if (type.Equals("sha1", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var crypto = new System.Security.Cryptography.SHA1CryptoServiceProvider())
                    return BitConverter.ToString(crypto.ComputeHash(source)).Replace("-", string.Empty);
            }

            if (type.Equals("sha256", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var crypto = new System.Security.Cryptography.SHA256CryptoServiceProvider())
                    return BitConverter.ToString(crypto.ComputeHash(source)).Replace("-", string.Empty);
            }

            if (type.Equals("sha512", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var crypto = new System.Security.Cryptography.SHA512CryptoServiceProvider())
                    return BitConverter.ToString(crypto.ComputeHash(source)).Replace("-", string.Empty);
            }

            using (var crypto = new System.Security.Cryptography.MD5CryptoServiceProvider())
                return BitConverter.ToString(crypto.ComputeHash(source)).Replace("-", string.Empty);
        }

        public static int IdDecrypt(string hash, string token = "")
        {
            return Configuration.OnDecrypt(hash, token);
        }

        public static string IdEncrypt(int id, string token = "")
        {
            return Configuration.OnEncrypt(id, token);
        }

        public static string JsonSerialize(this object source)
        {
            return Configuration.JsonProvider.Serialize(source);
        }

        public static T JsonDeserialize<T>(this string source)
        {
            return Configuration.JsonProvider.DeserializeObject<T>(source);
        }

        public static dynamic JsonDeserialize(this string source)
        {
            return Configuration.JsonProvider.DeserializeObject(source);
        }

        public static string Path(this string source, char path = '/')
        {
            if (string.IsNullOrEmpty(source))
                return source;

            /*
            if (source[0] != path)
                source = path + source;
            */

            if (source[source.Length - 1] != path)
                source = source + path;

            return source;
        }

        public static string PathMap(this string source)
        {
            return HttpContext.Current.Server.MapPath(source);
        }

        public static string PathTemporary(this string source)
        {
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Configuration.PathTemporary, source);
        }

        public static string PathData(this string source)
        {
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", source);
        }

        public static string PathHelpers(this string source)
        {
            return string.Format("~/Views/Helpers/{0}.cshtml", source);
        }

        public static string PathShared(this string source)
        {
            return string.Format("~/Views/Shared/{0}.cshtml", source);
        }

        public static string Conjugation(this DateTime source, string language = "")
        {
            return Configuration.OnDateConjugation(source, language);
        }

        public static T PropertyEach<T>(this T source, Action<T, PropertyInfo> onProperty)
        {
            if (source != null)
            {
                foreach (var p in source.GetType().GetProperties())
                    onProperty(source, p);
            }
            return source;
        }

        public static T PropertyTrim<T>(this T source, params string[] withoutProperty)
        {
            return PropertyEach<T>(source, (o, p) =>
            {
                if (withoutProperty != null && withoutProperty.Length > 0 && withoutProperty.Contains(p.Name))
                    return;

                if (p.PropertyType != ConfigurationCache.type_string)
                    return;

                var v = p.GetValue(o, null);
                if (v == null)
                    return;

                p.SetValue(o, v.ToString().Trim(), null);

            });
        }

        public static T PropertyUpperFirst<T>(this T source, params string[] property)
        {
            if (property == null || property.Length == 0)
                return source;

            return PropertyEach<T>(source, (o, p) =>
            {
                if (p.PropertyType != ConfigurationCache.type_string)
                    return;

                if (!property.Contains(p.Name))
                    return;

                var v = p.GetValue(o, null);
                if (v == null)
                    return;

                p.SetValue(o, v.ToString().UpperLower(0), null);

            });
        }

        public static string Join<T>(this T[] source, string delimiter = ", ")
        {
            var sb = new StringBuilder();
            foreach (T i in source)
                sb.Append((sb.Length > 0 ? delimiter : string.Empty) + Utils.To<string>(i));
            return sb.ToString();
        }

        public static string Join<T>(this IList<T> source, string delimiter = ", ")
        {
            var sb = new StringBuilder();
            foreach (T i in source)
                sb.Append((sb.Length > 0 ? delimiter : string.Empty) + Utils.To<string>(i));
            return sb.ToString();
        }

        public static string format(this string source, params object[] args)
        {
            return string.Format(Configuration.InvariantCulture, source, args);
        }

        public static bool Contains(this string source, params string[] value)
        {
            return Contains(source, false, value);
        }

        public static bool Contains(this string source, bool allMustExist, params string[] value)
        {
            if (allMustExist)
            {
                foreach (string j in value)
                    if (!source.Contains(j))
                        return false;
                return true;
            }

            foreach (string i in value)
                if (source.Contains(i))
                    return true;
            return false;
        }

        public static bool Contains<T>(this IList<T> source, bool allMustExist, params T[] value)
        {
            if (allMustExist)
            {
                foreach (T i in value)
                {
                    if (!source.Contains(i))
                        return false;
                }
                return true;
            }

            foreach (T i in value)
                if (source.Contains(i))
                    return true;

            return false;
        }

        public static string RegExReplace(this string source, string pattern, string replaceTo)
        {
            return Regex.Replace(source, pattern, replaceTo);
        }

        public static Match RegExMatch(this string source, string pattern)
        {
            return Regex.Match(source, pattern);
        }

        public static bool RegExContains(this string source, string pattern)
        {
            return Regex.Match(source, pattern).Success;
        }

        public static MatchCollection RegExMatches(this string source, string pattern)
        {
            return Regex.Matches(source, pattern);
        }

        public static string UrlEncode(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;
            return HttpUtility.UrlEncode(source);
        }

        public static string UrlDecode(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;
            return HttpUtility.UrlDecode(source);
        }

        public static string HtmlEncode(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;
            return source.Replace("<", "&lt;").Replace(">", "&gt").Replace("\"", "&quot;").Replace("'", "&#39;");
        }

        public static string HtmlDecode(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;
            return source.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&#39;", "'");
        }

        public static string Url(this string source)
        {
            if (source.IsEmpty())
                return source;

            if (source.StartsWith("http://"))
                return source;

            if (source.StartsWith("https://"))
                return source;
                
            return "http://" + source;
        }

        public static StringBuilder AppendConfiguration(this StringBuilder source, string name, object value, int padding = 15)
        {
            if (source.Length > 0 && source[source.Length - 1] != '\n')
                source.Append('\n');

            return source.Append(string.Format("{0}: {1}", name.PadRight(padding, ' '), value));
        }

        public static StringBuilder Append(this StringBuilder source, object value, string divider)
        {
            if (source.Length > 0)
                source.Append(divider);

            return source.Append(value);
        }

        public static StringBuilder AppendParam(this StringBuilder source, string name, object value)
        {
            if (source.Length > 0)
                source.Append('&');

            return source.Append(name).Append('=').Append(value == null ? string.Empty : value.ToString().UrlEncode());
        }

        public static StringBuilder AppendAttribute(this StringBuilder source, string param, object value)
        {
            if (source.Length > 0)
                source.Append(' ');

            return source.Append(param).Append("=\"").Append(value == null ? string.Empty : value.ToString().HtmlEncode()).Append('\"');
        }

        public static StringBuilder AppendStyle(this StringBuilder source, string param, object value)
        {
            if (source.Length > 0)
                source.Append(';');
            return source.Append(param).Append(':').Append(value);
        }

        public static string RSS(this DateTime source)
        {
            return source.ToString("r").Replace("GMT", "+0200");
        }

        public static T FindId<T>(this string source, bool fromBeg = true, char divider = '-')
        {
            int index;

            if (fromBeg)
            {
                index = source.IndexOf(divider);
                if (index > -1)
                    return source.Substring(0, index).To<T>();
            }
            else
            {
                index = source.LastIndexOf(divider);
                if (index > -1)
                {
                    index++;
                    return source.Substring(index, source.Length - index).To<T>();
                }
            }

            return default(T);
        }

        public static int Decrypt(this string source, string token = "")
        {
            return Utils.IdDecrypt(source, token);
        }

        public static string Encrypt(this int source, string token = "")
        {
            return Utils.IdEncrypt(source, token);
        }

        public static IList<string> Keywords(this string source, int minLength = 4, bool strict = false)
        {
            var output = new List<string>(5);

            if (string.IsNullOrEmpty(source) || source.Length < minLength)
                return output;

            var zoznam = new Dictionary<string, int>(10);
            var tmp = new Dictionary<string, string>(10);

            foreach (Match m in source.RegExMatches("\\w{" + minLength + ",}"))
            {
                var key = (strict ? m.Value : m.Value.ToLower().RemoveDiakritics().Replace('y', 'i'));
                string value;

                if (tmp.TryGetValue(key, out value))
                {
                    zoznam[value]++;
                    continue;
                }

                tmp.Add(key, m.Value);
                zoznam.Add(m.Value, 1);
            }

            foreach (var m in zoznam.OrderByDescending(n => n.Value))
                output.Add(m.Key);

            return output;
        }

        public static Dictionary<string, string> ParseConfiguration(this string body)
        {
            var zoznam = new Dictionary<string, string>(10);
            foreach (var m in body.Split('\n'))
            {
                var index = m.IndexOf(':');
                if (index == -1)
                    continue;

                var key = m.Substring(0, index).Trim();
                var value = m.Substring(index + 1).Trim();

                if (zoznam.ContainsKey(key))
                    zoznam[key] = value;
                else
                    zoznam.Add(key, value);
            }

            return zoznam;
        }

        public static IList<KeyValue> ParseKeyValues(this string source)
        {
            var zoznam = new List<KeyValue>(5);

            if (string.IsNullOrEmpty(source))
                return zoznam;

            foreach (var m in source.Split('\n'))
            {
                var value = ParseKeyValue(m);
                if (value != null)
                    zoznam.Add(value);
            }

            return zoznam;
        }

        public static KeyValue ParseKeyValue(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return null;

            var index = source.IndexOf(':');
            if (index == -1)
                return null;

            return new KeyValue(source.Substring(0, index).Trim(), source.Substring(index + 1).Trim());
        }

        public static void Head(this Controller source, string value)
        {
            var content = "";
            var key = "$HEAD";

            if (source.ViewData.ContainsKey(key))
                content = source.ViewData[key].ToString();

            if (value[0] == '<')
            {
                source.ViewData[key] = content + value;
                return;
            }

            var end = value.LastIndexOf('.');
            if (end == -1)
            {
                source.ViewData[key] = content + value;
                return;
            }

            var isOut = (value.StartsWith("//", StringComparison.InvariantCultureIgnoreCase) || value.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || value.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase));

            switch (value.Substring(end).ToLower())
            {
                case ".js":
                    value = "<script type=\"text/javascript\" src=\"{0}\"></script>".format((isOut ? String.Empty : Configuration.Url.JS) + value);
                    break;
                case ".css":
                    value = "<link type=\"text/css\" rel=\"stylesheet\" href=\"{0}\" />".format((isOut ? String.Empty : Configuration.Url.CSS) + value);
                    break;
            }

            source.ViewData[key] = content + value;
        }

        public static void Title(this Controller source, string value)
        {
            source.ViewData["Title"] = value;
        }

        public static void Description(this Controller source, string value)
        {
            source.ViewData["Description"] = value;
        }

        public static void Keywords(this Controller source, string value)
        {
            source.ViewData["Keywords"] = value;
        }

        public static T Proxy<T>(this Controller source, string key, string url, StringBuilder data = null, string method = "POST")
        {
            return Utils.Proxy<T>(key, url, data, method);
        }
    }
    #endregion

    // ===============================================================================================
    // 
    // Library.[ImageConverter]
    // 
    // ===============================================================================================

    // ===============================================================================================
    // Autor        : Peter Širka
    // Created      : 04. 05. 2008
    // Updated      : 20. 03. 2013
    // Description  : Image Converter for image manipulation
    // ===============================================================================================

    #region ImageConverter
    public sealed class ImageConverter : IDisposable
    {
        private class ConvolutionMatrix
        {
            public int MatrixSize = 3;

            public double[,] Matrix;
            public double Factor = 1;
            public double Offset = 1;

            public ConvolutionMatrix(int size)
            {
                MatrixSize = 3;
                Matrix = new double[size, size];
            }

            public void SetAll(double value)
            {
                for (int i = 0; i < MatrixSize; i++)
                {
                    for (int j = 0; j < MatrixSize; j++)
                        Matrix[i, j] = value;
                }
            }
        }

        private Bitmap BMP = null;
        public bool IsLoaded { get; set; }

        public Bitmap Bitmap
        {
            get { return BMP; }
            set { BMP = value; }
        }

        public int Width
        {
            get { return (BMP != null ? BMP.Width : 0); }
        }

        public int Height
        {
            get { return BMP == null ? 0 : BMP.Height; }
        }

        public static double ColorCompare(Color color1, Color color2)
        {
            return Math.Sqrt(Math.Pow(Convert.ToDouble(color1.B) - Convert.ToDouble(color2.B), 2.0) + Math.Pow(Convert.ToDouble(color1.G) - Convert.ToDouble(color2.G), 2.0) + Math.Pow(Convert.ToDouble(color1.R) - Convert.ToDouble(color2.R), 2.0));
        }

        public byte[] SaveAs(System.Drawing.Imaging.ImageFormat format)
        {
            using (var ms = new System.IO.MemoryStream())
            {
                using (var b = new Bitmap(BMP))
                {
                    b.Save(ms, format);
                    return ms.ToArray();
                }
            }
        }

        public static System.Drawing.Imaging.ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            System.Drawing.Imaging.ImageCodecInfo[] codecs = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
            for (int i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == mimeType)
                    return codecs[i];
            return null;
        }

        public byte[] SaveAsJpg(uint quality)
        {
            System.Drawing.Imaging.ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");
            System.Drawing.Imaging.EncoderParameters encoderParams = new System.Drawing.Imaging.EncoderParameters(1);
            encoderParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            using (var ms = new System.IO.MemoryStream())
            {
                using (var b = new Bitmap(BMP))
                {
                    b.Save(ms, jpegCodec, encoderParams);
                    return ms.ToArray();
                }
            }
        }

        public void SaveToStream(System.IO.Stream stream, System.Drawing.Imaging.ImageFormat format)
        {
            using (var b = new Bitmap(BMP))
                b.Save(stream, format);
        }

        public void SaveToStreamJpg(System.IO.Stream stream, uint quality)
        {
            System.Drawing.Imaging.ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");
            System.Drawing.Imaging.EncoderParameters encoderParams = new System.Drawing.Imaging.EncoderParameters(1);
            encoderParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            using (var b = new Bitmap(BMP))
                b.Save(stream, jpegCodec, encoderParams);
        }

        public Color Pixel(int x, int y)
        {
            return BMP.GetPixel(x, y);
        }

        public Color Pixel()
        {
            return BMP.GetPixel(1, 1);
        }

        public Graphics Graphics
        {
            get { return System.Drawing.Graphics.FromImage(BMP); }
        }

        public ImageConverter(string fileName)
        {
            IsLoaded = LoadFromFile(fileName);
        }

        public ImageConverter(byte[] fileData)
        {
            IsLoaded = LoadFromByte(fileData);
        }

        public ImageConverter(Uri url)
        {
            IsLoaded = LoadFromUrl(url);
        }

        public ImageConverter(System.IO.Stream stream)
        {
            IsLoaded = LoadFromStream(stream);
        }

        public ImageConverter(Image image)
        {
            BMP = new Bitmap(image);
            BMP.SetResolution(80, 60);
        }

        public ImageConverter(int width, int height)
        {
            BMP = new Bitmap(width, height);
            BMP.SetResolution(80, 60);
            IsLoaded = true;
        }

        public bool LoadFromFile(string fileName)
        {
            if (!System.IO.File.Exists(fileName))
            {
                IsLoaded = false;
                return false;
            }

            try
            {
                BMP = new Bitmap(fileName);
                BMP.SetResolution(80, 60);
            }
            catch
            {
                IsLoaded = false;
                return false;
            }

            IsLoaded = true;
            return true;
        }

        public bool LoadFromStream(System.IO.Stream stream)
        {
            try
            {
                BMP = new Bitmap(stream);
                BMP.SetResolution(80, 60);
            }
            catch
            {
                IsLoaded = false;
                return false;
            }

            IsLoaded = true;
            return true;
        }

        public bool LoadFromByte(byte[] fileData)
        {
            if (fileData.Length > 0)
            {
                try
                {
                    using (var MemoryStream = new System.IO.MemoryStream(fileData))
                    {
                        BMP = new Bitmap(MemoryStream);
                        BMP.SetResolution(80, 60);
                        IsLoaded = true;
                        return true;
                    }
                }
                catch
                {
                    IsLoaded = false;
                    return false;
                }
            }

            IsLoaded = false;
            return false;
        }

        public bool LoadFromUrl(Uri url)
        {
            using (var wc = new System.Net.WebClient())
            {
                try
                {
                    var b = wc.DownloadData(url);
                    if (b == null)
                        return false;

                    IsLoaded = LoadFromByte(b);
                    return IsLoaded;

                }
                catch
                {
                    IsLoaded = false;
                    return false;
                }
            }
        }

        private bool CheckBitmap()
        {
            return IsLoaded && BMP != null;
        }

        public ImageConverter SaveToFile(string fileName, System.Drawing.Imaging.ImageFormat format)
        {
            if (!CheckBitmap())
                return this;

            BMP.Save(fileName, format);
            return this;
        }

        public ImageConverter SaveToFileAsJpg(string fileName, byte quality)
        {
            if (!CheckBitmap())
                return this;
            System.IO.File.WriteAllBytes(fileName, SaveAsJpg (quality));
            return this;
        }

        public System.IO.Stream SaveToStream(System.Drawing.Imaging.ImageFormat format)
        {
            if (!CheckBitmap())
                return null;
            var OutStream = new System.IO.MemoryStream();
            BMP.Save(OutStream, format);
            return OutStream;
        }

        public void Free()
        {
            if (!CheckBitmap())
                return;

            BMP.Dispose();
            BMP = null;
        }

        public ImageConverter Resize(int width, int height, System.Drawing.Drawing2D.InterpolationMode resizeFilter)
        {
            if (!CheckBitmap())
                return this;

            var newBMP = new Bitmap(width, height);
            newBMP.SetResolution(80, 60);

            using (var picture = System.Drawing.Graphics.FromImage(newBMP))
            {
                picture.InterpolationMode = resizeFilter;
                picture.DrawImage(BMP, 0, 0, width, height);
                BMP.Dispose();
                BMP = newBMP;
            }
            return this;
        }

        public ImageConverter ResizeWidth(int width, System.Drawing.Drawing2D.InterpolationMode resizeFilter)
        {
            if (!CheckBitmap())
                return this;

            int BmpWidth = width;
            int BmpHeight = (int)(Math.Round((double)width / ((double)BMP.Width / (double)BMP.Height)));
            Resize(BmpWidth, BmpHeight, resizeFilter);
            return this;
        }

        public ImageConverter ResizeCenter(int width, int height, System.Drawing.Drawing2D.InterpolationMode resizeFilter)
        {

            if (width >= height)
            {
                if (Width > Height)
                    ResizeHeight(height, resizeFilter);
                else
                    ResizeWidth(width, resizeFilter);
            }
            else
            {
                if (Height > Width)
                    ResizeHeight(height, resizeFilter);
                else
                    ResizeWidth(width, resizeFilter);
            }

            SetSize(new SolidBrush(Pixel(1, 1)), width, height, width / 2 - Width / 2, height / 2 - Height / 2);
            return this;
        }

        public ImageConverter SetSize(Brush background, int width, int height, float x, float y)
        {
            if (!CheckBitmap())
                return this;

            var newBMP = new Bitmap(width, height);
            newBMP.SetResolution(80, 60);

            using (var g = Graphics.FromImage(newBMP))
            {
                g.FillRegion(background, new Region(new Rectangle(0, 0, width, height)));
                g.DrawImage(BMP, x, y, BMP.Width, BMP.Height);
            }

            BMP.Dispose();
            BMP = newBMP;
            return this;
        }

        public ImageConverter SetPosition(Brush background, float x, float y)
        {
            if (!CheckBitmap())
                return this;

            var newBMP = new Bitmap(BMP.Width, BMP.Height);
            newBMP.SetResolution(80, 60);

            using (var g = Graphics.FromImage(newBMP))
            {
                g.FillRegion(background, new Region(new Rectangle(0, 0, Width, Height)));
                g.DrawImage(BMP, x, y, BMP.Width, BMP.Height);
            }

            BMP.Dispose();
            BMP = newBMP;
            return this;
        }

        public ImageConverter ResizeHeight(int heigth, System.Drawing.Drawing2D.InterpolationMode resizeFilter)
        {
            if (!CheckBitmap())
                return this;

            int BmpWidth;
            int BmpHeight;

            BmpHeight = heigth;
            BmpWidth = (int)(Math.Round(Convert.ToDouble(heigth) / ((double)BMP.Height / (double)BMP.Width)));
            Resize(BmpWidth, BmpHeight, resizeFilter);
            return this;
        }

        public ImageConverter Watermark(string fileName, float opacity, int x, int y, System.Drawing.Drawing2D.InterpolationMode drawingFilter)
        {
            return Watermark(System.IO.File.ReadAllBytes(fileName), opacity, x, y, drawingFilter);
        }

        public ImageConverter Watermark(byte[] fileData, float opacity, int x, int y, System.Drawing.Drawing2D.InterpolationMode drawingFilter)
        {
            if (!CheckBitmap())
                return this;

            using (var newBMP = new Bitmap(new System.IO.MemoryStream(fileData)))
            {
                newBMP.SetResolution(80, 60);
                var b = ApplyOpacity(newBMP, opacity);

                using (var picture = System.Drawing.Graphics.FromImage(BMP))
                {

                    if (x == -1)
                    {
                        if (BMP.Width > newBMP.Width)
                            x = ((BMP.Width / 2) - (b.Width / 2));
                        else
                            x = ((b.Width / 2) - (BMP.Width / 2));
                    }

                    if (y == -1)
                    {
                        if (BMP.Height > newBMP.Height)
                            y = ((BMP.Height / 2) - (b.Height / 2));
                        else
                            y = ((b.Height / 2) - (BMP.Height / 2));
                    }

                    picture.InterpolationMode = drawingFilter;
                    picture.DrawImage(b, x, y, b.Width, b.Height);
                }
            }
            return this;
        }

        public ImageConverter ResizeTo(int width, int height, Brush background, System.Drawing.Drawing2D.InterpolationMode resizeFilter)
        {
            if (!CheckBitmap())
                return this;

            int BmpWidth;
            int BmpHeight;
            int x;
            int y;

            if (BMP.Width > BMP.Height)
            {
                if (width > height)
                {
                    BmpWidth = (int)(Math.Round((double)height / ((double)BMP.Height / (double)BMP.Width)));
                    BmpHeight = (int)(Math.Round((double)BmpWidth / ((double)BMP.Width / (double)BMP.Height)));
                    if (BmpWidth > width)
                    {
                        BmpHeight = (int)(Math.Round((double)width / ((double)BMP.Width / (double)BMP.Height)));
                        BmpWidth = (int)(Math.Round((double)BmpHeight / ((double)BMP.Height / (double)BMP.Width)));
                    }
                }
                else
                {
                    BmpHeight = (int)(Math.Round((double)width / ((double)BMP.Width / (double)BMP.Height)));
                    BmpWidth = (int)(Math.Round((double)BmpHeight / ((double)BMP.Height / (double)BMP.Width)));
                }
            }
            else
            {
                if (height > width)
                {
                    BmpWidth = (int)(Math.Round((double)height / ((double)BMP.Height / (double)BMP.Width)));
                    BmpHeight = (int)(Math.Round((double)BmpWidth / ((double)BMP.Width / (double)BMP.Height)));
                    if (BmpWidth > width)
                    {
                        BmpHeight = (int)(Math.Round((double)width / ((double)BMP.Width / (double)BMP.Height)));
                        BmpWidth = (int)(Math.Round((double)BmpHeight / ((double)BMP.Height / (double)BMP.Width)));
                    }
                }
                else
                {
                    BmpWidth = (int)(Math.Round((double)height / ((double)BMP.Height / (double)BMP.Width)));
                    BmpHeight = (int)(Math.Round((double)BmpWidth / ((double)BMP.Width / (double)BMP.Height)));
                }
            }

            Bitmap newBMP = new Bitmap(width, height);
            newBMP.SetResolution(80, 60);

            using (var picture = System.Drawing.Graphics.FromImage(newBMP))
            {
                picture.FillRegion(background, new Region(new Rectangle(0, 0, width, height)));

                x = (width / 2) - (BmpWidth / 2);
                y = (height / 2) - (BmpHeight / 2);

                picture.InterpolationMode = resizeFilter;
                picture.DrawImage(BMP, x, y, BmpWidth, BmpHeight);

                BMP.Dispose();
                BMP = newBMP;
            }
            return this;
        }

        public ImageConverter Crop(int top, int left, int bottom, int right)
        {
            if (!CheckBitmap())
                return this;

            Rectangle r = new Rectangle(left, top, BMP.Width - right, BMP.Height - bottom);
            Bitmap newBMP = BMP.Clone(r, BMP.PixelFormat);
            BMP.Dispose();
            BMP = newBMP;
            return this;
        }

        public ImageConverter Draw(Action<Graphics> onDraw)
        {
            using (var g = Graphics.FromImage(BMP))
                onDraw(g);
            return this;
        }

        public void Dispose()
        {
            if (BMP != null)
            {
                BMP.Dispose();
                BMP = null;
            }
        }

        public ImageConverter Rotate(Brush background, float angle)
        {
            if (!CheckBitmap())
                return this;

            Bitmap returnBitmap = new Bitmap(BMP.Width, BMP.Height);
            returnBitmap.SetResolution(80, 60);

            using (var g = System.Drawing.Graphics.FromImage(returnBitmap))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                g.FillRegion(background, new Region(new Rectangle(0, 0, Width, Height)));
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TranslateTransform((float)BMP.Width / 2, (float)BMP.Height / 2);
                g.RotateTransform(angle);
                g.TranslateTransform(-(float)BMP.Width / 2, -(float)BMP.Height / 2);
                g.DrawImage(BMP, new Point(0, 0));
                BMP.Dispose();
                BMP = returnBitmap;
            }
            return this;
        }

        public ImageConverter GrayScale()
        {
            if (!CheckBitmap())
                return this;

            var newBitmap = new Bitmap(BMP.Width, BMP.Height);
            newBitmap.SetResolution(80, 60);

            using (var g = System.Drawing.Graphics.FromImage(newBitmap))
            {
                var colorMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][] { new float[] { .3f, .3f, .3f, 0, 0 }, new float[] { .59f, .59f, .59f, 0, 0 }, new float[] { .11f, .11f, .11f, 0, 0 }, new float[] { 0, 0, 0, 1, 0 }, new float[] { 0, 0, 0, 0, 1 } });
                var attributes = new System.Drawing.Imaging.ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);

                g.DrawImage(BMP, new Rectangle(0, 0, BMP.Width, BMP.Height), 0, 0, BMP.Width, BMP.Height, GraphicsUnit.Pixel, attributes);
            }

            BMP.Dispose();
            BMP = newBitmap;
            return this;
        }

        public static Bitmap GrayScale(Bitmap bmp)
        {

            var newBitmap = new Bitmap(bmp.Width, bmp.Height);
            newBitmap.SetResolution(80, 60);

            using (var g = System.Drawing.Graphics.FromImage(newBitmap))
            {
                var colorMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][] { new float[] { .3f, .3f, .3f, 0, 0 }, new float[] { .59f, .59f, .59f, 0, 0 }, new float[] { .11f, .11f, .11f, 0, 0 }, new float[] { 0, 0, 0, 1, 0 }, new float[] { 0, 0, 0, 0, 1 } });
                var attributes = new System.Drawing.Imaging.ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);

                g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);
            }

            return newBitmap;
        }

        public ImageConverter SetOpacity(float opacity)
        {
            using (var bmp = new Bitmap(BMP.Width, BMP.Height))
            {
                bmp.SetResolution(80, 60);
                using (var bmpGraphics = System.Drawing.Graphics.FromImage(bmp))
                {
                    var colorMatrix = new System.Drawing.Imaging.ColorMatrix();
                    colorMatrix.Matrix33 = opacity;
                    var imageAttribute = new System.Drawing.Imaging.ImageAttributes();
                    imageAttribute.SetColorMatrix(colorMatrix, System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);
                    bmpGraphics.DrawImage(BMP, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, imageAttribute);
                    BMP.Dispose();
                    BMP = bmp;
                    return this;
                }
            }
        }

        public static Bitmap ApplyOpacity(Bitmap b, float opacity)
        {
            var nb = new Bitmap(b.Width, b.Height);
            nb.SetResolution(80, 60);
            using (var bmpGraphics = System.Drawing.Graphics.FromImage(b))
            {
                var colorMatrix = new System.Drawing.Imaging.ColorMatrix();
                colorMatrix.Matrix33 = opacity;
                var imageAttribute = new System.Drawing.Imaging.ImageAttributes();
                imageAttribute.SetColorMatrix(colorMatrix, System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);
                bmpGraphics.DrawImage(b, new Rectangle(0, 0, nb.Width, nb.Height), 0, 0, nb.Width, nb.Height, GraphicsUnit.Pixel, imageAttribute);
                return b;
            }
        }

        public Bitmap Copy()
        {
            return (Bitmap)BMP.Clone();
        }

        public static Bitmap ApplyGreyscale(Bitmap b)
        {
            byte A, R, G, B;
            Color pixelColor;
            var nb = (Bitmap)b.Clone();

            for (int y = 0; y < b.Height; y++)
            {
                for (int x = 0; x < b.Width; x++)
                {
                    pixelColor = b.GetPixel(x, y);
                    A = pixelColor.A;
                    R = (byte)((0.299 * pixelColor.R) + (0.587 * pixelColor.G) + (0.114 * pixelColor.B));
                    G = B = R;
                    nb.SetPixel(x, y, Color.FromArgb((int)A, (int)R, (int)G, (int)B));
                }
            }
            return nb;
        }

        public static Bitmap ApplyGamma(Bitmap bmp, double r, double g, double b)
        {
            byte A, R, G, B;
            Color pixelColor;

            var bitmapImage = (Bitmap)bmp.Clone();

            byte[] redGamma = new byte[256];
            byte[] greenGamma = new byte[256];
            byte[] blueGamma = new byte[256];

            for (int i = 0; i < 256; ++i)
            {
                redGamma[i] = (byte)Math.Min(255, (int)((255.0
                    * Math.Pow(i / 255.0, 1.0 / r)) + 0.5));
                greenGamma[i] = (byte)Math.Min(255, (int)((255.0
                    * Math.Pow(i / 255.0, 1.0 / g)) + 0.5));
                blueGamma[i] = (byte)Math.Min(255, (int)((255.0
                    * Math.Pow(i / 255.0, 1.0 / b)) + 0.5));
            }

            for (int y = 0; y < bitmapImage.Height; y++)
            {
                for (int x = 0; x < bitmapImage.Width; x++)
                {
                    pixelColor = bitmapImage.GetPixel(x, y);
                    A = pixelColor.A;
                    R = redGamma[pixelColor.R];
                    G = greenGamma[pixelColor.G];
                    B = blueGamma[pixelColor.B];
                    bitmapImage.SetPixel(x, y, Color.FromArgb((int)A, (int)R, (int)G, (int)B));
                }
            }
            return bitmapImage;
        }


        public static Bitmap ApplyContrast(Bitmap b, double contrast)
        {
            var bitmapImage = (Bitmap)b.Clone();

            double A, R, G, B;

            Color pixelColor;

            contrast = (100.0 + contrast) / 100.0;
            contrast *= contrast;

            for (int y = 0; y < bitmapImage.Height; y++)
            {
                for (int x = 0; x < bitmapImage.Width; x++)
                {
                    pixelColor = bitmapImage.GetPixel(x, y);
                    A = pixelColor.A;

                    R = pixelColor.R / 255.0;
                    R -= 0.5;
                    R *= contrast;
                    R += 0.5;
                    R *= 255;

                    if (R > 255)
                    {
                        R = 255;
                    }
                    else if (R < 0)
                    {
                        R = 0;
                    }

                    G = pixelColor.G / 255.0;
                    G -= 0.5;
                    G *= contrast;
                    G += 0.5;
                    G *= 255;
                    if (G > 255)
                    {
                        G = 255;
                    }
                    else if (G < 0)
                    {
                        G = 0;
                    }

                    B = pixelColor.B / 255.0;
                    B -= 0.5;
                    B *= contrast;
                    B += 0.5;
                    B *= 255;
                    if (B > 255)
                    {
                        B = 255;
                    }
                    else if (B < 0)
                    {
                        B = 0;
                    }

                    bitmapImage.SetPixel(x, y, Color.FromArgb((int)A, (int)R, (int)G, (int)B));
                }
            }
            return bitmapImage;
        }


        public static Bitmap ApplySepia(Bitmap b, int depth)
        {
            int A, R, G, B;
            Color pixelColor;
            var bitmapImage = (Bitmap)b.Clone();
            for (int y = 0; y < bitmapImage.Height; y++)
            {
                for (int x = 0; x < bitmapImage.Width; x++)
                {
                    pixelColor = bitmapImage.GetPixel(x, y);
                    A = pixelColor.A;
                    R = (int)((0.299 * pixelColor.R) + (0.587 * pixelColor.G) + (0.114 * pixelColor.B));
                    G = B = R;

                    R += (depth * 2);
                    if (R > 255)
                    {
                        R = 255;
                    }
                    G += depth;
                    if (G > 255)
                    {
                        G = 255;
                    }

                    bitmapImage.SetPixel(x, y, Color.FromArgb(A, R, G, B));
                }
            }
            return bitmapImage;
        }


        public static Bitmap ApplyBlur(Bitmap b, double weight)
        {
            ConvolutionMatrix matrix = new ConvolutionMatrix(3);
            matrix.SetAll(1);
            matrix.Matrix[0, 0] = weight / 2;
            matrix.Matrix[1, 0] = weight / 2;
            matrix.Matrix[2, 0] = weight / 2;
            matrix.Matrix[0, 1] = weight / 2;
            matrix.Matrix[1, 1] = weight;
            matrix.Matrix[2, 1] = weight / 2;
            matrix.Matrix[0, 2] = weight / 2;
            matrix.Matrix[1, 2] = weight / 2;
            matrix.Matrix[2, 2] = weight / 2;
            matrix.Factor = weight * 8;
            return Convolution3x3(b, matrix);
        }

        public static Bitmap ApplyEmboss(Bitmap b, double weight)
        {
            ConvolutionMatrix matrix = new ConvolutionMatrix(3);
            matrix.SetAll(1);
            matrix.Matrix[0, 0] = -1;
            matrix.Matrix[1, 0] = 0;
            matrix.Matrix[2, 0] = -1;
            matrix.Matrix[0, 1] = 0;
            matrix.Matrix[1, 1] = weight;
            matrix.Matrix[2, 1] = 0;
            matrix.Matrix[0, 2] = -1;
            matrix.Matrix[1, 2] = 0;
            matrix.Matrix[2, 2] = -1;
            matrix.Factor = 4;
            matrix.Offset = 127;
            return Convolution3x3(b, matrix);
        }

        public static Bitmap ApplySharpen(Bitmap b, double weight)
        {
            ConvolutionMatrix matrix = new ConvolutionMatrix(3);
            matrix.SetAll(1);
            matrix.Matrix[0, 0] = 0;
            matrix.Matrix[1, 0] = -2;
            matrix.Matrix[2, 0] = 0;
            matrix.Matrix[0, 1] = -2;
            matrix.Matrix[1, 1] = weight;
            matrix.Matrix[2, 1] = -2;
            matrix.Matrix[0, 2] = 0;
            matrix.Matrix[1, 2] = -2;
            matrix.Matrix[2, 2] = 0;
            matrix.Factor = weight - 8;
            return Convolution3x3(b, matrix);
        }

        public static Bitmap ApplyMeanRemoval(Bitmap b, double weight)
        {
            ConvolutionMatrix matrix = new ConvolutionMatrix(3);
            matrix.SetAll(1);
            matrix.Matrix[0, 0] = -1;
            matrix.Matrix[1, 0] = -1;
            matrix.Matrix[2, 0] = -1;
            matrix.Matrix[0, 1] = -1;
            matrix.Matrix[1, 1] = weight;
            matrix.Matrix[2, 1] = -1;
            matrix.Matrix[0, 2] = -1;
            matrix.Matrix[1, 2] = -1;
            matrix.Matrix[2, 2] = -1;
            matrix.Factor = weight - 8;
            return Convolution3x3(b, matrix);
        }

        public static Bitmap ApplySmooth(Bitmap b, float weight)
        {

            ConvolutionMatrix matrix = new ConvolutionMatrix(3);
            matrix.SetAll(1);
            matrix.Matrix[1, 1] = weight;
            matrix.Factor = weight + 8;
            return Convolution3x3(b, matrix);
        }

        private static Bitmap Convolution3x3(Bitmap b, ConvolutionMatrix m)
        {
            Bitmap newImg = (Bitmap)b.Clone();
            Color[,] pixelColor = new Color[3, 3];
            int A, R, G, B;

            for (int y = 0; y < b.Height - 2; y++)
            {
                for (int x = 0; x < b.Width - 2; x++)
                {
                    pixelColor[0, 0] = b.GetPixel(x, y);
                    pixelColor[0, 1] = b.GetPixel(x, y + 1);
                    pixelColor[0, 2] = b.GetPixel(x, y + 2);
                    pixelColor[1, 0] = b.GetPixel(x + 1, y);
                    pixelColor[1, 1] = b.GetPixel(x + 1, y + 1);
                    pixelColor[1, 2] = b.GetPixel(x + 1, y + 2);
                    pixelColor[2, 0] = b.GetPixel(x + 2, y);
                    pixelColor[2, 1] = b.GetPixel(x + 2, y + 1);
                    pixelColor[2, 2] = b.GetPixel(x + 2, y + 2);

                    A = pixelColor[1, 1].A;

                    R = (int)((((pixelColor[0, 0].R * m.Matrix[0, 0]) +
                                 (pixelColor[1, 0].R * m.Matrix[1, 0]) +
                                 (pixelColor[2, 0].R * m.Matrix[2, 0]) +
                                 (pixelColor[0, 1].R * m.Matrix[0, 1]) +
                                 (pixelColor[1, 1].R * m.Matrix[1, 1]) +
                                 (pixelColor[2, 1].R * m.Matrix[2, 1]) +
                                 (pixelColor[0, 2].R * m.Matrix[0, 2]) +
                                 (pixelColor[1, 2].R * m.Matrix[1, 2]) +
                                 (pixelColor[2, 2].R * m.Matrix[2, 2]))
                                        / m.Factor) + m.Offset);

                    if (R < 0)
                    {
                        R = 0;
                    }
                    else if (R > 255)
                    {
                        R = 255;
                    }

                    G = (int)((((pixelColor[0, 0].G * m.Matrix[0, 0]) +
                                 (pixelColor[1, 0].G * m.Matrix[1, 0]) +
                                 (pixelColor[2, 0].G * m.Matrix[2, 0]) +
                                 (pixelColor[0, 1].G * m.Matrix[0, 1]) +
                                 (pixelColor[1, 1].G * m.Matrix[1, 1]) +
                                 (pixelColor[2, 1].G * m.Matrix[2, 1]) +
                                 (pixelColor[0, 2].G * m.Matrix[0, 2]) +
                                 (pixelColor[1, 2].G * m.Matrix[1, 2]) +
                                 (pixelColor[2, 2].G * m.Matrix[2, 2]))
                                        / m.Factor) + m.Offset);

                    if (G < 0)
                    {
                        G = 0;
                    }
                    else if (G > 255)
                    {
                        G = 255;
                    }

                    B = (int)((((pixelColor[0, 0].B * m.Matrix[0, 0]) +
                                 (pixelColor[1, 0].B * m.Matrix[1, 0]) +
                                 (pixelColor[2, 0].B * m.Matrix[2, 0]) +
                                 (pixelColor[0, 1].B * m.Matrix[0, 1]) +
                                 (pixelColor[1, 1].B * m.Matrix[1, 1]) +
                                 (pixelColor[2, 1].B * m.Matrix[2, 1]) +
                                 (pixelColor[0, 2].B * m.Matrix[0, 2]) +
                                 (pixelColor[1, 2].B * m.Matrix[1, 2]) +
                                 (pixelColor[2, 2].B * m.Matrix[2, 2]))
                                        / m.Factor) + m.Offset);

                    if (B < 0)
                    {
                        B = 0;
                    }
                    else if (B > 255)
                    {
                        B = 255;
                    }
                    newImg.SetPixel(x + 1, y + 1, Color.FromArgb(A, R, G, B));
                }
            }
            return newImg;
        }

        public static Size ResizeTo(int width, int height, int maxWidth, int maxHeight, bool widthPriority = true)
        {
            Size size;

            if (widthPriority)
            {
                size = ResizeWidth(width, height, maxWidth);

                if (size.Height > maxHeight)
                    size = ResizeHeight(size.Width, size.Height, maxHeight);
            }
            else
            {
                size = ResizeHeight(width, height, maxHeight);

                if (size.Width > maxWidth)
                    size = ResizeWidth(size.Width, size.Height, maxWidth);
            }

            return size;
        }

        public static Size ResizeWidth(int width, int height, int toWidth)
        {
            var h = (int)(Math.Round((double)toWidth / ((double)width / (double)height)));
            return new Size(toWidth, h);
        }

        public static Size ResizeHeight(int width, int height, int toHeight)
        {
            if (height < toHeight)
                return new Size(width, height);

            var w = (int)(Math.Round((double)toHeight / ((double)height / (double)width)));
            return new Size(w, toHeight);
        }

        public static Size ResizeWidthMax(int width, int height, int maxWidth)
        {
            if (width < maxWidth)
                return new Size(width, height);

            var h = (int)(Math.Round((double)maxWidth / ((double)width / (double)height)));
            return new Size(maxWidth, h);
        }

        public static Size ResizeHeightMax(int width, int height, int maxHeight)
        {
            if (height < maxHeight)
                return new Size(width, height);

            var w = (int)(Math.Round((double)maxHeight / ((double)height / (double)width)));
            return new Size(w, maxHeight);
        }
    }
    #endregion

    // ===============================================================================================
    // 
    // Library.[ImageParser]
    // 
    // ===============================================================================================

    // ===============================================================================================
    // Autor        : Peter Širka
    // Created      : 09. 11. 2012
    // Updated      : 20. 03. 2013
    // Description  : Image Parser
    // ===============================================================================================

    #region ImageParser
    public class ImageParser
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Dimension { get; set; }
        public string Linker { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return string.Format("{0}X{1}x{2}", Id, Width, Height);
        }

        public string ToUrl()
        {
            return Configuration.OnImageParserUrl(Dimension, this);
        }

        public string ToUrl(string dimension)
        {
            return Configuration.OnImageParserUrl(dimension, this);
        }

        public static ImageParser Load(string id, string dimension = "")
        {
            var arr = id.Replace('X', 'x').Split('x');

            var image = new ImageParser();
            image.Id = arr.Read<int>(0, 0);
            image.Width = arr.Read<int>(1, 0);
            image.Height = arr.Read<int>(2, 0);
            image.Dimension = dimension;

            if (string.IsNullOrEmpty(dimension))
                return image;

            if (Configuration.OnImageParserDimension == null)
                return image;

            var size = Configuration.OnImageParserDimension(dimension);
            if (size.IsEmpty)
                return image;

            image.Width = size.Width;
            image.Height = size.Height;
            image.Dimension = dimension;
            return image;
        }

        public static ImageParser Load(int id, string dimension = "")
        {
            var image = new ImageParser();
            image.Id = id;
            image.Dimension = dimension;

            if (Configuration.OnImageParserDimension == null)
                return image;

            var size = Configuration.OnImageParserDimension(dimension);
            if (size.IsEmpty)
                return image;

            image.Width = size.Width;
            image.Height = size.Height;
            return image;
        }

        public static Size LoadDimension(string dimension)
        {
            if (Configuration.OnImageParserDimension == null)
                return default(Size);
            return Configuration.OnImageParserDimension(dimension);
        }

        public ImageParser Resize(string dimension)
        {
            Dimension = dimension;

            if (Configuration.OnImageParserDimension == null)
                return this;

            var size = Configuration.OnImageParserDimension(dimension);
            if (size.IsEmpty)
                return this;

            Width = size.Width;
            Height = size.Height;
            return this;
        }

        public HtmlString ToHtml(string className = "")
        {
            return new HtmlString(Render(className));
        }

        public string Render(string className = "")
        {
            return string.Format("<img src=\"{0}\"{1}{2} alt=\"{3}\" border=\"0\"{4} />", Configuration.OnImageParserUrl(Dimension, this), Width > 0 ? string.Format(" width=\"{0}\"", Width) : "", Height > 0 ? string.Format(" height=\"{0}\"", Height) : "", Name, string.IsNullOrEmpty(className) ? "" : string.Format(" class=\"{0}\"", className));
        }
    }
    #endregion

    // ===============================================================================================
    // 
    // Library.[Scheduler]
    // 
    // ===============================================================================================

    // ===============================================================================================
    // Autor        : Peter Širka
    // Created      : 11. 04. 2011
    // Updated      : 01. 02. 2014
    // Description  : Simple Scheduler
    // ===============================================================================================

    #region Scheduler
    public static class Scheduler
    {
        public class Schedule
        {
            public string Name { get; set; }
            public int Minutes { get; set; }
            public int Count { get; set; }
            public int Index { get; set; }
            public Action OnTick { get; set; }
            public Exception LastError { get; set; }
        }

        public static List<Schedule> Items = new List<Schedule>(3);
        private static System.Threading.AutoResetEvent handle = null;
        private static System.Threading.ReaderWriterLockSlim locker = new System.Threading.ReaderWriterLockSlim();

        public static void Add(string name, int minutes, Action onTick)
        {
            if (Items.Exists(n => n.Name != name))
                RemoveSchedule(name);

            minutes--;

            if (minutes < 0)
                minutes = 0;

            locker.EnterWriteLock();

            try
            {
                Items.Add(new Schedule { Name = name, Count = 0, Index = 0, Minutes = minutes, OnTick = onTick });

                handle = new System.Threading.AutoResetEvent(false);
                System.Threading.ThreadPool.RegisterWaitForSingleObject(handle, (state, timeout) =>
                {
                    foreach (var j in Items)
                    {
                        if (j.Index < j.Minutes)
                        {
                            j.Index++;
                            continue;
                        }

                        j.Index = 0;
                        j.Count++;

                        try
                        {
                            j.OnTick();
                        }
                        catch (Exception Ex)
                        {
                            j.LastError = Ex;
                        }
                    }
                }, null, TimeSpan.FromMinutes(1), false);

            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        public static bool Execute(string name)
        {
            var task = Items.FirstOrDefault(n => n.Name == name);
            if (task == null)
                return false;

            try
            {
                task.OnTick();
            }
            catch (Exception Ex)
            {
                task.LastError = Ex;
                return false;
            }

            return true;
        }

        public static void RemoveSchedule(string name)
        {
            locker.EnterWriteLock();
            try
            {
                Items.RemoveAll(n => n.Name == name);
                if (Items.Count == 0)
                    Clear();
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        public static void Clear()
        {
            locker.EnterWriteLock();

            try
            {
                Items.Clear();

                if (handle == null)
                    return;

                handle.Close();
                handle = null;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
    }
    #endregion

    // ===============================================================================================
    // 
    // Library.[Session<K, V>]
    // 
    // ===============================================================================================

    // ===============================================================================================
    // Autor        : Peter Širka
    // Created      : 05. 02. 2012
    // Updated      : 01. 02. 2014
    // Description  : Simple Session Management
    // ===============================================================================================

    #region Session default provider
    public class Session<K, V>
    {
        public class Item
        {
            public V Value { get; set; }
            public int Expire { get; set; }
        }

        public ConcurrentDictionary<K, Item> Items { get; set; }

        public Action<K, V> OnOnline { get; set; }
        public Action<K, V> OnOffline { get; set; }
        public Action OnSchedule { get; set; }

        public Session(int interval = 5, int limit = 100)
        {
            Items = new ConcurrentDictionary<K, Item>(1, limit);
            Scheduler.Add("session_" + Guid.NewGuid().ToString().Substring(0, 5), interval, Clear);
        }

        public void Clear()
        {
            var d = DateTime.Now.Ticks(true);

            foreach (var k in Items.Where(key => key.Value.Expire <= d).ToList())
            {
                Item item;
                Items.TryRemove(k.Key, out item);

                if (OnOffline != null)
                    OnOffline(k.Key, k.Value.Value);
            }

            if (OnSchedule != null)
                OnSchedule();
        }

        public V Add(K key, V value, DateTime expire)
        {
            Item obj;
            var ticks = expire.Ticks(true);

            if (Items.TryGetValue(key, out obj))
            {
                obj.Expire = ticks;
                obj.Value = value;
                return value;
            }

            Items.TryAdd(key, new Item { Expire = ticks, Value = value });

            if (OnOnline != null)
                OnOnline(key, value);

            return value;
        }

        public bool SetExpire(K key, DateTime expire)
        {
            Item obj;

            if (!Items.TryGetValue(key, out obj))
                return false;

            obj.Expire = expire.Ticks(true);
            return true;
        }

        public bool Remove(K key)
        {
            Item obj;
            var isDeleted = Items.TryRemove(key, out obj);

            if (OnOffline != null && obj != null)
                OnOffline(key, obj.Value);

            return isDeleted;
        }

        public V Read(K key)
        {
            Item obj;
            if (Items.TryGetValue(key, out obj))
                return obj.Value;
            return default(V);
        }
    }
    #endregion

    // ===============================================================================================
    // 
    // Library.[Markdown]
    // 
    // ===============================================================================================

    // ===============================================================================================
    // Autor        : Peter Širka
    // Created      : 14. 11. 2012
    // Updated      : 01. 09. 2013
    // Description  : Markdown syntax parser
    // ===============================================================================================

    #region Markdown
    public class Markdown
    {
        private const string REPLACE_ID = "@##";

        public enum MarkdownListType : byte
        {
            Plus = 0,
            Minus = 1,
            X = 2
        }

        public enum MarkdownBreakType : byte
        {
            Newline = 0,
            Line = 1,
            Pagebreak = 2
        }

        public enum MarkdownParagraphType : byte
        {
            Gt = 0,
            Slash = 1,
            Comment = 2,
            Line = 3
        }

        public enum MarkdownTitleType : byte
        {
            H1 = 0,
            H2 = 1,
            H3 = 2,
            H4 = 5,
            H5 = 6,
            H6 = 7
        }

        public enum MarkdownKeywordType : byte
        {
            Composite = 0,
            Square = 1
        }

        public enum MarkdownFormatType : byte
        {
            B = 0,
            Strong = 1,
            I = 2,
            Em = 3
        }

        private enum MarkdownParserType : byte
        {
            Empty = 0,
            Paragraph = 100,
            Embedded = 101,
            List = 102,
            Keyvalue = 103
        }

        public class MarkdownList
        {
            public MarkdownListType Type { get; set; }
            public string Value { get; set; }

            public MarkdownList(MarkdownListType type, string value)
            {
                Type = type;
                Value = value;
            }
        }

        public delegate string EmbeddedDelegate(string type, IList<string> lines);
        public delegate string KeywordDelegate(MarkdownKeywordType type, string value);
        public delegate string ParagraphDelegate(MarkdownParagraphType type, IList<string> lines);
        public delegate string LineDelegate(string line);
        public delegate string FormatDelegate(MarkdownFormatType type, string value);
        public delegate string LinkDelegate(string text, string url);
        public delegate string ImageDelegate(string alt, string src, int width, int height, string url);
        public delegate string ListDelegate(IList<MarkdownList> items);
        public delegate string KeyValueDelegate(IList<KeyValue> items);
        public delegate string BreakDelegate(MarkdownBreakType type);
        public delegate string TitleDelegate(MarkdownTitleType type, string value);

        public string Embedded { get; set; }
        public BreakDelegate OnBreak { get; set; }
        public LinkDelegate OnLink { get; set; }
        public ListDelegate OnList { get; set; }
        public FormatDelegate OnFormat { get; set; }
        public ImageDelegate OnImage { get; set; }
        public KeywordDelegate OnKeyword { get; set; }
        public KeyValueDelegate OnKeyValue { get; set; }
        public ParagraphDelegate OnParagraph { get; set; }
        public LineDelegate OnLine { get; set; }
        public EmbeddedDelegate OnEmbedded { get; set; }
        public TitleDelegate OnTitle { get; set; }

        private bool skip = false;
        private string command = "";
        private MarkdownParserType status = MarkdownParserType.Empty;

        private List<string> current = new List<string>(2);
        private List<KeyValue> format = new List<KeyValue>(2);
        private List<MarkdownList> currentList = new List<MarkdownList>(2);
        private List<KeyValue> currentKeyValue = new List<KeyValue>(2);

        private string output = "";

        private static readonly Regex reg_link_1 = new Regex(@"\<.*?\>+", RegexOptions.Compiled);
        private static readonly Regex reg_link_2 = new Regex(@"(!)?\[[^\]]+\][\:\s\(]+.*?[^)\s$]+", RegexOptions.Compiled);
        private static readonly Regex reg_link_3 = new Regex(@"(\b(https?|ftp|file):\/\/[-A-Z0-9+&@#\/%?=~_|!:,.;]*[-A-Z0-9+&@#\/%=~_|])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex reg_link_4 = new Regex(@"(^|[^\/])(www\.[\S]+(\b|$))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex reg_image = new Regex(@"(\[)?\!\[[^\]]+\][\:\s\(]+.*?[^\s$]+", RegexOptions.Compiled);
        private static readonly Regex reg_format = new Regex(@"\*{1,2}.*?\*{1,2}|_{1,3}.*?_{1,3}", RegexOptions.Compiled);
        private static readonly Regex reg_keyword = new Regex(@"(\[.*?\]|\{.*?\})", RegexOptions.Compiled);
        private static readonly Regex reg_format_replace_asterix = new Regex(@"^\*{1,2}|\*{1,2}$", RegexOptions.Compiled);
        private static readonly Regex reg_format_replace_underscore = new Regex(@"^_{1,2}|_{1,2}$$", RegexOptions.Compiled);

        public Markdown()
        {
            Embedded = "===";

            OnEmbedded = (type, lines) => lines.Join("\n");

            OnLink = (text, url) =>
            {
                if (!(url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase)))
                    url = "http://" + url;
                return string.Format("<a href=\"{1}\">{0}</a>", text, url);
            };

            OnFormat = (type, value) =>
            {
                var format = "<{0}>{1}</{0}>";
                switch (type)
                {
                    case MarkdownFormatType.B:
                        return string.Format(format, "b", value);
                    case MarkdownFormatType.Strong:
                        return string.Format(format, "strong", value);
                    case MarkdownFormatType.Em:
                        return string.Format(format, "em", value);
                    case MarkdownFormatType.I:
                        return string.Format(format, "i", value);
                }
                return "";
            };

            OnImage = (alt, src, width, height, url) =>
            {
                var sb = new StringBuilder();
                sb.Append("<img");
                sb.AppendAttribute("alt", alt);
                sb.AppendAttribute("src", src);

                if (width > 0)
                    sb.AppendAttribute("width", width);

                if (height > 0)
                    sb.AppendAttribute("height", height);

                sb.AppendAttribute("border", "0");
                sb.Append(" />");

                if (url.IsNotEmpty())
                    return string.Format("<a href=\"{0}\">{1}</a>", url, sb);

                return sb.ToString();
            };

            OnKeyword = (type, value) => "<span>{0}</span>".format(value);

            OnKeyValue = items => {
                var sb = new StringBuilder();

                foreach (var m in items)
                    sb.Append(string.Format("<dt>{0}</dt><dd>{1}</dd>", m.Key, m.Value));

                return string.Format("<dl>{0}</dl>", sb);
            };

            OnList = items => {
                var sb = new StringBuilder();

                foreach (var m in items)
                    sb.Append(string.Format("<li>{0}</li>", m.Value));

                return string.Format("<ul>{0}</ul>", sb);
            };

            OnParagraph = (type, lines) => string.Format("<p class=\"paragraph\">{0}</p>", lines.Join("<br />"));

            OnLine = line => string.Format("<p>{0}</p>", line);

            OnBreak = type => {
                switch (type) {
                case MarkdownBreakType.Line:
                case MarkdownBreakType.Pagebreak:
                    return "<hr />";
                case MarkdownBreakType.Newline:
                    return "<br />";
                }

                return "";
            };

            OnTitle = (type, value) =>
            {
                switch (type)
                {
                    case MarkdownTitleType.H1:
                        return string.Format("<h1>{0}</h1>", value);
                    case MarkdownTitleType.H2:
                        return string.Format("<h2>{0}</h2>", value);
                    case MarkdownTitleType.H3:
                        return string.Format("<h3>{0}</h3>", value);
                    case MarkdownTitleType.H4:
                        return string.Format("<h4>{0}</h4>", value);
                    case MarkdownTitleType.H5:
                        return string.Format("<h5>{0}</h5>", value);
                }

                return value;
            };
        }

        public string Compile(string text)
        {

            if (string.IsNullOrEmpty(text))
                return "";

            var arr = text.Split('\n');
            var length = arr.Length;

            output = "";
            status = MarkdownParserType.Empty;

            if (current.Count > 0)
                current.Clear();

            if (currentKeyValue.Count > 0)
                currentKeyValue.Clear();

            if (currentList.Count > 0)
                currentList.Clear();

            skip = false;

            for (var i = 0; i < length; i++)
            {
                if (skip)
                {
                    skip = false;
                    continue;
                }

                var line = arr[i];
                var next = i + 1 < length ? arr[i + 1] : "";

                if (ParseEmbedded(line))
                    continue;

                if (ParseBreak(line))
                    continue;

                if (ParseList(line))
                    continue;

                if (ParseKeyValue(line))
                    continue;

                if (ParseParagraph(line))
                    continue;

                if (ParseTitle(line, next))
                    continue;

                if (OnLine != null)
                    output += OnLine(ParseOther(line));
            }

            if (status != MarkdownParserType.Empty)
                Flush();

            return output;
        }

        private void Flush()
        {
            switch (status)
            {
                case MarkdownParserType.Embedded:
                    if (OnEmbedded != null)
                        output += OnEmbedded(command, current);
                    break;

                case MarkdownParserType.List:
                    if (OnList != null)
                        output += OnList(currentList);
                    break;

                case MarkdownParserType.Keyvalue:
                    if (OnKeyValue != null)
                        output += OnKeyValue(currentKeyValue);
                    break;

                case MarkdownParserType.Paragraph:

                    var typeParagraph = MarkdownParagraphType.Gt;

                    switch (command)
                    {
                        case "//":
                            typeParagraph = MarkdownParagraphType.Comment;
                            break;
                        case "|":
                            typeParagraph = MarkdownParagraphType.Line;
                            break;
                        case "/":
                            typeParagraph = MarkdownParagraphType.Slash;
                            break;
                    }

                    if (OnParagraph != null)
                        output += OnParagraph(typeParagraph, current);

                    break;
            }

            status = MarkdownParserType.Empty;
            current.Clear();
            currentKeyValue.Clear();
            currentList.Clear();
            command = "";
        }

        private bool ParseEmbedded(string line)
        {
            var chars = Embedded + (status != MarkdownParserType.Embedded ? " " : "");

            if (chars.Length > line.Length)
                return false;

            var has = line.Substring(0, chars.Length) == chars;

            if (status != MarkdownParserType.Embedded && !has)
                return false;

            if (status != MarkdownParserType.Embedded && has)
                Flush();

            if (status == MarkdownParserType.Embedded && has)
            {
                Flush();
                status = MarkdownParserType.Empty;
                return true;
            }

            if (has)
            {
                status = MarkdownParserType.Embedded;
                command = line.Substring(chars.Length);
                return true;
            }

            if (status == MarkdownParserType.Embedded)
                current.Add(line);

            return true;
        }

        private bool ParseBreak(string line)
        {
            if (!(line == "" || line == "***" || line == "---"))
                return false;

            if (status != MarkdownParserType.Empty)
                Flush();

            status = MarkdownParserType.Empty;
            MarkdownBreakType type = MarkdownBreakType.Newline;

            switch (line)
            {
                case "***":
                    type = MarkdownBreakType.Pagebreak;
                    break;
                case "---":
                    type = MarkdownBreakType.Line;
                    break;
            }

            if (OnBreak != null)
                output += OnBreak(type);

            return true;
        }

        private bool ParseList(string line)
        {
            var first = line[0];
            var second = line.Length > 1 ? line[1] : '\0';
            var has = (first == '-' || first == '+' || first == 'x') && (second == ' ');

            if (!has)
                return false;

            if (status != MarkdownParserType.List)
            {
                Flush();
                status = MarkdownParserType.List;
            }

            if (currentList == null)
                currentList = new List<MarkdownList>(5);

            MarkdownListType type = MarkdownListType.X;

            switch (first)
            {
                case '-':
                    type = MarkdownListType.Minus;
                    break;
                case '+':
                    type = MarkdownListType.Plus;
                    break;
            }

            currentList.Add(new MarkdownList(type, ParseOther(line.Substring(2))));
            return true;
        }

        private bool ParseTitle(string line, string next)
        {
            if (line.Length == 0)
                return false;

            var has = line[0] == '#';
            var type = "";

            if (!has)
            {
                var first = next.Length > 0 ? next[0] : '\0';
                has = (int)line[0] > 32 && (first == '=' || first == '-');

                if (has)
                    has = line.Length == next.Length;

                if (has)
                {
                    type = first == '=' ? "#" : "##";
                    skip = true;
                }
            }
            else
            {
                var index = line.IndexOf(' ');
                if (index == -1)
                    return false;

                type = line.Substring(0, index).Trim();
            }

            if (!has)
                return false;

            if (status != MarkdownParserType.Empty)
                Flush();

            var typeTitle = MarkdownTitleType.H1;
            switch (type)
            {
                case "#":
                    typeTitle = MarkdownTitleType.H1;
                    break;
                case "##":
                    typeTitle = MarkdownTitleType.H2;
                    break;
                case "###":
                    typeTitle = MarkdownTitleType.H3;
                    break;
                case "####":
                    typeTitle = MarkdownTitleType.H4;
                    break;
                case "#####":
                    typeTitle = MarkdownTitleType.H5;
                    break;
                case "######":
                    typeTitle = MarkdownTitleType.H6;
                    break;
            }

            if (OnTitle != null)
                output += OnTitle(typeTitle, ParseOther(skip ? line : line.Substring(type.Length + 1)));

            return true;
        }

        private bool ParseKeyValue(string line)
        {
            var index = line.IndexOf(':');
            if (index == -1)
                return false;

            var tmp = line.Substring(0, index);
            var length = tmp.Length;

            var countTab = 0;
            var countSpace = 0;

            for (var i = 0; i < length; i++)
            {
                var c = tmp[i];
                if (c == '\t')
                {
                    countTab++;
                    break;
                }

                if (c == ' ')
                {
                    countSpace++;
                    if (countSpace > 2)
                        break;
                }
                else
                    countSpace = 0;
            }

            if (countSpace < 3 && countTab <= 0)
                return false;

            if (status != MarkdownParserType.Keyvalue)
            {
                Flush();
                status = MarkdownParserType.Keyvalue;
            }

            if (currentKeyValue == null)
                currentKeyValue = new List<KeyValue>(3);

            currentKeyValue.Add(new KeyValue(ParseOther(tmp.Trim()), ParseOther(line.Substring(index + 1).Trim())));
            return true;
        }

        private bool ParseParagraph(string line)
        {
            var first = line[0];
            var second = line.Length > 1 ? line[1] : '\0';
            var index = 0;
            var has = false;

            switch (first)
            {
                case '>':
                case '|':
                    has = second == ' ';
                    break;
                case '/':
                    has = second == '/' || line[2] == '/';
                    index = 2;
                    break;
            }

            if (!has)
                return false;

            if (has)
            {
                var tmp = first + (first == '/' ? "/" : "");
                if (command != "" && command != tmp && status == MarkdownParserType.Paragraph)
                    Flush();
                command = tmp;
            }

            if (status != MarkdownParserType.Paragraph)
            {
                Flush();
                status = MarkdownParserType.Paragraph;
            }

            current.Add(ParseOther(line.Substring(index + 1).Trim()));
            return true;
        }

        private string ParseOther(string line)
        {
            if (format.Count > 0)
                format.Clear();

            line = ParseLink(line);
            line = ParseImage(line);
            line = ParseFormat(line);
            line = ParseLinkInline(line);
            line = ParseKeyword(line);

            if (format.Count == 0)
                return line;

            foreach (var m in format)
                line = line.Replace(m.Key, m.Value);

            return line;
        }

        private string GetReplace(string replace)
        {
            var key = REPLACE_ID + format.Count + ';';
            format.Add(new KeyValue(key, replace));
            return key;
        }

        private string ParseLink(string text)
        {
            var output = text;

            foreach (Match m in reg_link_1.Matches(output))
            {
                var o = m.Value;
                var url = o.Substring(1, o.Length - 2);
                output = output.Replace(o, GetReplace(OnLink(url, url)));
            }

            foreach (Match m in reg_link_2.Matches(output))
            {
                var o = m.Value.Trim();

                // Picture
                // Skip
                if (o.StartsWith("[![", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var index = o.IndexOf(']');
                if (index == -1)
                    continue;

                if (o[0] == '!')
                    continue;

                var name = o.Substring(1, index - 1).Trim();
                var url = o.Substring(index + 1).Trim();
                var first = url[0];

                if (first == '(' || first == ':')
                    url = url.Substring(1).Trim();
                else
                    continue;

                if (first == '(')
                    o += ')';

                var last = url[url.Length - 1].ToString();
                if (last == "," || last == "." || last == " ")
                    url = url.Substring(0, url.Length - 1);
                else
                    last = "";

                if (!(url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase)))
                    url = "http://" + url;

                output = output.Replace(o, GetReplace((OnLink(ParseFormat(name, true), url)) + last));
            }

            return output;
        }

        private string ParseLinkInline(string text)
        {
            foreach (Match m in reg_link_3.Matches(text))
            {
                var o = m.Value.Trim();
                var url = o;

                if (!(url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase)))
                    url = "http://" + url;

                text = text.Replace(o, GetReplace(OnLink(o, url)));
            }

            foreach (Match m in reg_link_4.Matches(text))
            {
                var o = m.Value.Trim();
                var url = o;

                if (!(url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase)))
                    url = "http://" + url;

                text = text.Replace(o, GetReplace(OnLink(o, url)));
            }

            return text;
        }

        private string ParseFormat(string text, bool flush = false)
        {
            foreach (Match m in reg_format.Matches(text))
            {
                var o = m.Value.Trim();
                var isAsterix = o[0] == '*';
                string value;

                if (isAsterix)
                    value = OnFormat(o[1] == '*' ? MarkdownFormatType.Em : MarkdownFormatType.I, reg_format_replace_asterix.Replace(o, ""));
                else
                    value = OnFormat(o[1] == '_' ? MarkdownFormatType.Strong : MarkdownFormatType.B, reg_format_replace_underscore.Replace(o, ""));

                text = text.Replace(o, flush ? value : GetReplace(value));
            }

            return text;
        }

        private string ParseImage(string text)
        {
            foreach (Match m in reg_image.Matches(text))
            {
                var o = m.Value.Trim();
                var indexBeg = 2;

                if (o.StartsWith("[![", StringComparison.InvariantCultureIgnoreCase))
                    indexBeg = 3;

                var index = o.IndexOf(']');
                if (index == -1)
                    continue;

                var name = o.Substring(indexBeg, index - indexBeg).Trim();
                var url = o.Substring(index + 1);

                var first = url[0];
                if (first != '(')
                    continue;

                index = o.LastIndexOf(')');
                if (index == -1)
                    continue;

                var find = o.Substring(0, index + 1);

                index = url.IndexOf('#');
                indexBeg = index;

                string src;
                var indexEnd = index == -1 ? url.Length : url.IndexOf(')', index);
                var width = 0;
                var height = 0;

                if (index > 0)
                {
                    var dimension = url.Substring(indexBeg + 1, (indexEnd - indexBeg) - 1).Split('x');
                    width = dimension.Read<int>(0);
                    height = dimension.Read<int>(1);
                    src = url.Substring(1, index - 1);
                }
                else
                    src = url.Substring(1, indexEnd - 2);

                indexBeg = url.IndexOf('(', indexEnd);
                indexEnd = url.LastIndexOf(')');

                if (indexBeg != -1 && indexBeg > index)
                    url = url.Substring(indexBeg + 1, (indexEnd - indexBeg) - 1);
                else
                    url = "";

                text = text.Replace(find, GetReplace(OnImage(name, src, width, height, url)));
            }

            return text;
        }

        private string ParseKeyword(string text)
        {
            foreach (Match m in reg_keyword.Matches(text))
            {
                var o = m.Value;
                var type = MarkdownKeywordType.Square;

                if (o[0] == '{')
                    type = MarkdownKeywordType.Composite;

                if (type == MarkdownKeywordType.Composite)
                    o = o.Replace(@"^\{{1}|(\}|]){1}$", "");
                else
                    o = o.Replace(@"^\[{1}|(\]|]){1}$", "");

                text = text.Replace(o, GetReplace(OnKeyword(type, o)));
            }

            return text;
        }
    }
    #endregion

    // ===============================================================================================
    // 
    // Library.[EncryptDecrypt]
    // 
    // ===============================================================================================

    // ===============================================================================================
    // Autor        : Jozef Gula
    // Created      : 23. 11. 2012
    // Updated      : 20. 03. 2013
    // Description  : Custom Cryptography Functions
    // ===============================================================================================

    #region EncryptDecrypt
    public static class EncryptDecrypt
    {
        public static bool ValidateHash(string key, string hash, string data)
        {
            string str = Decrypt(key, hash);
            return string.Compare(data, str) == 0;
        }

        public static string Decrypt(string key, string hash)
        {
            var index = hash.Length % 4;
            if (index != 0)
            {
                for (var i = 0; i < 4 - index; i++)
                    hash += '=';
            }

            try
            {
                byte[] key_data = Encoding.UTF8.GetBytes(key);
                byte[] value_data = Convert.FromBase64String(hash.Replace('_', '/').Replace('-', '+'));

                int length_key_data = key_data.Length;
                int length_value_data = value_data.Length;

                byte[] newData = new byte[length_value_data];
                for (int i = 0; i < length_value_data; i++)
                {
                    if (i % 2 != 0)
                        newData[i] = ((byte)(ChangeByte(value_data[i]) ^ key_data[i % length_key_data]));
                    else
                        newData[i] = ((byte)(value_data[i] ^ key_data[i % length_key_data]));
                }

                return ReverseString(Encoding.UTF8.GetString(newData));
            }
            catch
            {
                return null;
            }
        }

        public static string Encrypt(string key, string value)
        {
            byte[] key_data = Encoding.UTF8.GetBytes(key);
            byte[] value_data = Encoding.UTF8.GetBytes(ReverseString(value));
            int length_key_data = key_data.Length;
            int length_value_data = value_data.Length;

            byte[] newData = new byte[length_value_data];
            for (int i = 0; i < length_value_data; i++)
            {
                if (i % 2 != 0)
                    newData[i] = ChangeByte(((byte)(value_data[i] ^ key_data[i % length_key_data])));
                else
                    newData[i] = ((byte)(value_data[i] ^ key_data[i % length_key_data]));
            }

            var hash = Convert.ToBase64String(newData);
            var index = hash.IndexOf('=');
            if (index > 0)
                hash = hash.Substring(0, index);
            return hash.Replace('/', '_').Replace('+', '-');
        }

        private static string ReverseString(string s)
        {
            char[] array = s.ToCharArray();
            Array.Reverse(array);
            return new string(array);
        }

        private static byte ChangeByte(byte inByte)
        {
            byte result = 0x00;
            for (byte mask = 0x80; Convert.ToInt32(mask) > 0; mask >>= 1)
            {
                result >>= 1;
                byte tempbyte = (byte)(inByte & mask);
                if (tempbyte != 0x00)
                    result |= 0x80;
            }
            return (result);
        }

        public static string Hash(string value, int length = 14)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                length = length / 2;

                byte[] data = Encoding.UTF8.GetBytes(value);
                byte xor = ChangeByte(data[0]);

                int data_length = data.Length;
                byte[] response = new byte[length];

                int count = data_length > length ? data_length : length;
                int index;

                for (int i = 0; i < count; i++)
                {
                    index = i % data_length;
                    response[i % length] = (byte)((xor ^ data[i % data_length]) + index);
                }

                return BitConverter.ToString(response).Replace("-", "");
            }
            return null;
        }
    }
    #endregion

    // ===============================================================================================
    // 
    // Library.[Pagination]
    // 
    // ===============================================================================================

    // ===============================================================================================
    // Autor        : Peter Širka
    // Created      : 13. 03. 2013
    // Updated      : 01. 02. 2014
    // Description  : Pagination
    // ===============================================================================================

    #region Pagination
    public class Pagination
    {
        public class Page
        {
            public int Index { get; set; }
            public string Url { get; set; }
            public bool Selected { get; set; }

            public Page(int index, string url, bool selected)
            {
                Index = index;
                Url = url;
                Selected = selected;
            }
        }

        public bool IsNext { get; set; }
        public bool IsPrev { get; set; }

        public int Items { get; set; }
        public int Count { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public int Current { get; set; }
        public int Max { get; set; }

        public bool Visible { get; set; }

        [NoJsonParameter]
        public Func<int, string> OnUrl { get; set; }

        public IList<Page> Pages(int max)
        {
            var l = new List<Page>();

            if (max == 0)
            {
                for (var i = 1; i < Count + 1; i++)
                    l.Add(new Page(i, OnUrl(i), i == Current));
                return l;
            }

            int half = max / 2;
            var pages = Count;

            var pageFrom = Current - half;
            var pageTo = Current + half;
            var plus = 0;

            if (pageFrom <= 0)
            {
                plus = Math.Abs(pageFrom);
                pageFrom = 1;
                pageTo += plus;
            }

            if (pageTo >= pages)
            {
                pageTo = pages;
                pageFrom = pages - max;
            }

            if (pageFrom <= 0)
                pageFrom = 1;

            for (var i = pageFrom; i < pageTo + 1; i++)
                l.Add(new Page(i, OnUrl(i), i == Current));

            return l;
        }

        public void Refresh(int items, int page, int max)
        {
            Items = items;
            Current = page;

            if (Current < 1)
                Current = 1;

            if (max == 0)
                max = 20;

            Max = max;

            Count = (items / max) + (items % max > 0 ? 1 : 0);
            Current = Current - 1;

            if (Current < 0)
                Current = 0;

            Skip = Current * max;
            Take = max;
            Visible = Count > 0;
            IsPrev = Current > 0;
            IsNext = Current < Count - 1;

            Current++;
        }

        public Pagination(int items, int page, int max)
        {
            Refresh(items, page, max);
        }
    }
    #endregion

    // ===============================================================================================
    // 
    // Library.[FileCache]
    // 
    // ===============================================================================================

    // ===============================================================================================
    // Autor        : Peter Širka
    // Created      : 01. 02. 2014
    // Updated      : 01. 02. 2014
    // Description  : FileCache
    // ===============================================================================================

    #region FileCache
    public class FileCache
    {
        public class File
        {
            public string Id { get; set; }
            public string FileName { get; set; }
            public string Custom { get; set; }
            public int Expire { get; set; }

            public byte[] Bytes()
            {
                return System.IO.File.ReadAllBytes(Path);
            }

            public System.IO.Stream Stream()
            {
                return new System.IO.FileStream(Path, System.IO.FileMode.Open);
            }

            public string Path
            {
                get { return Configuration.PathTemporary + Id + ".filecache"; }
            }
        }

        public ConcurrentDictionary<string, File> Items { get; set; }
        public Action OnSchedule { get; set; }

        public FileCache(int interval = 5, int limit = 100)
        {
            Items = new ConcurrentDictionary<string, File>(1, limit);
            Scheduler.Add("filecache_" + Guid.NewGuid().ToString().Substring(0, 5), interval, Clear);
        }

        public void Clear()
        {
            var d = DateTime.Now.Ticks(true);

            foreach (var k in Items.Where(key => key.Value.Expire <= d).ToList())
                Remove(k.Key);

            if (OnSchedule != null)
                OnSchedule();
        }

        public bool SetExpire(string key, DateTime expire)
        {
            File file;

            if (!Items.TryGetValue(key, out file))
                return false;

            file.Expire = expire.Ticks(true);
            return true;
        }

        public string Add(HttpPostedFile file, DateTime expire, string custom = null)
        {
            var cache = new File();
            cache.Id = Guid.NewGuid().ToString();
            cache.FileName = file.FileName;
            cache.Expire = expire.Ticks(true);
            cache.Custom = custom;
            Items.TryAdd(cache.Id, cache);
            file.SaveAs(cache.Path);
            return cache.Id;
        }

        public void Remove(string id)
        {
            File file;

            if (!Items.TryRemove(id, out file))
                return;

            if (System.IO.File.Exists(file.Path))
                System.IO.File.Delete(file.Path);
        }

        public File Read(string id)
        {
            File file;
            if (Items.TryGetValue(id, out file))
                return file;
            return null;
        }
    }
    #endregion

    // ===============================================================================================
    // 
    // Library.[Sitemap]
    // 
    // ===============================================================================================

    // ===============================================================================================
    // Autor        : Peter Širka
    // Created      : 04. 05. 2008
    // Updated      : 01. 02. 2014
    // Description  : Sitemap
    // ===============================================================================================

    #region Sitemap
    public static class Sitemap
    {
        private const string ID = "$sitemap";

        public class Page
        {
            public int Priority { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
        }

        private static HashSet<Page> Items()
        {
            var l = HttpContext.Current.Items[ID] as HashSet<Page>;
            if (l == null)
            {
                l = new HashSet<Page>();
                HttpContext.Current.Items[ID] = l;
            }
            return l;
        }

        public static void Add(string name, string url = "", int priority = -1)
        {
            var l = Items();
            l.Add(new Page { Name = name, Url = url.Empty(HttpContext.Current.Request.Url()), Priority = priority == -1 ? l.Count : priority });
        }

        public static void Add(Action<HashSet<Page>> onAppend)
        {
            onAppend(Items());
        }

        public static bool Has
        {
            get
            {
                var l = HttpContext.Current.Items[ID] as HashSet<Page>;
                return l != null && l.Count > 0;
            }
        }

        public static IOrderedEnumerable<Page> Links
        {
            get
            {
                return Items().OrderBy(n => n.Priority);
            }
        }
    }
    #endregion

}
