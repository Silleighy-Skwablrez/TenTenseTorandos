using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StormGenerator : MonoBehaviour
{
    [System.Serializable]
    public class StormEvent : UnityEvent<StormData> { }
    
    [Header("Storm Names")]
    [Tooltip("List of potential storm names")]
    public List<string> stormNames = new List<string> {
        "Alex", "Bonnie", "Colin", "Danielle", "Earl", "Fiona",
        "Gaston", "Hermine", "Ian", "Julia", "Karl", "Lisa",
        "Martin", "Nicole", "Owen", "Paula", "Richard", "Shary",
        "Tobias", "Virginie", "Walter"
    };
    
    [Header("Storm Generation Settings")]
    [Tooltip("Base intensity for round 1")]
    [Range(0.5f, 3f)]
    public float baseIntensity = 1f;
    
    [Tooltip("How much intensity increases per round")]
    [Range(0.1f, 2f)]
    public float intensityIncreasePerRound = 0.5f;
    
    [Tooltip("Randomness factor to apply to storm metrics")]
    [Range(0f, 1f)]
    public float randomnessFactor = 0.3f;
    
    [Header("Sensor Accuracy")]
    [Tooltip("Base inaccuracy for sensor readings (0 = perfect accuracy)")]
    [Range(0f, 1f)]
    public float baseInaccuracy = 0.4f;
    
    [Tooltip("How much inaccuracy decreases per storm intensity point")]
    [Range(0f, 0.2f)]
    public float inaccuracyDecreasePerIntensity = 0.05f;
    
    [Header("Storm Type Weights")]
    [Range(0f, 2f)]
    public float floodingWeight = 1f;
    
    [Range(0f, 2f)]
    public float lightningWeight = 1f;
    
    [Range(0f, 2f)]
    public float windWeight = 1f;
    
    [Range(0f, 2f)]
    public float electricalWeight = 1f;
    
    [Header("Events")]
    public StormEvent onStormGenerated;
    
    private StormData currentStorm;
    private StormData currentStormReadings; // What the player sees (with inaccuracy)
    private List<string> unusedStormNames;
    
    private void Awake()
    {
        // Copy storm names to working list
        unusedStormNames = new List<string>(stormNames);
    }
    
    public StormData GenerateStorm(int roundNumber)
    {
        // Ensure round number is valid
        roundNumber = Mathf.Clamp(roundNumber, 1, 10);
        
        // Calculate base intensity for this round
        float roundIntensity = baseIntensity + (intensityIncreasePerRound * (roundNumber - 1));
        
        // Create new storm data
        StormData storm = new StormData();
        
        // Generate storm metrics with weights and randomness
        storm.flooding = GenerateStormMetric(roundIntensity, floodingWeight);
        storm.lightning = GenerateStormMetric(roundIntensity, lightningWeight);
        storm.wind = GenerateStormMetric(roundIntensity, windWeight);
        storm.electrical = GenerateStormMetric(roundIntensity, electricalWeight);
        
        // Set storm direction
        storm.direction = Random.Range(0, 360);
        
        // Set air pressure (lower for stronger storms)
        storm.airPressure = Mathf.RoundToInt(1013 - (roundIntensity * 10));
        
        // Set storm category based on overall intensity
        float overallIntensity = storm.GetOverallIntensity();
        storm.category = Mathf.Clamp(Mathf.CeilToInt(overallIntensity / 2f), 1, 5);
        
        // Assign storm name
        storm.stormName = GetStormName();
        
        // Store current storm
        currentStorm = storm;
        
        // Generate sensor readings with inaccuracy
        GenerateSensorReadings(storm);
        
        // Trigger event
        onStormGenerated?.Invoke(storm);
        
        return storm;
    }
    
    private void GenerateSensorReadings(StormData actualStorm)
    {
        // Create a copy of the actual storm
        currentStormReadings = actualStorm.Clone();
        
        // Calculate inaccuracy based on storm intensity
        float overallIntensity = actualStorm.GetOverallIntensity();
        float inaccuracy = Mathf.Max(0, baseInaccuracy - (overallIntensity * inaccuracyDecreasePerIntensity));
        
        // Apply inaccuracy to each metric
        currentStormReadings.flooding = ApplyInaccuracy(actualStorm.flooding, inaccuracy);
        currentStormReadings.lightning = ApplyInaccuracy(actualStorm.lightning, inaccuracy);
        currentStormReadings.wind = ApplyInaccuracy(actualStorm.wind, inaccuracy);
        currentStormReadings.electrical = ApplyInaccuracy(actualStorm.electrical, inaccuracy);
        
        // Direction can be off by up to 45 degrees with full inaccuracy
        float directionOffset = Random.Range(-45f, 45f) * inaccuracy;
        currentStormReadings.direction = Mathf.RoundToInt((actualStorm.direction + directionOffset) % 360);
        if (currentStormReadings.direction < 0) currentStormReadings.direction += 360;
        
        // Air pressure can be off by up to 15 hPa with full inaccuracy
        float pressureOffset = Random.Range(-15f, 15f) * inaccuracy;
        currentStormReadings.airPressure = Mathf.RoundToInt(actualStorm.airPressure + pressureOffset);
        
        // Category might be estimated incorrectly
        if (Random.value < inaccuracy * 0.5f)
        {
            int categoryOffset = Random.value < 0.5f ? -1 : 1;
            currentStormReadings.category = Mathf.Clamp(actualStorm.category + categoryOffset, 1, 5);
        }
    }
    
    private float ApplyInaccuracy(float actualValue, float inaccuracy)
    {
        // Maximum range of inaccuracy increases with the base value
        float maxOffset = actualValue * 0.3f * inaccuracy;
        float offset = Random.Range(-maxOffset, maxOffset);
        return Mathf.Clamp(actualValue + offset, 0, 10);
    }
    
    private float GenerateStormMetric(float baseValue, float weight)
    {
        // Calculate random offset within randomness factor
        float randomOffset = Random.Range(-randomnessFactor, randomnessFactor) * baseValue;
        
        // Apply weight and randomness
        float value = baseValue * weight + randomOffset;
        
        // Ensure within valid range
        return Mathf.Clamp(value, 0, 10);
    }
    
    private string GetStormName()
    {
        // Replenish names if needed
        if (unusedStormNames.Count == 0)
            unusedStormNames = new List<string>(stormNames);
            
        // Get random name from list
        int nameIndex = Random.Range(0, unusedStormNames.Count);
        string name = unusedStormNames[nameIndex];
        
        // Remove from unused list
        unusedStormNames.RemoveAt(nameIndex);
        
        return name;
    }
    
    public StormData GetCurrentStorm()
    {
        return currentStorm;
    }
    
    public StormData GetSensorReadings()
    {
        return currentStormReadings;
    }
    
    // Method to apply storm damage to all destructible structures
    public void ApplyStormDamage()
    {
        if (currentStorm == null)
            return;
            
        DestructableStructure[] structures = FindObjectsOfType<DestructableStructure>();
        foreach (DestructableStructure structure in structures)
        {
            structure.ApplyStormDamage(currentStorm);
        }
    }
    
    // Returns damage statistics to display after the storm
    public StormDamageStatistics GetStormDamageStatistics()
    {
        StormDamageStatistics stats = new StormDamageStatistics();
        if (currentStorm == null)
            return stats;
        
        DestructableStructure[] structures = FindObjectsOfType<DestructableStructure>();
        int totalStructures = structures.Length;
        int destroyedStructures = 0;
        float totalDamage = 0;
        
        foreach (DestructableStructure structure in structures)
        {
            if (structure.isDestroyed)
                destroyedStructures++;
                
            totalDamage += (structure.maxHealth - structure.GetCurrentHealth());
        }
        
        stats.totalStructures = totalStructures;
        stats.destroyedStructures = destroyedStructures;
        stats.percentageDestroyed = totalStructures > 0 ? (float)destroyedStructures / totalStructures * 100 : 0;
        stats.totalDamage = totalDamage;
        stats.storm = currentStorm;
        
        return stats;
    }
}

