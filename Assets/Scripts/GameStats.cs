using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameStats : MonoBehaviour
{
    private static GameStats _instance;
    public static GameStats Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject statsObj = new GameObject("GameStats");
                _instance = statsObj.AddComponent<GameStats>();
                DontDestroyOnLoad(statsObj);
            }
            return _instance;
        }
    }

    [SerializeField] private StatsSender statsSender;
    
    // Define all metrics for tracking locally (server has its own storage)
    private Dictionary<string, int> localStats = new Dictionary<string, int>
    {
        { "Levees Built", 0 },
        { "Houses Destroyed", 0 },
        { "Total Days Survived", 0 },
        { "Islands Created", 0 }
    };
    
    // Local event system
    public event Action<string, int> OnStatUpdated;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Find StatsSender if not assigned
        if (statsSender == null)
        {
            statsSender = FindObjectOfType<StatsSender>();
            if (statsSender == null)
            {
                Debug.LogWarning("StatsSender not found. Stats won't be sent to server.");
            }
        }
    }

    // Increment a stat and notify listeners
    public void IncrementStat(string statName, int amount = 1)
    {
        if (!localStats.ContainsKey(statName))
        {
            Debug.LogWarning($"Local tracking for stat '{statName}' doesn't exist. Creating it.");
            localStats[statName] = 0;
        }
        
        // Only proceed if amount is non-zero
        if (amount == 0) return;
        
        // Update local tracking
        localStats[statName] += amount;
        OnStatUpdated?.Invoke(statName, localStats[statName]);
        
        // Send to server
        if (statsSender != null)
        {
            statsSender.UpdateStat(statName, amount, 
                (stat) => Debug.Log($"[Stats] Updated: {stat.title} = {stat.value}"),
                (error) => Debug.LogError($"[Stats] Error: {error}")
            );
        }
    }

    // Get local stat value (useful for UI)
    public int GetLocalStatValue(string statName)
    {
        if (localStats.TryGetValue(statName, out int value))
        {
            return value;
        }
        return 0;
    }
    
    // Refresh stats from server
    public void RefreshStatsFromServer()
    {
        if (statsSender == null) return;
        
        statsSender.GetStats(
            (stats) => {
                Debug.Log($"[Stats] Refreshed {stats.Count} stats from server");
                // If needed, we can update local copies here
            },
            (error) => Debug.LogError($"[Stats] Failed to refresh stats: {error}")
        );
    }
}