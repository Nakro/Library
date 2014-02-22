﻿using System;
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

namespace Library
{

    #region Enums & Attributes & Classes
    public enum XmlSitemapFrequency
    {
        always,
        hourly,
        daily,
        weekly,
        monthly,
        yearly,
        never
    }

    public enum XmlSitemapPriority
    {
        random,
        low,
        medium,
        high,
        important
    }

    public enum Authorization
    {
        logged,
        unlogged,
        forbidden
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

        public KeyValue()
        {
        }

        public KeyValue(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }

        public KeyValue(string key, int value, bool autoFormat = true)
        {
            this.Key = key;
            this.Value = autoFormat ? value.Format() : value.ToString();
        }

        public KeyValue(string key, decimal value, bool autoFormat = true)
        {
            this.Key = key;
            this.Value = autoFormat ? value.Format(2) : value.ToString();
        }

        public KeyValue(string key, byte value)
        {
            this.Key = key;
            this.Value = value.ToString();
        }

        public KeyValue(string key, string format, params object[] param)
        {
            this.Key = key;
            this.Value = string.Format(Configuration.InvariantCulture, format, param);
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
        public string Title { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
        public int Tag { get; set; }
        public HtmlString Body { get; set; }
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
    // Updated      : 22. 02. 2014
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

    public class Configuration
    {
        public delegate void ProblemEventHandler(string source, string message, Uri uri);
        public delegate void ErrorEventHandler(string source, Exception ex, Uri uri);
        public delegate void ChangeEventHandler(string source, string message, Uri uri);

        public static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
        public static ConfigurationUrl Url { get; set; }

        public static event ProblemEventHandler Problem;
        public static event ErrorEventHandler Error;
        public static event ChangeEventHandler Change;

        public static string Name { get; set; }
        public static string Author { get; set; }
        public static string Version { get; set; }

        public static string PathTemporary { get; set; }

        public static int DefaultCacheExpireMinutes { get; set; }

        public static Func<DateTime, string, string> OnDateConjugation { get; set; }

        public static Func<string, string> OnVersion { get; set; }
        public static Func<string, string, string> OnResource { get; set; }

        public static Func<int, string, string> OnEncrypt { get; set; }
        public static Func<string, string, int> OnDecrypt { get; set; }

        public static Func<string, Size> OnImageParserDimension { get; set; }
        public static Func<string, ImageParser, string> OnImageParserUrl { get; set; }

        public static bool JsonUnicode { get; set; }

        public static bool IsDebug
        {
            get { return HttpContext.Current.IsDebuggingEnabled; }
        }

        public static bool IsRelease
        {
            get { return !HttpContext.Current.IsDebuggingEnabled; }
        }

        /// <summary>
        /// HttpContext, Roles, Users
        /// </summary>
        public static Func<HttpContextBase, string, string, Authorization> OnAuthorization { get; set; }
        public static Func<HttpRequestBase, Authorization, ActionResult> OnAuthorizationError { get; set; }

        public static Library.Modules.Analytics Analytics
        {
            get { return HttpContext.Current.ApplicationInstance.Modules["Analytics"] as Library.Modules.Analytics; }
        }

        public static void Defaults()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalFilters.Filters.Add(new HandleErrorAttribute());

            Configuration.Url = new ConfigurationUrl();

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

            MvcHandler.DisableMvcResponseHeader = true;
        }

        public static void InvokeProblem(string source, string message, Uri uri)
        {
            if (Problem != null)
                Problem(source, message, uri);
        }

        public static void InvokeError(string source, Exception error, Uri uri)
        {
            if (Error != null)
                Error(source, error, uri);
        }

        public static void InvokeChange(string source, string message, Uri uri)
        {
            if (Change != null)
                Change(source, message, uri);
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
    public class Utils
    {
        public static double GPS_distance_km(double latitudeA, double longitudeA, double latitudeB, double longitudeB)
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
            var priorita = new string[] { "0.1", "0.3", "0.5" };
            var prioritaIndex = 0;

            response.ContentType = "text/xml";
            response.HeaderEncoding = System.Text.Encoding.UTF8;
            response.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine + "<urlset xmlns=\"http://www.google.com/schemas/sitemap/0.84\">" + Environment.NewLine);

            prioritaIndex++;

            if (prioritaIndex == 3)
                prioritaIndex = 0;

            var action = new Action<string, DateTime, XmlSitemapFrequency, XmlSitemapPriority>((url, date, freq, priority) =>
            {
                response.Write("  <url>" + Environment.NewLine);
                response.Write("    <loc>" + url.Replace("&", "&amp;") + "</loc>" + Environment.NewLine);
                response.Write("    <lastmod>" + date.ToString("yyyy-MM-dd") + "</lastmod>" + Environment.NewLine);
                response.Write("    <changefreq>" + freq.ToString() + "</changefreq>" + Environment.NewLine);

                var p = "";

                switch (priority)
                {
                    case XmlSitemapPriority.random:
                        p = priorita[prioritaIndex];
                        break;
                    case XmlSitemapPriority.low:
                        p = "0.1";
                        break;
                    case XmlSitemapPriority.medium:
                        p = "0.4";
                        break;
                    case XmlSitemapPriority.high:
                        p = "0.7";
                        break;
                    case XmlSitemapPriority.important:
                        p = "1.0";
                        break;
                }

                response.Write("    <priority>" + p + "</priority>" + Environment.NewLine);
                response.Write("  </url>" + Environment.NewLine);
            });

            fn(action);

            response.Write("</urlset>");
        }

        public static void Problem(string source, string message, Uri uri)
        {
            Configuration.InvokeProblem(source, message, uri);
        }

        public static void Error(string source, Exception error, Uri uri)
        {
            Configuration.InvokeError(source, error, uri);
        }

        public static void Change(string source, string message, Uri uri)
        {
            Configuration.InvokeChange(source, message, uri);
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
                    if (predicate(n, m))
                    {
                        existuje = true;
                        if (onUpdate != null)
                            onUpdate(n, m);
                        break;
                    }
                }

                if (!existuje)
                    remove.Add(m);
            }

            foreach (var n in sourceForm)
            {
                var existuje = false;
                foreach (var m in sourceDB)
                {
                    if (predicate(n, m))
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
                    if (predicate(n, m))
                    {
                        exists = true;
                        if (onUpdate != null)
                            onUpdate(n, m);
                        break;
                    }
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

            if (t == typeof(string))
            {
                if (tv == null)
                    o = "";
                else
                    o = value.ToString();

                return o == null ? default(T) : (T)o;
            }

            if (t == typeof(char))
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

            if (t == typeof(int))
            {
                if (tv != null)
                {
                    var intVal = 0;
                    if (tv == typeof(string))
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

            if (t == typeof(bool))
            {
                if (tv != null)
                {
                    if (tv == typeof(string))
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

            if (t == typeof(byte))
            {
                if (tv != null)
                {
                    byte byteVal = 0;
                    if (tv == typeof(string))
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

            if (t == typeof(decimal))
            {
                if (tv != null)
                {
                    if (tv == typeof(string))
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

            if (t == typeof(Int64))
            {
                if (tv != null)
                {
                    Int64 int64Val = 0;
                    if (tv == typeof(string))
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

            if (t == typeof(Int16))
            {
                if (tv != null)
                {
                    Int16 int16Val = 0;
                    if (tv == typeof(string))
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

            if (t == typeof(float))
            {
                if (tv != null)
                {
                    if (tv == typeof(string))
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

            if (t == typeof(double))
            {
                if (tv != null)
                {
                    if (tv == typeof(string))
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

            if (t == typeof(Guid))
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

            if (t == typeof(DateTime))
            {
                try
                {
                    if (tv == typeof(string))
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

            if (t == typeof(byte[]))
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
                System.IO.StreamWriter writer = new System.IO.StreamWriter(request.GetRequestStream(), System.Text.Encoding.UTF8);
                if (data != null && data.Length > 0)
                    writer.Write(data.ToString());
                writer.Flush();
                writer.Close();
            }

            var response = request.GetResponse();

            using (System.IO.StreamReader Reader = new System.IO.StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8))
                return Reader.ReadToEnd().JsonDeserialize<T>();
        }

        public static string POST(string url, Action<Dictionary<string, string>> fnData, Action<System.Net.HttpWebRequest> options = null, System.Text.Encoding encoding = null)
        {
            System.Net.HttpWebRequest r = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            r.ContentType = "application/x-www-form-urlencoded";
            r.Method = "POST";

            if (encoding == null)
                encoding = System.Text.Encoding.Default;

            if (options != null)
                options(r);

            var sb = new StringBuilder();
            var data = new Dictionary<string, string>(10);

            fnData(data);

            foreach (var item in data)
                sb.Append(string.Format("{0}={1}", item.Key, System.Web.HttpUtility.UrlEncode(item.Value)), "&");

            System.IO.StreamWriter w = new System.IO.StreamWriter(r.GetRequestStream(), encoding);
            w.Write(sb.ToString());
            w.Flush();
            w.Close();

            var response = r.GetResponse();

            using (System.IO.StreamReader Reader = new System.IO.StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8))
                return Reader.ReadToEnd();
        }

        internal static string Token(int id, string token, bool session)
        {
            var current = System.Web.HttpContext.Current;

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
            var cache = HttpContext.Current.Cache;
            cache.Remove(key);
            cache.Add(key, value, null, expire, TimeSpan.Zero, System.Web.Caching.CacheItemPriority.Normal, null);
            return value;
        }

        public static T CacheRead<T>(string key, Func<string, T> onEmpty = null)
        {
            var value = HttpContext.Current.Cache[key];
            if (value != null)
                return (T)value;

            if (onEmpty != null)
                return onEmpty(key);

            return default(T);
        }

        public static void CacheRemove(string key)
        {
            HttpContext.Current.Cache.Remove(key);
        }

        public static void CacheRemove(Func<string, bool> predicate)
        {
            var cache = HttpContext.Current.Cache;
            foreach (System.Collections.DictionaryEntry item in cache)
            {
                var key = item.Key.ToString();
                if (predicate(key))
                    cache.Remove(key);
            }
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

            return l.ToArray(); ;
        }

        public static void Log(params object[] value)
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
                mail.SubjectEncoding = System.Text.Encoding.UTF8;
                mail.HeadersEncoding = System.Text.Encoding.UTF8;
                mail.BodyEncoding = System.Text.Encoding.UTF8;

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
                index = s.IndexOf("\\u", index);

                if (index > -1)
                {
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
                else
                    repeat = false;
            }
            return s;
        }

        public static string UnicodeEncode(string s)
        {
            var sb = new System.Text.StringBuilder();
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
    // Updated      : 01. 02. 2014
    // Description  : Extension methods
    // ===============================================================================================

    #region Extension
    public static class Extension
    {
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

            return allMustExist ? true : false;
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

                if (propSource.PropertyType == typeof(string))
                {
                    if (property.PropertyType == typeof(int))
                    {
                        property.SetValue(target, value.To<int>(), null);
                        continue;
                    }

                    if (property.PropertyType == typeof(decimal))
                    {
                        property.SetValue(target, value.To<decimal>(), null);
                        continue;
                    }

                    if (property.PropertyType == typeof(byte))
                    {
                        property.SetValue(target, value.To<byte>(), null);
                        continue;
                    }

                    if (property.PropertyType == typeof(short))
                    {
                        property.SetValue(target, value.To<short>(), null);
                        continue;
                    }

                    if (property.PropertyType == typeof(float))
                    {
                        property.SetValue(target, value.To<float>(), null);
                        continue;
                    }

                    if (property.PropertyType == typeof(bool))
                    {
                        property.SetValue(target, value.To<bool>(), null);
                        continue;
                    }

                    if (property.PropertyType == typeof(long))
                    {
                        property.SetValue(target, value.To<long>(), null);
                        continue;
                    }

                    if (property.PropertyType == typeof(double))
                    {
                        property.SetValue(target, value.To<double>(), null);
                        continue;
                    }

                    continue;
                }

                if (property.PropertyType != typeof(string))
                    continue;

                if (propSource.PropertyType == typeof(int) || propSource.PropertyType == typeof(byte) || propSource.PropertyType == typeof(short) || propSource.PropertyType == typeof(long) || propSource.PropertyType == typeof(bool))
                {
                    property.SetValue(target, value.ToString(), null);
                    continue;
                }

                if (propSource.PropertyType == typeof(decimal))
                {
                    property.SetValue(target, ((decimal)value).ToString(Configuration.InvariantCulture), null);
                    continue;
                }

                if (propSource.PropertyType == typeof(float))
                {
                    property.SetValue(target, ((float)value).ToString(Configuration.InvariantCulture), null);
                    continue;
                }

                if (propSource.PropertyType == typeof(double))
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

        public static ActionResult JsonSuccess(this Controller source, string param = "")
        {
            if (string.IsNullOrEmpty(param))
                return new { r = true }.Json();
            return new { r = true, param = param }.Json();
        }

        public static ActionResult JsonError(this Controller source, string error, string language = "")
        {
            return error.JsonError(language);
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

        public static bool isXHR(this Controller source)
        {
            return source.Request.IsAjaxRequest();
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

        public static string JsCompress(this string source, System.Text.Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            if (encoding == null)
                encoding = System.Text.Encoding.Default;

            var hash = "javascript." + EncryptDecrypt.Hash(source);

            return Utils.CacheRead<string>(hash, key =>
            {
                var js = new Modules.JavaScriptMinifier();
                var output = "";

                using (var src = new System.IO.MemoryStream(encoding.GetBytes(source)))
                    output = js.Minify(src).Replace('\n', ' ');

                if (Configuration.IsRelease)
                    return Utils.CacheWrite<string>(key, output, DateTime.Now.AddMinutes(5));

                return output;
            });
        }

        public static string CssCompress(this string source, System.Text.Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            if (encoding == null)
                encoding = System.Text.Encoding.Default;

            var hash = "css." + EncryptDecrypt.Hash(source);

            return Utils.CacheRead<string>(hash, key =>
            {
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
                return false; ;

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
            Configuration.InvokeProblem(source, message, controller.Request.Url);
        }

        public static void Change(this Controller controller, string message, string source = null)
        {
            if (source == null)
                source = controller.GetType().Name;
            Configuration.InvokeChange(source, message, controller.Request.Url);
        }

        public static void Error(this Controller controller, Exception error, string source = null)
        {
            if (source == null)
                source = controller.GetType().Name;
            Configuration.InvokeError(source, error, controller.Request.Url);
        }

        public static HtmlString RenderToHtmlString(this Controller source, string name, object model)
        {
            return new HtmlString(source.RenderToString(name, model));
        }

        public static ControllerCache Cache(this Controller source, string key, Action<Action<ControllerCache, DateTime?>> onEmpty)
        {
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

        public static ActionResult JsonError(this string source, string language = "", Func<string, string, string> onReplace = null)
        {
            return KeyValue.Create(source, "@").JsonError(language, onReplace);
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

        public static ActionResult JsonError(this KeyValue source, string language = "", Func<string, string, string> onReplace = null)
        {
            if (Configuration.OnResource != null && source.Value.IsNotEmpty() && source.Value[0] == '@')
                source.Value = Configuration.OnResource(source.Value == "@" ? source.Key : source.Value.Substring(1), language);

            if (onReplace != null)
                source.Value = onReplace(source.Key, source.Value);

            return new KeyValue[1] { source }.Json();
        }

        public static ActionResult JsonError(this IEnumerable<KeyValue> source, string language = "", Func<string, string, string> onReplace = null)
        {
            foreach (var m in source)
            {
                if (Configuration.OnResource != null && m.Value.IsNotEmpty() && m.Value[0] == '@')
                    m.Value = Configuration.OnResource(m.Value == "@" ? m.Key : m.Value.Substring(1), language);

                if (onReplace != null)
                    m.Value = onReplace(m.Key, m.Value);
            }

            return source.Json();
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

        public static ActionResult JsonError(this ModelStateDictionary source, string language = "", bool allowKeyDuplicate = true, Func<string, string, string> onReplace = null)
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
            return error.Json();
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

            for (int i = 0; i < n.ToString().Length; i++)
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

            var sb = new System.Text.StringBuilder();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(source.Trim(), "\\w+"))
                sb.Append((sb.Length > 0 ? "-" : string.Empty) + RemoveDiakritics(m.Value.ToLower()).Trim());

            var url = sb.ToString().Max(60, "");

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

            string ns = value.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < ns.Length; i++)
            {
                char c = ns[i];
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString();
        }

        public static string RemoveOperator(this string value, params char[] withoutOperator)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            char c;
            var sb = new System.Text.StringBuilder();

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

            source.MapRoute(string.Format("{0}.{1}.{2}", url, controller, action).MD5(), url, new { controller = controller, action = action });
        }

        public static void Route(this RouteCollection source, string url, object defaults)
        {
            var t = defaults.GetType();
            var controller = t.GetProperty("controller").GetValue(defaults, null).ToString();
            var action = t.GetProperty("action").GetValue(defaults, null).ToString();
            source.MapRoute(string.Format("{0}.{1}.{2}", url, controller, action).MD5(), url, defaults);
        }

        public static ContentResult Json(this object source, string contentType = "application/json", bool toUnicode = false)
        {
            var content = new ContentResult();
            content.Content = source.JsonSerialize();
            content.ContentType = contentType;
            content.ContentEncoding = System.Text.Encoding.UTF8;

            // safari resolve
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

        public static string MD5(this string source, Encoding encoding = null)
        {
            using (var crypto = new System.Security.Cryptography.MD5CryptoServiceProvider())
            {
                if (encoding == null)
                    encoding = Encoding.UTF8;

                var data = encoding.GetBytes(source);
                return BitConverter.ToString(crypto.ComputeHash(data)).Replace("-", "");
            }
        }

        public static string Hash(this string source, string type, string salt = "", Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(source))
                return "";

            if (type.Equals("sha1", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var crypto = new System.Security.Cryptography.SHA1CryptoServiceProvider())
                {
                    if (encoding == null)
                        encoding = Encoding.UTF8;

                    var data = encoding.GetBytes(source + salt);
                    return BitConverter.ToString(crypto.ComputeHash(data)).Replace("-", "");
                }
            }

            if (type.Equals("sha256", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var crypto = new System.Security.Cryptography.SHA256CryptoServiceProvider())
                {
                    if (encoding == null)
                        encoding = Encoding.UTF8;

                    var data = encoding.GetBytes(source + salt);
                    return BitConverter.ToString(crypto.ComputeHash(data)).Replace("-", "");
                }
            }

            if (type.Equals("sha512", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var crypto = new System.Security.Cryptography.SHA512CryptoServiceProvider())
                {
                    if (encoding == null)
                        encoding = Encoding.UTF8;

                    var data = encoding.GetBytes(source + salt);
                    return BitConverter.ToString(crypto.ComputeHash(data)).Replace("-", "");
                }
            }

            if (type.Equals("md5", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var crypto = new System.Security.Cryptography.MD5CryptoServiceProvider())
                {
                    if (encoding == null)
                        encoding = Encoding.UTF8;

                    var data = encoding.GetBytes(source + salt);
                    return BitConverter.ToString(crypto.ComputeHash(data)).Replace("-", "");
                }
            }

            return EncryptDecrypt.Hash(source + salt);
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
            return Library.Json.JsonSerializer.SerializeObject(source);
        }

        public static T JsonDeserialize<T>(this string source)
        {
            return Library.Json.JsonSerializer.DeSerializeObject<T>(source);
        }

        public static dynamic JsonDeserialize(this string source)
        {
            return Library.Json.JsonSerializer.DeSerializeObject(source);
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
            return string.Format("~/views/helpers/{0}.cshtml", source);
        }

        public static string PathShared(this string source)
        {
            return string.Format("~/views/shared/{0}.cshtml", source);
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
                if (p.PropertyType == typeof(string))
                {
                    var v = p.GetValue(o, null);
                    if (v == null)
                        return;
                    p.SetValue(o, v.ToString().Trim(), null);
                }
            });
        }

        public static T PropertyUpperFirst<T>(this T source, params string[] property)
        {
            if (property == null || property.Length == 0)
                return source;

            return PropertyEach<T>(source, (o, p) =>
            {
                if (p.PropertyType != typeof(string))
                    return;

                if (property.Contains(p.Name))
                {
                    var v = p.GetValue(o, null);
                    if (v == null)
                        return;
                    p.SetValue(o, v.ToString().UpperLower(0), null);
                }
            });
        }

        public static string Join<T>(this T[] source, string delimiter = ", ")
        {
            var sb = new System.Text.StringBuilder();
            foreach (T i in source)
                sb.Append((sb.Length > 0 ? delimiter : string.Empty) + Utils.To<string>(i));
            return sb.ToString();
        }

        public static string Join<T>(this IList<T> source, string delimiter = ", ")
        {
            var sb = new System.Text.StringBuilder();
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
            else
            {
                foreach (string i in value)
                    if (source.Contains(i))
                        return true;
                return false;
            }
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
            else
            {
                foreach (T i in value)
                    if (source.Contains(i))
                        return true;
                return false;
            }
        }

        public static string RegExReplace(this string source, string pattern, string replaceTo)
        {
            return System.Text.RegularExpressions.Regex.Replace(source, pattern, replaceTo);
        }

        public static System.Text.RegularExpressions.Match RegExMatch(this string source, string pattern)
        {
            return System.Text.RegularExpressions.Regex.Match(source, pattern);
        }

        public static bool RegExContains(this string source, string pattern)
        {
            return System.Text.RegularExpressions.Regex.Match(source, pattern).Success;
        }

        public static System.Text.RegularExpressions.MatchCollection RegExMatches(this string source, string pattern)
        {
            return System.Text.RegularExpressions.Regex.Matches(source, pattern);
        }

        public static string UrlEncode(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;
            return System.Web.HttpUtility.UrlEncode(source);
        }

        public static string UrlDecode(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;
            return System.Web.HttpUtility.UrlDecode(source);
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

        public static StringBuilder AppendConfiguration(this StringBuilder source, string name, object value, int padding = 15)
        {
            if (source.Length > 0 && source[source.Length - 1] != '\n')
                source.Append("\n");

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
                source.Append("&");

            return source.Append(name).Append('=').Append(value == null ? "" : value.ToString().UrlEncode());
        }

        public static StringBuilder AppendAttribute(this StringBuilder source, string param, object value)
        {
            if (source.Length > 0)
                source.Append(' ');

            return source.Append(param).Append("=\"").Append(value == null ? "" : value.ToString().HtmlEncode()).Append('\"');
        }

        public static StringBuilder AppendStyle(this StringBuilder source, string param, object value)
        {
            if (source.Length > 0)
                source.Append(";");
            return source.Append(param).Append(":").Append(value);
        }

        public static string RSS(this DateTime source)
        {
            return source.ToString("r").Replace("GMT", "+0200");
        }

        public static T FindId<T>(this string source, bool fromBeg = true, char divider = '-')
        {
            var index = 0;
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

            foreach (System.Text.RegularExpressions.Match m in source.RegExMatches("\\w{" + minLength + ",}"))
            {
                var key = (strict ? m.Value : m.Value.ToLower().RemoveDiakritics().Replace('y', 'i'));
                var value = m.Value;

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
                var value = ParseKeyValue(source);
                if (value != null)
                    zoznam.Add(value);
            }

            return null;
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

        private System.Drawing.Bitmap BMP = null;
        public bool IsLoaded { get; set; }

        public System.Drawing.Bitmap Bitmap
        {
            get { return this.BMP; }
            set { this.BMP = value; }
        }

        public int Width
        {
            get { return (this.BMP != null ? this.BMP.Width : 0); }
        }

        public int Height
        {
            get { return (this.BMP != null ? this.BMP.Height : 0); }
        }

        public static double ColorCompare(System.Drawing.Color color1, System.Drawing.Color color2)
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
            System.Drawing.Imaging.EncoderParameter Param = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
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
            System.Drawing.Imaging.EncoderParameter Param = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            System.Drawing.Imaging.ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");
            System.Drawing.Imaging.EncoderParameters encoderParams = new System.Drawing.Imaging.EncoderParameters(1);
            encoderParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            using (var b = new Bitmap(BMP))
                b.Save(stream, jpegCodec, encoderParams);
        }

        public System.Drawing.Color Pixel(int X, int Y)
        {
            return BMP.GetPixel(X, Y);
        }

        public System.Drawing.Color Pixel()
        {
            return BMP.GetPixel(1, 1);
        }

        public System.Drawing.Graphics Graphics
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

        public ImageConverter(System.Drawing.Image image)
        {
            BMP = new System.Drawing.Bitmap(image);
            BMP.SetResolution(80, 60);
        }

        public ImageConverter(int width, int height)
        {
            BMP = new System.Drawing.Bitmap(width, height);
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
                BMP = new System.Drawing.Bitmap(fileName);
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
                BMP = new System.Drawing.Bitmap(stream);
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
                        BMP = new System.Drawing.Bitmap(MemoryStream);
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
            System.IO.File.WriteAllBytes(fileName, this.SaveAsJpg(quality));
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

            var newBMP = new System.Drawing.Bitmap(width, height);
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

            int BmpWidth = 0;
            int BmpHeight = 0;

            BmpWidth = width;
            BmpHeight = (int)(Math.Round((double)width / ((double)BMP.Width / (double)BMP.Height)));
            Resize(BmpWidth, BmpHeight, resizeFilter);
            return this;
        }

        public ImageConverter ResizeCenter(int width, int height, System.Drawing.Drawing2D.InterpolationMode resizeFilter)
        {

            if (width >= height)
            {
                if (this.Width > this.Height)
                    this.ResizeHeight(height, resizeFilter);
                else
                    this.ResizeWidth(width, resizeFilter);
            }
            else
            {
                if (this.Height > this.Width)
                    this.ResizeHeight(height, resizeFilter);
                else
                    this.ResizeWidth(width, resizeFilter);
            }

            this.SetSize(new System.Drawing.SolidBrush(this.Pixel(1, 1)), width, height, width / 2 - this.Width / 2, height / 2 - this.Height / 2);
            return this;
        }

        public ImageConverter SetSize(System.Drawing.Brush background, int width, int height, float X, float Y)
        {
            if (!CheckBitmap())
                return this;

            var newBMP = new System.Drawing.Bitmap(width, height);
            newBMP.SetResolution(80, 60);

            using (var g = Graphics.FromImage(newBMP))
            {
                g.FillRegion(background, new System.Drawing.Region(new System.Drawing.Rectangle(0, 0, width, height)));
                g.DrawImage(BMP, X, Y, BMP.Width, BMP.Height);
            }

            BMP.Dispose();
            BMP = newBMP;
            return this;
        }

        public ImageConverter SetPosition(System.Drawing.Brush background, float X, float Y)
        {
            if (!CheckBitmap())
                return this;

            var newBMP = new System.Drawing.Bitmap(BMP.Width, BMP.Height);
            newBMP.SetResolution(80, 60);

            using (var g = Graphics.FromImage(newBMP))
            {
                g.FillRegion(background, new System.Drawing.Region(new System.Drawing.Rectangle(0, 0, Width, Height)));
                g.DrawImage(BMP, X, Y, BMP.Width, BMP.Height);
            }

            BMP.Dispose();
            BMP = newBMP;
            return this;
        }

        public ImageConverter ResizeHeight(int heigth, System.Drawing.Drawing2D.InterpolationMode resizeFilter)
        {
            if (!CheckBitmap())
                return this;

            int BmpWidth = 0;
            int BmpHeight = 0;

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

            using (var newBMP = new System.Drawing.Bitmap(new System.IO.MemoryStream(fileData)))
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

        public ImageConverter ResizeTo(int width, int height, System.Drawing.Brush background, System.Drawing.Drawing2D.InterpolationMode resizeFilter)
        {
            if (!CheckBitmap())
                return this;

            int BmpWidth = 0;
            int BmpHeight = 0;
            int x = 0;
            int y = 0;

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

            System.Drawing.Bitmap newBMP = new System.Drawing.Bitmap(width, height);
            newBMP.SetResolution(80, 60);

            using (var picture = System.Drawing.Graphics.FromImage(newBMP))
            {
                picture.FillRegion(background, new System.Drawing.Region(new System.Drawing.Rectangle(0, 0, width, height)));

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

            System.Drawing.Rectangle r = new System.Drawing.Rectangle(left, top, BMP.Width - right, BMP.Height - bottom);
            System.Drawing.Bitmap newBMP = BMP.Clone(r, BMP.PixelFormat);
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

        public ImageConverter Rotate(System.Drawing.Brush background, float angle)
        {
            if (!CheckBitmap())
                return this;

            System.Drawing.Bitmap returnBitmap = new System.Drawing.Bitmap(BMP.Width, BMP.Height);
            returnBitmap.SetResolution(80, 60);

            using (var g = System.Drawing.Graphics.FromImage(returnBitmap))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                g.FillRegion(background, new System.Drawing.Region(new System.Drawing.Rectangle(0, 0, Width, Height)));
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TranslateTransform((float)BMP.Width / 2, (float)BMP.Height / 2);
                g.RotateTransform(angle);
                g.TranslateTransform(-(float)BMP.Width / 2, -(float)BMP.Height / 2);
                g.DrawImage(BMP, new System.Drawing.Point(0, 0));
                BMP.Dispose();
                BMP = returnBitmap;
            }
            return this;
        }

        public ImageConverter GrayScale()
        {
            if (!CheckBitmap())
                return this;

            var newBitmap = new System.Drawing.Bitmap(BMP.Width, BMP.Height);
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

            var newBitmap = new System.Drawing.Bitmap(bmp.Width, bmp.Height);
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
            using (var bmp = new System.Drawing.Bitmap(BMP.Width, BMP.Height))
            {
                bmp.SetResolution(80, 60);
                using (var bmpGraphics = System.Drawing.Graphics.FromImage(bmp))
                {
                    var colorMatrix = new System.Drawing.Imaging.ColorMatrix();
                    colorMatrix.Matrix33 = opacity;
                    var imageAttribute = new System.Drawing.Imaging.ImageAttributes();
                    imageAttribute.SetColorMatrix(colorMatrix, System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);
                    bmpGraphics.DrawImage(BMP, new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, System.Drawing.GraphicsUnit.Pixel, imageAttribute);
                    BMP.Dispose();
                    BMP = bmp;
                    return this;
                }
            }
        }

        public static Bitmap ApplyOpacity(Bitmap b, float opacity)
        {
            var nb = new System.Drawing.Bitmap(b.Width, b.Height);
            nb.SetResolution(80, 60);
            using (var bmpGraphics = System.Drawing.Graphics.FromImage(b))
            {
                var colorMatrix = new System.Drawing.Imaging.ColorMatrix();
                colorMatrix.Matrix33 = opacity;
                var imageAttribute = new System.Drawing.Imaging.ImageAttributes();
                imageAttribute.SetColorMatrix(colorMatrix, System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);
                bmpGraphics.DrawImage(b, new System.Drawing.Rectangle(0, 0, nb.Width, nb.Height), 0, 0, nb.Width, nb.Height, System.Drawing.GraphicsUnit.Pixel, imageAttribute);
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
            return Configuration.OnImageParserUrl(this.Dimension, this);
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
            this.Dimension = dimension;

            if (Configuration.OnImageParserDimension == null)
                return this;

            var size = Configuration.OnImageParserDimension(dimension);
            if (size.IsEmpty)
                return this;

            this.Width = size.Width;
            this.Height = size.Height;
            return this;
        }

        public HtmlString ToHtml(string className = "")
        {
            return new HtmlString(Render(className));
        }

        public string Render(string className = "")
        {
            return string.Format("<img src=\"{0}\"{1}{2} alt=\"{3}\" border=\"0\"{4} />", Configuration.OnImageParserUrl(this.Dimension, this), Width > 0 ? string.Format(" width=\"{0}\"", Width) : "", Height > 0 ? string.Format(" height=\"{0}\"", Height) : "", Name, string.IsNullOrEmpty(className) ? "" : string.Format(" class=\"{0}\"", className));
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
    public class Scheduler
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
                Items.Add(new Schedule() { Name = name, Count = 0, Index = 0, Minutes = minutes, OnTick = onTick });

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

            if (this.OnSchedule != null)
                this.OnSchedule();
        }

        public V Add(K key, V value, DateTime expire)
        {
            Item obj = null;
            var ticks = expire.Ticks(true);

            if (Items.TryGetValue(key, out obj))
            {
                obj.Expire = ticks;
                obj.Value = value;
                return value;
            }

            Items.TryAdd(key, new Item() { Expire = ticks, Value = value });

            if (OnOnline != null)
                OnOnline(key, value);

            return value;
        }

        public bool SetExpire(K key, DateTime expire)
        {
            Item obj = null;

            if (!Items.TryGetValue(key, out obj))
                return false;

            obj.Expire = expire.Ticks(true);
            return true;
        }

        public bool Remove(K key)
        {
            Item obj = null;
            var isDeleted = Items.TryRemove(key, out obj);

            if (OnOffline != null && obj != null)
                OnOffline(key, obj.Value);

            return isDeleted;
        }

        public V Read(K key)
        {
            Item obj = null;
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

        public enum MarkdownListType : byte
        {
            plus = 0,
            minus = 1,
            x = 2
        }

        public enum MarkdownBreakType : byte
        {
            newline = 0,
            line = 1,
            page = 2
        }

        public enum MarkdownParagraphType : byte
        {
            gt = 0,
            slash = 1,
            comment = 2,
            line = 3
        }

        public enum MarkdownTitleType : byte
        {
            h1 = 0,
            h2 = 1,
            h3 = 2,
            h4 = 5,
            h5 = 6
        }

        public enum MarkdownKeywordType : byte
        {
            composite = 0,
            square = 1
        }

        public enum MarkdownFormatType : byte
        {
            b = 0,
            strong = 1,
            i = 2,
            em = 3
        }

        private enum MarkdownParserType : byte
        {
            empty = 0,
            paragraph = 100,
            embedded = 101,
            list = 102,
            keyvalue = 103
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
        public delegate string KeywordDelegate(MarkdownKeywordType type);
        public delegate string ParagraphDelegate(MarkdownParagraphType type, IList<string> lines);
        public delegate string LineDelegate(string line);
        public delegate string FormatDelegate(MarkdownFormatType type, string value);
        public delegate string LinkDelegate(string text, string url);
        public delegate string ImageDelegate(string alt, string src, int width, int height, string url);
        public delegate string ListDelegate(IList<MarkdownList> items);
        public delegate string KeyValueDelegate(IList<KeyValue> items);
        public delegate string BreakDelegate(MarkdownBreakType type);

        public string Embedded { get; set; }
        public BreakDelegate OnBreak { get; set; }

        private bool skip = false;
        private string id = "";
        private string command = "";
        private MarkdownParserType status = MarkdownParserType.empty;

        private List<string> current = new List<string>(5);
        private List<MarkdownList> currentList = null;
        private List<KeyValue> currentKeyValue = null;

        private string output = "";

        public Markdown()
        {
            Embedded = "===";
        }

        public string Compile(string text, string id = "")
        {
            var arr = text.Split('\n');
            var length = arr.Length;

            this.id = id;
            skip = false;

            for (var i = 0; i < arr.Length; i++)
            {
                if (skip)
                {
                    skip = false;
                    continue;
                }

                var line = arr[i];

                if (ParseEmbedded(line))
                    continue;

                if (ParseBreak(line))
                    continue;

            }

            return "";
        }

        private void Flush()
        {

        }

        private bool ParseEmbedded(string line)
        {
            var chars = Embedded + (status != MarkdownParserType.embedded ? " " : "");
            var has = line.Substring(0, chars.Length) == chars;

            if (status != MarkdownParserType.embedded && !has)
                return false;

            if (status != MarkdownParserType.embedded && has)
                Flush();

            if (status == MarkdownParserType.embedded && has)
            {
                Flush();
                status = MarkdownParserType.empty;
                return true;
            }

            if (has)
            {
                status = MarkdownParserType.embedded;
                command = line.Substring(chars.Length);
                return true;
            }

            if (status == MarkdownParserType.embedded)
                current.Add(line);

            return true;
        }

        private bool ParseBreak(string line)
        {
            if (!(line == "" || line == "***" || line == "---"))
                return false;

            if (status != MarkdownParserType.empty)
                Flush();

            status = MarkdownParserType.empty;
            MarkdownBreakType type = MarkdownBreakType.newline;

            switch (line)
            {
                case "***":
                    type = MarkdownBreakType.page;
                    break;
                case "---":
                    type = MarkdownBreakType.line;
                    break;
            }

            if (OnBreak != null)
                output += OnBreak(type);

            return true;
        }

        private bool ParseList(string line)
        {
            var first = line[0];
            var second = line[1];
            var has = (first == '-' || first == '+' || first == 'x') && (second == ' ');

            if (!has)
                return false;

            if (status != MarkdownParserType.list)
            {
                Flush();
                status = MarkdownParserType.list;
            }

            if (currentList == null)
                currentList = new List<MarkdownList>(5);

            MarkdownListType type = MarkdownListType.x;

            switch (first)
            {
                case '-':
                    type = MarkdownListType.minus;
                    break;
                case '+':
                    type = MarkdownListType.plus;
                    break;
            }

            currentList.Add(new MarkdownList(type, ParseOther(line.Substring(2))));
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

            if (status != MarkdownParserType.keyvalue)
            {
                Flush();
                status = MarkdownParserType.keyvalue;
            }

            if (currentKeyValue == null)
                currentKeyValue = new List<KeyValue>(3);

            currentKeyValue.Add(new KeyValue(ParseOther(tmp.Trim()), ParseOther(line.Substring(index + 1).Trim())));
            return true;
        }

        private string ParseOther(string line)
        {
            // 
            return line;
        }

    }


    public class MarkdownOLD
    {
        public class Table
        {
            public int ColumnCount { get; set; }
            public int Width { get; set; }
            public List<Row> Rows { get; set; }
        }

        public class Row
        {
            public int Index { get; set; }
            public List<Column> Columns { get; set; }
        }

        public class Column
        {
            public int Index { get; set; }
            public int Size { get; set; }
            public string Value { get; set; }
        }

        public class UL
        {
            public char Type { get; set; }
            public int Index { get; set; }
            public int Indent { get; set; }
            public string Value { get; set; }
        }

        public string Name { get; set; }

        /// <summary>
        /// type, line
        /// </summary>
        public Func<string, string, string> OnLine { get; set; }

        /// <summary>
        /// type, lines
        /// </summary>
        public Func<string, IList<string>, string> OnLines { get; set; }

        public Func<IList<UL>, string> OnUL { get; set; }
        public Func<Table, string> OnTable { get; set; }
        public Func<IList<KeyValue>, string> OnKeyValue { get; set; }

        /// <summary>
        /// name, url, return TAG
        /// </summary>
        public Func<string, string, string> OnLink { get; set; }

        /// <summary>
        /// type, value, return TAG
        /// </summary>
        public Func<string, string, string> OnFormat { get; set; }

        /// <summary>
        /// type, name, value, return TAG
        /// </summary>
        public Func<string, string, string, string> OnKeyword { get; set; }

        /// <summary>
        /// alt, url, width, height, return TAG
        /// </summary>
        public Func<string, string, int, int, string> OnImage { get; set; }

        private static Regex regFormatB = new Regex(@"(^|\s|\.|\,)[^\/]+_[^_]+[^\/]+_", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex regFormatSTRONG = new Regex(@"(^|\s|\.|\,)[^\/]+_{2}[^_]+[^\/]+_{2}", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex regFormatI = new Regex(@"(^|\s|\.|\,|\\)\*[^\*]+\*", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex regFormatEM = new Regex(@"(^|\s|\.|\,|\\)\*{2[^\*]+\*{2}", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex regKeyword = new Regex("(\\[.*?\\]|\\{.*?\\})", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex regImage = new Regex("\\!\\[[^\\]]+\\][\\:\\s\\(]+.*?[^)\\s$]+", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex regLink1 = new Regex("\\<.*?\\>+", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex regLink2 = new Regex("(!)?\\[[^\\]]+\\][\\:\\s\\(]+.*?[^)\\s]+", RegexOptions.Compiled | RegexOptions.Singleline);
        private const string ASTERIX = "#@42#";
        private const string UNDERSCORE = "#@95#";

        private string Clean(string text)
        {
            if (text.IsEmpty())
                return "";

            var sb = new System.Text.StringBuilder();
            foreach (var m in text)
            {
                if (m == 13 || m == 10)
                    continue;

                sb.Append(m);
            }

            return sb.ToString();
        }

        private bool IsFill(string line, char c)
        {
            foreach (var m in line)
            {
                if (m != c)
                    return false;
            }
            return true;
        }

        private int CharCount(string line, char c)
        {
            var count = 0;
            foreach (var m in line)
            {
                if (m == c)
                    count++;
            }
            return count;
        }

        private char Nearest(string line)
        {
            foreach (var m in line)
            {
                if (!char.IsControl(m) && !char.IsWhiteSpace(m))
                    return m;
            }

            return ' ';
        }

        private char FirstChar(string line)
        {
            if (line.Length == 0)
                return '\0';
            return line[0];
        }

        public string Load(string text, string name = "")
        {
            if (text.IsEmpty())
                return "";

            this.Name = name;

            Table tmpTable = null;
            var tmpName = "";
            var tmp = new List<string>(10);
            var tmpUL = new List<UL>(5);
            var tmpKeyValue = new List<KeyValue>(5);

            var lines = text.Split('\n');
            var read = new Func<int, string>(n => n < lines.Length ? Clean(lines[n]) : "");
            var isBlock = false;
            var isTable = false;
            var skip = false;
            var index = 0;
            var sb = new System.Text.StringBuilder();

            var flushUL = new Action(() =>
            {
                if (OnUL != null)
                {
                    sb.Append(OnUL(tmpUL));
                }
                else
                {
                    foreach (var m in tmpUL)
                        sb.Append(OnLine(null, m.Value));
                }

                tmpUL.Clear();
            });

            var flushKeyValue = new Action(() =>
            {
                if (OnKeyValue != null)
                    sb.Append(OnKeyValue(tmpKeyValue));
                tmpKeyValue.Clear();
            });

            var flushParagraph = new Action(() =>
            {
                isBlock = false;
                sb.Append(OnLines(tmpName, tmp));
                tmp.Clear();
            });

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Kroky
                // 1. kontrola
                // 2. je blok?
                // 3. je tabuľka?
                // 4. je odsek, čiara?
                // 5. je UL?
                // 6. je KeyValue?
                // 7. je paragraf?
                // 8. je nadpis?
                // 9. text

                if (skip)
                {
                    skip = false;
                    continue;
                }

                if (isBlock)
                {
                    if (line.Trim().StartsWith("==="))
                    {
                        flushParagraph();
                        continue;
                    }

                    tmp.Add(line);
                    continue;
                }

                var m = Clean(line);

                if (m.Length == 0)
                {
                    sb.Append(OnLine(null, "\n"));
                    continue;
                }

                var c = m[0];
                var cn = m.Length > 1 ? m[1] : '\0';

                // 2. je blok?
                if (line.StartsWith("==="))
                {
                    index = m.LastIndexOf('=') + 1;
                    if (index < m.Length)
                    {
                        tmpName = m.Substring(index + 1).Trim();
                        isBlock = true;
                        continue;
                    }
                }

                // 3. je tabuľka?
                if (m.Length > 1)
                {
                    if (c == '|' && m[1] == '-' && m[m.Length - 1] == '|')
                    {
                        isTable = !isTable;

                        if (isTable)
                        {
                            tmpTable = new Table();
                            tmpTable.ColumnCount = 0;
                            tmpTable.Rows = new List<Row>(3);
                        }
                        else
                        {
                            if (OnTable != null && tmpTable != null)
                                sb.Append(OnTable(tmpTable));
                        }
                        continue;
                    }

                    if (isTable)
                    {
                        var columns = m.Split('|');
                        var columnCount = columns.Length - 2;
                        var row = new Row();

                        row.Index = tmpTable.Rows.Count;
                        row.Columns = new List<Column>(columnCount);

                        if (tmpTable.ColumnCount < columnCount)
                            tmpTable.ColumnCount = columnCount;

                        for (var j = 0; j < columns.Length; j++)
                        {
                            var a = columns[j];
                            if (j > 0 && j < columns.Length - 1)
                            {
                                row.Columns.Add(new Column() { Index = row.Columns.Count, Size = a.Length, Value = Parse(a.Trim()) });
                                if (row.Index == 0)
                                    tmpTable.Width += a.Length;
                            }
                        }

                        tmpTable.Rows.Add(row);
                        continue;
                    }
                }

                // 4. je odsek, čiara?
                if (m.Length > 0 && (c == '*' || c == '-'))
                {
                    if (IsFill(m, c))
                    {
                        sb.Append(OnLine(c.ToString(), m));
                        continue;
                    }
                }

                var next = read(i + 1);

                // 5. je UL?
                if (IsUL(m))
                {
                    var value = m;
                    var a = c;

                    if (Char.IsWhiteSpace(c))
                    {
                        a = Nearest(m);
                        value = value.Substring(value.IndexOf(a));
                    }

                    var ul = new UL() { Index = tmpUL.Count, Indent = CharCount(m, c), Value = Parse(value.Substring(1)), Type = a };
                    tmpUL.Add(ul);

                    if (!IsUL(next))
                        flushUL();

                    continue;
                }

                if (tmpUL.Count > 0)
                    flushUL();

                // 6 je KeyValue?
                if (IsKeyValue(m))
                {
                    var keyvalue = m.ParseKeyValue();

                    if (keyvalue != null)
                    {
                        keyvalue.Key = Parse(keyvalue.Key);
                        keyvalue.Value = Parse(keyvalue.Value);
                        tmpKeyValue.Add(keyvalue);
                    }

                    if (!IsKeyValue(next))
                        flushKeyValue();

                    continue;
                }

                if (tmpKeyValue.Count > 0)
                    flushKeyValue();

                // 7. je paragraf?
                if (IsParagraph(c, cn))
                {
                    if (FirstChar(tmpName) != c && tmp.Count > 0)
                        flushParagraph();

                    tmpName = c == '/' ? "//" : c.ToString();
                    tmp.Add(Parse(m.Substring(1)));

                    c = FirstChar(read(i + 1));

                    if (!IsParagraph(c, cn))
                        flushParagraph();

                    continue;
                }

                // 8. je nadpis?
                if (c == '#')
                {
                    index = m.LastIndexOf(c);
                    if (index != m.Length)
                    {
                        index++;
                        sb.Append(OnLine(m.Substring(0, index), m.Substring(index, m.Length - index).Trim()));
                        continue;
                    }
                }

                // kontrola či nasledujíci riadok nie je čiara kvôli nadpisu
                if (m.Length == next.Length)
                {
                    c = FirstChar(next);
                    if (c == '-' || c == '=')
                    {
                        sb.Append(OnLine(c == '=' ? "#" : "##", m.Trim()));
                        skip = true;
                        continue;
                    }
                }

                sb.Append(OnLine(null, Parse(m)));
            }

            return sb.ToString();
        }

        private bool IsParagraph(char c, char next)
        {
            return c == '>' || c == '|' || (c == '/' && next == '/') || (c == '\\' && next == '\\');
        }

        private bool IsKeyValue(string line)
        {
            var index = line.IndexOf(':');
            if (index == -1)
                return false;

            var countSpace = 0;
            var countTab = 0;

            foreach (var m in line.Substring(0, index))
            {
                if (m == '\t')
                {
                    countTab = 1;
                    break;
                }

                if (m == ' ')
                {
                    countSpace++;
                    if (countSpace > 2)
                        break;
                }
                else
                    countSpace = 0;
            }

            return countSpace > 2 || countTab > 1;
        }

        private bool IsUL(string value)
        {
            if (value.IsEmpty())
                return false;

            var c = FirstChar(value);

            if (Char.IsWhiteSpace(c))
                c = Nearest(value);

            return (c == '-' || c == 'x' || c == '+') && value.IndexOf(' ') > -1;
        }

        private string Parse(string line)
        {
            var index = 0;

            if (OnLink != null)
                line = ParseLink(line, OnLink);

            if (OnImage != null)
                line = ParseImage(line, OnImage);

            if (OnFormat != null)
            {
                foreach (string m in FindFormat(line))
                {
                    if (m.Length < 3)
                        continue;

                    var value = m;
                    var max = 2;
                    var isMax = false;

                    if (value[0] != '*' && value[0] != '_')
                        value = value.Substring(1);

                    switch (value[0])
                    {
                        case '*':
                            isMax = value.StartsWith("**");
                            if (isMax)
                                max = value.Length > 3 ? 4 : value.Length;

                            var re = OnFormat(isMax ? "**" : "*", isMax ? value.Substring(2, value.Length - max) : value.Substring(1, value.Length - 2));
                            if (re != null)
                            {
                                line = ReplaceFirst(line, value, re);
                                // line = line.Replace(m.Value, re);
                            }

                            continue;

                        case '_':
                            var count = value.StartsWith("___") ? 3 : value.StartsWith("__") ? 2 : 1;
                            var rb = OnFormat(value.Substring(0, count), value.Substring(count, value.Length - (count * 2)));
                            if (rb != null)
                            {
                                line = ReplaceFirst(line, value, rb);
                                // line = line.Replace(m.Value, rb);
                            }
                            continue;
                    }
                }
            }

            if (OnKeyword != null)
            {
                line = ParseKeyword(line, val =>
                {
                    var key = val.Substring(1);
                    key = key.Substring(0, key.Length - 1);
                    var value = "";

                    index = key.IndexOf('(');

                    if (index > 0)
                    {
                        value = key.Substring(index + 1, key.Length - (index + 2));
                        key = key.Substring(0, index).Trim();
                    }

                    var normal = val[0] == '[';
                    return OnKeyword(normal ? "[]" : "{}", key, value);
                });
            }

            if (OnImage != null)
                line = line.Replace(ASTERIX, "*").Replace(UNDERSCORE, "_");

            return line.Trim();
        }

        private string ParseLink(string line, Func<string, string, string> callback)
        {
            if (line.IsEmpty())
                return line;

            var output = line;

            foreach (Match m in regLink1.Matches(line))
            {
                var url = m.Value.Substring(1, m.Value.Length - 2);
                output = output.Replace(m.Value, callback(url, url));
            }

            foreach (Match m in regLink2.Matches(line))
            {
                var o = m.Value;

                var index = o.IndexOf(']');
                if (index == -1)
                    continue;

                if (m.Value[0] == '!')
                    continue;

                var text = o.Substring(1, index - 1).Trim();
                var url = o.Substring(index + 1).Trim();

                var first = url[0];

                if (first == '(' || first == '(' || first == ':')
                    url = url.Substring(1).Trim();
                else
                    continue;

                if (first == '(')
                    o += ')';

                var last = url[url.Length - 1];

                if (last == ',' || last == '.' || last == ' ')
                    url = url.Substring(0, url.Length - 1);
                else
                    last = '\0';

                output = ReplaceFirst(output, o, callback(text, url) + (last == '\0' ? "" : last.ToString()));
                //output = output.Replace(o, callback(text, url) + (last == '\0' ? "" : last.ToString()));
            }

            return output;
        }

        private string ParseImage(string line, Func<string, string, int, int, string> callback)
        {
            if (line.IsEmpty())
                return line;

            var output = line;
            foreach (Match m in regImage.Matches(line))
            {
                var o = m.Value;
                var index = o.IndexOf(']');
                if (index == -1)
                    continue;

                var text = o.Substring(2, index - 2).Trim();
                var url = o.Substring(index + 1).Trim();

                var first = url[0];
                if (first != '(')
                    continue;

                url = url.Substring(1);

                index = url.IndexOf('#');
                string[] dimension = null;

                if (index > 0)
                {
                    dimension = url.Substring(index + 1).Split('x');
                    url = url.Substring(0, index);
                }

                output = ReplaceFirst(output, o + ')', callback(text, url, dimension != null ? dimension.Read<int>(0) : 0, dimension != null ? dimension.Read<int>(1) : 0).Replace("*", ASTERIX).Replace("_", UNDERSCORE));
                //output = output.Replace(o + ')', callback(text, url, dimension != null ? dimension.Read<int>(0) : 0, dimension != null ? dimension.Read<int>(1) : 0).Replace("*", ASTERIX).Replace("_", UNDERSCORE));
            }

            return output;
        }

        private string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
                return text;
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        private static IEnumerable<string> FindFormat(string line)
        {
            var zoznam = new List<string>(5);
            var beg = -1;
            var length = line.Length;

            for (var i = 0; i < length; i++)
            {
                var c = line[i];
                var prev = i > 0 ? line[i - 1] : '\0';
                var next = i + 1 < length ? line[i + 1] : '\0';

                if (beg != -1)
                {
                    if (c == '.' && (next == '\0' || next == ' '))
                        beg = -1;
                }

                if (c == '*' || c == '_')
                {

                    if (beg != -1)
                    {
                        if (next == '*' || next == '_')
                        {
                            i++;
                            next = i + 1 < length ? line[i + 1] : '\0';
                        }

                        if (next != '\0' && next != '.' && next != ',' && next != ' ' && next != '-' && next != ':')
                        {
                            beg = -1;
                            continue;
                        }

                        zoznam.Add(line.Substring(beg, (i - beg) + 1));
                        beg = -1;
                    }
                    else
                    {
                        if ((prev == '\0' || prev == ' '))
                        {
                            if (i == 0 || prev == '\0' || prev == '.' || prev == ',' || prev == ' ')
                                beg = i;

                            if (next == '*' || next == '_')
                                i++;
                        }

                    }
                }

            }

            return zoznam.OrderByDescending(n => n.Length);
        }

        private string ParseKeyword(string line, Func<string, string> cb)
        {
            if (line.IsEmpty())
                return line;

            var indexBegA = -1;
            var indexBegB = -1;
            var index = 0;
            var output = line;

            do
            {
                var c = line[index];

                switch (c)
                {
                    case '[':

                        indexBegA = index;
                        indexBegB = -1;
                        break;

                    case ']':

                        if (indexBegA > -1)
                        {
                            var value = line.Substring(indexBegA, (index - indexBegA) + 1);
                            output = ReplaceFirst(output, value, cb(value));
                            //output.Replace(value, cb(value));
                        }

                        break;
                    case '{':

                        indexBegB = index;
                        indexBegA = -1;
                        break;
                    case '}':
                        if (indexBegB > -1)
                        {
                            var value = line.Substring(indexBegB, (index - indexBegB) + 1);
                            output = ReplaceFirst(output, value, cb(value));
                            //output = output.Replace(value, cb(value));
                        }
                        break;
                }

                index++;
            } while (index < line.Length);

            return output;
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
    public sealed class EncryptDecrypt
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
                int index = 0;

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

            if (max == 0)
                max = 20;

            Count = (items / max) + (items % max > 0 ? 1 : 0);

            Current = Current - 1;

            if (Current < 0)
                Current = 0;

            Skip = Current * max;
            Take = max;
            Visible = Count > 0;
            IsPrev = Current > 0;
            IsNext = Current < Count - 1;

            this.Current++;
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

            if (this.OnSchedule != null)
                this.OnSchedule();
        }

        public bool SetExpire(string key, DateTime expire)
        {
            File file = null;

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
            File file = null;

            if (!Items.TryRemove(id, out file))
                return;

            if (System.IO.File.Exists(file.Path))
                System.IO.File.Delete(file.Path);
        }

        public File Read(string id)
        {
            File file = null;
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
    public class Sitemap
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
            l.Add(new Page() { Name = name, Url = url.Empty(HttpContext.Current.Request.Url()), Priority = priority == -1 ? l.Count : priority });
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
