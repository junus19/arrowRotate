using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Rendering.Universal;

public class JellyContainer : MonoBehaviour
{
    //[SerializeField]ScriptableRendererData renderData;
    [SerializeField] bool isGrowable = false;
    public GameObject dots;

    public Metaballs2D dot;
    //public int growAmount = 3;
    public List<JellySpawnPosition> spawnPosList;

    //private const int nonGrowableRenderFeatureIndex = 0;
    //private const int growableRenderFeatureIndex = 1;

    /*
    private void Awake()
    {
        SetRenderFeature();
    }
    private void SetRenderFeature(int renderFeatureIndex)
    {
        //renderData.rendererFeatures[renderFeatureIndex].SetActive(true);
    }

    private void DisableAllRenderFeatures()
    {
        foreach (var rFeature in renderData.rendererFeatures)
        {
            rFeature.SetActive(false);
        }
    }

    public void SetRenderFeature()
    {
        DisableAllRenderFeatures();

        if (isGrowable)
            SetRenderFeature(growableRenderFeatureIndex);
        else
            SetRenderFeature(nonGrowableRenderFeatureIndex);
    }*/

    public void grow()
    {

        if (isGrowable)
        {
           

            for (int i = 0; i < spawnPosList.Count; i++)
            {
                int k = Random.Range(1, 100);

                int total = 0;
                int spawnNum = 0;
                for (int ii = 0; ii < spawnPosList[i].spawnProbabilityList.Count; ii++)
                {
                    total += spawnPosList[i].spawnProbabilityList[ii];
                    if (total > k)
                    {
                        spawnNum = ii;
                        break;
                    }
                }

                //print("start grow www k" + k + "_num:" + spawnNum);

                if (spawnNum > 0)
                {
                    spawnPosList[i].spwan(dot, spawnNum, dots.transform);
                }

            }
        }
    }


}
