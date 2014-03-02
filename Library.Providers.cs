using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

using Library;
using Library.Json;

namespace Library.Providers
{

    #region DefaultJsonSerializerDeserializer
    internal class DefaultJsonSerializerDeserializer : ILibraryJsonProvider
    {
        public virtual string Serialize(object value, params string[] withoutProperty)
        {
            return JsonSerializer.SerializeObject(value);
        }

        public virtual T DeserializeObject<T>(string value)
        {
            return JsonSerializer.DeserializeObject<T>(value);
        }

        public virtual dynamic DeserializeObject(string value)
        {
            return JsonSerializer.DeserializeObject(value);
        }

        public virtual object DeserializeObject(string value, Type type)
        {
            return JsonSerializer.DeserializeObject(value, type);
        }
    }
    #endregion

    #region DefaultCacheProvider
    internal class DefaultCacheProvider : ILibraryCacheProvider
    {
        public T Write<T>(string key, T value, DateTime expire)
        {
            var cache = HttpContext.Current.Cache;
            cache.Remove(key);
            cache.Add(key, value, null, expire, TimeSpan.Zero, System.Web.Caching.CacheItemPriority.Normal, null);
            return value;
        }

        public T Read<T>(string key, Func<string, T> onEmpty = null)
        {
            var value = HttpContext.Current.Cache[key];
            if (value != null)
                return (T)value;

            if (onEmpty != null)
                return onEmpty(key);

            return default(T);
        }

        public void Remove(string key)
        {
            HttpContext.Current.Cache.Remove(key);
        }

        public void Remove(Func<string, bool> predicate)
        {
            var cache = HttpContext.Current.Cache;
            foreach (System.Collections.DictionaryEntry item in cache)
            {
                var key = item.Key.ToString();
                if (predicate(key))
                    cache.Remove(key);
            }
        }
    }
    #endregion

    #region DefaultAnalyticsProvider
    public class DefaultAnalyticsProvider : ILibraryAnalyticsProvider
    {
        public void Write(Others.Analytics.Statistics stats)
        {
            System.IO.File.AppendAllText("analytics-stats.json".PathData(), stats.JsonSerialize() + "\n");
        }

        public void WriteState(Others.Analytics.Statistics stats)
        {
            System.IO.File.WriteAllText("analytics-state.json".PathData(), stats.JsonSerialize());
        }

        public Others.Analytics.Statistics LoadState()
        {
            var filename = "analytics-state.json".PathData();

            if (!System.IO.File.Exists(filename))
                return new Others.Analytics.Statistics();

            return System.IO.File.ReadAllText(filename).JsonDeserialize<Others.Analytics.Statistics>();
        }

        public IList<Others.Analytics.Statistics> Yearly()
        {
            var items = new List<Others.Analytics.Statistics>(12);
            var filename = "analytics-stats.json".PathData();

            if (!System.IO.File.Exists(filename))
                return items;

            var lines = System.IO.File.ReadAllText(filename).Split('\n');

            foreach (var line in lines)
            {

                if (line.IsEmpty())
                    continue;

                var stats = line.JsonDeserialize<Library.Others.Analytics.Statistics>();
                if (stats == null)
                    continue;

                var item = items.FirstOrDefault(n => n.Year == stats.Year);
                if (item == null)
                {
                    item = new Library.Others.Analytics.Statistics();
                    item.Year = stats.Year;
                    items.Add(item);
                }

                item.Advert += stats.Advert;
                item.Count += stats.Count;
                item.Desktop += stats.Desktop;
                item.Direct += stats.Direct;
                item.Hits += stats.Hits;
                item.Mobile += stats.Mobile;
                item.Search += stats.Search;
                item.Social += stats.Social;
                item.Unique += stats.Unique;
                item.Unknown += stats.Unknown;
            }

            return items;
        }

        public IList<Others.Analytics.Statistics> Monthly(int year)
        {
            var items = new List<Others.Analytics.Statistics>(12);
            var filename = "analytics-stats.json".PathData();

            if (!System.IO.File.Exists(filename))
                return items;

            var lines = System.IO.File.ReadAllText(filename).Split('\n');

            foreach (var line in lines)
            {

                if (line.IsEmpty())
                    continue;

                var stats = line.JsonDeserialize<Others.Analytics.Statistics>();
                if (stats == null)
                    continue;

                if (stats.Year != year)
                    continue;

                var item = items.FirstOrDefault(n => n.Year == year && n.Month == stats.Month);
                if (item == null)
                {
                    item = new Library.Others.Analytics.Statistics();
                    item.Year = year;
                    item.Month = stats.Month;
                    items.Add(item);
                }

                item.Advert += stats.Advert;
                item.Count += stats.Count;
                item.Desktop += stats.Desktop;
                item.Direct += stats.Direct;
                item.Hits += stats.Hits;
                item.Mobile += stats.Mobile;
                item.Search += stats.Search;
                item.Social += stats.Social;
                item.Unique += stats.Unique;
                item.Unknown += stats.Unknown;
            }

            return items;
        }

        public IList<Others.Analytics.Statistics> Daily(int year, int month)
        {
            var items = new List<Others.Analytics.Statistics>(12);
            var filename = "analytics-stats.json".PathData();

            if (!System.IO.File.Exists(filename))
                return items;

            var lines = System.IO.File.ReadAllText(filename).Split('\n');

            foreach (var line in lines)
            {

                if (line.IsEmpty())
                    continue;

                var stats = line.JsonDeserialize<Library.Others.Analytics.Statistics>();
                if (stats == null)
                    continue;

                if (stats.Year != year && stats.Month != month)
                    continue;

                var item = items.FirstOrDefault(n => n.Year == year && n.Month == month && n.Day == stats.Day);
                if (item == null)
                {
                    item = new Library.Others.Analytics.Statistics();
                    item.Year = year;
                    item.Month = stats.Month;
                    item.Day = stats.Day;
                    items.Add(item);
                }

                item.Advert += stats.Advert;
                item.Count += stats.Count;
                item.Desktop += stats.Desktop;
                item.Direct += stats.Direct;
                item.Hits += stats.Hits;
                item.Mobile += stats.Mobile;
                item.Search += stats.Search;
                item.Social += stats.Social;
                item.Unique += stats.Unique;
                item.Unknown += stats.Unknown;
            }

            return items;
        }
    }
    #endregion

}
