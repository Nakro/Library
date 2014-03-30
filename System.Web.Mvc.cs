using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using System.Text;

using Library;

namespace System.Web.Mvc
{
    // ===============================================================================================
    // Autor        : Peter Širka
    // Created      : 14. 09. 2011
    // Updated      : 28. 02. 2014
    // Description  : MVC Custom Extension
    // ===============================================================================================

    #region Custom helpers
    public static class CustomHelpersExtension
    {
        private const string HEAD = "$head";

        public static string Title(this HtmlHelper source, string value = null)
        {
            if (value == null)
                return source.ViewData["Title"] as string;

            source.ViewData["Title"] = value;
            return "";
        }

        public static string Description(this HtmlHelper source, string value = null)
        {
            if (value == null)
                return source.ViewData["Description"] as string;

            source.ViewData["Description"] = value;
            return "";
        }

        public static string Keywords(this HtmlHelper source, string value = null)
        {
            if (value == null)
                return source.ViewData["Keywords"] as string;

            source.ViewData["Keywords"] = value;
            return "";
        }

        public static string Social(this HtmlHelper source, string value = null)
        {
            if (value == null)
                return source.ViewData["social"] as string;

            var url = Library.Configuration.Url.Image;

            if (!(url.StartsWith("//", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase)))
            {
                var uri = source.ViewContext.HttpContext.Request.Url;
                url = string.Format("{0}://{1}/{2}/", uri.Scheme, uri.Host, url);
            }

            source.ViewData["Social"] = url + Library.Configuration.OnVersion(value);
            return "";
        }

        public static HtmlString RenderMeta(this HtmlHelper source, string plus = "")
        {
            var output = "";

            var value = source.ViewData["Title"] as string;

            if (!string.IsNullOrEmpty(value))
                output += string.Format("<title>{0}</title>" + Environment.NewLine, value.HtmlEncode() + plus.HtmlDecode());

            value = source.ViewData["Description"] as string;

            if (!string.IsNullOrEmpty(value))
                output += string.Format("<meta name=\"description\" content=\"{0}\" />" + Environment.NewLine, value.HtmlEncode());

            value = source.ViewData["Keywords"] as string;

            if (!string.IsNullOrEmpty(value))
                output += string.Format("<meta name=\"keywords\" content=\"{0}\" />" + Environment.NewLine, value.HtmlEncode());

            return new HtmlString(output.Trim());
        }

        public static HtmlString RenderHead(this HtmlHelper source)
        {
            var value = source.ViewData["$HEAD"] as string;

            if (value == null)
                value = "";

            value = "<meta name=\"author\" content=\"{0}\" />".format(Utils.Config("author")) + value;
            return new HtmlString(value);
        }

        public static HtmlString Raw(this string source, bool allowTags = true)
        {
            if (!allowTags)
                return new HtmlString(source.HtmlEncode());

            return new HtmlString(source);
        }

        public static void Dns(this HtmlHelper source, params string[] url)
        {
            var str = "";

            foreach (var m in url)
                str += "<link rel=\"dns-prefetch\" href=\"{0}\" />".format(m);

            Head(source, str);
        }

        public static void Head(this HtmlHelper source, string value)
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
                    value = "<script type=\"text/javascript\" src=\"{0}\"></script>".format((isOut ? String.Empty : Library.Configuration.Url.JS) + value);
                    break;
                case ".css":
                    value = "<link type=\"text/css\" rel=\"stylesheet\" href=\"{0}\" />".format((isOut ? String.Empty : Library.Configuration.Url.CSS) + value);
                    break;
            }

