using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Animator Mapping", menuName = "Animation/Animator State Clip Mapping")]
public class AnimatorStateClipMapping : ScriptableObject
{
    [System.Serializable]
    public class StateClipPair
    {
        public string stateName;
        public string clipName;
    }
    
    public List<StateClipPair> stateClipPairs = new List<StateClipPair>();
    private Dictionary<string, string> _stateToClipMap;
    private Dictionary<string, string> _clipToStateMap;
    
    public void Initialize()
    {
        _stateToClipMap = new Dictionary<string, string>();
        _clipToStateMap = new Dictionary<string, string>();
        
        foreach (var pair in stateClipPairs)
        {
            _stateToClipMap[pair.stateName] = pair.clipName;
            _clipToStateMap[pair.clipName] = pair.stateName;
        }
    }
    
    public void AddMapping(string stateName, string clipName)
    {
        var pair = new StateClipPair { stateName = stateName, clipName = clipName };
        stateClipPairs.Add(pair);
    }
    
    public string GetClipName(string stateName)
    {
        if (_stateToClipMap == null)
            Initialize();
            
        if (_stateToClipMap.TryGetValue(stateName, out string clipName))
            return clipName;
            
        return null;
    }
    
    public string GetStateName(string clipName)
    {
        if (_clipToStateMap == null)
            Initialize();
            
        if (_clipToStateMap.TryGetValue(clipName, out string stateName))
            return stateName;
            
        return null;
    }
}