using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DestructableStructure : MonoBehaviour
{
    [Header("Resilience Values")]
    [Tooltip("Resistance to flooding damage (0-1)")]
    [Range(0, 1)]
    public float floodingResilience = 0.5f;
    
    [Tooltip("Resistance to lightning damage (0-1)")]
    [Range(0, 1)]
    public float lightningResilience = 0.5f;
    
    [Tooltip("Resistance to wind damage (0-1)")]
    [Range(0, 1)]
    public float windResilience = 0.5f;
    
    [Tooltip("Resistance to electrical damage (0-1)")]
    [Range(0, 1)]
    public float electricalResilience = 0.5f;
    
    [Header("Structure Properties")]
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    public bool isDestroyed { get; private set; } = false;
    
    [Header("Visual Effects")]
    public GameObject destroyedVersionPrefab;
    public ParticleSystem damageEffect;
    public ParticleSystem destructionEffect;

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onDestroyed;
    
    private void Start()
    {
        currentHealth = maxHealth;
    }
    
    public void ApplyStormDamage(StormData storm)
    {
        if (isDestroyed)
            return;
            
        // Calculate damage from each storm attribute
        float floodingDamage = CalculateDamage(storm.GetFloodingMetric(), floodingResilience);
        float lightningDamage = CalculateDamage(storm.GetLightningMetric(), lightningResilience);
        float windDamage = CalculateDamage(storm.GetWindMetric(), windResilience);
        float electricalDamage = CalculateDamage(storm.GetElectricalMetric(), electricalResilience);
        
        // Apply total damage
        float totalDamage = floodingDamage + lightningDamage + windDamage + electricalDamage;
        ApplyDamage(totalDamage);
    }
    
    private float CalculateDamage(float stormValue, float resilience)
    {
        // Higher resilience means less damage
        return Mathf.Max(0, stormValue * (1 - resilience));
    }
    
    public void ApplyDamage(float damage)
    {
        if (damage <= 0 || isDestroyed)
            return;
            
        currentHealth -= damage;
        
        // Play damage effect
        if (damageEffect != null)
            damageEffect.Play();
            
        onDamaged?.Invoke();
        
        // Check if structure is destroyed
        if (currentHealth <= 0)
            DestroyStructure();
    }
    
    private void DestroyStructure()
    {
        if (isDestroyed)
            return;
            
        isDestroyed = true;

        if (gameObject.CompareTag("House") || gameObject.name.Contains("House"))
        {
            GameStats.Instance.IncrementStat("Houses Destroyed");
        }
        
        // Play destruction effect
        if (destructionEffect != null)
            destructionEffect.Play();
            
        // Spawn destroyed version
        if (destroyedVersionPrefab != null)
            Instantiate(destroyedVersionPrefab, transform.position, transform.rotation);
        
        // Disable renderers
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = false;
            
        // Disable colliders but keep trigger colliders
        foreach (Collider collider in GetComponentsInChildren<Collider>())
            if (!collider.isTrigger)
                collider.enabled = false;
        
        onDestroyed?.Invoke();
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }
}