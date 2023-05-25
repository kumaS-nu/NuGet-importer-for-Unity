using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEditor;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor
{
    /// <summary>
    /// <para>Class for using NuGet API.</para>
    /// <para>NuGetのAPIを使うためのクラス。</para>
    /// </summary>
    public static class NuGet
    {
        private static readonly List<string> SearchQueryService = new List<string> { "https://azuresearch-usnc.nuget.org/query" };
        private static string _packageBaseAddress = "https://api.nuget.org/v3-flatcontainer/";
        private static string _registrationsBaseUrl = "https://api.nuget.org/v3/registration5-gz-semver2/";
        private static HttpClient _client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
        private static readonly Dictionary<string, (long packageSize, FileStream downloaded)> Downloading = new Dictionary<string, (long packageSize, FileStream downloaded)>();

        private static readonly List<Task> TimeoutSet = new List<Task>();
        private static readonly Stack<TimeSpan> TimeoutStack = new Stack<TimeSpan>();
        private static readonly Dictionary<Guid, Task> WorkingTask = new Dictionary<Guid, Task>();

        private static readonly Dictionary<string, SearchResult> SearchCache = new Dictionary<string, SearchResult>();
        private static readonly List<string> SearchLog = new List<string>();
        private static readonly Dictionary<string, Task> GettingSearches = new Dictionary<string, Task>();

        /// <value>
        /// <para>For test.</para>
        /// </value>
        internal static readonly Dictionary<string, Catalog> CatalogCache = new Dictionary<string, Catalog>();
        private static readonly List<string> CatalogLog = new List<string>();
        private static readonly Dictionary<string, Task<string>> GettingCatalogs = new Dictionary<string, Task<string>>();

        /// <summary>
        /// <para>Initialize the API endpoint.</para>
        /// <para>APIのエンドポイントを初期化する。</para>
        /// </summary>
        /// <returns>
        /// <c>Task</c>
        /// </returns>
        [InitializeOnLoadMethod]
        public static async void InitializeAPIEndPoint()
        {
            _client.Timeout = TimeSpan.FromSeconds(NuGetImporterSettings.Instance.Timeout);
            var responseText = await _client.GetStringAsync("https://api.nuget.org/v3/index.json");
            APIList apiList = JsonUtility.FromJson<APIList>(RefineJson(responseText));
            var searchQueryServices = new List<string>();
            foreach (Resource apiInfo in apiList.resources)
            {
                switch (apiInfo.nuget_type)
                {
                    case "SearchQueryService":
                        if (apiInfo.comment.Contains("primary"))
                        {
                            searchQueryServices.Insert(0, apiInfo.nuget_id);
                        }
                        else
                        {
                            searchQueryServices.Add(apiInfo.nuget_id);
                        }
                        break;
                    case "PackageBaseAddress/3.0.0":
                        _packageBaseAddress = apiInfo.nuget_id;
                        break;
                    case "RegistrationsBaseUrl/3.6.0":
                        _registrationsBaseUrl = apiInfo.nuget_id;
                        break;
                }
            }
        }

        /// <summary>
        /// <para>Delete the cache of search and catalog.</para>
        /// <para>カタログと検索結果のキャッシュを削除する。</para>
        /// </summary>
        public static void DeleteCache()
        {
            lock (SearchCache)
            {
                SearchCache.Clear();
                SearchLog.Clear();
                GettingSearches.Clear();
            }

            lock (CatalogCache)
            {
                CatalogCache.Clear();
                CatalogLog.Clear();
                GettingCatalogs.Clear();
            }
        }

        /// <summary>
        /// <para>Set Timeout.</para>
        /// <para>タイムアウト時間を再設定。</para>
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static async Task SetTimeout(TimeSpan timeout)
        {
            lock (TimeoutStack)
            {
                if (TimeoutStack.Any())
                {
                    TimeoutStack.Push(timeout);
                    return;
                }
                TimeoutStack.Push(timeout);
            }
            Task task = SetWebClientTasks();
            TimeoutSet.Add(task);
            await task;
            TimeoutSet.Clear();
        }

        private static async Task SetWebClientTasks()
        {
            await Task.WhenAll(WorkingTask.Values.ToArray());
            _client.Dispose();
            _client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
            lock (TimeoutStack)
            {
                _client.Timeout = TimeoutStack.Pop();
                TimeoutStack.Clear();
            }
        }

        /// <summary>
        /// <para>Search for packages.</para>
        /// <para>パッケージを検索する。</para>
        /// </summary>
        /// <param name="q">
        /// <para>Search word.</para>
        /// <para>検索語句。</para>
        /// </param>
        /// <param name="skip">
        /// <para>Number of packages to skip.</para>
        /// <para>スキップするパッケージの数。</para>
        /// </param>
        /// <param name="take">
        /// <para>Number of packages to take.</para>
        /// <para>取得するパッケージの数。</para>
        /// </param>
        /// <param name="prerelease">
        /// <para>Whether include prerelease.</para>
        /// <para>プリリリースを含めるか。</para>
        /// </param>
        /// <returns>
        /// <para>Search result.</para>
        /// <para>検索結果。</para>
        /// </returns>
        public static async Task<SearchResult> SearchPackage(string q = "", int skip = -1, int take = -1, bool prerelease = false)
        {
            var query = "";
            void Concat()
            {
                query += query == "" ? "?" : "&";
            }

            if (q != "")
            {
                Concat();
                query += "q=" + q;
            }
            if (skip > 0)
            {
                Concat();
                query += "skip=" + skip;
            }
            if (take > 0)
            {
                Concat();
                query += "take=" + take;
            }
            Concat();
            query += "semVerLevel=2.0.0";
            if (prerelease)
            {
                Concat();
                query += "prerelease=true";
            }

            // The below code is the cache process.
            lock (SearchCache)
            {
                if (SearchCache.TryGetValue(query, out SearchResult package))
                {
                    SearchLog.Remove(query);
                    SearchLog.Add(query);
                    return package;
                }
            }
            if (TimeoutSet.Any())
            {
                await Task.WhenAll(TimeoutSet.ToArray());
            }
            var id = Guid.NewGuid();
            Task<SearchResult> task = GetSearchResult(query);
            WorkingTask.Add(id, task);
            SearchResult ret = await task;
            WorkingTask.Remove(id);
            return ret;
        }

        private static async Task<SearchResult> GetSearchResult(string query)
        {
            var isGetting = false;
            lock (GettingSearches)
            {
                isGetting = GettingSearches.ContainsKey(query);
            }
            if (isGetting)
            {
                await GettingSearches[query];
                return SearchCache[query];
            }

            Task task = GetQueryResult(query);
            lock (GettingSearches)
            {
                GettingSearches.Add(query, task);
            }
            await task;
            lock (GettingSearches)
            {
                GettingSearches.Remove(query);
            }

            lock (SearchCache)
            {
                return SearchCache[query];
            }
        }

        private static async Task GetQueryResult(string query)
        {
            IEnumerable<Func<Task<string>>> request = SearchQueryService.Select<string, Func<Task<string>>>(endpoint => { return () => _client.GetStringAsync(endpoint + query); });
            var responseText = await GetResponseWithRetry(GettingSearches, query, request.ToArray());
            SearchResult result = JsonUtility.FromJson<SearchResult>(RefineJson(responseText));
            lock (SearchCache)
            {
                SearchCache[query] = result;
                SearchLog.Add(query);
                while (SearchCache.Count > NuGetImporterSettings.Instance.SearchCacheLimit && SearchCache.Count > 0)
                {
                    var delete = SearchLog[0];
                    SearchLog.RemoveAt(0);
                    SearchCache.Remove(delete);
                }
            }
        }

        private static async Task<string> GetResponseWithRetry(IDictionary getting, string key, params Func<Task<string>>[] actions)
        {
            var tryLimit = NuGetImporterSettings.Instance.RetryLimit + 1;
            var totalTryCount = tryLimit * actions.Length;
            for (var i = 0; i < tryLimit; i++)
            {
                foreach (Func<Task<string>> action in actions)
                {
                    totalTryCount--;
                    try
                    {
                        var responseText = await action();
                        return responseText;
                    }
                    catch (Exception)
                    {
                        if (totalTryCount <= 0)
                        {
                            getting.Remove(key);
                            throw;
                        }
                        await Task.Delay(1000);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// <para>Get the specified package.</para>
        /// <para>指定したパッケージを取得する。</para>
        /// </summary>
        /// <param name="packageName">
        /// <para>Package name.</para>
        /// <para>パッケージの名前。</para>
        /// </param>
        /// <param name="version">
        /// <para>Version.</para>
        /// <para>バージョン。</para>
        /// </param>
        /// <param name="savePath">
        /// <para>Destination directory. It will be saved as a .nupkg file in this directory.</para>
        /// <para>保存先のディレクトリ。このディレクトリ内に.nupkgファイルとして保存される。</para>
        /// </param>
        /// <returns>
        /// <para>Task</para>
        /// </returns>
        public static async Task GetPackage(string packageName, string version, string savePath)
        {
            var fileName = packageName.ToLowerInvariant() + "." + version.ToLowerInvariant();
            using (var fileStream = new FileStream(Path.Combine(savePath, fileName + ".nupkg"), FileMode.Create, FileAccess.Write, FileShare.None))
            {
                // It takes much time for the header response, so set the package first.
                lock (Downloading)
                {
                    Downloading[packageName] = (0, fileStream);
                }

                var cachePath = Environment.OSVersion.Platform == PlatformID.Win32NT ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) : "~";
                cachePath = Path.Combine(cachePath, ".nuget", "packages", packageName.ToLowerInvariant(), version.ToLowerInvariant());

                if (Directory.Exists(cachePath))
                {
                    using (FileStream sourceStream = File.Open(Path.Combine(cachePath, fileName + ".nupkg"), FileMode.Open))
                    {
                        lock (Downloading)
                        {
                            Downloading[packageName] = (sourceStream.Length, fileStream);
                        }
                        await sourceStream.CopyToAsync(fileStream);
                        lock (Downloading)
                        {
                            Downloading.Remove(packageName);
                        }
                    }
                }
                else
                {
                    if (TimeoutSet.Any())
                    {
                        await Task.WhenAll(TimeoutSet.ToArray());
                    }
                    var id = Guid.NewGuid();
                    Task task = GetContent(packageName, version, fileName, fileStream);
                    WorkingTask.Add(id, task);
                    await task;
                    WorkingTask.Remove(id);
                }
            }
        }

        private static async Task GetContent(string packageName, string version, string fileName, FileStream fileStream)
        {
            var tryCount = NuGetImporterSettings.Instance.RetryLimit + 1;
            for (var i = 0; i < tryCount; i++)
            {
                try
                {
                    HttpResponseMessage response = await _client.GetAsync(_packageBaseAddress + packageName.ToLowerInvariant() + "/" + version.ToLowerInvariant() + "/" + fileName + ".nupkg", HttpCompletionOption.ResponseHeadersRead);
                    using (Stream responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        lock (Downloading)
                        {
                            Downloading[packageName] = (response.Content.Headers.ContentLength.GetValueOrDefault(), fileStream);
                        }
                        await responseStream.CopyToAsync(fileStream);
                    }
                    break;
                }
                catch (Exception)
                {
                    if (i >= tryCount - 1)
                    {
                        throw;
                    }
                    fileStream.Seek(0, SeekOrigin.Begin);
                    await Task.Delay(1000);
                }
                finally
                {
                    if (i >= tryCount - 1)
                    {
                        lock (Downloading)
                        {
                            Downloading.Remove(packageName);
                        }
                    }
                }
            }

            lock (Downloading)
            {
                Downloading.Remove(packageName);
            }
        }

        /// <summary>
        /// <para>Get the downloaded byte length of the package currently downloading.</para>
        /// <para>ダウンロード中のパッケージのダウンロードしたバイト数を取得する。</para>
        /// </summary>
        /// <param name="packageName">
        /// <para>Name of the package.</para>
        /// <para>取得したいパッケージの名前。</para>
        /// </param>
        /// <returns>
        /// <para>Current downloaded byte length.</para>
        /// <para>現在のダウンロードしたバイト数。</para>
        /// </returns>
        /// <exception cref="System.ArgumentException">
        /// <para>Thrown when the given package is not downloading.</para>
        /// <para>与えられたパッケージがダウンロード中でないときthrowされる。</para>
        /// </exception>
        public static (long packageSize, long downloadedSize) GetDownloadingProgress(string packageName)
        {
            lock (Downloading)
            {
                return Downloading.ContainsKey(packageName)
                    ? ((long packageSize, long downloadedSize))(Downloading[packageName].packageSize, Downloading[packageName].downloaded.Length)
                    : throw new ArgumentException(packageName + " is not downloading now.");
            }
        }

        public static bool TryGetDownloadingProgress(string packageName, out long packageSize, out long downloadedSize)
        {
            lock (Downloading)
            {
                if (Downloading.ContainsKey(packageName))
                {
                    packageSize = Downloading[packageName].packageSize;
                    downloadedSize = Downloading[packageName].downloaded.Length;
                    return true;
                }

                packageSize = -1;
                downloadedSize = -1;
                return false;
            }
        }

        /// <summary>
        /// <para>Get the specified catalog.</para>
        /// <para>指定したカタログを所得する。</para>
        /// </summary>
        /// <param name="packageName">
        /// <para>Package name.</para>
        /// <para>パッケージの名前。</para>
        /// </param>
        /// <returns>
        /// <para>Specified catalog.</para>
        /// <para>指定したカタログ。</para>
        /// </returns>
        public static async Task<Catalog> GetCatalog(string packageName)
        {
            // The below code is the cache process.
            lock (CatalogCache)
            {
                if (CatalogCache.TryGetValue(packageName, out Catalog catalog))
                {
                    CatalogLog.Remove(packageName);
                    CatalogLog.Add(packageName);
                    return catalog;
                }
            }
            if (TimeoutSet.Any())
            {
                await Task.WhenAll(TimeoutSet.ToArray());
            }
            var id = Guid.NewGuid();
            Task<Catalog> task = GetCatalogResult(packageName);
            WorkingTask.Add(id, task);
            Catalog ret = await task;
            WorkingTask.Remove(id);
            return ret;
        }

        private static async Task<Catalog> GetCatalogResult(string packageName)
        {
            var isGetting = false;
            lock (GettingCatalogs)
            {
                isGetting = GettingCatalogs.ContainsKey(packageName);
            }
            if (isGetting)
            {
                await GettingCatalogs[packageName];
                while (GettingCatalogs.ContainsKey(packageName))
                {
                    await Task.Delay(100);
                }
                return CatalogCache[packageName];
            }

            using Task<string> request = GetResponseWithRetry(GettingCatalogs, packageName, () => _client.GetStringAsync(_registrationsBaseUrl + packageName.ToLowerInvariant() + "/index.json"));
            lock (GettingCatalogs)
            {
                GettingCatalogs.Add(packageName, request);
            }

            var responseText = await request;
            Catalog catalog = JsonUtility.FromJson<Catalog>(RefineJson(responseText));
            if (catalog.items[0].items == null)
            {
                var tasks = new List<Task>();
                for (var i = 0; i < catalog.items.Length; i++)
                {
                    tasks.Add(GetItem(catalog, i));
                }
                await Task.WhenAll(tasks);
            }
            lock (CatalogCache)
            {
                CatalogCache[packageName] = catalog;
                CatalogLog.Add(packageName);
                while (CatalogCache.Count > NuGetImporterSettings.Instance.CatalogCacheLimit && CatalogCache.Count > 0)
                {
                    var delete = CatalogLog[0];
                    CatalogLog.RemoveAt(0);
                    CatalogCache.Remove(delete);
                }
            }
            lock (GettingCatalogs)
            {
                GettingCatalogs.Remove(packageName);
            }
            lock (CatalogCache)
            {
                return CatalogCache[packageName];
            }
        }

        private static async Task GetItem(Catalog catalog, int index)
        {
            var itemText = await _client.GetStringAsync(catalog.items[index].nuget_id);
            catalog.items[index] = JsonUtility.FromJson<Item>(RefineJson(itemText));
        }

        private static string RefineJson(string json)
        {
            return json.Replace(@"""@id"":", @"""nuget_id"":").Replace(@"""@type"":", @"""nuget_type"":");
        }
    }
}
