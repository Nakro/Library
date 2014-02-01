using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;

namespace Library.Handler
{
    // ===============================================================================================
    // 
    // Library.Handler.[CSS]
    // 
    // ===============================================================================================

    // ===============================================================================================
    // Autor        : Peter Širka
    // Created      : 10. 01. 2012
    // Updated      : 03. 06. 2013
    // Description  : CSS Handler LESS, Minify
    // ===============================================================================================

    #region CSS
    public class CSS : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var path = context.Request.RawUrl.Replace('/', '\\');
            if (path[0] == '\\')
                path = '~' + path;

            context.Response.ContentType = "text/css";
            context.Response.ContentEncoding = System.Text.Encoding.UTF8;

            if (System.IO.File.Exists(context.Server.MapPath(path)))
            {
                if (Configuration.IsDebug)
                {
                    context.Response.BinaryWrite(System.Text.Encoding.UTF8.GetBytes(Module.Less.Compile(System.IO.File.ReadAllText(context.Server.MapPath(path)))));
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
                    return Utils.CacheWrite<string>(key, Module.Less.Compile(System.IO.File.ReadAllText(context.Server.MapPath(path))), expire);
                });

                context.Response.BinaryWrite(System.Text.Encoding.UTF8.GetBytes(data));
                return;

            }
            else
            {
                context.Response.StatusCode = 404;
                context.Response.StatusDescription = "File not found.";
            }
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
    // Library.Handler.[JS]
    // 
    // ===============================================================================================

    // ===============================================================================================
    // Autor        : Peter Širka
    // Created      : 10. 01. 2012
    // Updated      : 03. 06. 2013
    // Description  : JavaScript Handler Minify
    // ===============================================================================================

    #region JS
    public class JS : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var path = context.Request.RawUrl.Replace('/', '\\');
            if (path[0] == '\\')
                path = '~' + path;

            context.Response.ContentType = "text/javascript";
            context.Response.ContentEncoding = System.Text.Encoding.UTF8;

            if (System.IO.File.Exists(context.Server.MapPath(path)))
            {
                if (Configuration.IsDebug)
                {

                    if (path.Contains(".min.") || path.Contains("-min."))
                    {
                        context.Response.WriteFile(context.Server.MapPath(path));
                        return;
                    }

                    using (var ms = new System.IO.MemoryStream(System.IO.File.ReadAllBytes(context.Server.MapPath(path))))
                        context.Response.BinaryWrite(System.Text.Encoding.UTF8.GetBytes(new Module.JavaScriptMinifier().Minify(ms)));

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
                        return Utils.CacheWrite<string>(key, new Module.JavaScriptMinifier().Minify(ms), expire);
                });

                context.Response.BinaryWrite(System.Text.Encoding.UTF8.GetBytes(data));
            }
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
