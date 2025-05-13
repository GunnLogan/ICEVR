using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using ShapesXR.Editor.Editor;
using ShapesXR.Import.Resources;
using ShapesXR.Import.Settings;
using UnityEditor;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace ShapesXR.Import
{
    public static class SpaceImporter
    {
        private const string ShapesXRSendAnalyticsKey = "shapes_xr_plugin_send_analytics";
            
        private static bool _messagePackInitialized;
        private const int RequestTimeout = 5 * 1000;
        
        public static string? ErrorMessage { get; private set; }

        public static bool SendAnalytics
        {
            get => EditorPrefs.GetBool(ShapesXRSendAnalyticsKey, true);
            set => EditorPrefs.SetBool(ShapesXRSendAnalyticsKey, value);
        }

        public static void ImportSpace(string accessCode)
        {
            var importTimer = Stopwatch.StartNew();
            ErrorMessage = string.Empty;

            if (!CheckSettingsValidness())
            {
                Analytics.SendEvent(EventStatus.invalid_import_settings);
                return;
            }

            var spaceUrl = GetSpaceUrl(accessCode);

            if (string.IsNullOrEmpty(spaceUrl))
            {
                Analytics.SendEvent(EventStatus.cannot_get_space_url);
                ErrorMessage = "The code is incorrect. If you're sure you're entering a valid code, contact us at hey@shapesxr.com";
                Debug.LogError(ErrorMessage);
                return;
            }

            var spaceInfoData = DownloadSpace(spaceUrl!);
            if (spaceInfoData == null)
            {
                Analytics.SendEvent(EventStatus.cannot_get_space_data);
                ErrorMessage = $"Error while downloading space. If you see continue to see this, contact us at hey@shapesxr.com";
                Debug.LogError(ErrorMessage);
                return;
            }

            var importError = ShapesXRExternal.ImportSpace(spaceInfoData.Value, out var space);
            if (importError != null)
            {
                Analytics.SendEvent(EventStatus.cannot_deserialize_space_data);
                ErrorMessage = importError;
                Debug.LogError(ErrorMessage);
                
                return;
            }

            var spaceDescriptor = SpaceDescriptor.Create(space!, accessCode);
            spaceDescriptor.ReadObjectProperties();
            AssetDatabase.Refresh();
            
            // For some reason, only in unity 2020 parallel downloads do not work as intended.
            // They cause some race conditions where they should not and generally are very inconsistent.
#if ENABLE_PARALLEL_DOWNLOADS
            ResourceDownloader.DownloadAllResourcesInParallel(spaceDescriptor);
#else
            ResourceDownloader.DownloadAllResources(spaceDescriptor);
#endif
            ResourcePostprocessor.PostProcessAllResources(spaceDescriptor);
            _ = new ObjectSpawner(spaceDescriptor);
            Selection.activeObject = spaceDescriptor.gameObject;
            Debug.Log($"Space import finished in: {importTimer.Elapsed.TotalSeconds}");
            Analytics.SendEvent(EventStatus.success, SendAnalytics ? spaceDescriptor.AccessCode : string.Empty, importTimer.Elapsed.TotalSeconds);
        }

        private static bool CheckSettingsValidness()
        {
            ImportSettingsProvider.ImportSettings = ImportSettings.Instance;

#if UNITY_EDITOR
            ImportCore.Utils.UnityEditor.Wrapper = new UnityEditorWrapper();
#endif
            
            if (PathUtils.PathContainsInvalidCharacters(ImportSettingsProvider.ImportSettings.ImportedDataDirectory))
            {
                ErrorMessage = $"Incorrect Imported Data Directory path: {ImportSettingsProvider.ImportSettings.ImportedDataDirectory}";
                Debug.LogError(ErrorMessage);
                return false;
            }

            if (ImportSettingsProvider.ImportSettings.MaterialMap == null)
            {
                ErrorMessage = "Material Map not found. Please specify one in ShapesXR Importer settings";
                Debug.LogError(ErrorMessage);
                return false;
            }
            
            return true;
        }
        
        private static string? GetSpaceUrl(string spaceCode)
        {
            var requestUrl = $"{Configuration.ServerURL}accesscode/space-url/{spaceCode}";
            var webRequest = (HttpWebRequest)WebRequest.Create(requestUrl);
            webRequest.Method = "GET";
            webRequest.Timeout = RequestTimeout;
            
            try
            {
                var response = (HttpWebResponse)webRequest.GetResponse();
                var reader = new StreamReader(response.GetResponseStream());

                return reader.ReadToEnd().Trim('"');
            }
            catch (WebException e)
            {
                Debug.LogError($"Error getting space Url: {e.Message}");
                return null;
            }
        }

        private static ReadOnlyMemory<byte>? DownloadSpace(string spaceUrl)
        {
            var downloadHandler = new DownloadHandlerBuffer();
            using var webRequest = new UnityWebRequest(spaceUrl, UnityWebRequest.kHttpVerbGET, downloadHandler, null);

            ulong? contentLength = null;

            var operation = webRequest.SendWebRequest();
            while (!operation.isDone)
            {
                if (!contentLength.HasValue && ulong.TryParse(webRequest.GetResponseHeader("Content-Length"), out var length))
                    contentLength = length;
            }

            var statusCode = (HttpStatusCode)webRequest.responseCode;
            ArraySegment<byte> received = downloadHandler.data;
            if (webRequest.result == UnityWebRequest.Result.Success && statusCode == HttpStatusCode.OK && received.Count != 0)
                return received;

            Debug.LogWarning($"Failed to download space: Result - {webRequest.result}, Status Code - {statusCode}, Bytes received - {received.Count}");
            return null;
        }
    }
}