using UnityEngine;
using System;
using System.Collections.Generic;
using Flyweight;
using UnityUtils;

public class HitPredictionZoneManager : PersistentSingleton<HitPredictionZoneManager>
{
    [Header("Default Prefabs for Different Shapes")]
    public FlyweightSettings circlePrefabSettings;
    public FlyweightSettings squarePrefabSettings;
    public FlyweightSettings linePrefabSettings;

    private Dictionary<int, PredictionZone> activeZones = new Dictionary<int, PredictionZone>();
    private Dictionary<PredictionType, FlyweightSettings> defaultSettings = new Dictionary<PredictionType, FlyweightSettings>();
    private int idCounter = 0;

    protected override void Awake()
    {
        base.Awake();
        
        // Initialize default settings
        defaultSettings[PredictionType.Circle] = circlePrefabSettings;
        defaultSettings[PredictionType.Square] = squarePrefabSettings;
        defaultSettings[PredictionType.Line] = linePrefabSettings;
    }

    /// <summary>
    /// Creates or updates a prediction zone.
    /// </summary>
    public int CreateOrUpdateZone(int id, PredictionType type, Transform target, Vector3 offset, Vector3 scale, bool hasProgression = false, Action<float> onProgressUpdated = null, FlyweightSettings overrideSettings = null)
    {
        if (id == -1)
            id = idCounter++;

        if (!activeZones.ContainsKey(id))
        {
            FlyweightSettings settingsToUse = overrideSettings ?? GetDefaultSettings(type);
            if (settingsToUse == null)
            {
                Debug.LogError($"No settings assigned for PredictionType: {type}");
                return -1;
            }

            // Use the Flyweight Factory to create the visual
            Flyweight.Flyweight flyweight = Flyweight.FlyweightFactory.Spawn(settingsToUse);
            if (flyweight == null)
            {
                Debug.LogError($"Failed to spawn flyweight for PredictionType: {type}");
                return -1;
            }

            GameObject newZone = flyweight.gameObject;
            activeZones[id] = new PredictionZone(newZone, type, target, offset, scale, hasProgression, onProgressUpdated);
            
            // Store the flyweight reference in the PredictionZone's visual gameObject for later use
            newZone.AddComponent<FlyweightReference>().Initialize(flyweight);
        }

        // Update properties
        var zone = activeZones[id];
        zone.UpdateZone(target, offset, scale, hasProgression, onProgressUpdated);

        return id;
    }

    /// <summary>
    /// Updates the progression state of a specific zone.
    /// </summary>
    public void UpdateZoneProgression(int id, float progression)
    {
        if (activeZones.ContainsKey(id))
        {
            activeZones[id].SetProgression(progression);
        }
    }

    void Update()
    {
        foreach (var zone in activeZones.Values)
        {
            zone.UpdateVisual();
        }
    }

    private FlyweightSettings GetDefaultSettings(PredictionType type)
    {
        if (defaultSettings.TryGetValue(type, out FlyweightSettings settings))
        {
            return settings;
        }
        return null;
    }

    public void RemoveZone(int id)
    {
        if (activeZones.ContainsKey(id))
        {
            var zone = activeZones[id];
            if (zone != null && zone.visual != null)
            {
                zone.ResetZone();
                
                // Get the flyweight reference and return it to the pool
                var flyweightRef = zone.visual.GetComponent<FlyweightReference>();
                if (flyweightRef && flyweightRef.flyweight)
                {
                    try
                    {
                        // Set the reference to null to prevent double returns
                        var flyweight = flyweightRef.flyweight;
                        flyweightRef.flyweight = null;
                        
                        // Return to pool
                        Flyweight.FlyweightFactory.ReturnToPool(flyweight);
                    }
                    catch (System.InvalidOperationException e)
                    {
                        // Log the error but don't crash
                        Debug.LogWarning($"Failed to return flyweight to pool: {e.Message}");
                    }
                }
                else
                {
                    // Fallback if for some reason we don't have the flyweight reference
                    Destroy(zone.visual);
                }
            }
            // Remove from active zones dictionary
            activeZones.Remove(id);
        }
    }
    
    // Helper class to store flyweight reference
    private class FlyweightReference : MonoBehaviour
    {
        public Flyweight.Flyweight flyweight;
        
        public void Initialize(Flyweight.Flyweight flyweight)
        {
            this.flyweight = flyweight;
        }
    }
}
