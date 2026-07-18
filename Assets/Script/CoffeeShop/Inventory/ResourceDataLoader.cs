using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class ResourceJson
{
    public string id;
    public string displayName;
    public int startingAmount;
}

[Serializable]
public class ResourceDatabase
{
    public List<ResourceJson> resources = new();
}

[DisallowMultipleComponent]
public class ResourceDataLoader : MonoBehaviour
{
    [SerializeField] private bool autoLoadOnAwake = true;
    [SerializeField] private bool persistAcrossScenes = true;

    public static ResourceDataLoader Instance { get; private set; }
    public bool IsLoaded { get; private set; }
    public string LastError { get; private set; } = string.Empty;

    private static ResourceDatabase _cachedDatabase;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (persistAcrossScenes) { transform.SetParent(null); DontDestroyOnLoad(gameObject); }
        if (autoLoadOnAwake) Load();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Load()
    {
        LastError = string.Empty;
        var path = Path.Combine(Application.streamingAssetsPath, "Resources", "resources.json");
        if (!File.Exists(path)) { Fail("resources.json not found: " + path); return; }

        string json;
        try { json = File.ReadAllText(path); }
        catch (Exception e) { Fail("Cannot read resources.json: " + e.Message); return; }

        try { _cachedDatabase = JsonUtility.FromJson<ResourceDatabase>(json); }
        catch (Exception e) { Fail("Cannot parse resources.json: " + e.Message); return; }

        IsLoaded = true;
        Debug.Log($"[ResourceDataLoader] Loaded {_cachedDatabase.resources.Count} resources.");
    }

    public ResourceJson GetResource(string id)
    {
        if (_cachedDatabase == null) return null;
        foreach (var r in _cachedDatabase.resources)
            if (r.id == id) return r;
        return null;
    }

    public int GetStartingAmount(string id) => GetResource(id)?.startingAmount ?? 0;
    public string GetDisplayName(string id) => GetResource(id)?.displayName ?? id;

    public IEnumerable<ResourceJson> GetAllResources()
    {
        if (_cachedDatabase == null) return System.Array.Empty<ResourceJson>();
        return _cachedDatabase.resources;
    }

    /// <summary>静态方法：不依赖 Instance，直接从磁盘读取（供 InventorySystem.Awake 使用）</summary>
    public static ResourceDatabase LoadDirect()
    {
        if (_cachedDatabase != null) return _cachedDatabase;

        var path = Path.Combine(Application.streamingAssetsPath, "Resources", "resources.json");
        if (!File.Exists(path)) { Debug.LogError("[ResourceDataLoader] resources.json not found"); return null; }

        try
        {
            var json = File.ReadAllText(path);
            _cachedDatabase = JsonUtility.FromJson<ResourceDatabase>(json);
            return _cachedDatabase;
        }
        catch (Exception e)
        {
            Debug.LogError("[ResourceDataLoader] Cannot load resources.json: " + e.Message);
            return null;
        }
    }

    private void Fail(string msg)
    {
        LastError = msg;
        IsLoaded = false;
        Debug.LogError("[ResourceDataLoader] " + msg);
    }
}
