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
        public override bool IsValid(object propertyValue)
        {
            var str = propertyValue as string;
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
            this.IsDecimal = isDecimal;
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
            this.HostValid = true;
        }

        public XhrAttribute(bool hostValid)
        {
            this.HostValid = hostValid;
        }

        public XhrAttribute(bool hostValid, int httpStatus)
        {
            this.HostValid = hostValid;
            this.HttpStatus = httpStatus;
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
            this.Name = name;
            this.Value = value;
        }

        public HeaderAttribute(string name, string value, int httpStatus)
        {
            this.Name = name;
            this.Value = value;
            this.HttpStatus = httpStatus;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (HttpStatus == 0)
                HttpStatus = 403;

            var value = filterContext.HttpContext.Request.Headers[this.Name];
            if (value != this.Value)
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
        private Authorization authorization = Authorization.unlogged;
        public UnLoggedAttribute() { }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Configuration.OnAuthorization == null)
                return;

            this.authorization = Configuration.OnAuthorization(filterContext.HttpContext, string.Empty, string.Empty);

            if (this.authorization != Authorization.unlogged)
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
        private Authorization authorization = Authorization.unlogged;

        public LoggedAttribute() { }

        public string ErrorMessage { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (Configuration.OnAuthorization == null)
                return false;

            authorization = Configuration.OnAuthorization(httpContext, this.Roles, this.Users);
            return authorization == Authorization.logged;
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
                filterContext.Result = Configuration.OnAuthorizationError(filterContext.RequestContext.HttpContext.Request, this.authorization);
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
        private Authorization authorization = Authorization.unlogged;

        public UserAttribute() { }

        public string ErrorMessage { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (Configuration.OnAuthorization == null)
                return false;

            authorization = Configuration.OnAuthorization(httpContext, this.Roles, this.Users);
            return authorization == Authorization.logged;
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
                filterContext.Result = Configuration.OnAuthorizationError(filterContext.RequestContext.HttpContext.Request, this.authorization);
                return;
            }

            HttpCachePolicyBase cachePolicy = filterContext.HttpContext.Response.Cache;
            cachePolicy.SetProxyMaxAge(new TimeSpan(0));
            cachePolicy.AddValidationCallback(CacheValidateHandler, null);
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
            this.Key = "Library." + key.Hash("sha256");
            this.Method = "POST";
            this.HttpStatus = httpStatus;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;

            if (request.HttpMethod != this.Method || request.Headers["X-Proxy"] != this.Key)
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

        public FormAttribute()
        {
            this.HostValid = true;
            this.HttpStatus = 403;
            this.Json = true;
        }

        public FormAttribute(bool hostValid = true, int httpStatus = 401, bool Json = true)
        {
            this.HostValid = hostValid;
            this.HttpStatus = httpStatus;
            this.Json = Json;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;

            if (request.IsAjaxRequest() == false)
            {
                filterContext.Result = new HttpStatusCodeResult(HttpStatus);
                base.OnActionExecuting(filterContext);
                return;
            }

            if (Json)
            {
                if (request.ContentType.StartsWith("application/json") == false)
                {
                    filterContext.Result = new HttpStatusCodeResult(HttpStatus);
                    base.OnActionExecuting(filterContext);
                    return;
                }
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

            base.OnActionExecuting(filterContext);
        }
    }
    #endregion

    // FOR CONTROLLERS

    #region Analylitcs
    public class Analytics : ActionFilterAttribute, IActionFilter
    {
        public bool AllowXhr { get; set; }

        public Analytics(bool allowXhr = false)
        {
            AllowXhr = allowXhr;
        }

        void IActionFilter.OnActionExecuting(ActionExecutingContext filterContext)
        {
            Configuration.Analytics.Request(filterContext.HttpContext, AllowXhr);
            this.OnActionExecuting(filterContext);
        }
    }
    #endregion

}