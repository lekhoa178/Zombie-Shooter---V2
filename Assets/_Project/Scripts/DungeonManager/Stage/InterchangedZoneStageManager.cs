namespace DungeonGeneration
{
    public class InterchangedZoneStageManager : StageBaseManager
    {
        protected void Awake()
        {
            DungeonManager.Instance.TransitionToNextZone();
        }
    }
}