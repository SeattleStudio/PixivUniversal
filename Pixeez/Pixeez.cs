﻿//PixivUniversal
//Copyright(C) 2017 Pixeez Plus Project

//This program is free software; you can redistribute it and/or
//modify it under the terms of the GNU General Public License
//as published by the Free Software Foundation; version 2
//of the License.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Pixeez.Objects;
using System.Linq;
using System.IO;

namespace Pixeez
{
    public enum MethodType
    {
        GET = 0,
        POST = 1,
        DELETE = 2,
    }

    public class AsyncResponse : IDisposable
    {
        public AsyncResponse(HttpResponseMessage source)
        {
            this.Source = source;
        }

        public HttpResponseMessage Source { get; }

        public Task<Stream> GetResponseStreamAsync()
        {
            return this.Source.Content.ReadAsStreamAsync();
        }

        public Task<string> GetResponseStringAsync()
        {
            return this.Source.Content.ReadAsStringAsync();
        }

        public Task<byte[]> GetResponseByteArrayAsync()
        {
            return this.Source.Content.ReadAsByteArrayAsync();
        }

        public void Dispose()
        {
            this.Source?.Dispose();
        }
    }

    public struct AuthResult
    {
        public Tokens Tokens;
        public Authorize Authorize;
    }

    public class Auth
    {
        /// <summary>
        /// <para>Available parameters:</para>
        /// <para>- <c>string</c> username (required)</para>
        /// <para>- <c>string</c> password (required)</para>
        /// </summary>
        /// <returns>Tokens.</returns>
        public static async Task<AuthResult> AuthorizeAsync(string username, string password, string refreshtoken)
        {
            var httpClient = new HttpClient();
            //httpClient.DefaultRequestHeaders.Add("Referer", "http://www.pixiv.net/");
            httpClient.DefaultRequestHeaders.Add("App-OS", "ios");
            httpClient.DefaultRequestHeaders.Add("App-OS-Version", "10.2.1");
            httpClient.DefaultRequestHeaders.Add("App-Version", "6.4.0");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "PixivIOSApp/6.0.9 (iOS 10.2.1; iPhone8,1)");

