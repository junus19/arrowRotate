using UnityEngine;

namespace GameBrain.Casual
{
    public abstract class BoardObject : MonoBehaviour
    {
        [SerializeField] protected BoardObjectData boardObjectData;
        [SerializeField] protected BoardObjectType boardObjectType;
        [SerializeField] protected ICell currentCell;
        public BoardObjectType BoardObjectType => boardObjectType;
        public ICell CurrentCell => currentCell;
        protected IGameplayManager _gameplayManager;
        // public UnityEvent OnBoardObjectUnloaded;

        public virtual void LoadBoardObject(ICell cell, int objectValue)
        {
            currentCell = cell;
            currentCell.LoadBoardObject(this);
        }

        public abstract void UnloadAndDestroyBoardObject();

        public virtual void ApplyCellStatusChanges(){}

        protected void DelayedDestroyCall(float delay)
        {
            Invoke("DestroyBoardObject", delay);
        }
    }
}
