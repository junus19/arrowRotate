using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace GameBrain.Casual
{
    public class SwapBooster : BaseBooster
    {
        [SerializeField] private Vector3 _dragOffset = new Vector3(0f, 2f, 1f);
        private ICell _draggedCell;
        private ICell _targetCell;

        protected override void StartBoosterAction(BoosterRequestedEvent eventInfo)
        {
            base.StartBoosterAction(eventInfo);

            if (eventInfo.BoosterType != boosterItemData.BoosterType)
                return;

            int draggableCellCount = _gameplayManager.CellList.Count(cell => !cell.HasBoardObject() && cell.CellInfo.HexInfo.Count > 0);
            bool isThereAnyEmptyCell = _gameplayManager.CellList.Any(cell => !cell.HasBoardObject() && cell.CellInfo.HexInfo.Count == 0);
            
            if (draggableCellCount != 0 && isThereAnyEmptyCell || draggableCellCount >= 2)
                return;
            
            EndBoosterAction(false);
        }

        protected override void UpdateLogic()
        {
            base.UpdateLogic();

            if (Input.GetMouseButtonDown(0) && _draggedCell == null)
            {
                Ray ray = CameraManager.Instance.GameplayCamera.ScreenPointToRay(Input.mousePosition);
            
                int layerMask = LayerMask.GetMask("HexCell");
                if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, Mathf.Infinity, layerMask))
                {
                    if (hit.transform.parent.gameObject.TryGetComponent(out ICell cell))
                    {
                        if (!cell.HasBoardObject() && cell.CellInfo.HexInfo.Count > 0)
                        {
                            _draggedCell = cell;
                        }
                        else if (cell.HasBoardObject() || cell.CellInfo.HexInfo.Count == 0)
                           EndBoosterAction(false);
                    }
                    else
                        EndBoosterAction(false);
                }
                else
                    EndBoosterAction(false);
            }
            
            if (Input.GetMouseButton(0) && _draggedCell != null)
            {
                Ray ray = CameraManager.Instance.GameplayCamera.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, CameraManager.Instance.GameplayCamera.farClipPlane));
                int layerMask = LayerMask.GetMask("Board");
            
                if (!Physics.Raycast(ray.origin, ray.direction * 250, out RaycastHit hit, Mathf.Infinity, layerMask)) 
                    return;
            
                _draggedCell.HexContainer.transform.position = hit.point + _dragOffset;
                
                
                ray = CameraManager.Instance.GameplayCamera.ScreenPointToRay(Input.mousePosition);
                layerMask = LayerMask.GetMask("HexCell");
                if (Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity, layerMask))
                {
                    if (hit.transform.parent.gameObject.TryGetComponent(out ICell cell))
                    {
                        if (cell != _draggedCell && !cell.HasBoardObject())
                        {
                            if(_targetCell != null && cell != _targetCell) 
                                _targetCell.HexContainer.transform.localPosition = Vector3.zero;
                            _targetCell = cell;
                            _targetCell.HexContainer.transform.position = _draggedCell.transform.position;
                        }
                        else
                        {
                            if(_targetCell != null) 
                                _targetCell.HexContainer.transform.localPosition = Vector3.zero;
                            _targetCell = null;
                        }
                    }
                    else
                    {
                        if(_targetCell != null)
                            _targetCell.HexContainer.transform.localPosition = Vector3.zero;
                        _targetCell = null;
                    }
                }
                else
                {
                    if(_targetCell != null)
                        _targetCell.HexContainer.transform.localPosition = Vector3.zero;
                    _targetCell = null;
                }
            }
            
            if (Input.GetMouseButtonUp(0) && _draggedCell != null)
            {
                Ray ray = CameraManager.Instance.GameplayCamera.ScreenPointToRay(Input.mousePosition);
                int layerMask = LayerMask.GetMask("HexCell");
                if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, Mathf.Infinity, layerMask))
                {
                    if (hit.transform.parent.gameObject.TryGetComponent(out ICell cell))
                    {
                        if (cell != _draggedCell && !cell.HasBoardObject())
                        {
                            _targetCell = cell;
                            ExecuteBoosterAction();
                        }
                        else
                            ReleaseDraggedCell();
                    }
                    else
                        ReleaseDraggedCell();
                }
                else
                    ReleaseDraggedCell();
            }
        }

        private void ReleaseDraggedCell()
        {
            if (_draggedCell == null) return;
            _draggedCell.HexContainer.transform.localPosition = Vector3.zero;
            _draggedCell = null;
        }
        
        protected override void ExecuteBoosterAction()
        {
            base.ExecuteBoosterAction();
            
            _gameplayManager.SwapCells(_draggedCell, _targetCell);
            
            DOVirtual.DelayedCall(.5f, ()=> EndBoosterAction(true));
        }
        
        protected override void EndBoosterAction(bool isUsed)
        {
            base.EndBoosterAction(isUsed);

            _draggedCell = null;
            _targetCell = null;
            if (!isUsed || boosterManager == null) return;
            UseBooster();
        }
    }
}