            source.ViewData[key] = content + value;
        }

        public static void Prev(this HtmlHelper source, string url)
        {
            Head(source, "<link rel=\"prev\" href=\"{0}\" />".format(url));
        }

        public static void Next(this HtmlHelper source, string url)
        {
            Head(source, "<link rel=\"next\" href=\"{0}\" />".format(url));
        }

        public static void Canonical(this HtmlHelper source, string url)
        {
            Head(source, "<link rel=\"canonical\" href=\"{0}\" />".format(url));
        }

        public static void Prefetch(this HtmlHelper source, params string[] url)
        {
            var str = "";

            foreach (var m in url)
                str += "<link rel=\"prefetch\" href=\"{0}\" />".format(m);

            Head(source, str);
        }

        public static void Prerender(this HtmlHelper source, params string[] url)
        {
            var str = "";

            foreach (var m in url)
                str += "<link rel=\"prerender\" href=\"{0}\" />".format(m);

            Head(source, str);
        }

        public static HtmlString Resource(this HtmlHelper htmlHelper, string name, string language)
        {
            return new HtmlString(Utils.Resource(name, language));
        }

        public static HtmlString If(this HtmlHelper htmlHelper, bool condition, object ifTrue, object ifFalse = null)
        {
            var value = condition ? ifTrue as string : ifFalse as string;
            return new HtmlString(value == null ? "" : value);
        }

        public static MvcHtmlString CheckBoxClassicFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<Func<TModel, TProperty>> expression, object htmlAttributes = null)
        {
            var member = (expression.Body as MemberExpression);
            if (member == null)
                throw new InvalidCastException();

            var value = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData).Model;

            if (value != null)
            {
                if (value.GetType() != ConfigurationCache.type_bool)
                    throw new InvalidCastException();
            }

            var sb = new System.Text.StringBuilder();
            sb.Append("<input type=\"checkbox\"");

            var name = member.Member.Name;
            var id = false;

            if (htmlAttributes != null)
            {
                foreach (var a in htmlAttributes.GetType().GetProperties())
                {
                    var attName = a.Name.ToLower();

                    if (attName == "name" || attName == "value")
                        continue;

                    if (attName == "id")
                        id = true;

                    sb.Append(string.Format(" {0}=\"{1}\"", attName, a.GetValue(htmlAttributes, null)));
                }
            }

            if (!id)
                sb.Append(string.Format(" id=\"{0}\"", name));

            sb.Append(string.Format(" name=\"{0}\" value=\"true\"", name));

            if (value != null && (bool)value)
                sb.Append(" checked=\"checked\"");

            sb.Append(" />");
            return new MvcHtmlString(sb.ToString());
        }

        public static HtmlString Options<T>(this HtmlHelper htmlHelper, IList<T> list, string text = "Key", string value = "Value", object selected = null)
        {
            var sb = new System.Text.StringBuilder();

            var sel = "";

            if (selected != null)
                sel = selected.ToString();

            foreach (var m in list)
            {
                if (string.IsNullOrEmpty(value))
                    value = text;

                var propName = m.GetType().GetProperty(text);
                var propValue = m.GetType().GetProperty(value);
                var val = propValue.GetValue(m, null).ToString();
                sb.AppendFormat(string.Format("<option value=\"{0}\"{2}>{1}</option>", val, propName.GetValue(m, null), val == sel ? " selected=\"selected\"" : ""));
            }

            return new HtmlString(sb.ToString());
        }

        public static HtmlString Json(this HtmlHelper htmlHelper, object model, string id = "")
        {
            if (id.IsEmpty())
                return new HtmlString(model.JsonSerialize());
            return new HtmlString("<script type=\"application/json\" id=\"{1}\">{0}</script>".format(model.JsonSerialize(), id));
        }

        public static HtmlString InputCheckbox(this HtmlHelper htmlHelper, string name, string label, bool required = false, bool disabled = false)
        {
            return Input(htmlHelper, "checkbox", name, "", 0, required, disabled, true, label);
        }

        public static HtmlString InputText(this HtmlHelper htmlHelper, string name, string className = "", int maxLength = 50, bool required = false, bool disabled = false, bool autocomplete = true)
        {
            return Input(htmlHelper, "text", name, className, maxLength, required, disabled);
        }

        public static HtmlString InputPassword(this HtmlHelper htmlHelper, string name, string className = "", int maxLength = 50, bool required = false, bool disabled = false, bool autocomplete = true)
        {
            return Input(htmlHelper, "password", name, className, maxLength, required, disabled);
        }

        public static HtmlString InputHidden(this HtmlHelper htmlHelper, string name)
        {
            return Input(htmlHelper, "hidden", name);
        }

        public static HtmlString Input(this HtmlHelper htmlHelper, string type, string name, string className = "", int maxLength = 0, bool required = false, bool disabled = false, bool autocomplete = true, string label = "")
        {
            var sb = new System.Text.StringBuilder();
            var format = string.IsNullOrEmpty(label) ? "{0} />" : string.Format("<label>{0} /><span>{1}</span></label>", "{0}", label);

            sb.Append("<input");
            sb.AppendAttribute("type", type);

            sb.AppendAttribute("name", name);
            sb.AppendAttribute("id", name);

            if (!string.IsNullOrEmpty(className))
                sb.AppendAttribute("class", className);

            if (maxLength > 0)
                sb.AppendAttribute("maxlength", maxLength);

            if (required)
                sb.AppendAttribute("required", "required");

            if (disabled)
                sb.AppendAttribute("disabled", "disabled");

            if (!autocomplete)
                sb.AppendAttribute("autocomplete", "off");

            var model = htmlHelper.ViewData.Model;

            if (model == null)
                return new HtmlString(string.Format(format, sb.ToString()));

            var property = model.GetType().GetProperty(name);

            if (property == null)
                return new HtmlString(string.Format(format, sb.ToString()));

            var value = property.GetValue(model, null);
            var isChecked = false;

            if (property.PropertyType == ConfigurationCache.type_bool)
            {
                isChecked = (bool)value;
                sb.AppendAttribute("value", "true");
            }
            else
                sb.AppendAttribute("value", value);

            if (isChecked)
                sb.AppendAttribute("checked", "checked");

            return new HtmlString(string.Format(format, sb.ToString()));
        }

        public static HtmlString Textarea(this HtmlHelper htmlHelper, string name, string className = "", bool required = false, bool disabled = false, int maxLength = 0, bool wrap = true)
        {
            var sb = new System.Text.StringBuilder();
            var format = "{0}>{1}</textarea>";

            sb.Append("<textarea");

            sb.AppendAttribute("name", name);
            sb.AppendAttribute("id", name);

            if (!string.IsNullOrEmpty(className))
                sb.AppendAttribute("class", className);

            if (maxLength > 0)
                sb.AppendAttribute("maxlength", maxLength);

            if (required)
                sb.AppendAttribute("required", "required");

            if (disabled)
                sb.AppendAttribute("disabled", "disabled");

            if (!wrap)
                sb.AppendAttribute("wrap", "off");

            var model = htmlHelper.ViewData.Model;

            if (model == null)
                return new HtmlString(string.Format(format, sb.ToString(), ""));

            var property = model.GetType().GetProperty(name);

            if (property == null)
                return new HtmlString(string.Format(format, sb.ToString(), ""));

            var value = property.GetValue(model, null);
            return new HtmlString(string.Format(format, sb.ToString(), value == null ? "" : value.ToString().HtmlEncode()));
        }

        public static MvcHtmlString CheckBoxClassic(this HtmlHelper htmlHelper, string name, bool isChecked, object htmlAttributes = null)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<input type=\"checkbox\"");

            var id = false;

            if (htmlAttributes != null)
            {
                foreach (var a in htmlAttributes.GetType().GetProperties())
                {
                    var attName = a.Name.ToLower();

                    if (attName == "name" || attName == "value")
                        continue;

                    if (attName == "id")
                        id = true;

                    sb.Append(string.Format(" {0}=\"{1}\"", attName, a.GetValue(htmlAttributes, null)));
                }
            }

            if (!id)
                sb.Append(string.Format(" id=\"{0}\"", name));

            sb.Append(string.Format(" name=\"{0}\" value=\"true\"", name));

            if (isChecked)
                sb.Append(" checked=\"checked\"");

            sb.Append(" />");
            return new MvcHtmlString(sb.ToString());
        }

        public static System.Web.WebPages.HelperResult Repeater<T>(this IEnumerable<T> items, Func<T, System.Web.WebPages.HelperResult> template)
        {
            return new System.Web.WebPages.HelperResult(writer =>
            {
                foreach (var item in items)
                    template(item).WriteTo(writer);
            });
        }

        public static HtmlString JsCompress<TModel>(this HtmlHelper<TModel> htmlHelper, Func<WebViewPage<TModel>, object> content, bool compress = true)
        {
            var value = content((WebViewPage<TModel>)htmlHelper.ViewDataContainer).ToString();
            return new HtmlString(compress ? value.JsCompress(System.Text.Encoding.UTF8) : value);
        }

        public static HtmlString CssCompress<TModel>(this HtmlHelper<TModel> htmlHelper, Func<WebViewPage<TModel>, object> content, bool compress = true)
        {
            var value = content((WebViewPage<TModel>)htmlHelper.ViewDataContainer).ToString();
            return new HtmlString(compress ? value.CssCompress(System.Text.Encoding.UTF8) : value);
        }

        public static HtmlString Template(this HtmlHelper source, string id, string content)
        {
            return new HtmlString(string.Format("<script type=\"text/html\" id=\"{0}\">{1}</script>", id, content));
        }

        public static MvcHtmlString Visible<TModel>(this HtmlHelper<TModel> htmlHelper, bool visible, Func<WebViewPage<TModel>, object> content)
        {
            return visible ? MvcHtmlString.Create(content((WebViewPage<TModel>)htmlHelper.ViewDataContainer).ToString()) : MvcHtmlString.Create("");
        }

        public static HtmlString Selected(this HtmlHelper source, bool isSelected = true)
        {
            return new HtmlString(isSelected ? " selected=\"selected\"" : "");
        }

        public static HtmlString Checked(this HtmlHelper source, bool isChecked = true)
        {
            return new HtmlString(isChecked ? " checked=\"checked\"" : "");
        }

        public static HtmlString Readonly(this HtmlHelper source, bool isChecked = true)
        {
            return new HtmlString(isChecked ? " readonly=\"readonly\"" : "");
        }

        public static HtmlString Disabled(this HtmlHelper source, bool isDisabled = true)
        {
            return new HtmlString(isDisabled ? " disabled=\"disabled\"" : "");
        }

        public static void Meta(this HtmlHelper source, string title, string description = null, string keywords = null, string image = null)
        {
            if (title.IsNotEmpty())
                Title(source, title);

            if (description.IsNotEmpty())
                Description(source, description);

            if (keywords.IsNotEmpty())
                Keywords(source, keywords);

            if (image.IsNotEmpty())
                Social(source, image);
        }

        public static HtmlString Css(this HtmlHelper source, string url = "default.css")
        {
            var format = "<link type=\"text/css\" rel=\"stylesheet\" href=\"{0}\" />";

            if (url.StartsWith("//", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                return new HtmlString(string.Format(format, url));

            return new HtmlString(string.Format(format, Library.Configuration.Url.CSS + Library.Configuration.OnVersion(url)));
        }

        public static HtmlString Js(this HtmlHelper source, string url = "default.js")
        {
            var format = "<script type=\"text/javascript\" src=\"{0}\"></script>";

            if (url.StartsWith("//", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                return new HtmlString(string.Format(format, url));

            return new HtmlString(string.Format(format, Library.Configuration.Url.JS + Library.Configuration.OnVersion(url)));
        }

        public static HtmlString Image(this HtmlHelper source, string url, int width = 0, int height = 0, string alt = "", string cls = "")
        {
            var sb = new System.Text.StringBuilder();

            sb.Append("<img");

            if (url.StartsWith("//", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                sb.AppendAttribute("src", url);
            else
                sb.AppendAttribute("src", Library.Configuration.Url.Image + Library.Configuration.OnVersion(url));

            sb.AppendAttribute("border", 0);

            if (width > 0)
                sb.AppendAttribute("width", width);

            if (height > 0)
                sb.AppendAttribute("height", height);

            sb.AppendAttribute("alt", alt);

            if (cls.IsNotEmpty())
                sb.AppendAttribute("class", cls);

            sb.Append(" />");

            return new HtmlString(sb.ToString());
        }

        public static HtmlString Favicon(this HtmlHelper source, string url = "favicon.ico")
        {
            var format = "<link rel=\"icon\" href=\"{0}\" type=\"image/{1}\" /><link rel=\"shortcut icon\" href=\"{0}\" type=\"image/{1}\" />";
            var type = "x-icon";

            if (url.LastIndexOf(".png") != -1)
                type = "png";

            if (url.StartsWith("//", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                return new HtmlString(string.Format(format, url, type));

            if (url[0] != '/')
                url = '/' + url;

            return new HtmlString(string.Format(format, url, type));
        }

        public static HtmlString Download(this HtmlHelper source, string name, string content, string downloadName = "", string className = "")
        {
            return new HtmlString("<a href=\"" + Library.Configuration.Url.Download + name + "\"" + (downloadName.IsNotEmpty() ? " download=\"" + downloadName + "\"" : "") + (className.IsNotEmpty() ? " class=\"" + className + "\"" : "") + ">" + content + "</a>");
        }

        public static void Sitemap(this UrlHelper source, string name, string url = "", int priority = -1)
        {
            Library.Sitemap.Add(name, url, priority);
        }

        public static string Host(this UrlHelper source, string path = "/")
        {
            return source.RequestContext.HttpContext.Request.Host(path);
        }

        public static string Url(this UrlHelper source, bool host = true)
        {
            return source.RequestContext.HttpContext.Request.Url(host);
        }

        public static string RouteStatic(this UrlHelper source, string url)
        {
            if (url.StartsWith("//", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                return url;
            return Library.Configuration.Url.Static + url;
        }

        public static string RouteDownload(this UrlHelper source, string url)
        {
            if (url.StartsWith("//", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                return url;
            return Library.Configuration.Url.Download + url;
        }

        public static string RouteImage(this UrlHelper source, string url)
        {
            if (url.StartsWith("//", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                return url;
            return Library.Configuration.Url.Image + url;
        }

        public static void Ng(this HtmlHelper source, string version, params string[] name)
        {
            if (name == null && name.Length == 0)
                name = new string[1] { "angular" };

            foreach (var m in name)
            {
                var filename = m.ToLower();

                if (filename != "angular" && filename.IndexOf("angular") == -1)
                    filename = "angular-" + filename;

                Head(source, "//ajax.googleapis.com/ajax/libs/angularjs/" + version + '/' + filename + ".min.js");
            }
        }

        public static void NgController(this HtmlHelper source, params string[] name)
        {
            if (name == null && name.Length == 0)
                return;

            foreach (var m in name)
            {
                var filename = m;
                if (filename.LastIndexOf(".js") == -1)
                    filename += ".js";

                Head(source, "/app/controllers/" + filename);
            }
        }

        public static void NgDirective(this HtmlHelper source, params string[] name)
        {
            if (name == null && name.Length == 0)
                return;

            foreach (var m in name)
            {
                var filename = m;
                if (filename.LastIndexOf(".js") == -1)
                    filename += ".js";

                Head(source, "/app/directives/" + filename);
            }
        }

        public static void NgService(this HtmlHelper source, params string[] name)
        {
            if (name == null && name.Length == 0)
                return;

            foreach (var m in name)
            {
                var filename = m;
                if (filename.LastIndexOf(".js") == -1)
                    filename += ".js";

                Head(source, "/app/services/" + filename);
            }
        }

        public static void NgFilter(this HtmlHelper source, params string[] name)
        {
            if (name == null && name.Length == 0)
                return;

            foreach (var m in name)
            {
                var filename = m;
                if (filename.LastIndexOf(".js") == -1)
                    filename += ".js";

                Head(source, "/app/filters/" + filename);
            }
        }

        public static void NgResource(this HtmlHelper source, params string[] name)
        {
            if (name == null && name.Length == 0)
                return;

            foreach (var m in name)
            {
                var filename = m;
                if (filename.LastIndexOf(".js") == -1)
                    filename += ".js";

                Head(source, "/app/resources/" + filename);
            }
        }

        public static HtmlString NgTemplate(this HtmlHelper source, string name, string id = "")
        {
            if (id == "")
                id = name;

            var tmp = "";
            var filename = string.Format("/app/templates/{0}.html", name);

            if (System.IO.File.Exists(filename))
                tmp = System.IO.File.ReadAllText(filename);

            return new HtmlString(string.Format("<script type=\"text/ng-template\" id=\"{0}\">{1}</script>", id, tmp));
        }
    }
    #endregion

}