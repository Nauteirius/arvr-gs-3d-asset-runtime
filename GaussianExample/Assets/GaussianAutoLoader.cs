using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using UnityEngine;
using GaussianSplatting.Runtime;   // needed for GaussianSplatRuntimeAssetCreator & GaussianSplatRuntimeAsset

public class GaussianAutoPlyRuntime : MonoBehaviour
{
    [Header("Watch settings")]
    public string watchFolder = @"C:\models\UnityGaussianSplatting\projects\Auto";
    [Tooltip("We only react to this file; sample/samply/etc. are ignored.")]
    public string fileNameToWatch = "output.ply";
    public bool processExistingOnStart = true;
    public bool importCamerasJson = false; // set true if cameras.json is present

    private FileSystemWatcher _watcher;
    private readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
    private GaussianSplatRenderer _renderer;
    private GaussianSplatRuntimeAssetCreator _creator;

    void Awake()
    {
        _renderer = GetComponent<GaussianSplatRenderer>() ?? gameObject.AddComponent<GaussianSplatRenderer>();
        _creator = new GaussianSplatRuntimeAssetCreator(); // uses default (Medium) quality internally
    }

    void OnEnable()
    {
        if (!Directory.Exists(watchFolder))
        {
            Debug.LogWarning($"[GS] Watch folder missing: {watchFolder}");
            return;
        }

        _watcher = new FileSystemWatcher(watchFolder, "*.ply")
        {
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite | NotifyFilters.CreationTime
        };
        _watcher.Created += OnFsEvent;
        _watcher.Changed += OnFsEvent;
        _watcher.Renamed += OnFsRenamed;
        _watcher.EnableRaisingEvents = true;

        if (processExistingOnStart)
            foreach (var p in Directory.EnumerateFiles(watchFolder, "*.ply"))
                EnqueueIfWanted(p);

        Debug.Log($"[GS] Watching for '{fileNameToWatch}' in: {watchFolder}");
    }

    void OnDisable()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    private void OnFsEvent(object s, FileSystemEventArgs e) => EnqueueIfWanted(e.FullPath);
    private void OnFsRenamed(object s, RenamedEventArgs e) => EnqueueIfWanted(e.FullPath);

    private void EnqueueIfWanted(string path)
    {
        if (!string.Equals(Path.GetFileName(path), fileNameToWatch, StringComparison.OrdinalIgnoreCase))
            return; // ignore sample/samply/etc.
        _queue.Enqueue(path);
    }

    void Update()
    {
        while (_queue.TryDequeue(out var plyPath))
            StartCoroutine(ImportWhenReady(plyPath));
    }

    IEnumerator ImportWhenReady(string plyPath)
    {
        // wait until file is fully written (size stable)
        const int stableChecks = 4;
        const float interval = 0.25f;
        long last = -1;
        int stable = 0;

        while (!File.Exists(plyPath))
            yield return new WaitForSeconds(interval);

        while (stable < stableChecks)
        {
            long len = -1;
            try { using (var fs = new FileStream(plyPath, FileMode.Open, FileAccess.Read, FileShare.Read)) len = fs.Length; }
            catch { len = -1; }

            if (len > 0 && len == last) stable++; else stable = 0;
            last = len;
            yield return new WaitForSeconds(interval);
        }

        // create runtime asset directly from the PLY
        GaussianSplatRuntimeAsset rtAsset = null;
        try
        {
            string name = Path.GetFileNameWithoutExtension(plyPath);
            rtAsset = _creator.CreateAsset(name, plyPath, importCamerasJson);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GS] CreateAsset failed for {plyPath}\n{ex}");
        }

        if (rtAsset == null)
        {
            Debug.LogError($"[GS] Creator returned null for {plyPath}");
            yield break;
        }

        // assign to renderer (different forks expose property vs field)
        var prop = typeof(GaussianSplatRenderer).GetProperty("RuntimeAsset",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (prop != null) prop.SetValue(_renderer, rtAsset);
        else
        {
            var field = typeof(GaussianSplatRenderer).GetField("m_RuntimeAsset",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (field != null) field.SetValue(_renderer, rtAsset);
            else Debug.LogWarning("[GS] Could not find RuntimeAsset property/field on GaussianSplatRenderer.");
        }

        Debug.Log($"[GS] Assigned runtime splat from {plyPath} (count: {rtAsset.splatCount})");
    }
}
