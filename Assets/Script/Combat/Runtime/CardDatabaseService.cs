using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace KiKs.Combat
{
    /// <summary>
    /// Loads StreamingAssets/CardData once. Selection UI and battle setup share this repository.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CardDatabaseService : MonoBehaviour
    {
        [SerializeField] private string relativeDirectory = "CardDataV2";
        [SerializeField] private bool autoLoadOnAwake = true;
        [SerializeField] private bool persistAcrossScenes = true;

        private bool _isLoading;

        public static CardDatabaseService Instance { get; private set; }
        public CardJsonRepository Repository { get; private set; }
        public bool IsLoaded => Repository != null;
        public bool IsLoading => _isLoading;
        public string LastError { get; private set; } = string.Empty;
        public event Action<CardJsonRepository> Loaded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (persistAcrossScenes) DontDestroyOnLoad(gameObject);
            if (autoLoadOnAwake) StartCoroutine(EnsureLoaded());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public IEnumerator EnsureLoaded()
        {
            if (IsLoaded) yield break;

            if (_isLoading)
            {
                while (_isLoading) yield return null;
                yield break;
            }

            _isLoading = true;
            LastError = string.Empty;

            string manifestText = null;
            string readError = null;
            yield return ReadText("manifest.json", text => manifestText = text, error => readError = error);
            if (!string.IsNullOrEmpty(readError))
            {
                Fail(readError);
                yield break;
            }

            IReadOnlyList<string> fileNames;
            try
            {
                fileNames = CardJsonRepository.ReadManifestFiles(manifestText);
            }
            catch (Exception exception)
            {
                Fail("Card manifest is invalid: " + exception.Message);
                yield break;
            }

            var texts = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var fileName in fileNames)
            {
                string fileText = null;
                readError = null;
                yield return ReadText(fileName, text => fileText = text, error => readError = error);
                if (!string.IsNullOrEmpty(readError))
                {
                    Fail(readError);
                    yield break;
                }

                texts.Add(fileName, fileText);
            }

            try
            {
                Repository = CardJsonRepository.Load(manifestText, fileName => texts[fileName]);
            }
            catch (Exception exception)
            {
                Fail("Card database validation failed: " + exception.Message);
                yield break;
            }

            _isLoading = false;
            Loaded?.Invoke(Repository);
            Debug.Log("Loaded " + Repository.Cards.Count + " cards from JSON.", this);
        }

        private IEnumerator ReadText(string fileName, Action<string> onSuccess, Action<string> onFailure)
        {
            var root = Application.streamingAssetsPath;
            var relative = string.IsNullOrWhiteSpace(relativeDirectory)
                ? fileName
                : relativeDirectory.Trim('/', '\\') + "/" + fileName;
            var path = root.TrimEnd('/', '\\') + "/" + relative;

            string uri;
            try
            {
                uri = path.Contains("://") ? path : new Uri(Path.GetFullPath(path)).AbsoluteUri;
            }
            catch (Exception exception)
            {
                onFailure("Cannot build card-data path for " + fileName + ": " + exception.Message);
                yield break;
            }

            using (var request = UnityWebRequest.Get(uri))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    onFailure("Cannot load " + fileName + ": " + request.error);
                    yield break;
                }

                onSuccess(request.downloadHandler.text);
            }
        }

        private void Fail(string message)
        {
            LastError = message;
            Repository = null;
            _isLoading = false;
            Debug.LogError(message, this);
        }
    }
}
