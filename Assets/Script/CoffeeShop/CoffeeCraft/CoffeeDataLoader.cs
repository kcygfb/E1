using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class RecipeEntryJson
{
    public string resourceId;
    public int amount;
}

[Serializable]
public class CraftStepJson
{
    public string id;
    public string resourceId;
    public int amount;
}

[Serializable]
public class CoffeeDataJson
{
    public string coffeeId;
    public string coffeeName;
    public int sellPrice;
    public bool locked;
    public string unlockItemId;
    public int unlockAmount;
    public List<RecipeEntryJson> recipe = new();
    public List<CraftStepJson> steps = new();
}

[DisallowMultipleComponent]
public class CoffeeDataLoader : MonoBehaviour
{
    [SerializeField] private string relativeDirectory = "CoffeeData";
    [SerializeField] private bool autoLoadOnAwake = true;
    [SerializeField] private bool persistAcrossScenes = true;

    public static CoffeeDataLoader Instance { get; private set; }
    public bool IsLoaded { get; private set; }
    public string LastError { get; private set; } = string.Empty;

    private readonly Dictionary<string, CoffeeDataJson> _coffees = new(StringComparer.Ordinal);

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
        var dir = Path.Combine(Application.streamingAssetsPath, relativeDirectory);
        if (!Directory.Exists(dir)) { Fail("CoffeeData directory not found: " + dir); return; }

        var files = Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly);
        _coffees.Clear();

        foreach (var filePath in files)
        {
            string json;
            try { json = File.ReadAllText(filePath); }
            catch (Exception e) { Fail("Cannot read " + Path.GetFileName(filePath) + ": " + e.Message); return; }

            CoffeeDataJson data;
            try { data = JsonUtility.FromJson<CoffeeDataJson>(json); }
            catch (Exception e) { Fail("Cannot parse " + Path.GetFileName(filePath) + ": " + e.Message); return; }

            if (data == null || string.IsNullOrEmpty(data.coffeeId))
            { Fail("Invalid coffee data in " + Path.GetFileName(filePath)); return; }

            _coffees[data.coffeeId] = data;
        }

        IsLoaded = true;
        Debug.Log($"[CoffeeDataLoader] Loaded {_coffees.Count} coffees.");
    }

    public CoffeeDataJson GetCoffee(string id) =>
        string.IsNullOrEmpty(id) ? null : _coffees.GetValueOrDefault(id);

    public bool TryGetCoffee(string id, out CoffeeDataJson coffee) =>
        _coffees.TryGetValue(id, out coffee);

    public IEnumerable<CoffeeDataJson> GetAllCoffees() => _coffees.Values;

    private void Fail(string msg)
    {
        LastError = msg;
        IsLoaded = false;
        Debug.LogError("[CoffeeDataLoader] " + msg);
    }
}
