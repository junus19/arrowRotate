using System.Collections.Generic;
using System;
using UnityEngine;
using DG.Tweening;

namespace GameBrain
{    
    public class Tutorial
    {
        public GameObject _tutorialHand;
        private List<TutorialHandInfo> tutorialHandPositions;
        private int tutorialIndex = 0;

        public Tutorial(GameObject tutorialHand)
        {
            _tutorialHand = tutorialHand;
            tutorialHandPositions = new List<TutorialHandInfo>();
        }

        public void StartShowingTutorialHand()
        {
            if(tutorialIndex >= tutorialHandPositions.Count)
                return;
                Debug.Log("newtut " + tutorialIndex);
            DG.Tweening.DOVirtual.DelayedCall(tutorialHandPositions[tutorialIndex].Delay, ShowTutorialHand);
        }

        private void ShowTutorialHand()
        {
            _tutorialHand.transform.position = tutorialHandPositions[tutorialIndex].Position;
            _tutorialHand.gameObject.SetActive(true);
        }


        public void HideTutorialHand()
        {
            _tutorialHand.gameObject.SetActive(false);
        }

        public void AddHandTutorialPositions(Vector3 pos, float delay)
        {
            TutorialHandInfo tutorialHandInfo = new TutorialHandInfo();
            tutorialHandInfo.Position = pos;
            tutorialHandInfo.Delay = delay;
            tutorialHandPositions.Add(tutorialHandInfo);
        }

        public void OnClick()
        {
            HideTutorialHand();
            tutorialIndex++;
        }
    }

    public class TutorialHandInfo
    {
        public Vector3 Position;
        public float Delay;
    }
}
