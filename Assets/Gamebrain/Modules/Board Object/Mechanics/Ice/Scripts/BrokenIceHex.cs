using System.Collections.Generic;

//using DG.Tweening;
//using MoreMountains.Tools;
using UnityEngine;

namespace Gameplay
{
    public class BrokenIceHex : MonoBehaviour
    {/*
        private static GameObject _brokenIceHexHolder;
        private static List<GameObject> _brokenIceL1Pool = new List<GameObject>();
        private static List<GameObject> _brokenIceL2Pool = new List<GameObject>();
        private static List<GameObject> _brokenIceL3Pool = new List<GameObject>();
        
        private static bool _onGameplay = true;
        
        #region Public Methods

        public static bool OnGameplay
        {
            set
            {
                _onGameplay = value;
                ClearLists();
            }
        }
        
        public static void Create(int cost, Vector3 position)
        {
            var brokenPiece = GetSpawnedBrokenIce(cost);
            brokenPiece.transform.SetPositionAndRotation(position, Quaternion.identity);
            brokenPiece.SetActive(true);
        }

        #endregion

        #region Private Methods

        private static GameObject GetSpawnedBrokenIce(int cost)
        {
            GameObject brokenIce = null;
            var pool = GetPoolByCost(cost);

            if (!_brokenIceHexHolder)
            {
                _brokenIceHexHolder = new GameObject("BrokenIceHexHolder");
                ChangeLayerOfObject(_brokenIceHexHolder.transform);
            }
        
            if (pool.Count == 0 || !pool[0])
            {
                pool.Clear();
                brokenIce = Instantiate(GetObjectToSpawnByCost(cost), _brokenIceHexHolder.transform);
                ChangeLayerOfObject(brokenIce.transform);
            }
            else
            {
                brokenIce = pool[0];
                pool.RemoveAt(0);
            }
        
            DOVirtual.DelayedCall(3, () =>
            {
                if (brokenIce)
                {
                    brokenIce.gameObject.SetActive(false);
                    pool.Add(brokenIce);
                }
            });
        
            return brokenIce;
        }
        
        private static List<GameObject> GetPoolByCost(int cost)
        {
            switch (cost)
            {
                case 0:
                    return _brokenIceL1Pool;
                case 1:
                    return _brokenIceL2Pool;
                case 2:
                    return _brokenIceL3Pool;
                default:
                    return null;
            }
        }

        private static GameObject GetObjectToSpawnByCost(int cost)
        {
            switch (cost)
            {
                case 0:
                    return Resources.Load<GameObject>("BrokenIceHex-L1");
                case 1:
                    return Resources.Load<GameObject>("BrokenIceHex-L2");
                case 2:
                    return Resources.Load<GameObject>("BrokenIceHex-L3");
                default:
                    return null;
            }
        }
        
        private static void ChangeLayerOfObject(Transform obj)
        {
            if (!obj) return;

            if (_onGameplay)
            {
                obj.transform.ChangeLayersRecursively("Gameplay");
            }
            else
            {
                obj.transform.ChangeLayersRecursively("GameplayFtue");
            }
        }

        private static void ClearLists()
        {
            if (_brokenIceL1Pool != null)
            {
                for (int i = 0; i < _brokenIceL1Pool.Count; i++)
                {
                    if (_brokenIceL1Pool[i])
                        Destroy(_brokenIceL1Pool[i]);
                }
                
                _brokenIceL1Pool.Clear();
            }

            if (_brokenIceL2Pool != null)
            {
                for (int i = 0; i < _brokenIceL2Pool.Count; i++)
                {
                    if (_brokenIceL2Pool[i])
                        Destroy(_brokenIceL2Pool[i]);
                }
                
                _brokenIceL2Pool.Clear();
            }

            if (_brokenIceL3Pool != null)
            {
                for (int i = 0; i < _brokenIceL3Pool.Count; i++)
                {
                    if (_brokenIceL3Pool[i])
                        Destroy(_brokenIceL3Pool[i]);
                }
                
                _brokenIceL3Pool.Clear();
            }

            if (_brokenIceHexHolder)
            {
                Destroy(_brokenIceHexHolder);
                _brokenIceHexHolder = null;
            }
        }
        
        #endregion*/
    }
}