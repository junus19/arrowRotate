using System.Collections.Generic;
using UnityEngine;

namespace GameBrain.Casual
{
    public class IceTokenVisual : MonoBehaviour
    {
        [SerializeField] GameObject Ice_Lvl01;
        [SerializeField] GameObject Ice_Broken_Lvl01;
        [SerializeField] GameObject Ice_Lvl02;
        [SerializeField] GameObject Ice_Broken_Lvl02;
        [SerializeField] GameObject Ice_Lvl03;
        [SerializeField] GameObject Ice_Broken_Lvl03;

        public void UpdateVisual(int health, bool activateParticle)
        {
            if (health == 2)
            {
                Ice_Lvl01.SetActive(false);
                    Ice_Lvl02.SetActive(true);
                if(activateParticle)
                {
                    Ice_Broken_Lvl01.SetActive(true);
                }
            }
            else if (health == 1)
            {
                Ice_Lvl02.SetActive(false);
                    Ice_Lvl03.SetActive(true);
                if(activateParticle)
                {
                    Ice_Broken_Lvl02.SetActive(true);
                }
            }
            else if (health == 0)
            {
                Ice_Lvl01.SetActive(false);
                Ice_Lvl02.SetActive(false);
                Ice_Lvl03.SetActive(false);
                if(activateParticle)
                {
                    Ice_Broken_Lvl03.SetActive(true);
                }
            }
        }

    }
}
