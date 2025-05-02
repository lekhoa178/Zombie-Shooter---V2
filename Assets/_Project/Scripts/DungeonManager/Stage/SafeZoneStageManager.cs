using System;

namespace DungeonGeneration
{
    public class SafeZoneStageManager : StageBaseManager
    {
        protected void Awake()
        {
            DungeonManager.Instance.StartDungeonGeneration();
        }
    }
}