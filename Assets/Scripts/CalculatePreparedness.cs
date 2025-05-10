using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using skner.DualGrid;

[System.Serializable]
public class PreparationTileInfo
{
    public DualGridRuleTile tile;
    public string tileName;
    
    [Tooltip("How effective this tile is against flood damage (0-1)")]
    [Range(0f, 1f)]
    public float floodingProtection = 0.25f;
    
    [Tooltip("How effective this tile is against lightning damage (0-1)")]
    [Range(0f, 1f)]
    public float lightningProtection = 0.25f;
    
    [Tooltip("How effective this tile is against wind damage (0-1)")]
    [Range(0f, 1f)]
    public float windProtection = 0.25f;
    
    [Tooltip("How effective this tile is against electrical damage (0-1)")]
    [Range(0f, 1f)]
    public float electricalProtection = 0.25f;
    
    [Tooltip("How much this tile counts towards the preparation ratio")]
    public float preparationValue = 1.0f;
}

[System.Serializable]
public class StructureResistances
{
    public float floodingResilience;
    public float lightningResilience;
    public float windResilience;
    public float electricalResilience;
}

public class CalculatePreparedness : MonoBehaviour
{
    [Header("Preparation Tiles")]
    [Tooltip("Tiles that provide protection against storms")]
    public List<PreparationTileInfo> preparationTiles = new List<PreparationTileInfo>();
    
    [Header("Protection Settings")]
    [Tooltip("Search radius around structures for protection tiles")]
    public float protectionRadius = 5f;
    
    [Tooltip("Optimal ratio of preparation tiles per structure")]
    public float optimalPrepRatio = 4f;
    
    [Header("Structure Types")]
    [Tooltip("Additional preparation needed for large structures")]
    public float largeStructureMultiplier = 1.5f;
    
    [Header("Balance Settings")]
    [Tooltip("Maximum protection value per damage type (0-1)")]
    [Range(0f, 1f)]
    public float maxProtectionValue = 0.8f;
    
    [Tooltip("Apply diminishing returns to protection values")]
    public bool useDiminishingReturns = true;
    
    [Header("References")]
    public WorldHandler worldHandler;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    // Reference to all destructible structures
    private DestructableStructure[] structures;
    
    // Store preparedness values for each structure
    private Dictionary<DestructableStructure, float> preparednessValues = new Dictionary<DestructableStructure, float>();
    
    // Store original resistance values to reset after storm
    private Dictionary<DestructableStructure, StructureResistances> originalResistances = 
        new Dictionary<DestructableStructure, StructureResistances>();
    
    // Quick lookup for preparation tiles
    private Dictionary<DualGridRuleTile, PreparationTileInfo> prepTileLookup = 
        new Dictionary<DualGridRuleTile, PreparationTileInfo>();
    
    void Start()
    {
        if (worldHandler == null)
            worldHandler = FindObjectOfType<WorldHandler>();
            
        if (worldHandler == null)
            Debug.LogError("CalculatePreparedness: WorldHandler reference not set!");
            
        // Build the quick lookup dictionary
        foreach (var tileInfo in preparationTiles)
        {
            if (tileInfo.tile != null)
                prepTileLookup[tileInfo.tile] = tileInfo;
        }
    }
    
    // Call this before the storm hits to calculate preparedness
    public void UpdatePreparedness()
    {
        // Find all destructible structures in the scene
        structures = FindObjectsOfType<DestructableStructure>();
        preparednessValues.Clear();
        
        // First, store original values
        StoreOriginalResistances();
        
        foreach (var structure in structures)
        {
            // Calculate and apply preparedness for this structure
            CalculateAndApplyPreparedness(structure);
            
            if (showDebugInfo)
            {
                Debug.Log($"Structure {structure.name}: preparedness={preparednessValues[structure]:F2}, " +
                          $"flood res={structure.floodingResilience:F2}, wind res={structure.windResilience:F2}");
            }
        }
    }
    
