using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace Library.Others
{

    // ===============================================================================================
    // 
    // Library.[Analytics]
    // 
    // ===============================================================================================

    // ===============================================================================================
    // Autor        : Peter Širka
    // Created      : 04. 05. 2008
    // Updated      : 01. 02. 2014
    // Description  : Online stats
    // ===============================================================================================

    #region Analytics
    public class Analytics
    {
        private const string COOKIE = "__po";

        public class Statistics
        {
            public int Day { get; set; }
            public int Month { get; set; }
            public int Year { get; set; }

            public int Hits { get; set; }
            public int Unique { get; set; }
            public int Count { get; set; }

            public int Search { get; set; }
            public int Direct { get; set; }
            public int Social { get; set; }
            public int Unknown { get; set; }
            public int Advert { get; set; }

            public int Mobile { get; set; }
            public int Desktop { get; set; }
        }

        public class OnlineIp
        {
            public string Ip { get; set; }
            public string Url { get; set; }
            public string Browser { get; set; }

            public OnlineIp(string ip, string url, string browser)
            {
                Ip = ip;
                Url = url;
                Browser = browser;
            }
        }

        public int Online
        {
            get { return Generation[0] + Generation[1]; }
        }

        public DateTime LastVisit { get; set; }
        public Statistics Stats { get; set; }
        public Func<HttpRequestBase, bool> OnValidation { get; set; }

        private long Ticks { get; set; }
        private int Last { get; set; }
        private int Interval { get; set; }

        private int[] Generation = new int[2] { 0, 0 };

        private Regex reg_robot = new Regex("bot|crawler", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private Regex reg_mobile = new Regex("Android|webOS|iPhone|iPad|iPod|BlackBerry|Windows.?Phone", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private List<string> ListSocial = new List<string>(8);
        private List<string> ListSearch = new List<string>(3);

        private System.Timers.Timer Timer = null;

        public Func<HttpRequest, bool> OnValid { get; set; }
        public List<OnlineIp> Ip { get; set; }

        public Analytics()
        {
            ListSocial.AddRange(new string[] { "plus.url.google", "plus.google", "twitter", "facebook", "linkedin", "tumblr", "flickr", "instagram" });
            ListSearch.AddRange(new string[] { "google", "bing", "yahoo" });
            Ip = new List<OnlineIp>(10);
            Timer = new System.Timers.Timer(30000);
            Timer.Enabled = true;
            Timer.Elapsed += new System.Timers.ElapsedEventHandler(Service);
            Stats = new Statistics();
            Load();
        }

        public void Request(HttpContextBase context, bool allowXhr)
        {
            var req = context.Request;
            var res = context.Response;

            if (req.HttpMethod != "GET")
                return;

            if (req.UserLanguages == null || req.UserLanguages.Length == 0)
                return;

            if (req.AcceptTypes == null || req.AcceptTypes.Length == 0)
                return;

            var ua = req.UserAgent;

            if (ua.IsEmpty())
                return;

            if (req.Headers["X-moz"] == "prefetch")
                return;

            if (reg_robot.IsMatch(ua))
                return;

            if (req.Headers["X-Requested-With"] == "XMLHttpRequest")
                return;

            if (OnValidation != null && !OnValidation(req))
                return;

            long user = 0;

            var cookie = req.Cookies[COOKIE];
            var now = DateTime.Now;
            var ticks = now.Ticks;

            if (cookie != null)
                user = cookie.Value.To<long>();

            var sum = user == 0 ? 1000 : (ticks - user) / TimeSpan.TicksPerSecond;
            var exists = sum < 31;

            var referer = (req.UrlReferrer == null ? "" : req.UrlReferrer.Host.Empty(""));

            Stats.Hits++;
            Change(req);

            if (exists)
                return;

            var isUnique = false;

            if (user > 0)
            {
                sum = Math.Abs(this.Ticks - user) / TimeSpan.TicksPerSecond;
                if (sum < 41)
                    return;

                var date = new DateTime(user);
                if (date.Day != now.Day && date.Month != now.Month && date.Year != now.Year)
                    isUnique = true;
            }
            else
                isUnique = true;

            if (isUnique)
            {
                Stats.Unique++;
                if (reg_mobile.IsMatch(ua))
                    Stats.Mobile++;
                else
                    Stats.Desktop++;
            }

            Generation[1]++;
            res.Cookies.Set(new HttpCookie(COOKIE, ticks.ToString()) { Expires = now.AddDays(5) });

            Ip.Add(new OnlineIp(req.UserHostAddress, req.RawUrl, req.Browser.Browser));

            var online = Online;

            if (Last != online)
            {
                if (Last > 0)
                {
                    var count = Last - Online;
                    if (count > 0)
                        Ip = Ip.Skip(count).ToList();
                }
                Last = online;
            }

            Stats.Count++;

            if (req.QueryString["utm_medium"].IsNotEmpty() || req.QueryString["utm_source"].IsNotEmpty())
            {
                Stats.Advert++;
                return;
            }

            if (referer.IsEmpty())
            {
                Stats.Direct++;
                return;
            }

            for (var i = 0; i < ListSocial.Count; i++)
            {
                if (referer.Contains(ListSocial[i]))
                {
                    Stats.Social++;
                    return;
                }
            }

            for (var i = 0; i < ListSearch.Count; i++)
            {
                if (referer.Contains(ListSearch[i]))
                {
                    Stats.Search++;
                    return;
                }
            }

            Stats.Unknown++;
        }

        private void Service(object sender, System.Timers.ElapsedEventArgs e)
        {
            Interval++;
            try
            {
                var now = DateTime.Now;
                Ticks = now.Ticks;

                if (Stats.Day != now.Day && Stats.Month != now.Month && Stats.Year != now.Year)
                {
                    if (Stats.Day != 0)
                    {
                        Append();
                        Stats.Advert = 0;
                        Stats.Count = 0;
                        Stats.Desktop = 0;
                        Stats.Direct = 0;
                        Stats.Hits = 0;
                        Stats.Mobile = 0;
                        Stats.Search = 0;
                        Stats.Social = 0;
                        Stats.Unique = 0;
                        Stats.Unknown = 0;
                    }

                    Stats.Day = now.Day;
                    Stats.Month = now.Month;
                    Stats.Year = now.Year;
                    Save();
                }
                else if (Interval % 2 == 0)
                    Save();

                var tmp0 = Generation[0];
                var tmp1 = Generation[1];

                Generation[1] = 0;
                Generation[0] = tmp1;

                if (tmp0 != Generation[0] || tmp1 != Generation[1])
                {
                    var online = Generation[0] + Generation[1];
                    if (online != Last)
                    {
                        if (tmp0 > 0)
                            Ip = Ip.Skip(tmp0).ToList();
                        Last = online;
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.Error("Analytics.Service", ex, null);
            }
        }

        private void Append()
        {
            var stats = Stats;
            if (stats != null)
                Configuration.AnalyticsProvider.Write(stats);
        }

        private void Save()
        {
            var stats = Stats;
            if (stats != null)
                Configuration.AnalyticsProvider.WriteState(stats);
        }

        private void Load()
        {
            var stats = Configuration.AnalyticsProvider.LoadState();
            if (stats == null)
                stats = new Statistics();

            Stats = stats;
        }

        private void Change(HttpRequestBase req)
        {
            var referer = (req.UrlReferrer == null ? "" : req.UrlReferrer.ToString());
            if (referer.IsEmpty())
                return;

            var browser = req.Browser.Browser;
            var ip = req.UserHostAddress;

            var item = Ip.FirstOrDefault(n => n.Ip == ip && n.Url == referer && n.Browser == browser);
            if (item == null)
                return;

            item.Url = req.Url.ToString();
        }

        public IList<Statistics> Yearly()
        {
            return Configuration.AnalyticsProvider.Yearly();
        }

        public IList<Statistics> Monthly(int year)
        {
            return Configuration.AnalyticsProvider.Monthly(year);
        }

        public IList<Statistics> Daily(int year, int month)
        {
            return Configuration.AnalyticsProvider.Daily(year, month);
        }
    }
    #endregion

}
