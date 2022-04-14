﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using System.Linq;
using Supabase.Functions.Responses;

namespace Supabase.Functions
{
    internal static class Helpers
    {
        private static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// Helper to make a request using the defined parameters to an API Endpoint and coerce into a model. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        /// <param name="url"></param>
        /// <param name="reqParams"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static async Task<T> MakeRequest<T>(HttpMethod method, string url, object data = null, Dictionary<string, string> headers = null)
        {
            var baseResponse = await MakeRequest(method, url, data, headers);
            return JsonConvert.DeserializeObject<T>(baseResponse.Content);
        }

        /// <summary>
        /// Helper to make a request using the defined parameters to an API Endpoint.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="url"></param>
        /// <param name="reqParams"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static async Task<BaseResponse> MakeRequest(HttpMethod method, string url, object data = null, Dictionary<string, string> headers = null)
        {
            var builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);

            if (data != null && method == HttpMethod.Get)
            {
                // Case if it's a Get request the data object is a dictionary<string,string>
                if (data is Dictionary<string, string> reqParams)
                {
                    foreach (var param in reqParams)
                        query[param.Key] = param.Value;
                }

            }

            builder.Query = query.ToString();

            using (var requestMessage = new HttpRequestMessage(method, builder.Uri))
            {

                if (data != null && method != HttpMethod.Get)
                {
                    requestMessage.Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                }

                if (headers != null)
                {
                    foreach (var kvp in headers)
                    {
                        requestMessage.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                    }
                }

                var response = await client.SendAsync(requestMessage);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var obj = new ErrorResponse
                    {
                        Content = content,
                        Message = content
                    };
                    throw new RequestException(response, obj);
                }
                else
                {
                    return new BaseResponse { Content = content, ResponseMessage = response };
                }
            }
        }
    }

    public class RequestException : Exception
    {
        public HttpResponseMessage Response { get; private set; }
        public ErrorResponse Error { get; private set; }

        public RequestException(HttpResponseMessage response, ErrorResponse error) : base(error.Message)
        {
            Response = response;
            Error = error;
        }
    }
}