    // Calculate preparedness and apply bonuses for a single structure
    private void CalculateAndApplyPreparedness(DestructableStructure structure)
    {
        Vector3 structurePosition = structure.transform.position;
        
        // Track protections by type
        float floodingProtection = 0f;
        float lightningProtection = 0f;
        float windProtection = 0f;
        float electricalProtection = 0f;
        float totalPrepValue = 0f;
        
        // Is this a large structure?
        bool isLargeStructure = structure.gameObject.CompareTag("House") || 
                              structure.name.Contains("House") || 
                              structure.name.Contains("Large");
        float sizeMultiplier = isLargeStructure ? largeStructureMultiplier : 1.0f;
        
        // Check grid around structure
        for (int x = -Mathf.RoundToInt(protectionRadius); x <= Mathf.RoundToInt(protectionRadius); x++)
        {
            for (int y = -Mathf.RoundToInt(protectionRadius); y <= Mathf.RoundToInt(protectionRadius); y++)
            {
                Vector3Int tilePos = new Vector3Int(
                    Mathf.RoundToInt(structurePosition.x) + x,
                    Mathf.RoundToInt(structurePosition.y) + y,
                    0
                );
                
                // Check if within actual radius
                if (Vector3.Distance(structurePosition, tilePos) <= protectionRadius)
                {
                    DualGridRuleTile tile = GetTileAtPosition(tilePos);
                    if (tile != null && prepTileLookup.TryGetValue(tile, out var tileInfo))
                    {
                        floodingProtection += tileInfo.floodingProtection;
                        lightningProtection += tileInfo.lightningProtection;
                        windProtection += tileInfo.windProtection;
                        electricalProtection += tileInfo.electricalProtection;
                        totalPrepValue += tileInfo.preparationValue;
                    }
                }
            }
        }
        
        // Calculate preparedness ratio, adjusted for structure size
        float adjustedOptimalRatio = optimalPrepRatio * sizeMultiplier;
        float overallPreparedness = Mathf.Min(1.0f, totalPrepValue / adjustedOptimalRatio);
        preparednessValues[structure] = overallPreparedness;
        
        // Apply diminishing returns if enabled
        if (useDiminishingReturns)
        {
            floodingProtection = ApplyDiminishingReturns(floodingProtection);
            lightningProtection = ApplyDiminishingReturns(lightningProtection);
            windProtection = ApplyDiminishingReturns(windProtection);
            electricalProtection = ApplyDiminishingReturns(electricalProtection);
        }
        
        // Cap protection at maximum value
        floodingProtection = Mathf.Min(floodingProtection, maxProtectionValue);
        lightningProtection = Mathf.Min(lightningProtection, maxProtectionValue);
        windProtection = Mathf.Min(windProtection, maxProtectionValue);
        electricalProtection = Mathf.Min(electricalProtection, maxProtectionValue);
        
        // Apply bonuses if original values were stored
        if (originalResistances.TryGetValue(structure, out var original))
        {
            // Apply protection bonus - add to the original value
            structure.floodingResilience = Mathf.Clamp01(original.floodingResilience + 
                                                      (floodingProtection * overallPreparedness));
            structure.lightningResilience = Mathf.Clamp01(original.lightningResilience + 
                                                        (lightningProtection * overallPreparedness));
            structure.windResilience = Mathf.Clamp01(original.windResilience + 
                                                   (windProtection * overallPreparedness));
            structure.electricalResilience = Mathf.Clamp01(original.electricalResilience + 
                                                         (electricalProtection * overallPreparedness));
        }
    }
    
    // Apply diminishing returns function to protection values
    private float ApplyDiminishingReturns(float value)
    {
        // Using smooth diminishing returns function: 1 - 1/(1+x)
        return value > 0 ? maxProtectionValue * (1f - 1f/(1f + value * 0.5f)) : 0f;
    }
    
    // Helper method to get tile at position - adapt this to your world handler
    private DualGridRuleTile GetTileAtPosition(Vector3Int position)
    {
        // Adjust this based on your actual WorldHandler implementation
        if (worldHandler != null)
        {
            // Assuming WorldHandler has this method
            return worldHandler.getTile(position);
        }
        return null;
    }
    
    // Store original resistance values for all structures
    private void StoreOriginalResistances()
    {
        originalResistances.Clear();
        
        foreach (var structure in structures)
        {
            originalResistances[structure] = new StructureResistances {
                floodingResilience = structure.floodingResilience,
                lightningResilience = structure.lightningResilience,
                windResilience = structure.windResilience,
                electricalResilience = structure.electricalResilience
            };
        }
    }
    
    // Reset all structures to their original resistance values
    public void ResetStructureResistances()
    {
        foreach (var pair in originalResistances)
        {
            DestructableStructure structure = pair.Key;
            StructureResistances original = pair.Value;
            
            if (structure != null) // Check if the structure still exists
            {
                structure.floodingResilience = original.floodingResilience;
                structure.lightningResilience = original.lightningResilience;
                structure.windResilience = original.windResilience;
                structure.electricalResilience = original.electricalResilience;
            }
        }
    }
    
    // Get overall preparedness percentage for reporting
    public float GetOverallPreparedness()
    {
        if (structures == null || structures.Length == 0)
            return 0f;
            
        float totalPreparedness = 0f;
        foreach (var structure in structures)
        {
            if (preparednessValues.TryGetValue(structure, out float prep))
                totalPreparedness += prep;
        }
        
        return totalPreparedness / structures.Length;
    }
    
    // Get user-friendly preparedness status
    public string GetPreparednessStatus()
    {
        float prep = GetOverallPreparedness() * 100f;
        
        if (prep >= 90f)
            return $"Excellent Preparation: {prep:F0}%";
        else if (prep >= 75f)
            return $"Good Preparation: {prep:F0}%";
        else if (prep >= 50f)
            return $"Adequate Preparation: {prep:F0}%";
        else if (prep >= 25f)
            return $"Poor Preparation: {prep:F0}%";
        else
            return $"Minimal Preparation: {prep:F0}%";
    }
}