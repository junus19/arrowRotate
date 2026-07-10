using UnityEngine;
using DG.Tweening;
using System.Linq;
using GameBrain.Utils;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    public class FireCrackerBoardObject : DamageableBoardObject
    {
        [SerializeField] private FireCrackerRocket fireCrackerRocketPrefab;
        [SerializeField] private List<FireCrackerRocket> fireCrackerRockets = new List<FireCrackerRocket>();
        [SerializeField] private List<Transform> fireCrackerSpawnPoints;
        [SerializeField] private Transform fireCrackerRocketsParent;
        [SerializeField] private Transform fireCrackerRocketLAunchTransform;
        [SerializeField] private GameObject FireCrackerSmoke;

        public override void LoadBoardObject(ICell cell, int _health)
        {
            base.LoadBoardObject(cell, 4);
        }

        public override void TakeDamage(int damage)
        {
            if (!CanTakeDamage) return;
            if (health.Current <= 0) return;
            if (fireCrackerRockets.Count <= 0) return;
            ICell cell = FindTarget();
            if (cell == null) return;
            health.Decrease(damage);
            FireCrackerRocket rocket = fireCrackerRockets[0];
            fireCrackerRockets.Remove(rocket);
            FireCrackerSmoke.SetActive(true);
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.RocketLaunch));
            DOVirtual.DelayedCall(0.15f, () =>
            {
                //rocket.transform.position = fireCrackerRocketLAunchTransform.position;
                //rocket.transform.rotation = fireCrackerRocketLAunchTransform.rotation;
                rocket.Launch(cell, OnRocketExplode);
                transform.DOShakePosition(0.6f, 0.2f, 30);

                //if (health.Current <= 0)
                //    UnloadAndDestroyBoardObject();
            });
        }

        private ICell FindTarget()
        {
            if (_gameplayManager == null) return null;

            List<ICell> cellsWithDamageableBoardObject = _gameplayManager.CellList.Where(cell => cell.HasDamageableBoardObject() && cell.BoardObject is not FireCrackerBoardObject && cell != currentCell).ToList();
            if (cellsWithDamageableBoardObject.Count > 0)
                return cellsWithDamageableBoardObject[Random.Range(0, cellsWithDamageableBoardObject.Count)];

            List<ICell> cellsWithHex = _gameplayManager.CellList.Where(cell => cell.HexContainer.transform.childCount > 0 && cell != currentCell).ToList();
            if (cellsWithHex.Count > 0)
                return cellsWithHex[Random.Range(0, cellsWithHex.Count)];
            return null;
        }
        
        private void OnRocketExplode()
        {
            if (health.Current <= 0)
            {
                UnloadAndDestroyBoardObject();
            }
        }

        protected override void SetVisual()
        {
            base.SetVisual();
            foreach (Transform spawnPoint in fireCrackerSpawnPoints)
            {
                FireCrackerRocket rocket = Instantiate(fireCrackerRocketPrefab, spawnPoint.position, spawnPoint.rotation, fireCrackerRocketsParent);
                fireCrackerRockets.Add(rocket);
            }
        }

        public override void UnloadAndDestroyBoardObject()
        {
            EventBus<BoardObjectBrokenEvent>.Raise(new BoardObjectBrokenEvent(BoardObjectType.FireCracker, transform.position));
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.BoardObjectTookHit));
            currentCell.UnLoadBoardObject();
            GameObject bucket = fireCrackerRocketsParent.gameObject.GetComponentInChildren<MeshRenderer>().gameObject;
            bucket.transform.DOScale(Vector2.zero, 0.2f).SetEase(Ease.InBack);
            // OnBoardObjectUnloaded.Invoke();
        }
    }
}
