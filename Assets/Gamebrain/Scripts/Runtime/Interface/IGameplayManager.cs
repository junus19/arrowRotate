using System;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    public interface IGameplayManager
    {
        bool IsCompleted { get; }
        bool IsFailed { get; }
        List<ICell> CellList { get; }
        List<IShape> ShapeList { get; }
        Action OnLevelComplete { get; set; }
        Action OnLevelFail { get; set; }
        void SetInteractable(bool interactable);
        void Revive();
        void DropShape();
        void AbandonShape();
        bool RefreshShapes();
        void SwapCells(ICell draggedCell, ICell targetCell);
    }
}