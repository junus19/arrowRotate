using UnityEngine;

namespace GameBrain.Casual
{
    public interface IHex
    {
        Transform transform { get; }
        int HexType { get; }
    }
}