using GameBrain.Store;
using UnityEngine;

namespace GameBrain.Casual
{
    [DisallowMultipleComponent]
    public class GUIService : MonoBehaviour
    {
        [SerializeField] private UIPanel _mainPanel;
        [SerializeField] private UIPanel _gameplayPanel;
        [SerializeField] private UIPanel _levelFailPanel;
        [SerializeField] private UIPanel _levelCompletePanel;
        [SerializeField] private UIPanel _settingsPanel;
        [SerializeField] private UIPanel _levelGoalPanel;
        [SerializeField] private UIPanel _topPanel;
        [SerializeField] private UIPanel _shopPanel;


        public MainMenuPanel MainPanel => _mainPanel as MainMenuPanel;
        public GameplayPanel GameplayPanel => _gameplayPanel as GameplayPanel;
        public LevelCompletePanel LevelCompletePanel => _levelCompletePanel as LevelCompletePanel;
        public LevelFailPanel LevelFailPanel => _levelFailPanel as LevelFailPanel;
        public SettingsPanel SettingsPanel => _settingsPanel as SettingsPanel;
        public LevelGoalPanel LevelGoalPanel => _levelGoalPanel as LevelGoalPanel;
        public TopPanel TopPanel => _topPanel as TopPanel;
        public StorePanel ShopPanel => _shopPanel as StorePanel;


        public void DisableAllPanels()
        {
            _mainPanel.SetActive(false);
            _gameplayPanel.SetActive(false);
            _levelCompletePanel.SetActive(false);
            _levelFailPanel.SetActive(false);
            _settingsPanel.SetActive(false);
            _topPanel.SetActive(false);
            _shopPanel.SetActive(false);
        }
    }
}
