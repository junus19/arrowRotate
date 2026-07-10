using System.Collections;
using System.Collections.Generic;

//using DG.Tweening;
using UnityEngine;

namespace Gameplay
{
    public class IceHex : MonoBehaviour
    {/*
        [SerializeField] private IceHexPieces _singleIceHexObject;
        private List<IceHexPieces> _icePiecesList;
        private int _totalCost;
        // private int _totalPieces;
        [SerializeField] private GameObject ParticleSystem;
        [SerializeField] private GameplaySounds GameplaySounds;
        [SerializeField] private Sprite Icon;
        [SerializeField] private Color Color;
        private ICellHexListener _cellHexListener;

        #region Public Methods

        public void GenerateIce(List<Block> Blocks, int cost, ICellHexListener cellHexListener)
        {
            _cellHexListener = cellHexListener;
            float[] yrots = { 60, 120, 180, 240, 300 };
            float yRot = yrots[Random.Range(0, yrots.Length)];

            _icePiecesList = new List<IceHexPieces>();
            
            for (int i = 0; i < Blocks.Count; i++)
            {
                var block = Blocks[i];
                block.transform.localScale = new Vector3(0.9f, 0.8f, 0.9f);
                var ice = Instantiate(_singleIceHexObject, transform);
                ice.gameObject.SetActive(true);
                ice.transform.localPosition = block.transform.localPosition;
                ice.transform.localEulerAngles = new Vector3(0, yRot, 0);
                _icePiecesList.Add(ice);
            }
            
            #if UNITY_EDITOR
            Debug.Log(_icePiecesList[0].IcePieces.Length);
            # endif
            for (int i = 0; i < _icePiecesList.Count; i++)
                _icePiecesList[i].SetState(cost);
        }

        public void SetState(int value)
        {
            if (value <= 0)
            {
                Destroy(gameObject);
                return;
            }

            DOVirtual.DelayedCall(0.3f, () =>
            {
                for (int i = 0; i < _icePiecesList.Count; i++)
                {
                    _icePiecesList[i].SetState(value);
                }
                if(_icePiecesList != null || _icePiecesList.Count >0)
                    _icePiecesList[^1].PlayBrokenPieceAnimation(value);

                GameplaySounds.PlayIceBreakSound(value);
            });
        }

        public void Animate(List<Block> blocks, int cost)
        {
            StartCoroutine(AnimationCoroutine(blocks, cost));
        }
        
        #endregion

        #region IEnumerators
        
        private IEnumerator AnimationCoroutine(List<Block> blocks, int cost)
        {
            yield return new WaitForSeconds(0.3f);

            for (int i = 0; i < _icePiecesList.Count; i++)
            {
                _icePiecesList[i].SetState(0);
            }

            if(_icePiecesList != null && _icePiecesList.Count >0)
                _icePiecesList[^1].PlayBrokenPieceAnimation(cost);

            for (int i = 0; i < blocks.Count; i++)
                blocks[i].transform.localScale = Vector3.one;

            GameplaySounds.PlayIceBreakSound(cost);

            var position = transform.position;
            position.y += ((blocks.Count + 1) * 0.25f);
            BlockMergeParticle.Create(position, LevelGoalType.Ice, Color, Icon, () => _cellHexListener?.GoalUpdated(LevelGoalType.Ice));
            yield return new WaitForSeconds(1.5f);
            Destroy(gameObject);
        }

        #endregion
        */
    }
}