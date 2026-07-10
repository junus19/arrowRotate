using UnityEngine;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class ScoreUpdateEvent : IEvent
    {
        public int score;
        public int scoreGoal;
        public Vector3 cellPositionToGiveScore;
        public Color towerColor;

        public ScoreUpdateEvent(int _score, int _scoreGoal, Vector3 _cellPositionToGiveScore, Color _towerColor)
        {
            score = _score;
            scoreGoal = _scoreGoal;
            cellPositionToGiveScore = _cellPositionToGiveScore;
            towerColor = _towerColor;
        }
    }
}
