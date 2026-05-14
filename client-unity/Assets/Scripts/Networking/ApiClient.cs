using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Ludo.Core;
using Ludo.Utils;

namespace Ludo.Networking
{
    /// <summary>
    /// Thin REST client for /api/* endpoints. Uses UnityWebRequest + retry policy.
    /// </summary>
    public class ApiClient
    {
        private readonly AppConfig _cfg;

        public ApiClient(AppConfig cfg) { _cfg = cfg; }

        public Task<TResp> PostJsonAsync<TResp>(string path, object body, string token = null)
            => SendAsync<TResp>("POST", path, body, token);

        public Task<TResp> GetAsync<TResp>(string path, string token = null)
            => SendAsync<TResp>("GET", path, null, token);

        private async Task<TResp> SendAsync<TResp>(string method, string path, object body, string token)
        {
            return await RetryPolicy.RunAsync(async () =>
            {
                var url = _cfg.apiBaseUrl.TrimEnd('/') + path;
                using var req = new UnityWebRequest(url, method);
                if (body != null)
                {
                    var json = JsonUtility.ToJson(body);
                    var bytes = Encoding.UTF8.GetBytes(json);
                    req.uploadHandler = new UploadHandlerRaw(bytes);
                    req.SetRequestHeader("Content-Type", "application/json");
                }
                req.downloadHandler = new DownloadHandlerBuffer();
                req.timeout = Mathf.Max(5, _cfg.actionTimeoutMs / 1000);
                if (!string.IsNullOrEmpty(token))
                    req.SetRequestHeader("Authorization", "Bearer " + token);
                req.SetRequestHeader("Accept", "application/json");

                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

#if UNITY_2020_2_OR_NEWER
                var failed = req.result != UnityWebRequest.Result.Success;
#else
                var failed = req.isHttpError || req.isNetworkError;
#endif
                if (failed)
                {
                    var msg = $"HTTP {req.responseCode} {req.error} body={req.downloadHandler.text}";
                    // 4xx: do not retry; 5xx and network errors: retryable.
                    if (req.responseCode >= 400 && req.responseCode < 500)
                        throw new ApiException(msg, retryable: false, status: (int)req.responseCode);
                    throw new ApiException(msg, retryable: true, status: (int)req.responseCode);
                }

                var text = req.downloadHandler.text;
                if (string.IsNullOrEmpty(text)) return default;
                return JsonUtility.FromJson<TResp>(text);
            }, attempts: 3);
        }
    }

    public class ApiException : Exception
    {
        public bool Retryable { get; }
        public int Status { get; }
        public ApiException(string msg, bool retryable, int status) : base(msg)
        { Retryable = retryable; Status = status; }
    }
}
