using CardSystem;
using DG.Tweening;

namespace DungeonGeneration
{
    public class ShopStageManager : StageBaseManager
    {
        protected override void Start()
        {
            base.Start();
            
            DOVirtual.DelayedCall(2f, () =>
            {
                ShopManager.Instance.OpenShop();
                
                onStageCompleted.Invoke();
            });
        }
    }
}