using UnityEngine;

namespace GameBrain.Casual
{
    public interface IShape
    {
        Transform transform { get; }
        int ShapeIndex { get; }
        bool IsUsed { get; }
        ShapeInfo ShapeInfo { get; }
        void Preload(ShapeInfo shapeInfo);
    }
}