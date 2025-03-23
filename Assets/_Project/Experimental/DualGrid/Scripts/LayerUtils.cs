using UnityEngine;

public static class LayerUtils
{
    public static readonly LayerMask GroundLayer = LayerMask.GetMask("Ground");

    public static bool IsInLayerMask(GameObject obj, LayerMask layerMask)
    {
        return (layerMask.value & (1 << obj.layer)) != 0;
    }

    // Create a LayerMask from layer names
    public static LayerMask CreateLayerMask(params string[] layerNames)
    {
        return LayerMask.GetMask(layerNames);
    }

    // Create a LayerMask from layer indices
    public static LayerMask CreateLayerMaskFromIndices(params int[] layerIndices)
    {
        int mask = 0;
        foreach (int layerIndex in layerIndices)
        {
            mask |= 1 << layerIndex;
        }
        return mask;
    }

    // Get the name of a layer by its index
    public static string GetLayerName(int layerIndex)
    {
        return LayerMask.LayerToName(layerIndex);
    }

    // Get the index of a layer by its name
    public static int GetLayerIndex(string layerName)
    {
        return LayerMask.NameToLayer(layerName);
    }
}
