using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;

namespace Library.Handlers
{
    // ===============================================================================================
    //
    // Library.Handler.[Css]
    //
    // ===============================================================================================

    // ===============================================================================================
    // Autor        : Peter Širka
    // Created      : 10. 01. 2012
    // Updated      : 10. 08. 2014
    // Description  : CSS Handler LESS, Minify
    // ===============================================================================================

    #region Css
    public class Css : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var path = context.Request.RawUrl;

            if (Configuration.IsWindows)
                path = path.Replace('/', '\\');

            if (path[0] == '\\')
                path = '~' + path;

            context.Response.ContentType = "text/css";
            context.Response.ContentEncoding = System.Text.Encoding.UTF8;

            if (!System.IO.File.Exists(context.Server.MapPath(path)))
            {
                context.Response.Write("File not found.");
                context.Response.StatusCode = 404;
                return;
            }

            if (Configuration.IsDebug)
            {
                context.Response.BinaryWrite(System.Text.Encoding.UTF8.GetBytes(Modules.Less.Compile(System.IO.File.ReadAllText(context.Server.MapPath(path)))));
                return;
            }

            var etag = path.ETag();
            if (context.Request.NotModified(etag))
            {
                context.Response.StatusCode = 304;
                return;
            }

            context.Response.SetETag(etag);

            if (path.Contains(".min.") || path.Contains("-min."))
            {
                context.Response.WriteFile(context.Server.MapPath(path));
                return;
            }

            var data = Utils.CacheRead<string>(path.Substring(1), key =>
            {
                var expire = DateTime.Now.AddMinutes(10);
                return Utils.CacheWrite<string>(key, Modules.Less.Compile(System.IO.File.ReadAllText(context.Server.MapPath(path))), expire);
            });

            context.Response.BinaryWrite(System.Text.Encoding.UTF8.GetBytes(data));
        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    }
    #endregion

    // ===============================================================================================
    //
    // Library.Handler.[Js]
    //
    // ===============================================================================================

    // ===============================================================================================
    // Autor        : Peter Širka
    // Created      : 10. 01. 2012
    // Updated      : 10. 08. 2014
    // Description  : JavaScript Handler Minify
    // ===============================================================================================

    #region Js
    public class Js : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var path = context.Request.RawUrl;

            if (Configuration.IsWindows)
                path = path.Replace('/', '\\');

            if (path[0] == '\\')
                path = '~' + path;

            if (!System.IO.File.Exists(context.Server.MapPath(path)))
            {
                context.Response.Write("File not found.");
                context.Response.StatusCode = 404;
                return;
            }

            context.Response.ContentType = "text/javascript";
            context.Response.ContentEncoding = System.Text.Encoding.UTF8;

            if (Configuration.IsDebug)
            {
                if (path.Contains(".min.") || path.Contains("-min."))
                {
                    context.Response.WriteFile(context.Server.MapPath(path));
                    return;
                }

                using (var ms = new System.IO.MemoryStream(System.IO.File.ReadAllBytes(context.Server.MapPath(path))))
                    context.Response.BinaryWrite(System.Text.Encoding.UTF8.GetBytes(new Modules.JavaScriptMinifier().Minify(ms)));

                return;
            }

            var etag = path.ETag();

            if (context.Request.Headers["If-None-Match"] == etag)
            {
                context.Response.StatusCode = 304;
                return;
            }

            context.Response.SetETag(etag);

            if (path.Contains(".min.") || path.Contains("-min."))
            {
                context.Response.WriteFile(context.Server.MapPath(path));
                return;
            }

            var data = Utils.CacheRead<string>(path.Substring(1), key =>
            {
                var expire = DateTime.Now.AddMinutes(10);
                using (var ms = new System.IO.MemoryStream(System.IO.File.ReadAllBytes(context.Server.MapPath(path))))
                    return Utils.CacheWrite<string>(key, new Modules.JavaScriptMinifier().Minify(ms), expire);
            });

            context.Response.BinaryWrite(System.Text.Encoding.UTF8.GetBytes(data));
        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    }
    #endregion

}
