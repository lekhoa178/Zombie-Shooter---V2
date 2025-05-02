using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using UnityEngine;
using UnityEngine.Serialization;
using UnityUtils;

public class CameraShakeManager : PersistentSingleton<CameraShakeManager>
{
    [SerializeField] private MMF_Player normalShakeFeedback;
    
    public void Shake(float duration = 0.1f, float amplitude = 0.1f, float frequency = 0.1f)
    {
        var cinemachineImpulse = normalShakeFeedback.GetFeedbackOfType<MMF_CinemachineImpulse>();
        
        if (cinemachineImpulse != null)
        {
            cinemachineImpulse.m_ImpulseDefinition.ImpulseDuration = duration;
            cinemachineImpulse.m_ImpulseDefinition.AmplitudeGain = amplitude;
            cinemachineImpulse.m_ImpulseDefinition.FrequencyGain = frequency;
        }
        
        PlayShakeFeedback();
    }
    
    private void PlayShakeFeedback()
    {
        normalShakeFeedback?.PlayFeedbacks();
    }
}
