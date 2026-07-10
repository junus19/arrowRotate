using UnityEngine;
using GameBrain.Utils;
using GameBrain.Casual;

namespace GameBrain.Casual
{
    public class ClayBoardObject : DamageableBoardObject
    {
        [SerializeField] GameObject clayVisual;
        [SerializeField] Transform clayParticleParent;

        //[SerializeField] List<Material> matList;
        //[SerializeField] MeshRenderer mesh;

        public override void LoadBoardObject(ICell cell, int _health)
        {
            base.LoadBoardObject(cell, 1);
            //mesh.material = matList[Random.Range(0, matList.Count)];
        }
        
        public override void TakeDamage(int damage)
        {
            if(!CanTakeDamage) return;
            health.Decrease(1000);

            if(health.Current <= 0)
            {
                clayParticleParent.SetParent(null);
                clayParticleParent.gameObject.SetActive(true);
                EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Clay));

                UnloadAndDestroyBoardObject();
                gameObject.SetActive(false);
            }
        }

        public override void UnloadAndDestroyBoardObject()
        {
            EventBus<BoardObjectBrokenEvent>.Raise(new BoardObjectBrokenEvent(BoardObjectType.Clay, transform.position));
            currentCell.UnLoadBoardObject();
            clayVisual.SetActive(false);
            // OnBoardObjectUnloaded.Invoke();
        }
    }
}
