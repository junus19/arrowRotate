using ArrowRotate.Core;
using GameBrain.Casual;
using UnityEngine;

namespace ArrowRotate.Integration
{
    /// <summary>
    /// Hexa Arrows level'ı = seed + zorluk konfigürasyonu. Grid runtime'da deterministik üretilir
    /// (aynı seed → aynı level; hata raporlarında seed loglanır). Scene alanı boş bırakılır —
    /// Level.Load() sahne yüklemeyi atlar, Game sahnesi GameState_Gameplay tarafından yüklenir.
    /// </summary>
    [CreateAssetMenu(menuName = "Arrow Rotate/Hexa Level Data", fileName = "HexaLevel_000", order = 0)]
    public class HexaLevelData : LevelData
    {
        [Header("Hexa Arrows")]
        [SerializeField] private int _seed = 1;
        [Tooltip("Zorluk tablosu satırı (SKILL.md §7): 1, 2, 3-4, 5+")]
        [SerializeField] private int _difficultyLevel = 1;

        public int Seed => _seed;
        public int DifficultyLevel => _difficultyLevel;

        public LevelConfig BuildConfig() => LevelConfig.ForLevel(_difficultyLevel);

#if UNITY_EDITOR
        public void EditorInit(int seed, int difficultyLevel)
        {
            _seed = seed;
            _difficultyLevel = difficultyLevel;
        }
#endif
    }
}
