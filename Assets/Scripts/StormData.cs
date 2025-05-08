using System;
using UnityEngine;

[Serializable]
public class StormData
{
    [Header("Storm Metrics")]
    [Tooltip("Causes water damage, saturates wood, damages low-elevation electronics")]
    [Range(0, 10)]
    public float flooding = 0;
    
    [Tooltip("Starts fires and damages high-elevation electronics")]
    [Range(0, 10)]
    public float lightning = 0;
    
    [Tooltip("Damages structures based on height and size")]
    [Range(0, 10)]
    public float wind = 0;
    
    [Tooltip("EMP-like effect that damages electronics")]
    [Range(0, 10)]
    public float electrical = 0;
    
    [Header("Storm Properties")]
    [Tooltip("Direction in degrees (0-359)")]
    [Range(0, 359)]
    public int direction = 0;
    
    [Tooltip("Air pressure in hPa")]
    [Range(900, 1100)]
    public int airPressure = 1013; // Standard atmospheric pressure
    
    [Tooltip("Name of the storm")]
    public string stormName = "Unnamed Storm";
    
    [Tooltip("Category of the storm (1-5)")]
    [Range(1, 5)]
    public int category = 1;
    
    public float GetFloodingMetric() => flooding;
    public float GetLightningMetric() => lightning;
    public float GetWindMetric() => wind;
    public float GetElectricalMetric() => electrical;
    public int GetDirection() => direction;
    public int GetAirPressure() => airPressure;
    public string GetStormName() => stormName;
    public int GetCategory() => category;
    
    public float GetOverallIntensity()
    {
        return (flooding + lightning + wind + electrical) / 4f;
    }
    
    public StormData Clone()
    {
        return new StormData
        {
            flooding = this.flooding,
            lightning = this.lightning,
            wind = this.wind,
            electrical = this.electrical,
            direction = this.direction,
            airPressure = this.airPressure,
            stormName = this.stormName,
            category = this.category
        };
    }
}