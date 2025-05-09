using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class StatsSender : MonoBehaviour
{
    [SerializeField] private string serverUrl = "http://localhost:5000";
    
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
    
    /// <summary>
    /// Gets all statistics from the server
    /// </summary>
    public void GetStats(Action<List<Stat>> onComplete, Action<string> onError = null)
    {
        StartCoroutine(GetStatsCoroutine(onComplete, onError));
    }
    
    /// <summary>
    /// Updates a specific statistic on the server
    /// </summary>
    /// <param name="statTitle">The title of the stat to update</param>
    /// <param name="amount">The amount to increment (can be negative)</param>
    public void UpdateStat(string statTitle, int amount, Action<Stat> onComplete = null, Action<string> onError = null)
    {
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
        // Create JSON payload
        Dictionary<string, object> payload = new Dictionary<string, object>
        {
            { "title", statTitle },
            { "amount", amount }
        };
        
        string jsonPayload = JsonUtility.ToJson(new Serializable<Dictionary<string, object>>(payload));
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        
        using (UnityWebRequest request = new UnityWebRequest($"{serverUrl}/update_stat", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
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
    
    // Helper class to serialize dictionary
    [Serializable]
    private class Serializable<T>
    {
        public T value;
        
        public Serializable(T value)
        {
            this.value = value;
        }
    }
    
    // Example usage methods
    public void ExampleUpdateLevees(int amount)
    {
        UpdateStat("Levees Built", amount, 
            (stat) => Debug.Log($"Updated: {stat.title} = {stat.value}"),
            (error) => Debug.LogError(error));
    }
    
    public void ExampleGetAllStats()
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
}