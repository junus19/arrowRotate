using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace GameBrain.SDK
{
    public class InitializationIndicator : MonoBehaviour
    {
        //[SerializeField] private Image _loadingIndicatorImage;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private Image _loaderImage;

        
        /*private void OnEnable()
        {
            StartIndicator();
        }

        private void OnDisable()
        {
            StopIndicator();
        }
        
        public void StartIndicator()
        {
            StopCoroutine(nameof(LoadingRoutine));
            StartCoroutine(LoadingRoutine());
        }*/

        public void StopIndicator()
        {
            //StopCoroutine(nameof(LoadingRoutine));
        }

/*
        private IEnumerator LoadingRoutine()
        {
            
            while (true)
            {
                yield return new WaitForSeconds(0.05f);
                _loadingIndicatorImage.transform.eulerAngles += Vector3.back * 30;
            }
        }*/
        
        public void SetProgressText(int text)
        {
            _loaderImage.fillAmount = (float)text / 100f;
            _progressText.text = text.ToString()+"%";
        }
    }
}
