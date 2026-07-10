using System;
using UnityEngine;

namespace GameBrain.Casual
{
    public class LevelManager
    {
        private readonly LevelData[] _levelData;
        private readonly LevelData[] _randomLevelData;

        private ILevel _currentLevel;
        private LevelData _selectedLevelData;

        public ILevel CurrentLevel => _currentLevel;
        public LevelData SelectedLevelData => _selectedLevelData;

        public event Action<ILevel> LevelLoaded;
        public event Action<ILevel> LevelUnloaded;

        public ILevel CreateLevel(LevelData data, LevelStats stats) => new Level(data, stats);

        public LevelManager(LevelData[] levelData, LevelData[] randomLevelData)
        {
            _levelData = levelData;
            _randomLevelData = randomLevelData;
        }

        public async Awaitable LoadLevel(ILevel level)
        {
            _currentLevel = level;
            foreach (LevelObjective objective in _currentLevel.Data.Objectives)
                objective.Reset();
            await _currentLevel.Load();
            Debug.Log("Loaded Level : " + _currentLevel.Data.name);
            LevelLoaded?.Invoke(_currentLevel);
        }

        public async Awaitable LoadLevel(int id, int randomLevelLoopStartIndex)
        {
            int levelCount = _levelData.Length;
            int levelToLoad;
            if (id < levelCount)
                levelToLoad = id;
            else
                levelToLoad = UnityEngine.Random.Range(randomLevelLoopStartIndex, levelCount);

            await LoadLevel(CreateLevel(_levelData[levelToLoad], null));
        }

        public LevelData SelectLevelData(GameData gameData)
        {
            int levelCount = _levelData.Length;
            int levelIndex = gameData.Data.RealLevel < levelCount ? gameData.GetRealLevelIndex() : gameData.GetLevelIndex();

            LevelData selected;
            if (levelIndex < levelCount)
            {
                selected = _levelData[levelIndex];
            }
            else
            {
                int levelToLoad;
                if (gameData.Data.SelectedRandomLevel > -1 &&
                    gameData.Data.SelectedRandomLevel < levelCount)
                {
                    levelToLoad = gameData.Data.SelectedRandomLevel;
                }
                else
                {
                    levelToLoad = GetRandomLevelIndex(gameData.RandomLevelLoopStartIndex,
                        gameData.Data.LastCompletedRandomLevel, levelCount);
                    gameData.SetSelectedRandomLevel(levelToLoad);
                }

                // Guard against a shorter random list than the fixed list.
                levelToLoad = Mathf.Clamp(levelToLoad, 0,
                    Mathf.Max(0, _randomLevelData.Length - 1));
                selected = _randomLevelData[levelToLoad];
            }

            _selectedLevelData = selected;
            return selected;
        }

        public async Awaitable LoadLevel(GameData gameData)
        {
            // Use the level already chosen in the main menu; otherwise select now.
            LevelData data = _selectedLevelData != null
                ? _selectedLevelData
                : SelectLevelData(gameData);

            _selectedLevelData = null; // consumed — next main-menu visit re-selects
            await LoadLevel(CreateLevel(data, null));
        }

        public bool IsHardLevel(GameData gameData)
        {
            return SelectLevelData(gameData).Difficulty is LevelDifficulty.Hard;
        }

        private int GetRandomLevelIndex(int randomLevelStartIndex, int lastSelectedRandomLevelIndex, int levelCount)
        {
            int randomLevelIndex = UnityEngine.Random.Range(randomLevelStartIndex, levelCount);
            if (randomLevelIndex == lastSelectedRandomLevelIndex)
                randomLevelIndex = UnityEngine.Random.Range(randomLevelStartIndex, levelCount);

            if (randomLevelIndex == lastSelectedRandomLevelIndex)
                randomLevelIndex = UnityEngine.Random.Range(randomLevelStartIndex, levelCount);

            if (randomLevelIndex == lastSelectedRandomLevelIndex)
                randomLevelIndex = UnityEngine.Random.Range(randomLevelStartIndex, levelCount);

            return randomLevelIndex;
        }

        public void LevelCompleted(GameData gameData)
        {
            int realLevelIndex = gameData.GetRealLevelIndex();

            if (realLevelIndex < _levelData.Length)
                gameData.RealLevelComplete();
            else
                gameData.LevelComplete();
        }

        public async Awaitable UnLoadLevel(ILevel level)
        {
            if (_currentLevel == level)
                _currentLevel = null;

            await level.Unload();
            LevelUnloaded?.Invoke(level);
        }

        // public async Awaitable RestartCurrentLevel()
        // {
        //     if (_currentLevel == null)
        //     {
        //         const string message = "The level that is requested to restart cannot be null!";
        //         throw new NullReferenceException(message);
        //     }
        //
        //     // int currentLevelId = (int) _currentLevel.Data.ID;
        //     await UnLoadLevel(_currentLevel);
        //     // await LoadLevel(currentLevelId);
        //     Debug.Log("Level restarted!!");
        // }
    }
}
