using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using DG.Tweening;
using TMPro;
public class RopeObject : MonoBehaviour
{


    
    public Color lightColor;
    public Color darkColor;

    public List<GameObject> dots_0;
    public List<GameObject> dots_1;
    public LineRenderer lineRender_0;
    public LineRenderer lineRender_1;

    public GameObject cutPoint;
    public GameObject lockObject;

    public TextMeshPro txt;
    bool untied = false;

    //bool isDead = false;

    //float pos;

    public int live;
    

    void Awake ()
    {

        
        lineRender_0.positionCount = dots_0.Count-1 ;// dots_0.Count;
        lineRender_1.positionCount = dots_1.Count-1;



        for (int i = 0; i < dots_0.Count; i++)
        {
            dots_0[i].GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        }


        for (int i = 0; i < dots_1.Count; i++)
        {
            dots_1[i].GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        }

        txt.text = live.ToString();

        show();

        //hide();


        /*
        pos = this.transform.position.y;
        this.transform.position -= new Vector3(0, 30, 0);
        //runTheRope();
        */
    }

    // Update is called once per frame
    void Update()
    {
        /*
        if (isDead)
        {
            return;
        }*/

        
        for (int i = 0; i < dots_0.Count-1; i++)
        {
            lineRender_0.SetPosition(i, dots_0[i].transform.position);

        }

        for (int i = 0; i < dots_1.Count-1; i++)
        {
            lineRender_1.SetPosition(i, dots_1[i+1].transform.position);

        }

    }


     void unTie()
    {
        
        cutPoint.GetComponent<HingeJoint2D>().connectedBody = cutPoint.GetComponent<Rigidbody2D>();

        lockObject.transform.parent = null;
        lockObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
        untied = true;
     

        StartCoroutine(deleteLock());
        StartCoroutine(ropeClear(2));
    }

    public IEnumerator ropeClear(float delay)
    {
        yield return new WaitForSeconds(delay);

        
        dots_0[dots_0.Count - 1].SetActive(false);
        dots_0.RemoveAt(dots_0.Count - 1);

        dots_1[0].SetActive(false);
        dots_1.RemoveAt(0);


        if (dots_0.Count > 0)
        {
            lineRender_0.positionCount = dots_0.Count - 1;
        }
        if (dots_1.Count > 0)
        {
            lineRender_1.positionCount = dots_1.Count - 1;
        }




        if (dots_0.Count > 0)
        {
            StartCoroutine(ropeClear(.05f));
        }
        else
        {
            for (int i = 0; i < dots_0.Count; i++)
            {
                dots_0[i].SetActive(false);
            }


            for (int i = 0; i < dots_1.Count; i++)
            {
                dots_1[i].SetActive(false);
            }

            lineRender_0.gameObject.SetActive(false);
            lineRender_1.gameObject.SetActive(false);
        }
    }

    public IEnumerator deleteLock()
    {
        yield return new WaitForSeconds(.5f);
        Destroy(lockObject.gameObject);

        /*
        yield return new WaitForSeconds(2f);

        isDead = true;
        for (int i = 0; i < dots_0.Count; i++)
        {
            dots_0[i].SetActive(false);
        }


        for (int i = 0; i < dots_1.Count; i++)
        {
            dots_1[i].SetActive(false);
        }

        lineRender_0.gameObject.SetActive(false);
        lineRender_1.gameObject.SetActive(false);*/
    }
    

    public void show()
    {



        this.gameObject.SetActive(true);

        /*
        lineRender_0.materials[0].color = lightColor;
        lineRender_0.materials[0].DOColor(darkColor, t);

        dots_0[0].GetComponent<SpriteRenderer>().color = lightColor;
        dots_0[0].GetComponent<SpriteRenderer>().DOColor(darkColor, t);



        lineRender_1.materials[0].color = lightColor;
        lineRender_1.materials[0].DOColor(darkColor, t);

        dots_1[dots_1.Count - 1].GetComponent<SpriteRenderer>().color = lightColor;
        dots_1[dots_1.Count - 1].GetComponent<SpriteRenderer>().DOColor(darkColor, t);

        
        this.transform.DOMoveY(pos, .75f).OnComplete(() => {

     

            for (int i = 1; i < dots_0.Count; i++)
            {

                dots_0[i].GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
            }



            for (int ii = 0; ii < dots_1.Count - 1; ii++)
            {
                dots_1[ii].GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
            }

      



        });

  */
        for (int i = 1; i < dots_0.Count; i++)
        {

            dots_0[i].GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
        }



        for (int ii = 0; ii < dots_1.Count - 1; ii++)
        {
            dots_1[ii].GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
        }

        //updateLive(0);
    }


    public void checkUntie(int score)
    {
       
        if (untied)
        {
            return;
        }

        /*
       live += value;


       if (live < 1)
       {
           txt.text = "";
           unTie();
       }
       else
       {
           txt.text = live.ToString();

       }*/
        if (score > live - 1)
        {
            txt.text = "";
            unTie();
        }
        else
        {
            txt.text = (live - score).ToString();
        }

    }

    /*
    public void hideTxt()
    {
        //txt.gameObject.SetActive(false);
    }

    public void updateTxt()
    {
        
        txt.text = value + "/" + target;
        if (untied)
        {
            txt.text = "";
        }
    }*/

    public void hide()//float t)
    {
       

        this.gameObject.SetActive(false);

        /*
        lineRender_0.materials[0].color = darkColor;
        lineRender_0.materials[0].DOColor(lightColor, t).OnComplete(() =>
        {
            this.gameObject.SetActive(false);
        });


        dots_0[0].GetComponent<SpriteRenderer>().color = darkColor;
        dots_0[0].GetComponent<SpriteRenderer>().DOColor(lightColor, t);

        //dots_0[dots_0.Count - 1].GetComponent<SpriteRenderer>().color = darkColor;
        //dots_0[dots_0.Count - 1].GetComponent<SpriteRenderer>().DOColor(lightColor, t);


        lineRender_1.materials[0].color = darkColor;
        lineRender_1.materials[0].DOColor(lightColor, t);

        //dots_1[0].GetComponent<SpriteRenderer>().color = darkColor;
        //dots_1[0].GetComponent<SpriteRenderer>().DOColor(lightColor, t);

        dots_1[dots_1.Count - 1].GetComponent<SpriteRenderer>().color = darkColor;
        dots_1[dots_1.Count - 1].GetComponent<SpriteRenderer>().DOColor(lightColor, t);*/
    }


}
