using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.Mvc;
using System.Web.Caching;
using System.Web.Routing;
using System.ComponentModel.DataAnnotations;

namespace Library
{
    // FOR PROPERTIES

    #region Email Attribute
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class EmailAttribute : RegularExpressionAttribute
    {
        public EmailAttribute()
            : base(@"([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})")
        { }
    }
    #endregion

    #region Json Attribute
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class JsonAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            var str = value as string;
            if (string.IsNullOrEmpty(str))
                return true;
            return str.IsJson();
        }
    }
    #endregion

    #region Number Attribute
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class NumberAttribute : ValidationAttribute
    {
        public bool IsDecimal { get; set; }

        public NumberAttribute(bool isDecimal = false)
            : base("The number is not valid.")
        {
            IsDecimal = isDecimal;
        }

        public override bool IsValid(object value)
        {
            if (value == null)
                return true;

            var val = value.ToString();
            if (val.IsEmpty())
                return true;

            return IsDecimal ? val.To<decimal>() > 0M : val.To<int>() > 0;
        }
    }
    #endregion

    #region Checked Attribute
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class CheckedAttribute : ValidationAttribute
    {
        public CheckedAttribute()
            : base("You must agree.")
        { }

        public override bool IsValid(object value)
        {
            if (value == null)
                return true;

            var val = value.ToString();
            if (val.IsEmpty())
                return true;

            return val == "1" || val.ToLower() == "true";
        }
    }
    #endregion

    // FOR ACTIONS

    #region Xhr Attribute
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class XhrAttribute : ActionFilterAttribute
    {
        public bool HostValid { get; set; }
        public int HttpStatus { get; set; }

        public XhrAttribute()
        {
            HostValid = true;
        }

        public XhrAttribute(bool hostValid)
        {
            HostValid = hostValid;
        }

        public XhrAttribute(bool hostValid, int httpStatus)
        {
            HostValid = hostValid;
            HttpStatus = httpStatus;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (HttpStatus == 0)
                HttpStatus = 406;

            if (!filterContext.HttpContext.Request.IsAjaxRequest())
                filterContext.Result = new HttpStatusCodeResult(HttpStatus);

            if (HostValid)
            {
                var referrer = filterContext.HttpContext.Request.UrlReferrer;

                if (referrer == null || referrer.Host != filterContext.HttpContext.Request.Url.Host)
                    filterContext.Result = new HttpStatusCodeResult(HttpStatus);
            }

            base.OnActionExecuting(filterContext);
        }
    }
    #endregion

    #region Header Attribute
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class HeaderAttribute : ActionFilterAttribute
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public int HttpStatus { get; set; }

        public HeaderAttribute() { }

        public HeaderAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public HeaderAttribute(string name, string value, int httpStatus)
        {
            Name = name;
            Value = value;
            HttpStatus = httpStatus;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (HttpStatus == 0)
                HttpStatus = 403;

            var value = filterContext.HttpContext.Request.Headers[Name];
            if (value != Value)
                filterContext.Result = new HttpStatusCodeResult(HttpStatus);

            base.OnActionExecuting(filterContext);
        }
    }
    #endregion

    #region Referer Attribute
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RefererAttribute : ActionFilterAttribute
    {
        [DefaultValue(404)]
        public int HttpStatus { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var referrer = filterContext.HttpContext.Request.UrlReferrer;

            if (referrer == null || referrer.Host != filterContext.HttpContext.Request.Url.Host)
                filterContext.Result = new HttpStatusCodeResult(HttpStatus);

            base.OnActionExecuting(filterContext);
        }
    }
    #endregion

    #region UnLogged Attribute
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class UnLoggedAttribute : ActionFilterAttribute
    {
        private Authorization authorization = Authorization.Unlogged;

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Configuration.OnAuthorization == null)
                return;

            authorization = Configuration.OnAuthorization(filterContext.HttpContext, string.Empty, string.Empty);

            if (authorization != Authorization.Unlogged)
            {
                filterContext.Result = Configuration.OnAuthorizationError(filterContext.RequestContext.HttpContext.Request, authorization);
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
    #endregion

    #region Logged Attribute
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class LoggedAttribute : AuthorizeAttribute
    {
        private Authorization authorization = Authorization.Unlogged;

        public string ErrorMessage { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (Configuration.OnAuthorization == null)
                return false;

            authorization = Configuration.OnAuthorization(httpContext, Roles, Users);
            return authorization == Authorization.Logged;
        }

        private void CacheValidateHandler(HttpContext context, object data, ref HttpValidationStatus validationStatus)
        {
            validationStatus = OnCacheAuthorization(new HttpContextWrapper(context));
        }

        protected override HttpValidationStatus OnCacheAuthorization(HttpContextBase httpContext)
        {
            return AuthorizeCore(httpContext) ? HttpValidationStatus.Valid : HttpValidationStatus.Invalid;
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (!AuthorizeCore(filterContext.HttpContext))
            {
                filterContext.Result = Configuration.OnAuthorizationError(filterContext.RequestContext.HttpContext.Request, authorization);
                return;
            }

            HttpCachePolicyBase cachePolicy = filterContext.HttpContext.Response.Cache;
            cachePolicy.SetProxyMaxAge(new TimeSpan(0));
            cachePolicy.AddValidationCallback(CacheValidateHandler, null);
        }
    }
    #endregion

    #region User Attribute
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class UserAttribute : AuthorizeAttribute
    {
        private Authorization authorization = Authorization.Unlogged;
        public string ErrorMessage { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (Configuration.OnAuthorization == null)
                return false;

            authorization = Configuration.OnAuthorization(httpContext, Roles, Users);
            return authorization == Authorization.Logged;
        }

        private void CacheValidateHandler(HttpContext context, object data, ref HttpValidationStatus validationStatus)
        {
            validationStatus = OnCacheAuthorization(new HttpContextWrapper(context));
        }

        protected override HttpValidationStatus OnCacheAuthorization(HttpContextBase httpContext)
        {
            return AuthorizeCore(httpContext) ? HttpValidationStatus.Valid : HttpValidationStatus.Invalid;
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (!AuthorizeCore(filterContext.HttpContext))
            {
                filterContext.Result = Configuration.OnAuthorizationError(filterContext.RequestContext.HttpContext.Request, authorization);
                return;
            }

            HttpCachePolicyBase cachePolicy = filterContext.HttpContext.Response.Cache;
            cachePolicy.SetProxyMaxAge(new TimeSpan(0));
            cachePolicy.AddValidationCallback(CacheValidateHandler, null);
        }
    }
    #endregion

    #region UnLogged Attribute
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AnonymAttribute : ActionFilterAttribute
    {
        private Authorization authorization = Authorization.Unlogged;

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Configuration.OnAuthorization == null)
                return;

            authorization = Configuration.OnAuthorization(filterContext.HttpContext, string.Empty, string.Empty);

            if (authorization != Authorization.Unlogged)
            {
                filterContext.Result = Configuration.OnAuthorizationError(filterContext.RequestContext.HttpContext.Request, authorization);
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
    #endregion

    #region Proxy Attribute
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ProxyAttribute : ActionFilterAttribute
    {
        public int HttpStatus { get; set; }
        public string Key { get; set; }
        public string Method { get; set; }

        public ProxyAttribute()
        {
            HttpStatus = 403;
        }

        public ProxyAttribute(string key, string method = "POST", int httpStatus = 434)
        {
            Key = "Library." + key.Hash("sha256");
            Method = "POST";
            HttpStatus = httpStatus;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;

            if (request.HttpMethod != Method || request.Headers["X-Proxy"] != Key)
                filterContext.Result = new HttpStatusCodeResult(HttpStatus);

            base.OnActionExecuting(filterContext);
        }
    }
    #endregion

    #region Form Attribute
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class FormAttribute : ActionFilterAttribute
    {
        public bool HostValid { get; set; }
        public bool Json { get; set; }
        public int HttpStatus { get; set; }

        public string Parameter { get; set; }
        public Type ParameterType { get; set; }

        public FormAttribute()
        {
            HostValid = true;
            HttpStatus = 403;
            Json = true;
        }

        public FormAttribute(Type type, string parameter = "model", bool hostValid = true, int httpStatus = 401, bool json = true)
        {
            ParameterType = type;
            Parameter = parameter;
            HostValid = hostValid;
            HttpStatus = httpStatus;
            Json = json;
        }

        public FormAttribute(bool hostValid = true, int httpStatus = 401, bool json = true)
        {
            HostValid = hostValid;
            HttpStatus = httpStatus;
            Json = json;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;

            if (!request.IsAjaxRequest()) {
                filterContext.Result = new HttpStatusCodeResult(HttpStatus);
                base.OnActionExecuting(filterContext);
                return;
            }

            if (HostValid)
            {
                var referrer = filterContext.HttpContext.Request.UrlReferrer;
                if (referrer == null || referrer.Host != filterContext.HttpContext.Request.Url.Host)
                {
                    filterContext.Result = new HttpStatusCodeResult(HttpStatus);
                    base.OnActionExecuting(filterContext);
                    return;
                }
            }

            if (Json)
            {
                if (request.ContentType.StartsWith("application/json", StringComparison.InvariantCultureIgnoreCase) == false)
                {
                    filterContext.Result = new HttpStatusCodeResult(HttpStatus);
                    base.OnActionExecuting(filterContext);
                    return;
                }

                if (!string.IsNullOrEmpty(Parameter))
                {
                    string inputContent;
                    using (var sr = new StreamReader(filterContext.HttpContext.Request.InputStream))
                        inputContent = sr.ReadToEnd();

                    var value = Configuration.JsonProvider.DeserializeObject(inputContent, ParameterType);
                    filterContext.ActionParameters[Parameter] = value;
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
    #endregion

    // FOR CONTROLLERS

    #region Analylitcs
    public class AnalyticsAttribute : ActionFilterAttribute, IActionFilter
    {
        public bool AllowXhr { get; set; }

        public AnalyticsAttribute(bool allowXhr = false)
        {
            AllowXhr = allowXhr;
        }

        void IActionFilter.OnActionExecuting(ActionExecutingContext filterContext)
        {
            Configuration.Analytics.Request(filterContext.HttpContext, AllowXhr);
            OnActionExecuting(filterContext);
        }
    }
    #endregion

    #region Stopwatch
    public class StopwatchAttribute : ActionFilterAttribute, IResultFilter
    {
        private DateTime start;

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Configuration.AllowStopwatch)
                start = DateTime.Now;
            base.OnActionExecuting(filterContext);
        }

        void IResultFilter.OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (Configuration.AllowStopwatch)
                Configuration.InvokeStopwatch(filterContext.Controller.GetType().Name, DateTime.Now - start, filterContext.RequestContext.HttpContext.Request);
            base.OnResultExecuted(filterContext);
        }

    }
    #endregion

}