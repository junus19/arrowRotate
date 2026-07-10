using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace GameBrain.Casual
{    
    public class LevelGoalPanel : UIPanel
    {
        [SerializeField] Canvas canvas;
        public Canvas Canvas => canvas;
        public GameObject itemsContainter;

        [SerializeField] LevelObjectivesContainerUI levelObjectivesContainerUI;
        public LevelObjectivesContainerUI LevelObjectivesContainerUI => levelObjectivesContainerUI;


        public override void OnInject(object[] args)
        {
            base.OnInject(args);
        }
        public Vector3 GetFlyingPositionForScoreUpdate()
        {
            Vector3[] corners = new Vector3[4];
            levelObjectivesContainerUI.ScoreLevelObjectiveUI.flyTarget.GetWorldCorners(corners);
            return (corners[0] + corners[1] +  corners[2] +  corners[3]) / 4.0f;
        }

        public void HideLevelGoalPanel()
        {
            levelObjectivesContainerUI.mainCanvasGroup.blocksRaycasts = false;
            levelObjectivesContainerUI.mainCanvasGroup.DOKill();
            levelObjectivesContainerUI.mainCanvasGroup.DOFade(0f,.5f);

        }

        public void ShowLevelGoalPanel()
        {
            levelObjectivesContainerUI.mainCanvasGroup.blocksRaycasts = true;
            levelObjectivesContainerUI.mainCanvasGroup.DOKill();
            levelObjectivesContainerUI.mainCanvasGroup.DOFade(1.0f,.5f);
        }
        public void EnableLevelGoalPanel()
        {
            itemsContainter.SetActive(true);
        }

        public void DisableLevelGoalPanel()
        {
            itemsContainter.SetActive(false);
        }
    }
}