// Class to hold storm damage statistics
[System.Serializable]
public class StormDamageStatistics
{
    public int totalStructures;
    public int destroyedStructures;
    public float percentageDestroyed;
    public float totalDamage;
    public StormData storm;
    
    public string GetSummary()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Storm {storm.GetStormName()} - Category {storm.GetCategory()}");
        sb.AppendLine($"Structures destroyed: {destroyedStructures}/{totalStructures} ({percentageDestroyed:F1}%)");
        
        // Add damage type information
        if (storm.GetFloodingMetric() > 3)
            sb.AppendLine($"Flooding Damage: Severe");
        else if (storm.GetFloodingMetric() > 1)
            sb.AppendLine($"Flooding Damage: Moderate");
            
        if (storm.GetLightningMetric() > 3)
            sb.AppendLine($"Lightning Strikes: Numerous");
        else if (storm.GetLightningMetric() > 1)
            sb.AppendLine($"Lightning Strikes: Scattered");
            
        if (storm.GetWindMetric() > 3)
            sb.AppendLine($"Wind Damage: Extensive");
        else if (storm.GetWindMetric() > 1)
            sb.AppendLine($"Wind Damage: Moderate");
            
        if (storm.GetElectricalMetric() > 3)
            sb.AppendLine($"Electrical Systems: Severely Damaged");
        else if (storm.GetElectricalMetric() > 1)
            sb.AppendLine($"Electrical Systems: Partially Damaged");
            
        return sb.ToString();
    }
}