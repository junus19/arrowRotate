using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    public class NavigationBar : MonoBehaviour
    {
        [SerializeField] private Transform _buttonContainer; 
        private List<NavigationButton> _navigationButtons;
        private NavigationButton _currentSelection;
        
        public event Action<int> OnSelectionChange;

        private void Awake()
        {
            _navigationButtons = _buttonContainer.GetComponentsInChildren<NavigationButton>(true).ToList();
            _currentSelection = _navigationButtons[1];
        }

        private void OnEnable()
        {
            foreach (NavigationButton button in _navigationButtons)
            {
                button.OnSelect += OnNavigationButtonClick;
            }
        }

        private void OnDisable()
        {
            foreach (NavigationButton button in _navigationButtons)
            {
                button.OnSelect -= OnNavigationButtonClick;
            }
        }

        private void OnNavigationButtonClick(int index)
        {
            if (_currentSelection == _navigationButtons[index]) return;
            
            _currentSelection?.SetSelected(false);
            _currentSelection = _navigationButtons[index];
            _currentSelection.SetSelected(true);
            
            OnSelectionChange?.Invoke(index);
        }

        public void SetSelection(int index)
        {
            foreach (NavigationButton button in _navigationButtons) 
                button.SetSelected(false);
            _currentSelection = _navigationButtons[index];
            _currentSelection.SetSelected(true);
            OnSelectionChange?.Invoke(index);
        }
    }
}
