using System.Collections.Generic;
using UnityEngine;

public class FakePlay : MonoBehaviour
{


        [SerializeField]
    private List<RadialColorReveal2D> reveals =
        new List<RadialColorReveal2D>();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            for (int i = 0; i < reveals.Count; i++)
            {
                if (reveals[i] != null)
                {
                    reveals[i].PlayRandomPoints();
                }
            }
        }
    }

}