            FormUrlEncodedContent param;
            if (refreshtoken == null)
            {
                param = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "get_secure_url","1" },
                    { "client_id", "bYGKuGVw91e0NMfPGp44euvGt59s" },
                    { "client_secret", "HP3RmkgAmEGro0gn1x9ioawQE8WMfvLXDz3ZqxpK" },
                    { "grant_type", "password" },
                    { "username" ,username },
                    { "password" ,password },
                });
            }
            else
            {
                param = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "get_secure_url","1" },
                    { "client_id", "bYGKuGVw91e0NMfPGp44euvGt59s" },
                    { "client_secret", "HP3RmkgAmEGro0gn1x9ioawQE8WMfvLXDz3ZqxpK" },
                    { "grant_type", "refresh_token" },
                    { "refresh_token" ,refreshtoken },
                });
            }
            var response = await httpClient.PostAsync("https://oauth.secure.pixiv.net/auth/token", param);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException();

            var json = await response.Content.ReadAsStringAsync();
            var authorize = JToken.Parse(json).SelectToken("response").ToObject<Authorize>();

            var result = new Pixeez.AuthResult();
            result.Authorize = authorize;
            result.Tokens = new Tokens(authorize.AccessToken);
            return result;
        }

        public static Tokens AuthorizeWithAccessToken(string accessToken)
        {
            return new Tokens(accessToken);
        }
    }

    public class Tokens
    {
        public string RefreshToken { get; set; }

        public string AccessToken { get; private set; }

        internal Tokens(string accessToken)
        {
            this.AccessToken = accessToken;
        }
        public async Task<AsyncResponse> SendRequestWithAuthAsync(MethodType type, string url, IDictionary<string, string> param = null, IDictionary<string, string> headers = null)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Referer", "http://spapi.pixiv.net/");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "PixivIOSApp/5.8.7");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + this.AccessToken);
            return await SendRequestWithoutHeaderAsync(type, url, param, headers, httpClient);
        }
        public async Task<AsyncResponse> SendRequestToGetImageAsync(MethodType type, string url, IDictionary<string, string> param = null, IDictionary<string, string> headers = null)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Referer", "https://app-api.pixiv.net/");
            return await SendRequestWithoutHeaderAsync(type, url, param, headers, httpClient);
        }
        public async Task<AsyncResponse> SendRequestWithoutAuthAsync(MethodType type, string url, bool needauth = false, IDictionary<string, string> param = null, IDictionary<string, string> headers = null)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("App-OS", "ios");
            httpClient.DefaultRequestHeaders.Add("App-OS-Version", "10.2.1");
            httpClient.DefaultRequestHeaders.Add("App-Version", "6.4.0");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "PixivIOSApp/6.0.9 (iOS 10.2.1; iPhone8,1)");
            if (needauth) httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + this.AccessToken);
            return await SendRequestWithoutHeaderAsync(type, url, param, headers, httpClient);
        }

        private static async Task<AsyncResponse> SendRequestWithoutHeaderAsync(MethodType type, string url, IDictionary<string, string> param, IDictionary<string, string> headers, HttpClient httpClient)
        {
            if (headers != null)
            {
                foreach (var header in headers)
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            AsyncResponse asyncResponse = null;

            if (type == MethodType.POST)
            {
                var reqParam = new FormUrlEncodedContent(param);
                var response = await httpClient.PostAsync(url, reqParam);
                asyncResponse = new AsyncResponse(response);
            }
            else if (type == MethodType.DELETE)
            {
                var uri = url;

                if (param != null)
                {
                    var query_string = "";
                    foreach (KeyValuePair<string, string> kvp in param)
                    {
                        if (query_string == "")
                            query_string += "?";
                        else
                            query_string += "&";

                        query_string += kvp.Key + "=" + WebUtility.UrlEncode(kvp.Value);
                    }
                    uri += query_string;
                }

                var response = await httpClient.DeleteAsync(uri);
                asyncResponse = new AsyncResponse(response);
            }
            else
            {
                var uri = url;

                if (param != null)
                {
                    var query_string = "";
                    foreach (KeyValuePair<string, string> kvp in param)
                    {
                        if (query_string == "")
                            query_string += "?";
                        else
                            query_string += "&";

                        query_string += kvp.Key + "=" + WebUtility.UrlEncode(kvp.Value);
                    }
                    uri += query_string;
                }

                var response = await httpClient.GetAsync(uri);
                asyncResponse = new AsyncResponse(response);
            }

            return asyncResponse;
        }


        //public async Task GetMyFollowingWorksAsync(string restrict = "public",string offset=null)
        //{
        //    var dic = new Dictionary<string, string> { { "restrict", restrict } };
        //    if (offset != null)
        //        dic["offset"] = offset;
        //    await SendRequestWithoutAuthAsync(MethodType.GET, "https://app-api.pixiv.net/v2/illust/follow", param: dic, needauth: true);
        //}

        //public async Task GetMyFollowingUsers(string authorid,int page= 1,int per_page= 30)
        //{
        //    string url = "https://public-api.secure.pixiv.net/v1/users/"+ authorid +"/following.json";
        //    await SendRequestWithAuthAsync(MethodType.GET, url, new Dictionary<string, string> { { "page", page.ToString() }, { "per_page", per_page.ToString() } });
        //}
        public string format_bool(bool value)
        {
            if (value)
            {
                return "true";
            }
            return "false";
        }

        public async Task<RecommendedRootobject> GetRecommendedWorks(string content_type = "illust", bool include_ranking_label = true, string filter = "for_ios",
            string max_bookmark_id_for_recommend = null, string min_bookmark_id_for_recent_illust = null,
string offset = null, bool? include_ranking_illusts = null, string bookmark_illust_ids = null, bool req_auth = true)
        {
            string url;
            if (req_auth)
                url = "https://app-api.pixiv.net/v1/illust/recommended";
            else
                url = "https://app-api.pixiv.net/v1/illust/recommended-nologin";
            var dic = new Dictionary<string, string>() {
                { "content_type",content_type},
                { "include_ranking_label", format_bool( include_ranking_label)},
                { "filter", filter }
            };
            if (max_bookmark_id_for_recommend != null)
                dic["max_bookmark_id_for_recommend"] = max_bookmark_id_for_recommend;
            if (min_bookmark_id_for_recent_illust != null)
                dic["min_bookmark_id_for_recent_illust"] = min_bookmark_id_for_recent_illust;
            if (offset != null)
                dic["offset"] = offset;
            if (include_ranking_illusts.HasValue)
                dic["include_ranking_illusts"] = format_bool(include_ranking_illusts.Value);
            if (!req_auth)
            {
                dic["bookmark_illust_ids"] = bookmark_illust_ids;
            }

            return await AccessNewApiAsync<RecommendedRootobject>(url, req_auth, dic);
        }

        public async Task<T> AccessNewApiAsync<T>(string url, bool req_auth = true, Dictionary<string, string> dic = null, MethodType methodtype = MethodType.GET)
        {
            using (var res = await SendRequestWithoutAuthAsync(methodtype, url, req_auth, dic))
            {
                var str = await res.GetResponseStringAsync();
                return JToken.Parse(str).ToObject<T>();
            }
        }


        public async Task AddFavouriteUser(long user_id, string publicity = "public")
        {
            await SendRequestAsync(MethodType.POST, "https://public-api.secure.pixiv.net/v1/me/favorite-users.json", new Dictionary<string, string> { { "target_user_id", user_id.ToString() }, { "publicity", publicity } });
        }
        /// <summary>
        /// 可批量解除，逗号分隔
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="publicity"></param>
        /// <returns></returns>
        public async Task DeleteFavouriteUser(string user_id, string publicity = "public")
        {
            await SendRequestAsync(MethodType.DELETE, "https://public-api.secure.pixiv.net/v1/me/favorite-users.json", new Dictionary<string, string> { { "delete_ids", user_id.ToString() }, { "publicity", publicity } });
        }

        /// <summary>
        /// <para>Available parameters:</para>
        /// <para>- <c>MethodType</c> type (required) [ GET, POST ]</para>
        /// <para>- <c>string</c> url (required)</para>
        /// <para>- <c>IDictionary</c> param (required)</para>
        /// <para>- <c>IDictionary</c> header (optional)</para>
        /// </summary>
        /// <returns>AsyncResponse.</returns>
        public async Task<AsyncResponse> SendRequestAsync(MethodType type, string url, IDictionary<string, string> param = null, IDictionary<string, string> headers = null)
        {
            return await SendRequestWithAuthAsync(type, url, param, headers);
        }

        private async Task<T> AccessApiAsync<T>(MethodType type, string url, IDictionary<string, string> param, IDictionary<string, string> headers = null) where T : class
        {
            return await AccessApiAsync<T>(await this.SendRequestAsync(type, url, param, headers));
        }
        private async Task<T> AccessApiAsync<T>(AsyncResponse res) where T : class
        {
            using (var response = res)
            {
                var json = await response.GetResponseStringAsync();
                var obj = JToken.Parse(json).SelectToken("response").ToObject<T>();

                if (obj is IPagenated)
                    ((IPagenated)obj).Pagination = JToken.Parse(json).SelectToken("pagination").ToObject<Pagination>();

                return obj;
            }
        }

        public async Task<Illusts> GetRelatedWorks(long illust_id, string filter = "for_ios", string seed_illust_ids = null, bool req_auth = true)
        {
            string url = "https://app-api.pixiv.net/v1/illust/related";
            var dic = new Dictionary<string, string>()
            {
                { "illust_id", illust_id.ToString() },
                { "filter", filter }
            };
            //            if type(seed_illust_ids) == str:
            //            params['seed_illust_ids'] = seed_illust_ids
            //        if type(seed_illust_ids) == list:
            //params['seed_illust_ids'] = ",".join([str(iid) for iid in seed_illust_ids])
            return await AccessNewApiAsync<Illusts>(url, req_auth, dic);
        }

        /// <summary>
        /// <para>Available parameters:</para>
        /// <para>- <c>long</c> illustId (required)</para>
        /// </summary>
        /// <returns>Works.</returns>
        [Obsolete]
        public async Task<List<NormalWork>> GetWorksAsync(long illustId)
        {
            var url = "https://public-api.secure.pixiv.net/v1/works/" + illustId.ToString() + ".json";

            var param = new Dictionary<string, string>
            {
                { "profile_image_sizes", "px_170x170,px_50x50" },
                { "image_sizes", "px_128x128,small,medium,large,px_480mw" },
                { "include_stats", "true" },
            };

            return await this.AccessApiAsync<List<NormalWork>>(MethodType.GET, url, param);
        }

        /// <summary>
        /// <para>Available parameters:</para>
        /// <para>- <c>long</c> authorId (required)</para>
        /// </summary>
        /// <returns>Users.</returns>
        public async Task<List<User>> GetUsersAsync(long authorId)
        {
            var url = "https://public-api.secure.pixiv.net/v1/users/" + authorId.ToString() + ".json";

            var param = new Dictionary<string, string>
            {
                { "profile_image_sizes", "px_170x170,px_50x50" } ,
                { "image_sizes", "px_128x128,small,medium,large,px_480mw" } ,
                { "include_stats", "1" } ,
                { "include_profile", "1" } ,
                { "include_workspace", "1" } ,
                { "include_contacts", "1" } ,
            };

            return await this.AccessApiAsync<List<User>>(MethodType.GET, url, param);
        }

        /// <summary>
        /// 获取我的订阅
        /// <para>Available parameters:</para>
        /// <para>- <c>long</c> maxId (optional)</para>
        /// <para>- <c>bool</c> showR18 (optional)</para>
        /// </summary>
        /// <returns>Feeds.</returns>
        public async Task<List<Feed>> GetMyFeedsAsync(long maxId = 0, bool showR18 = true)
        {
            var url = "https://public-api.secure.pixiv.net/v1/me/feeds.json";

            var param = new Dictionary<string, string>
            {
                { "relation", "all" } ,
                { "type", "touch_nottext" } ,
                { "show_r18", Convert.ToInt32(showR18).ToString() } ,
            };
            
            if (maxId != 0)
                param.Add("max_id", maxId.ToString());

            return await this.AccessApiAsync<List<Feed>>(MethodType.GET, url, param);
        }

        /// <summary>
        /// <para>Available parameters:</para>
        /// <para>- <c>int</c> page (optional)</para>
        /// <para>- <c>int</c> perPage (optional)</para>
        /// <para>- <c>string</c> publicity (optional) [ public, private ]</para>
        /// <para>- <c>bool</c> includeSanityLevel (optional)</para>
        /// </summary>
        /// <returns>UsersFavoriteWorks. (Pagenated)</returns>
        public async Task<Illusts> GetUserFavoriteWorksAsync(long user_id, string restrict = "public",string filter= "for_ios",int? max_bookmark_id= null,string tag= null,bool req_auth= true)
        {
            var url = "https://app-api.pixiv.net/v1/user/bookmarks/illust";

            var param = new Dictionary<string, string>
            {
                { "user_id", user_id.ToString() } ,
                { "restrict", restrict.ToString() } ,
                { "filter", filter } ,
            };
            if(max_bookmark_id.HasValue)
            {
                param.Add("max_bookmark_id", max_bookmark_id.Value.ToString());
            }
            if (tag != null) param.Add("tag", tag);
            return await this.AccessNewApiAsync<Illusts>(url, dic:param,req_auth:req_auth);
        }
        
        /// <summary>
        /// <para>Available parameters:</para>
        /// <para>- <c>long</c> workID (required)</para>
        /// <para>- <c>string</c> publicity (optional) [ public, private ]</para>
        /// </summary>
        /// <returns>UsersWorks. (Pagenated)</returns>
        public async Task<List<UsersFavoriteWork>> AddMyFavoriteWorksAsync(long workId, IEnumerable<string> tags = null, string publicity = "public")
        {
            var url = "https://app-api.pixiv.net/v1/illust/bookmark/add";

            var param = new Dictionary<string, string>
            {
                { "illust_id", workId.ToString() } ,
                { "restrict", publicity } ,
            };

            if (tags != null)
                param.Add("tags", string.Join(",", tags));

            return await this.AccessApiAsync<List<UsersFavoriteWork>>(MethodType.POST, url, param);
        }

        /// <summary>
        /// <para>Available parameters:</para>
        /// <para>- <c>IEnumerable</c> workIds (required)</para>
        /// <para>- <c>string</c> publicity (optional) [ public, private ]</para>
        /// </summary>
        /// <returns>UsersWorks. (Pagenated)</returns>
        public async Task<List<UsersFavoriteWork>> DeleteMyFavoriteWorksAsync(IEnumerable<long> workIds, string publicity = "public")
        {
            var url = "https://app-api.pixiv.net/v1/illust/bookmark/delete";

            var param = new Dictionary<string, string>
            {
                { "illust_id", string.Join(",", workIds.Select(x => x.ToString())) } ,
                //{ "publicity", publicity } ,
            };

            return await this.AccessApiAsync<List<UsersFavoriteWork>>(MethodType.POST, url, param);
        }

        /// <summary>
        /// <para>Available parameters:</para>
        /// <para>- <c>long</c> workId (required)</para>
        /// <para>- <c>string</c> publicity (optional) [ public, private ]</para>
        /// </summary>
        /// <returns>UsersWorks. (Pagenated)</returns>
        public async Task<Paginated<UsersFavoriteWork>> DeleteMyFavoriteWorksAsync(long workId, string publicity = "public")
        {
            var url = "https://app-api.pixiv.net/v1/illust/bookmark/delete";

            var param = new Dictionary<string, string>
            {
                { "illust_id", workId.ToString() } ,
                //{ "publicity", publicity } ,
            };

            return await this.AccessApiAsync<Paginated<UsersFavoriteWork>>(MethodType.POST, url, param);
        }

        /// <summary>
        /// <para>Available parameters:</para>
        /// <para>- <c>long</c> authorId (required)</para>
        /// <para>- <c>int</c> page (optional)</para>
        /// <para>- <c>int</c> perPage (optional)</para>
        /// <para>- <c>string</c> publicity (optional) [ public, private ]</para>
        /// <para>- <c>bool</c> includeSanityLevel (optional)</para>
        /// </summary>
        /// <returns>UsersWorks. (Pagenated)</returns>
        public async Task<RecommendedRootobject> GetMyFollowingWorksAsync(string restrict="public",int? offset= null)
        {
            var url = "https://app-api.pixiv.net/v2/illust/follow";

            var param = new Dictionary<string, string>
            {
                { "restrict", restrict } ,
            };
            if (offset.HasValue)
                param.Add("offset", offset.Value.ToString());

            return await this.AccessNewApiAsync<RecommendedRootobject>(url, dic: param);
        }
        /// <summary>
        /// 获取用户作品列表 (无需登录)
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="type"></param>
        /// <param name="filter"></param>
        /// <param name="offset"></param>
        /// <param name="req_auth"></param>
        /// <returns></returns>
        public async Task<Illusts> GetUserWorksAsync(long user_id,string type= "illust",string filter="for_ios",int? offset= null,bool req_auth= true)
        {
            var url = "https://app-api.pixiv.net/v1/user/illusts";

            var param = new Dictionary<string, string>
            {
                {"user_id",user_id.ToString() },
                {
                    "type",type
                },
                {
                    "filter",
                    filter
                }
            };
            if(offset!=null)
            {
                param["offset"] = offset.Value.ToString();
            }
            return await this.AccessNewApiAsync<Illusts>(url,req_auth, param);
        }
        /// <summary>
        /// <para>Available parameters:</para>
        /// <para>- <c>long</c> authorId (required)</para>
        /// <para>- <c>int</c> page (optional)</para>
        /// <para>- <c>int</c> perPage (optional)</para>
        /// <para>- <c>string</c> publicity (optional) [ public, private ]</para>
        /// <para>- <c>bool</c> includeSanityLevel (optional)</para>
        /// </summary>
        /// <returns>UsersWorks. (Pagenated)</returns>
        [Obsolete]
        public async Task<Paginated<UsersWork>> GetUsersWorksAsync(long authorId, int page = 1, int perPage = 30, string publicity = "public", bool includeSanityLevel = true)
        {
            var url = "https://public-api.secure.pixiv.net/v1/users/" + authorId.ToString() + "/works.json";

            var param = new Dictionary<string, string>
            {
                { "page", page.ToString() } ,
                { "per_page", perPage.ToString() } ,
                { "publicity", publicity } ,
                { "include_stats", "1" } ,
                { "include_sanity_level", Convert.ToInt32(includeSanityLevel).ToString() } ,
                { "image_sizes", "px_128x128,small,medium,large,px_480mw" } ,
                { "profile_image_sizes", "px_170x170,px_50x50" } ,
            };

            return await this.AccessApiAsync<Paginated<UsersWork>>(MethodType.GET, url, param);
        }

        /// <summary>
        /// <para>Available parameters:</para>
        /// <para>- <c>long</c> authorId (required)</para>
        /// <para>- <c>int</c> page (optional)</para>
        /// <para>- <c>int</c> perPage (optional)</para>
        /// <para>- <c>string</c> publicity (optional) [ public, private ]</para>
        /// <para>- <c>bool</c> includeSanityLevel (optional)</para>
        /// </summary>
        /// <returns>UsersFavoriteWorks. (Pagenated)</returns>
        [Obsolete]
        public async Task<Paginated<UsersFavoriteWork>> GetUsersFavoriteWorksAsync(long authorId, int page = 1, int perPage = 30, string publicity = "public", bool includeSanityLevel = true)
        {
            var url = "https://public-api.secure.pixiv.net/v1/users/" + authorId.ToString() + "/favorite_works.json";

            var param = new Dictionary<string, string>
            {
                { "page", page.ToString() } ,
                { "per_page", perPage.ToString() } ,
                { "publicity", publicity } ,
                { "include_stats", "1" } ,
                { "include_sanity_level", Convert.ToInt32(includeSanityLevel).ToString() } ,
                { "image_sizes", "px_128x128,small,medium,large,px_480mw" } ,
                { "profile_image_sizes", "px_170x170,px_50x50" } ,
            };

            return await this.AccessApiAsync<Paginated<UsersFavoriteWork>>(MethodType.GET, url, param);
        }

        /// <summary>
        /// <para>Available parameters:</para>
        /// <para>- <c>long</c> maxId (optional)</para>
        /// <para>- <c>bool</c> showR18 (optional)</para>
        /// </summary>
        /// <returns>Feed.</returns>
        public async Task<List<Feed>> GetUsersFeedsAsync(long authorId, long maxId = 0, bool showR18 = true)
        {
            var url = "https://public-api.secure.pixiv.net/v1/users/" + authorId.ToString() + "/feeds.json";

            var param = new Dictionary<string, string>
            {
                { "relation", "all" } ,
                { "type", "touch_nottext" } ,
                { "show_r18", Convert.ToInt32(showR18).ToString() } ,
            };

            if (maxId != 0)
                param.Add("max_id", maxId.ToString());

            return await this.AccessApiAsync<List<Feed>>(MethodType.GET, url, param);
        }

        /// <summary>
        /// <para>Available parameters:</para>
        /// <para>- <c>string</c> mode (optional) [ daily, weekly, monthly, male, female, rookie, daily_r18, weekly_r18, male_r18, female_r18, r18g ]</para>
        /// <para>- <c>int</c> page (optional)</para>
        /// <para>- <c>int</c> perPage (optional)</para>
        /// <para>- <c>string</c> date (optional) [ 2015-04-01 ]</para>
        /// <para>- <c>bool</c> includeSanityLevel (optional)</para>
        /// </summary>
        /// <returns>RankingAll. (Pagenated)</returns>
        public async Task<Paginated<Rank>> GetRankingAllAsync(string mode = "daily", int page = 1, int perPage = 30, string date = "", bool includeSanityLevel = true)
        {
            var url = "https://public-api.secure.pixiv.net/v1/ranking/all";

            var param = new Dictionary<string, string>
            {
                { "mode", mode } ,
                { "page", page.ToString() } ,
                { "per_page", perPage.ToString() } ,
                { "include_stats", "1" } ,
                { "include_sanity_level", Convert.ToInt32(includeSanityLevel).ToString() } ,
                { "image_sizes", "px_128x128,small,medium,large,px_480mw" } ,
                { "profile_image_sizes", "px_170x170,px_50x50" } ,
            };

            if (!string.IsNullOrWhiteSpace(date))
                param.Add("date", date);

            return await this.AccessApiAsync<Paginated<Rank>>(MethodType.GET, url, param);
        }

        /// <summary>
        /// <para>Available parameters:</para>
        /// <para>- <c>string</c> q (required)</para>
        /// <para>- <c>int</c> page (optional)</para>
        /// <para>- <c>int</c> perPage (optional)</para>
        /// <para>- <c>string</c> mode (optional) [ text, tag, exact_tag, caption ]</para>
        /// <para>- <c>string</c> period (optional) [ all, day, week, month ]</para>
        /// <para>- <c>string</c> order (optional) [ desc, asc ]</para>
        /// <para>- <c>string</c> sort (optional) [ date, popular ]</para>
        /// <para>- <c>bool</c> includeSanityLevel (optional)</para>
        /// </summary>
        /// <returns>Works. (Pagenated)</returns>
        public async Task<Paginated<NormalWork>> SearchWorksAsync(string query, int page = 1, int perPage = 30, string mode = "text", string period = "all", string order = "desc", string sort = "date", bool includeSanityLevel = true)
        {
            var url = "https://public-api.secure.pixiv.net/v1/search/works.json";

            var param = new Dictionary<string, string>
            {
                { "q", query } ,
                { "page", page.ToString() } ,
                { "per_page", perPage.ToString() } ,
                { "period", period } ,
                { "order", order } ,
                { "sort", sort } ,
                { "mode", mode } ,

                { "include_stats", "1" } ,
                { "include_sanity_level", Convert.ToInt32(includeSanityLevel).ToString() } ,
                { "image_sizes", "px_128x128,small,medium,large,px_480mw" } ,
                { "profile_image_sizes", "px_170x170,px_50x50" } ,
            };

            return await this.AccessApiAsync<Paginated<NormalWork>>(MethodType.GET, url, param);
        }

        /// <summary>
        /// <para>Available parameters:</para>
        /// <para>- <c>int</c> page (optional)</para>
        /// <para>- <c>int</c> perPage (optional)</para>
        /// <para>- <c>bool</c> includeSanityLevel (optional)</para>
        /// </summary>
        /// <returns>Works. (Pagenated)</returns>
        public async Task<Paginated<NormalWork>> GetLatestWorksAsync(int page = 1, int perPage = 30, bool includeSanityLevel = true)
        {
            var url = "https://public-api.secure.pixiv.net/v1/works.json";

            var param = new Dictionary<string, string>
            {
                { "page", page.ToString() } ,
                { "per_page", perPage.ToString() } ,

                { "include_stats", "1" } ,
                { "include_sanity_level", Convert.ToInt32(includeSanityLevel).ToString() } ,
                { "image_sizes", "px_128x128,small,medium,large,px_480mw" } ,
                { "profile_image_sizes", "px_170x170,px_50x50" } ,
            };

            return await this.AccessApiAsync<Paginated<NormalWork>>(MethodType.GET, url, param);
        }
    }
}
