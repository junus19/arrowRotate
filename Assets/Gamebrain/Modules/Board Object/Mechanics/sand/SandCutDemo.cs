
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

using Random = UnityEngine.Random;

public class SandCutDemo : MonoBehaviour
{




    public RegionGenerator land = new RegionGenerator();

    public GameObject digger;

    public GameObject positions;

    List<Vector3> posList;


    void Start()
    {





        posList = new List<Vector3>();
        for (int i = 0; i < positions.transform.childCount; i++)
        {
            posList.Add(positions.transform.GetChild(i).transform.position);
        }
        posList = Shuffle<Vector3>(posList);


        Vector3[] path = new Vector3[positions.transform.childCount];
        for (int i = 0; i < posList.Count; i++)
        {
            path[i] = posList[i];
        }




        land.prepareRegion();
        land.showNow();

        float startArea = land.totalArea();


        digger.transform.DOPath(path, 10f, PathType.Linear).SetEase(Ease.Linear).SetDelay(.3f).OnUpdate(() =>
        {
            land.circleCut(digger.transform.position, 1f, true, true, false, false);


            //txt.text = (100f - (land.totalArea() * 100f / startArea)).ToString("F0") + "%";

        }).OnComplete(() =>
        {
            //txt.text = "";
            land.gameObject.SetActive(false);
           
        });




    }


    public static List<T> Shuffle<T>(List<T> _list)
    {
        for (int i = 0; i < _list.Count; i++)
        {
            T temp = _list[i];
            int randomIndex = Random.Range(i, _list.Count);
            _list[i] = _list[randomIndex];
            _list[randomIndex] = temp;
        }

        return _list;
    }


    void Update()
    {

    }





    



}