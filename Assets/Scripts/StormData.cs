using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StormData : MonoBehaviour
{
    public double flooding = 0; // Saturates wood, damages low-elevation electronics, and can collapse structures at high levels.
    public double lightning = 0; // Starts fires and can damage high-elevation electronics.
    public double wind = 0; // Can forcibly drop premature coconuts from trees, damage structures, and (possibly) damage thin and tall structures more severely
    public double electrical = 0; // This is more of a fantasy metric, an EMP or electromagnetic storm can damage electronics.

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public double getFloodingMetric()
    {
        return flooding;
    }

    public double getLightningMetric()
    {
        return lightning;
    }

    public double getWindMetric()
    {
        return wind;
    }

    public double getElectricalMetric()
    {
        return electrical;
    }
}
