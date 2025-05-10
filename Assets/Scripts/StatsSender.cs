using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class StatsSender : MonoBehaviour
{
    [SerializeField] private string serverUrl = "http://localhost:5000";
    
    [Header("Settings")]
    [SerializeField] private bool statsEnabled = true;
    [Tooltip("When disabled, no stats are sent or retrieved from server")]
    
    [Serializable]
    public class Stat
    {
        public string title;
        public int value;
    }
    
    [Serializable]
    public class StatList
    {
        public List<Stat> stats;
    }
    
    // New class for the update payload
    [Serializable]
    private class StatUpdatePayload
    {
        public string title;
        public int amount;
    }
    
    /// <summary>
    /// Gets all statistics from the server
    /// </summary>
    public void GetStats(Action<List<Stat>> onComplete, Action<string> onError = null)
    {
        if (!statsEnabled)
        {
            Debug.Log("Stats are disabled. Skipping GetStats request.");
            onComplete?.Invoke(new List<Stat>());
            return;
        }
        
        StartCoroutine(GetStatsCoroutine(onComplete, onError));
    }
    
    /// <summary>
    /// Updates a specific statistic on the server
    /// </summary>
    /// <param name="statTitle">The title of the stat to update</param>
    /// <param name="amount">The amount to increment (can be negative)</param>
    public void UpdateStat(string statTitle, int amount, Action<Stat> onComplete = null, Action<string> onError = null)
    {
        if (!statsEnabled)
        {
            Debug.Log($"Stats are disabled. Skipping UpdateStat for '{statTitle}'");
            // Create a dummy stat for the callback to avoid null checks downstream
            if (onComplete != null)
            {
                Stat dummyStat = new Stat { title = statTitle, value = 0 };
                onComplete(dummyStat);
            }
            return;
        }
        
        StartCoroutine(UpdateStatCoroutine(statTitle, amount, onComplete, onError));
    }
    
    private IEnumerator GetStatsCoroutine(Action<List<Stat>> onComplete, Action<string> onError)
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/data"))
        {
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Error: {request.error}");
            }
            else
            {
                try
                {
                    List<Stat> stats = JsonUtility.FromJson<StatList>("{\"stats\":" + request.downloadHandler.text + "}").stats;
                    onComplete?.Invoke(stats);
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Parse error: {ex.Message}");
                }
            }
        }
    }
    
    private IEnumerator UpdateStatCoroutine(string statTitle, int amount, Action<Stat> onComplete, Action<string> onError)
    {
        // Create a proper serializable object
        StatUpdatePayload payload = new StatUpdatePayload
        {
            title = statTitle,
            amount = amount
        };
        
        string jsonPayload = JsonUtility.ToJson(payload);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        
        using (UnityWebRequest request = new UnityWebRequest($"{serverUrl}/update_stat", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Request failed: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                Debug.LogError($"Request URL: {request.url}");
                Debug.LogError($"Sent payload: {jsonPayload}");
                onError?.Invoke($"Error: {request.error}");
            }
            else
            {
                try
                {
                    Stat updatedStat = JsonUtility.FromJson<Stat>(request.downloadHandler.text);
                    onComplete?.Invoke(updatedStat);
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Parse error: {ex.Message}");
                }
            }
        }
    }
    
    // Example usage methods
    public void UpdateLevees(int amount)
    {
        UpdateStat("Levees Built", amount, 
            (stat) => Debug.Log($"Updated: {stat.title} = {stat.value}"),
            (error) => Debug.LogError(error));
    }

    public void GetAllStats()
    {
        GetStats(
            (stats) => {
                foreach (var stat in stats)
                {
                    Debug.Log($"{stat.title}: {stat.value}");
                }
            },
            (error) => Debug.LogError(error));
    }
    
    // Public getter/setter for statsEnabled
    public bool StatsEnabled 
    {
        get { return statsEnabled; }
        set { statsEnabled = value; }
    }
}