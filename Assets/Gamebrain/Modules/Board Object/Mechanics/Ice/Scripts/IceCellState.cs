using System.Collections.Generic;

//using DG.Tweening;
using UnityEngine;

namespace Gameplay
{
    public class IceCellState : MonoBehaviour//ACellState
    {/*
        private IceHex _iceHex;
        private List<Block> _blocks;

        public IceCellState(ICellStateContext cell, CellState cellState, LevelGoalTracker goalTracker, List<Block> blocks, CellStateProperties stateProperties)
            : base(cell, cellState, goalTracker, stateProperties)
        {
            _blocks = blocks;
            
            RegisterListeners();
            Init();
        }

        #region Public Methods

        public override void OnDestroy()
        {
            base.OnDestroy();
            UnregisterListeners();
            
            if (_iceHex)
                Object.Destroy(_iceHex.gameObject);
        }

        public override void PowerUpUsed()
        {
            CollectIcePiece();
        }

        public override void FtueUse(Cell currentCell, Cell cellToInteract)
        {
            UnregisterListeners();
            CollectIcePiece();
        }

        #endregion

        #region Private Methods

        private void Init()
        {
            _cell.SetCellMeshState(true, false);

            if (_cell.GetCost() <= 0)
            {
                _cell.ChangeState(CellState.Open);
                return;
            }

            _iceHex = Object.Instantiate(_stateProperties.HexPrefab as IceHex, _cell.GetTransform());
            _iceHex.GenerateIce(_blocks, _cell.GetCost(), this);
        }

        private void OnCellMerge(ICellStateContext cell, HexType hexType, int count)
        {
            if (!_cell.IsNeighbour(cell)) return;
            
            CollectIcePiece();
        }

        private void CollectIcePiece()
        {
            if (_cell.GetCost() <= 0 || _iceHex == null) return;
            
            int newCost = _cell.GetCost() - 1;
            _cell.SetCost(newCost);
            
            if (newCost <= 0)
                UnlockIce();
            else
                _iceHex.SetState(newCost);
        }
        
        private void UnlockIce()
        {
            _iceHex.Animate(_blocks, _cell.GetCost());

            if (!GameplayFtue.IsActive)
            {
                _goalTracker.CellStateChanged(_cellState);
            }

            // DOVirtual.DelayedCall(1.5f, () =>
            // {
                UnregisterListeners();
                _cell.ChangeState(CellState.Open);
            // });
        }

        private void RegisterListeners()
        {
            Cell.Merged += OnCellMerge;
        }

        private void UnregisterListeners()
        {
            Cell.Merged -= OnCellMerge;
        }

        #endregion*/
    }
}