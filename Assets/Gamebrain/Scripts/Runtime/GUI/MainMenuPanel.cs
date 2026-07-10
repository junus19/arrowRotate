using System.Linq;
using UnityEngine;

namespace GameBrain.Casual
{
    public class MainMenuPanel : UIPanel
    {
        [SerializeField] private Transform _contentContainer;
        [SerializeField] private GameObject _hardLevelTag;
        // [SerializeField] private NavigationBar _navigationBar;
        private UIPanel[] _panels;

        protected override void Awake()
        {
            base.Awake();
            _panels = _contentContainer.GetComponentsInChildren<UIPanel>(true);
        }

        private void OnEnable()
        {
            // _navigationBar.OnSelectionChange += OnSelectionChanged;
            // _navigationBar.SetSelection(1);
        }

        private void OnDisable()
        {
            // _navigationBar.OnSelectionChange -= OnSelectionChanged;
        }
        
        private void OnSelectionChanged(int index)
        {
            foreach (UIPanel panel in _panels)
            {
                panel.SetActive(false);
            }
            _panels[index].SetActive(true);
        }

        public T GetPanel<T>() where T : UIPanel => _panels.First(panel => panel is T) as T;

        public void SetHardLevelTagActive(bool active) => _hardLevelTag.SetActive(active);
    }
}
