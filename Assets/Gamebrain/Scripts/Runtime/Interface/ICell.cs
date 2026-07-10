using System.Collections.Generic;
using UnityEngine;

namespace GameBrain.Casual
{
    public interface ICell
    {
        Transform transform { get; }
        Transform HexContainer { get; }
        CellInfo CellInfo { get; }
        BoardObject BoardObject { get; }
        Dictionary<Direction, Transform> Neighbours { get; }
        void Preload(CellInfo cellInfo);
        void ShowHint();
        void HideHint();
        void ShowHighlight();
        void HideHighlight();
        void FindNeighbors();
        int GetTotalTokenCount();
        void LoadBoardObject(BoardObject boardObject);
        void UnLoadBoardObject();
        bool HasBoardObject();
        bool HasDamageableBoardObject();
        BoardObjectType GetBoardObjectType();
        void Clear();
        void ClearHexes();
        bool ContainsHex();
        bool ContainsHexWithType(int hexType);
    }
}