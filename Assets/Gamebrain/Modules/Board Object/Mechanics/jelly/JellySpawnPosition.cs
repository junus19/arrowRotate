using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JellySpawnPosition : MonoBehaviour
{

    public List<int> spawnProbabilityList;

    public void spwan(Metaballs2D dot, int amount, Transform parent)
    {
        StartCoroutine(growCoroutine(dot, amount, parent));
    }


    IEnumerator growCoroutine(Metaballs2D dot, int amount, Transform parent)
    {


        for (int i = 0; i < amount; i++)
        {
            Vector3 pos = this.transform.position;//this.transform.position + new Vector3(Random.Range(-.1f, .1f), Random.Range(-.1f, .1f), 0);//find a proper position
            Instantiate(dot, pos, Quaternion.identity, parent);

            yield return new WaitForSeconds(.05f);
        }
    }
}
