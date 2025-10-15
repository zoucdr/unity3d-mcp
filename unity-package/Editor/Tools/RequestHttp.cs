using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityMcp.Models;
using System.Linq;

namespace UnityMcp.Tools
{
    /// <summary>
    /// Handles network operations including HTTP requests, file downloads, and API calls.
    /// Corresponding method name: request_http
    /// </summary>
    [ToolName("request_http", "Network function")]
    public class RequestHttp : StateMethodBase
    {
        /// <summary>
        /// Create parameter key list supported by current method
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("action", "Operation type: get, post, put, delete, download, upload, ping, batch_download", false),
                new MethodKey("url", "Request URL address", false),
                new MethodKey("data", "Request data (used for POST/PUT, Json format)", true),
                new MethodKey("headers", "Request headers dictionary", true),
                new MethodKey("save_path", "Save path (used for download, relative to Assets or absolute path)", true),
                new MethodKey("file_path", "File path (used for upload)", true),
                new MethodKey("timeout", "Timeout (seconds), default 30 seconds", true),
                new MethodKey("method", "HTTP method (GET, POST, PUT, DELETE, etc.)", true),
                new MethodKey("content_type", "Content type, default application/json", true),
                new MethodKey("user_agent", "User agent string", true),
                new MethodKey("accept_certificates", "Whether to accept all certificates (for testing)", true),
                new MethodKey("follow_redirects", "Whether to follow redirects", true),
                new MethodKey("encoding", "Text encoding, default UTF-8", true),
                new MethodKey("form_data", "Form data (key-value pairs)", true),
                new MethodKey("query_params", "Query parameters (key-value pairs)", true),
                new MethodKey("auth_token", "Authentication token (Bearer Token)", true),
                new MethodKey("basic_auth", "Basic authentication (username:password)", true),
                new MethodKey("retry_count", "Retry count, default 0", true),
                new MethodKey("retry_delay", "Retry delay (seconds), default 1 second", true),
                new MethodKey("urls", "URL array (used for batch download)", true)
            };
        }

        /// <summary>
        /// Create state tree
        /// </summary>
        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("get", HandleGetRequest)
                    .Leaf("post", HandlePostRequest)
                    .Leaf("put", HandlePutRequest)
                    .Leaf("delete", HandleDeleteRequest)
                    .Leaf("download", HandleDownloadFile)
                    .Leaf("upload", HandleUploadFile)
                    .Leaf("ping", HandlePingRequest)
                    .Leaf("batch_download", HandleBatchDownload)
                .Build();
        }
        // --- Request processing method ---

        /// <summary>
        /// HandleGETRequest
        /// </summary>
        private object HandleGetRequest(StateTreeContext ctx)
        {
            LogInfo("[RequestHttp] Executing GET request");
            return ExecuteHttpRequest(ctx, "GET");
        }

        /// <summary>
        /// HandlePOSTRequest
        /// </summary>
        private object HandlePostRequest(StateTreeContext ctx)
        {
            LogInfo("[RequestHttp] Executing POST request");
            return ExecuteHttpRequest(ctx, "POST");
        }

        /// <summary>
        /// HandlePUTRequest
        /// </summary>
        private object HandlePutRequest(StateTreeContext ctx)
        {
            LogInfo("[RequestHttp] Executing PUT request");
            return ExecuteHttpRequest(ctx, "PUT");
        }

        /// <summary>
        /// HandleDELETERequest
        /// </summary>
        private object HandleDeleteRequest(StateTreeContext ctx)
        {
            LogInfo("[RequestHttp] Executing DELETE request");
            return ExecuteHttpRequest(ctx, "DELETE");
        }

        /// <summary>
        /// Handle file download（Default use coroutine）
        /// </summary>
        private object HandleDownloadFile(StateTreeContext ctx)
        {
            LogInfo("[RequestHttp] Starting coroutine file download");
            return DownloadFileCoroutine(ctx);
        }

        /// <summary>
        /// Download file with coroutine（Async）
        /// </summary>
        private object DownloadFileCoroutine(StateTreeContext ctx)
        {
            string url = ctx["url"]?.ToString();
            string savePath = ctx["save_path"]?.ToString();
            float timeout = ctx.TryGetValue("timeout", out int timeoutValue) ? timeoutValue : 60f;

            // Parameter validation
            if (string.IsNullOrEmpty(url))
            {
                ctx.Complete(Response.Error("URL parameter is required"));
                return null;
            }

            if (string.IsNullOrEmpty(savePath))
            {
                ctx.Complete(Response.Error("save_path parameter is required"));
                return null;
            }

            LogInfo($"[RequestHttp] Start coroutine download: {url}");

            // Start coroutine download
            CoroutineRunner.StartCoroutine(DownloadFileAsync(url, savePath, timeout, (result) =>
            {
                ctx.Complete(result);
            }, null, ctx));

            // Returnnull，Indicates asynchronous execution
            return null;
        }

        /// <summary>
        /// Handle file upload
        /// </summary>
        private object HandleUploadFile(StateTreeContext ctx)
        {
            LogInfo("[RequestHttp] Starting file upload");
            return UploadFile(ctx);
        }

        /// <summary>
        /// HandlePINGRequest
        /// </summary>
        private object HandlePingRequest(StateTreeContext ctx)
        {
            LogInfo("[RequestHttp] Executing PING request");
            return PingHost(ctx);
        }

        /// <summary>
        /// Handle batch download（Default use coroutine）
        /// </summary>
        private object HandleBatchDownload(StateTreeContext ctx)
        {
            LogInfo("[RequestHttp] Starting coroutine batch download");
            return BatchDownloadCoroutine(ctx);
        }

        /// <summary>
        /// Batch download via coroutine（Async）
        /// </summary>
        private object BatchDownloadCoroutine(StateTreeContext ctx)
        {
            var urlsToken = ctx["urls"];
            string saveDirectory = ctx["save_directory"]?.ToString() ?? ctx["save_path"]?.ToString();

            // Parameter validation
            if (urlsToken == null)
            {
                ctx.Complete(Response.Error("urls parameter is required"));
                return null;
            }

            if (string.IsNullOrEmpty(saveDirectory))
            {
                ctx.Complete(Response.Error("save_directory or save_path parameter is required"));
                return null;
            }

            LogInfo($"[RequestHttp] Start batch download coroutine");

            // Start batch download coroutine
            CoroutineRunner.StartCoroutine(BatchDownloadAsync(ctx, (result) =>
            {
                ctx.Complete(Json.FromObject(result));
            }));

            // Returnnull，Indicates asynchronous execution
            return null;
        }

        // --- Core implementation method ---

        /// <summary>
        /// ExecuteHTTPGeneral request method
        /// </summary>
        private object ExecuteHttpRequest(StateTreeContext ctx, string defaultMethod)
        {
            try
            {
                string url = ctx["url"]?.ToString();
                if (string.IsNullOrEmpty(url))
                {
                    return Response.Error("URL parameter is required");
                }

                // Parse parameters
                string method = ctx["method"]?.ToString() ?? defaultMethod;
                int timeout = ctx.TryGetValue<int>("timeout", out var timeoutValue) ? timeoutValue : 30;
                string contentType = ctx["content_type"]?.ToString() ?? "application/json";
                string userAgent = ctx["user_agent"]?.ToString() ?? "Unity-MCP-Network-Manager/1.0";
                bool acceptCertificates = ctx.TryGetValue<bool>("accept_certificates", out var acceptCerts) ? acceptCerts : true;
                bool followRedirects = ctx.TryGetValue<bool>("follow_redirects", out var followRedirectsValue) ? followRedirectsValue : true;
                int retryCount = ctx.TryGetValue<int>("retry_count", out var retryCountValue) ? retryCountValue : 0;
                float retryDelay = ctx.TryGetValue<float>("retry_delay", out var retryDelayValue) ? retryDelayValue : 1f;

                // Build completeURL（Include query parameters）
                string fullUrl = BuildUrlWithQueryParams(url, ctx["query_params"] as JsonClass);

                // Execute request（With retry mechanism）
                return ExecuteWithRetry(() => PerformHttpRequest(fullUrl, method, ctx, timeout, contentType, userAgent, acceptCertificates, followRedirects), retryCount, retryDelay);
            }
            catch (Exception e)
            {
                LogError($"[RequestHttp] HTTPRequest execution failed: {e.Message}");
                return Response.Error($"HTTP request execution failed: {e.Message}");
            }
        }

        /// <summary>
        /// Execute specificHTTPRequest
        /// </summary>
        private object PerformHttpRequest(string url, string method, StateTreeContext ctx, int timeout, string contentType, string userAgent, bool acceptCertificates, bool followRedirects)
        {
            UnityWebRequest request = null;

            try
            {
                // Create request according to method type
                switch (method.ToUpper())
                {
                    case "GET":
                        request = UnityWebRequest.Get(url);
                        break;
                    case "POST":
                        request = CreatePostRequest(url, ctx, contentType);
                        break;
                    case "PUT":
                        request = CreatePutRequest(url, ctx, contentType);
                        break;
                    case "DELETE":
                        request = UnityWebRequest.Delete(url);
                        break;
                    default:
                        request = new UnityWebRequest(url, method);
                        request.downloadHandler = new DownloadHandlerBuffer();
                        break;
                }

                // Configure request
                ConfigureRequest(request, ctx, timeout, userAgent, acceptCertificates, followRedirects);

                // Synchronously execute request
                var asyncOp = request.SendWebRequest();

                // Wait for request to complete（Sync wait，But do not block main thread）
                var startTime = EditorApplication.timeSinceStartup;
                while (!asyncOp.isDone && (EditorApplication.timeSinceStartup - startTime) < timeout)
                {
                    // LetUnityHandle other events，Do not block main thread
                    System.Threading.Thread.Yield();
                }

                // Check timeout
                if (!asyncOp.isDone)
                {
                    request.Abort();
                    return Response.Error($"Request timeout ({timeout} seconds)");
                }

                // Handle response
                return ProcessHttpResponse(request);
            }
            catch (Exception e)
            {
                LogError($"[RequestHttp] Request execution error: {e.Message}");
                return Response.Error($"Request execution error: {e.Message}");
            }
            finally
            {
                request?.Dispose();
            }
        }

        /// <summary>
        /// CreatePOSTRequest
        /// </summary>
        private UnityWebRequest CreatePostRequest(string url, StateTreeContext ctx, string contentType)
        {
            UnityWebRequest request;

            // Check if form data is used
            var formData = ctx["form_data"] as JsonClass;
            if (formData != null)
            {
                var form = new WWWForm();
                foreach (KeyValuePair<string, JsonNode> pair in formData.Properties())
                {
                    form.AddField(pair.Key, pair.Value.Value);
                }
                request = UnityWebRequest.Post(url, form);
            }
            else
            {
                // UseJSONData
                string jsonData = GetRequestBodyData(ctx);
                byte[] bodyRaw = string.IsNullOrEmpty(jsonData) ? null : Encoding.UTF8.GetBytes(jsonData);
                request = new UnityWebRequest(url, "POST");
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", contentType);
            }

            return request;
        }

        /// <summary>
        /// CreatePUTRequest
        /// </summary>
        private UnityWebRequest CreatePutRequest(string url, StateTreeContext ctx, string contentType)
        {
            string jsonData = GetRequestBodyData(ctx);
            byte[] bodyRaw = string.IsNullOrEmpty(jsonData) ? null : Encoding.UTF8.GetBytes(jsonData);
            var request = UnityWebRequest.Put(url, bodyRaw);
            request.SetRequestHeader("Content-Type", contentType);
            return request;
        }

        /// <summary>
        /// Configure request parameters
        /// </summary>
        private void ConfigureRequest(UnityWebRequest request, StateTreeContext ctx, int timeout, string userAgent, bool acceptCertificates, bool followRedirects)
        {
            // Basic config
            request.timeout = timeout;
            request.SetRequestHeader("User-Agent", userAgent);

            // Certificate validation
            if (acceptCertificates)
            {
                request.certificateHandler = new AcceptAllCertificatesHandler();
            }

            // Redirect
            request.redirectLimit = followRedirects ? 10 : 0;

            // Add custom request header
            var headers = ctx["headers"] as JsonClass;
            if (headers != null)
            {
                foreach (KeyValuePair<string, JsonNode> header in headers.Properties())
                {
                    request.SetRequestHeader(header.Key, header.Value.Value);
                }
            }

            // Authentication
            SetAuthentication(request, ctx);
        }

        /// <summary>
        /// Set authentication info
        /// </summary>
        private void SetAuthentication(UnityWebRequest request, StateTreeContext ctx)
        {
            // Bearer TokenAuthentication
            string authToken = ctx["auth_token"]?.ToString();
            if (!string.IsNullOrEmpty(authToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {authToken}");
            }

            // BasicAuthentication
            string basicAuth = ctx["basic_auth"]?.ToString();
            if (!string.IsNullOrEmpty(basicAuth))
            {
                string encodedAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes(basicAuth));
                request.SetRequestHeader("Authorization", $"Basic {encodedAuth}");
            }
        }

        /// <summary>
        /// Get request body data
        /// </summary>
        private string GetRequestBodyData(StateTreeContext ctx)
        {
            var data = ctx["data"];
            if (data == null) return null;

            if (data is JsonClass || data is JsonArray)
            {
                return data.ToString();
            }
            else
            {
                return data.ToString();
            }
        }

        /// <summary>
        /// Construct with query parameters includedURL
        /// </summary>
        private string BuildUrlWithQueryParams(string baseUrl, JsonClass queryParams)
        {
            if (queryParams == null || queryParams.Count == 0)
                return baseUrl;

            var sb = new StringBuilder(baseUrl);
            bool hasQuery = baseUrl.Contains("?");

            foreach (KeyValuePair<string, JsonNode> param in queryParams.Properties())
            {
                sb.Append(hasQuery ? "&" : "?");
                sb.Append(Uri.EscapeDataString(param.Key));
                sb.Append("=");
                sb.Append(Uri.EscapeDataString(param.Value.Value));
                hasQuery = true;
            }

            return sb.ToString();
        }

        /// <summary>
        /// HandleHTTPResponse
        /// </summary>
        private JsonClass ProcessHttpResponse(UnityWebRequest request, string filePath = null)
        {
            bool isSuccess = request.result == UnityWebRequest.Result.Success;
            long responseCode = request.responseCode;
            string responseText = request.downloadHandler?.text ?? "";
            byte[] responseData = request.downloadHandler?.data;

            // Get response header
            var responseHeaders = new Dictionary<string, string>();
            if (request.GetResponseHeaders() != null)
            {
                foreach (var header in request.GetResponseHeaders())
                {
                    responseHeaders[header.Key] = header.Value;
                }
            }

            // Determine if data is file or large content
            bool isFileData = IsFileData(responseHeaders, responseData);
            bool isLargeContent = responseData != null && responseData.Length > 1024 * 1024; // 1MBThreshold

            // Try parsingJSONResponse（Only for non-file and non-large content）
            object parsedData = null;
            if (!isFileData && !isLargeContent)
            {
                try
                {
                    if (!string.IsNullOrEmpty(responseText) && (responseText.Trim().StartsWith("{") || responseText.Trim().StartsWith("[")))
                    {
                        parsedData = JsonNode.Parse(responseText);
                    }
                    else
                    {
                        parsedData = responseText;
                    }
                }
                catch
                {
                    parsedData = responseText;
                }
            }

            var result = new
            {
                success = isSuccess,
                status_code = responseCode,
                headers = responseHeaders,
                data = isFileData || isLargeContent ? null : parsedData, // No return for file data or large contentdata
                raw_text = isFileData || isLargeContent ? null : responseText, // No return for file data or large contentraw_text
                error = isSuccess ? null : request.error,
                url = request.url,
                method = request.method,
                content_type = GetContentType(responseHeaders),
                content_length = responseData?.Length ?? 0,
                is_file_data = isFileData,
                is_large_content = isLargeContent,
                file_path = isFileData ? filePath : null, // If it is file data，Return file path
                file_name = isFileData && !string.IsNullOrEmpty(filePath) ? System.IO.Path.GetFileName(filePath) : null
            };

            if (isSuccess)
            {
                string message = isFileData ?
                    $"File download succeeded (status code: {responseCode})" :
                    $"HTTP request successful (status code: {responseCode})";
                return Response.Success(message, result);
            }
            else
            {
                return Response.Error($"HTTP request failed (status code: {responseCode}): {request.error}", result);
            }
        }

        /// <summary>
        /// Check if it is file data
        /// </summary>
        private bool IsFileData(Dictionary<string, string> headers, byte[] data)
        {
            // CheckContent-TypeIs file type
            if (headers.TryGetValue("Content-Type", out string contentType))
            {
                string lowerContentType = contentType.ToLower();

                // Image file
                if (lowerContentType.StartsWith("image/"))
                    return true;

                // Video file
                if (lowerContentType.StartsWith("video/"))
                    return true;

                // Audio file
                if (lowerContentType.StartsWith("audio/"))
                    return true;

                // Document file
                if (lowerContentType.Contains("application/pdf") ||
                    lowerContentType.Contains("application/msword") ||
                    lowerContentType.Contains("application/vnd.ms-excel") ||
                    lowerContentType.Contains("application/zip") ||
                    lowerContentType.Contains("application/x-rar") ||
                    lowerContentType.Contains("application/octet-stream"))
                    return true;

                // Font file
                if (lowerContentType.Contains("font/") || lowerContentType.Contains("application/font"))
                    return true;
            }

            // CheckContent-DispositionHeader
            if (headers.TryGetValue("Content-Disposition", out string contentDisposition))
            {
                if (contentDisposition.ToLower().Contains("attachment") ||
                    contentDisposition.ToLower().Contains("filename"))
                    return true;
            }

            // If data is not empty and no explicit textContent-Type，Could be binary file
            if (data != null && data.Length > 0)
            {
                if (!headers.TryGetValue("Content-Type", out string ct) ||
                    (!ct.ToLower().Contains("text/") &&
                     !ct.ToLower().Contains("application/json") &&
                     !ct.ToLower().Contains("application/xml")))
                {
                    // Check if data is binary（ContainnullByte）
                    for (int i = 0; i < Math.Min(data.Length, 1024); i++)
                    {
                        if (data[i] == 0)
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get content type
        /// </summary>
        private string GetContentType(Dictionary<string, string> headers)
        {
            return headers.TryGetValue("Content-Type", out string contentType) ? contentType : "unknown";
        }

        /// <summary>
        /// Coroutine-based file downloader
        /// </summary>
        /// <param name="url">DownloadURL</param>
        /// <param name="savePath">Save path</param>
        /// <param name="timeout">Timeout（Second）</param>
        /// <param name="callback">Completion callback</param>
        /// <param name="progressCallback">Progress callback，Optional</param>
        /// <param name="ctx">Context，For fetching extra config</param>
        /// <returns>Coroutine enumerator</returns>
        IEnumerator DownloadFileAsync(string url, string savePath, float timeout, Action<JsonClass> callback,
            Action<float> progressCallback = null, StateTreeContext ctx = null)
        {
            LogInfo($"[RequestHttp] Start coroutine download: {url}");

            // Parameter validation
            if (string.IsNullOrEmpty(url))
            {
                callback?.Invoke(Response.Error("URL parameter is required"));
                yield break;
            }

            if (string.IsNullOrEmpty(savePath))
            {
                callback?.Invoke(Response.Error("save_path parameter is required"));
                yield break;
            }

            // Normalize save path
            string fullSavePath = GetFullPath(savePath);
            string directory = Path.GetDirectoryName(fullSavePath);

            // Ensure directory exists
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception e)
            {
                callback?.Invoke(Response.Error($"Unable to create directory: {e.Message}"));
                yield break;
            }

            // Create download request
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = (int)timeout;

                // If has context，Use it to configure request
                if (ctx != null)
                {
                    ConfigureRequest(request, ctx, (int)timeout, "Unity-MCP-Downloader/1.0", true, true);
                }
                else
                {
                    request.SetRequestHeader("User-Agent", "Unity-MCP-Downloader/1.0");
                }

                // Send request
                var operation = request.SendWebRequest();
                float startTime = Time.realtimeSinceStartup;

                // Wait for download completion，Report progress simultaneously
                while (!operation.isDone)
                {
                    // Check timeout
                    float elapsedTime = Time.realtimeSinceStartup - startTime;
                    if (elapsedTime > timeout)
                    {
                        request.Abort();
                        callback?.Invoke(Response.Error($"Download timeout ({timeout} seconds)"));
                        yield break;
                    }

                    // Report download progress
                    if (progressCallback != null && request.downloadHandler != null)
                    {
                        float progress = operation.progress;
                        progressCallback(progress);
                    }

                    yield return null; // Wait one frame
                }

                // Check download result
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        // Save file
                        File.WriteAllBytes(fullSavePath, request.downloadHandler.data);

                        // If isUnityResource，RefreshAssetDatabase
                        if (fullSavePath.StartsWith(Application.dataPath))
                        {
                            string relativePath = "Assets" + fullSavePath.Substring(Application.dataPath.Length);
                            AssetDatabase.ImportAsset(relativePath);
                            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                        }

                        LogInfo($"[RequestHttp] Coroutine download succeeded: {Path.GetFileName(fullSavePath)}");

                        // Success callback - UseProcessHttpResponseObtain consistent file path return
                        var response = ProcessHttpResponse(request, fullSavePath);
                        callback?.Invoke(response);
                    }
                    catch (Exception e)
                    {
                        callback?.Invoke(Response.Error($"Failed to save file: {e.Message}"));
                    }
                }
                else
                {
                    // Download failed
                    string errorMessage = $"Download failed: {request.error}";
                    if (request.responseCode > 0)
                    {
                        errorMessage += $" (HTTP {request.responseCode})";
                    }

                    LogError($"[RequestHttp] {errorMessage}");
                    callback?.Invoke(Response.Error(errorMessage));
                }
            }
        }

        /// <summary>
        /// Batch download files by coroutine
        /// </summary>
        /// <param name="ctx">Context</param>
        /// <param name="callback">Completion callback</param>
        /// <returns>Coroutine enumerator</returns>
        IEnumerator BatchDownloadAsync(StateTreeContext ctx, Action<object> callback)
        {
            var urlsToken = ctx["urls"];
            string saveDirectory = ctx["save_directory"]?.ToString() ?? ctx["save_path"]?.ToString();
            float timeout = ctx.TryGetValue("timeout", out int timeoutValue) ? timeoutValue : 60f;

            // ParseURLArray
            string[] urls = null;
            if (urlsToken is JsonArray urlArray)
            {
                urls = urlArray.ToStringList().ToArray();
            }
            else if (urlsToken is string urlString)
            {
                urls = new[] { urlString };
            }

            if (urls == null || urls.Length == 0)
            {
                callback?.Invoke(Response.Error("urls array cannot be empty"));
                yield break;
            }

            // Normalize save directory
            string fullSaveDirectory = GetFullPath(saveDirectory);

            // Ensure directory exists
            try
            {
                if (!Directory.Exists(fullSaveDirectory))
                {
                    Directory.CreateDirectory(fullSaveDirectory);
                }
            }
            catch (Exception e)
            {
                callback?.Invoke(Response.Error($"Unable to create directory: {e.Message}"));
                yield break;
            }

            LogInfo($"[RequestHttp] Begin batch coroutine download {urls.Length} Files to: {fullSaveDirectory}");

            var downloadResults = new List<object>();
            var errors = new List<string>();
            int completed = 0;
            int total = urls.Length;

            // Concurrent download counter
            int activeDownloads = 0;
            const int maxConcurrentDownloads = 3; // Max concurrent downloads

            for (int i = 0; i < urls.Length; i++)
            {
                string url = urls[i];

                // Wait for idle slot
                while (activeDownloads >= maxConcurrentDownloads)
                {
                    yield return null;
                }

                // FromURLGenerate filename
                string fileName = GetFileNameFromUrl(url);
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = $"download_{i + 1}";
                }

                string filePath = Path.Combine(fullSaveDirectory, fileName);

                LogInfo($"[RequestHttp] Download file with coroutine {i + 1}/{total}: {url}");

                activeDownloads++;

                // Start a single file download coroutine
                CoroutineRunner.StartCoroutine(DownloadFileAsync(url, filePath, timeout, (result) =>
                {
                    lock (downloadResults)
                    {
                        bool downloadSuccess = IsSuccessResponse(result);

                        downloadResults.Add(new
                        {
                            url = url,
                            file_path = filePath,
                            success = downloadSuccess,
                            result = result
                        });

                        if (!downloadSuccess)
                        {
                            string errorMsg = $"File {Path.GetFileName(filePath)} Download failed";
                            if (result != null)
                            {
                                var errorProp = result.GetType().GetProperty("error");
                                if (errorProp != null)
                                {
                                    errorMsg += $": {errorProp.GetValue(result)}";
                                }
                            }
                            errors.Add(errorMsg);
                        }

                        completed++;
                        activeDownloads--;

                        LogInfo($"[RequestHttp] Batch download progress: {completed}/{total} ({(completed * 100f / total):F1}%)");
                    }
                }, null, ctx));
            }

            // Wait for all downloads to complete
            while (completed < total)
            {
                yield return null;
            }

            // Produce final result
            int successCount = downloadResults.Count(r =>
            {
                var successProp = r.GetType().GetProperty("success");
                return successProp != null && (bool)successProp.GetValue(r);
            });

            var finalResult = Response.Success(
                $"Batch download completed: {successCount}/{total} Files succeeded",
                new
                {
                    total_files = total,
                    successful = successCount,
                    failed = total - successCount,
                    save_directory = fullSaveDirectory,
                    results = downloadResults,
                    errors = errors
                }
            );

            LogInfo($"[RequestHttp] Batch coroutine download completed: {successCount}/{total} Success");
            callback?.Invoke(finalResult);
        }

        /// <summary>
        /// Download file
        /// </summary>
        private object DownloadFile(StateTreeContext ctx)
        {
            try
            {
                string url = ctx["url"]?.ToString();
                string savePath = ctx["save_path"]?.ToString();

                if (string.IsNullOrEmpty(url))
                {
                    return Response.Error("URL parameter is required");
                }

                if (string.IsNullOrEmpty(savePath))
                {
                    return Response.Error("save_path parameter is required");
                }

                // Normalize save path
                string fullSavePath = GetFullPath(savePath);
                string directory = Path.GetDirectoryName(fullSavePath);

                // Ensure directory exists
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                int timeout = ctx.TryGetValue<int>("timeout", out var timeoutValue) ? timeoutValue : 60;

                using (var request = UnityWebRequest.Get(url))
                {
                    request.timeout = timeout;
                    ConfigureRequest(request, ctx, timeout, "Unity-MCP-Downloader/1.0", true, true);

                    var asyncOp = request.SendWebRequest();

                    // Wait for download completion（Synchronous）
                    var startTime = EditorApplication.timeSinceStartup;
                    while (!asyncOp.isDone && (EditorApplication.timeSinceStartup - startTime) < timeout)
                    {
                        System.Threading.Thread.Yield(); // LetUnityHandle other events
                    }

                    if (!asyncOp.isDone)
                    {
                        request.Abort();
                        return Response.Error($"Download timeout ({timeout} seconds)");
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        // Save file
                        File.WriteAllBytes(fullSavePath, request.downloadHandler.data);

                        // If isUnityResource，RefreshAssetDatabase
                        if (fullSavePath.StartsWith(Application.dataPath))
                        {
                            string relativePath = "Assets" + fullSavePath.Substring(Application.dataPath.Length);
                            AssetDatabase.ImportAsset(relativePath);
                        }

                        // UseProcessHttpResponseObtain consistent file path return
                        return ProcessHttpResponse(request, fullSavePath);
                    }
                    else
                    {
                        return Response.Error($"Download failed: {request.error}");
                    }
                }
            }
            catch (Exception e)
            {
                LogError($"[RequestHttp] File download failed: {e.Message}");
                return Response.Error($"File download failed: {e.Message}");
            }
        }

        /// <summary>
        /// Upload file
        /// </summary>
        private object UploadFile(StateTreeContext ctx)
        {
            try
            {
                string url = ctx["url"]?.ToString();
                string filePath = ctx["file_path"]?.ToString();

                if (string.IsNullOrEmpty(url))
                {
                    return Response.Error("URL parameter is required");
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    return Response.Error("file_path parameter is required");
                }

                string fullFilePath = GetFullPath(filePath);
                if (!File.Exists(fullFilePath))
                {
                    return Response.Error($"File does not exist: {fullFilePath}");
                }

                byte[] fileData = File.ReadAllBytes(fullFilePath);
                string fileName = Path.GetFileName(fullFilePath);

                var form = new WWWForm();
                form.AddBinaryData("file", fileData, fileName);

                // Add extra form fields
                if (ctx["form_data"] as JsonClass != null)
                {
                    foreach (KeyValuePair<string, JsonNode> pair in (ctx["form_data"] as JsonClass).Properties())
                    {
                        form.AddField(pair.Key, pair.Value.Value);
                    }
                }

                int timeout = ctx.TryGetValue<int>("timeout", out var timeoutValue) ? timeoutValue : 60;

                using (var request = UnityWebRequest.Post(url, form))
                {
                    request.timeout = timeout;
                    ConfigureRequest(request, ctx, timeout, "Unity-MCP-Uploader/1.0", true, true);

                    var asyncOp = request.SendWebRequest();

                    // Wait for upload completion（Synchronous）
                    var startTime = EditorApplication.timeSinceStartup;
                    while (!asyncOp.isDone && (EditorApplication.timeSinceStartup - startTime) < timeout)
                    {
                        System.Threading.Thread.Yield(); // LetUnityHandle other events
                    }

                    if (!asyncOp.isDone)
                    {
                        request.Abort();
                        return Response.Error($"Upload timeout ({timeout} seconds)");
                    }

                    return ProcessHttpResponse(request);
                }
            }
            catch (Exception e)
            {
                LogError($"[RequestHttp] File upload failed: {e.Message}");
                return Response.Error($"File upload failed: {e.Message}");
            }
        }

        /// <summary>
        /// PINGHost
        /// </summary>
        private object PingHost(StateTreeContext ctx)
        {
            try
            {
                string url = ctx["url"]?.ToString();
                if (string.IsNullOrEmpty(url))
                {
                    return Response.Error("URL parameter is required");
                }

                // Simple connectivity test，UseHEADRequest
                using (var request = UnityWebRequest.Head(url))
                {
                    request.timeout = ctx.TryGetValue<int>("timeout", out var timeoutValue) ? timeoutValue : 10;

                    var startTime = EditorApplication.timeSinceStartup;
                    var asyncOp = request.SendWebRequest();

                    while (!asyncOp.isDone && (EditorApplication.timeSinceStartup - startTime) < request.timeout)
                    {
                        System.Threading.Thread.Yield(); // LetUnityHandle other events
                    }

                    if (!asyncOp.isDone)
                    {
                        request.Abort();
                        return Response.Error("Ping timeout");
                    }

                    double responseTime = (EditorApplication.timeSinceStartup - startTime) * 1000; // Convert to milliseconds

                    return Response.Success(
                        $"PingSuccess: {url}",
                        new
                        {
                            url = url,
                            status_code = request.responseCode,
                            response_time_ms = Math.Round(responseTime, 2),
                            success = request.result == UnityWebRequest.Result.Success
                        }
                    );
                }
            }
            catch (Exception e)
            {
                LogError($"[RequestHttp] PingFailed: {e.Message}");
                return Response.Error($"Ping failed: {e.Message}");
            }
        }

        // --- Auxiliary method ---

        /// <summary>
        /// Execution method with retry mechanism
        /// </summary>
        private object ExecuteWithRetry(Func<object> action, int retryCount, float retryDelay)
        {
            object lastResult = null;

            for (int i = 0; i <= retryCount; i++)
            {
                try
                {
                    lastResult = action();

                    // If response is successful，Directly return
                    if (IsSuccessResponse(lastResult))
                    {
                        return lastResult;
                    }

                    // If last attempt，Return result
                    if (i == retryCount)
                    {
                        return lastResult;
                    }

                    // Wait and retry（Synchronous）
                    if (retryDelay > 0)
                    {
                        var delayStart = EditorApplication.timeSinceStartup;
                        while ((EditorApplication.timeSinceStartup - delayStart) < retryDelay)
                        {
                            System.Threading.Thread.Yield();
                        }
                    }
                }
                catch (Exception e)
                {
                    lastResult = Models.Response.Error($"Retry {i + 1}/{retryCount + 1} failed: {e.Message}");

                    if (i == retryCount)
                    {
                        return lastResult;
                    }

                    if (retryDelay > 0)
                    {
                        var delayStart = EditorApplication.timeSinceStartup;
                        while ((EditorApplication.timeSinceStartup - delayStart) < retryDelay)
                        {
                            System.Threading.Thread.Yield();
                        }
                    }
                }
            }

            return lastResult;
        }

        /// <summary>
        /// Check if response indicates success
        /// </summary>
        private bool IsSuccessResponse(object response)
        {
            if (response == null) return false;

            // Check anonymous object with reflectionsuccessProperty
            var successProperty = response.GetType().GetProperty("success");
            if (successProperty != null && successProperty.PropertyType == typeof(bool))
            {
                return (bool)successProperty.GetValue(response);
            }

            return false;
        }

        /// <summary>
        /// Batch download files（Support real-time console refresh）
        /// </summary>
        private object BatchDownload(StateTreeContext ctx)
        {
            try
            {
                var urlsToken = ctx["urls"];
                string saveDirectory = ctx["save_directory"]?.ToString() ?? ctx["save_path"]?.ToString();

                if (urlsToken == null)
                {
                    return Response.Error("urls parameter is required");
                }

                // ParseURLArray
                string[] urls;
                if (urlsToken is JsonArray urlArray)
                {
                    urls = urlArray.ToStringList().ToArray();
                }
                else
                {
                    // If it is a string，Try splitting by comma
                    string urlString = urlsToken.ToString();
                    urls = urlString.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(url => url.Trim())
                                   .Where(url => !string.IsNullOrEmpty(url))
                                   .ToArray();
                }

                if (urls.Length == 0)
                {
                    return Response.Error("No valid URLs found");
                }

                if (string.IsNullOrEmpty(saveDirectory))
                {
                    saveDirectory = "Assets/Downloads";
                }

                // Ensure save directory exists
                string fullSaveDirectory = GetFullPath(saveDirectory);
                if (!Directory.Exists(fullSaveDirectory))
                {
                    Directory.CreateDirectory(fullSaveDirectory);
                }

                int timeout = ctx.TryGetValue<int>("timeout", out var timeoutValue) ? timeoutValue : 60;
                var downloadResults = new List<object>();
                var errors = new List<string>();

                LogInfo($"[RequestHttp] Start batch download {urls.Length} Files to {fullSaveDirectory}");

                // Download files one by one
                for (int i = 0; i < urls.Length; i++)
                {
                    string url = urls[i];
                    try
                    {
                        // FromURLGenerate filename
                        string fileName = GetFileNameFromUrl(url);
                        if (string.IsNullOrEmpty(fileName))
                        {
                            fileName = $"download_{i + 1}";
                        }

                        string filePath = Path.Combine(fullSaveDirectory, fileName);

                        // Create single download parameter
                        var downloadArgs = new JsonClass
                        {
                            ["url"] = url,
                            ["save_path"] = filePath,
                            ["timeout"] = timeout
                        };

                        // Copy authentication and request header parameters
                        if (ctx["headers"] as JsonClass != null) downloadArgs["headers"] = ctx.JsonData["headers"];
                        if (ctx["auth_token"]?.ToString() != null) downloadArgs["auth_token"] = ctx.JsonData["auth_token"];
                        if (ctx["basic_auth"]?.ToString() != null) downloadArgs["basic_auth"] = ctx.JsonData["basic_auth"];
                        if (ctx["user_agent"]?.ToString() != null) downloadArgs["user_agent"] = ctx.JsonData["user_agent"];

                        LogInfo($"[RequestHttp] Download file {i + 1}/{urls.Length}: {url}");

                        // Invoke single file download
                        var downloadContext = new StateTreeContext(downloadArgs);
                        var result = DownloadFile(downloadContext);

                        bool downloadSuccess = IsSuccessResponse(result);

                        downloadResults.Add(new
                        {
                            url = url,
                            file_path = filePath,
                            file_name = fileName,
                            success = downloadSuccess,
                            result = result
                        });

                        // After each file download，Immediately output log and refresh console
                        string statusMessage = downloadSuccess ? "✅ Success" : "❌ Failed";
                        LogInfo($"[RequestHttp] File {i + 1}/{urls.Length} {statusMessage}: {fileName}");

                        // Force refreshUnityConsole，Allow users to see progress in real time
                        System.Threading.Thread.Yield(); // LetUnityHave time to handle log display
                    }
                    catch (Exception e)
                    {
                        string error = $"Download failed {url}: {e.Message}";
                        errors.Add(error);
                        LogError($"[RequestHttp] {error}");

                        downloadResults.Add(new
                        {
                            url = url,
                            success = false,
                            error = e.Message
                        });
                    }
                }

                int successCount = downloadResults.Where(r =>
                {
                    var result = r.GetType().GetProperty("success")?.GetValue(r);
                    return result is bool success && success;
                }).Count();

                return Response.Success(
                    $"Batch download completed：Success {successCount}/{urls.Length} Files",
                    new
                    {
                        total_urls = urls.Length,
                        successful_downloads = successCount,
                        failed_downloads = urls.Length - successCount,
                        save_directory = fullSaveDirectory,
                        results = downloadResults,
                        errors = errors
                    }
                );
            }
            catch (Exception e)
            {
                LogError($"[RequestHttp] Batch download failed: {e.Message}");
                return Response.Error($"Batch download failed: {e.Message}");
            }
        }

        /// <summary>
        /// FromURLExtract filename
        /// </summary>
        private string GetFileNameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                string fileName = Path.GetFileName(uri.LocalPath);

                // If no extension，Try to get from query parameters
                if (string.IsNullOrEmpty(Path.GetExtension(fileName)))
                {
                    // Attempt fromURLInfer file type in
                    if (url.Contains("image") || url.Contains("img"))
                    {
                        fileName += ".jpg"; // Default image format
                    }
                    else
                    {
                        fileName += ".bin"; // Default binary file
                    }
                }

                // Ensure filename is valid
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }

                return fileName;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get full path
        /// </summary>
        private string GetFullPath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }

            // If path starts withAssetsPrefix，Use project path
            if (path.StartsWith("Assets"))
            {
                return Path.Combine(Directory.GetCurrentDirectory(), path);
            }

            // Otherwise relative toAssetsFolder
            return Path.Combine(Application.dataPath, path);
        }
    }

    /// <summary>
    /// Handler that accepts all certificates（For development test only）
    /// </summary>
    public class AcceptAllCertificatesHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}