using UnityEngine;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;
using DG.Tweening;

public class RegionGenerator : MonoBehaviour
{

    public bool isCircle = false;
    public bool isMud = false;
    public float circleRatio = 1;
    public float scale = 1;
    public int width;
    public int height;
    //public float cutRadius = 0.5f;

    //[SerializeField] bool randomObstacle = false;
    //GameObject[] obstacleSets;
    //GameObject[] obstacleStumps;

    float[,] map;
    float[,] mapTemp;
    float[,] mapTemp2;

    //bool isCut = false;
    public MeshGenerator meshGenerator;
    //Camera mainCamera;
    Vector3 offset;
    bool showing = false;
    bool refreshing = false;
    bool hiding = false;
    bool balancing = false;

    private float timer = 0.0f;

    float showSpeed = 2;
    float hideSpeed = 2;
    float balanceSpeed = .5f;

    Vector2 startPos = new Vector2();


    [Range(0, 1000)]
    public int soilingMinX;
    [Range(0, 1000)]
    public int soilingMaxX;
    [Range(0, 1000)]
    public int soilingMinY;
    [Range(0, 1000)]
    public int soilingMaxY;
    [Range(0f, 100f)]
    public float soilingDelay;



    public float turnSpeed = 0;
    public float kFactor = 1;
    public float growthAmount = 0.1f;
    float turnZ = 0;

    private void Awake()
    {
        meshGenerator = GetComponent<MeshGenerator>();
        //mainCamera = Camera.main;

        offset = new Vector3(width * scale / 2 + scale / 2, height * scale / 2 + scale / 2, 0) - Vector3.one * scale;
        /*if (randomObstacle)
            obstacleSets = Resources.LoadAll<GameObject>("Prebuilds");*/

        
        if (meshGenerator.antiMesh != null)
        {
            meshGenerator.antiMesh.set(width, height);
        }
    }

    private void Start()
    {
        //manager = GameObject.FindGameObjectWithTag("manager");

    }

    public void say()
    {

        for (int a = 0; a < height; a++)
        {
            string ss = "";
            for (int b = 0; b < width; b++)
            {
                ss += map[a, b].ToString("F1") + "_";
            }
            print("sss:" + ss);
        }

    }

    public void prepareRegion()
    {
        GenerateMap();
        CutStamps();

    }



    public void showRegion(float speed)
    {

        startPos.x = Random.Range(soilingMinX, soilingMaxX);//Random.Range(sw, width);
        startPos.y = Random.Range(soilingMinY, soilingMaxY);//200; Random.Range(sh, height);
        showSpeed = speed;
        showing = true;

       // print("eee" + startPos.y);

    }

    public void showNow()
    {


        meshGenerator.GenerateMesh(map, scale);
    }


    public void hideRegion(float speed)
    {


        this.gameObject.transform.DORotate(new Vector3(0, 0, 0), .2f);

        hideSpeed = speed;
        int k = Random.Range(0, 100);

        /*
        if (k < 25)
        {
            startPos.x = 0;
            startPos.y = Random.Range(0, height);
        }else if (k < 50)
        {
            startPos.x = width - 1;
            startPos.y = Random.Range(0, height);
        }
        else if (k < 75)
        {
            startPos.x = Random.Range(0, width);
            startPos.y = 0;
        }
        else
        {
            startPos.x = Random.Range(0, width);
            startPos.y = height - 1;
        }*/

        startPos.x = width / 2;
        startPos.y = height / 2;


        turnSpeed = 0;
        timer = 0;
        refreshing = false;
        balancing = false;
        hiding = true;

    }

    public void refreshRegion()//(int xx, int yy)
    {

        startPos.x = Random.Range(soilingMinX, soilingMaxX);// Random.Range(sw, width);
        startPos.y = Random.Range(soilingMinY, soilingMaxY);// Random.Range(sh, height);

        timer = 0;

        refreshing = true;

    }

    public void updateShadow()
    {
        float rad = this.gameObject.transform.eulerAngles.z * Mathf.Deg2Rad;
        meshGenerator.shadow.transform.localPosition = new Vector3(Mathf.Sin(rad) * -0.2f, Mathf.Cos(rad) * -0.2f, .6f);
    }

    private void Update()
    {

        if (turnSpeed != 0)
        {


            turnZ = Time.deltaTime * turnSpeed;
            this.gameObject.transform.Rotate(new Vector3(0, 0, turnZ));


            updateShadow();

        }



        if (showing)
        {
            bool cont = false;
            timer += Time.deltaTime;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (map[x, y] > mapTemp[x, y])
                    {

                        cont = true;

                        if(Vector2.Distance(new Vector2(x,y),new Vector2(startPos.x, startPos.y)) + soilingDelay < timer * 60f)
                        {
                            mapTemp[x, y] += Time.deltaTime * showSpeed;//Time.deltaTime * Random.Range(.3f, .5f);
                        }

                        //
                    }

                    if (mapTemp[x, y] > map[x, y])
                    {
                        mapTemp[x, y] = map[x, y];
                    }


                }
            }

            if (cont == false)
            {
                showing = false;
                meshGenerator.GenerateMesh(map, scale);
            }
            else
            {
                meshGenerator.GenerateMesh(mapTemp, scale);
            }

        }else if (refreshing)
        {

            bool cont = false;
            timer += Time.deltaTime;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (map[x, y] < mapTemp[x, y])
                    {

                        cont = true;

                        if (Vector2.Distance(new Vector2(x, y), new Vector2(startPos.x, startPos.y)) < timer * 60f)
                        {
                            map[x, y] += Time.deltaTime * showSpeed;
                        }

                    }

                    if (mapTemp[x, y] < map[x, y])
                    {
                        map[x, y] = mapTemp[x, y];
                    }

                    mapTemp2[x, y] = map[x, y];

                }
            }


            meshGenerator.GenerateMesh(map, scale);

            if (cont == false)
            {
                refreshing = false;


            }


        }else if (hiding)
        {

            bool cont = false;
            timer += Time.deltaTime;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (map[x, y] > 0)
                    {

                        cont = true;

                        if (Vector2.Distance(new Vector2(x, y), new Vector2(startPos.x, startPos.y)) < timer * 60f)
                        {
                            map[x, y] -= Time.deltaTime * hideSpeed;
                        }

                    }

                    if (map[x, y] < 0)
                    {
                        map[x, y] = 0;
                    }


                }
            }


            meshGenerator.GenerateMesh(map, scale);

            if (cont == false)
            {
                hiding = false;


            }

        }
        else if(balancing)
        {



            bool cont = false;
            timer += Time.deltaTime;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (map[x, y] < mapTemp2[x, y])
                    {

                        cont = true;

                        map[x, y] += Time.deltaTime * balanceSpeed;

                    }

                    /*
                    if (0 > map[x, y])
                    {
                        map[x, y] = 0;
                    }*/


                }
            }


            meshGenerator.GenerateMesh(map, scale);

            if (cont == false)
            {
                balancing = false;


            }


        }


    }
    


    void GenerateMap()
    {
        map = new float[width, height];
        mapTemp = new float[width, height];
        mapTemp2 = new float[width, height];


        if (isCircle)
        {
            int midx = width / 2;
            int midy = height / 2;



            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {

                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(midx, midy));

                    
                    map[x, y] = 1f - (dist / circleRatio);
                    mapTemp[x, y] = Random.Range(.5f, .7f);
                    mapTemp2[x, y] = map[x, y];
                    //17->24
                    //19->28
                    //25->36



                }
            }



        }
        else
        {

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    {
                        mapTemp[x, y] = 0f;
                        map[x, y] = 0f;
                        mapTemp2[x, y] = map[x, y];
                    }
                    else
                    {
                        mapTemp[x, y] = Random.Range(.5f, .7f);
                        map[x, y] = 1f;
                        mapTemp2[x, y] = map[x, y];
                    }

                }
            }

        }
        




    }

    void CutStamps()
    {
        Stamp[] stamps = GetComponentsInChildren<Stamp>();

        foreach (Stamp stamp in stamps)
        {
            //stamp.stampCut(this);
            switch (stamp.cutterType)
            {
                case Stamp.CutterType.Circle:
                    circleCut(stamp.transform.position, stamp.radius, false, true, false, false);
                    break;
                case Stamp.CutterType.AntiCircle:
                    circleCut(stamp.transform.position, stamp.radius, false, true, false, true);
                    break;
                case Stamp.CutterType.Rectangle:
                    rectCut(stamp.leftTop, stamp.rightTop, stamp.rightBottom, stamp.leftBottom);
                    break;
                case Stamp.CutterType.Triangle:
                    // triangleCut(stamp.leftTop2, stamp.rightTop2, stamp.rightBottom2);
                    triangleCut(radius: stamp.radius, zRotation: stamp.rotation, stamp.transform);
                    break;
            }

        }
    }


    // public void RegionGrow()
    // {

    //     if (!isMud)
    //     {
    //         return;
    //     }

    //     for (int x = 0; x < width; x++)
    //     {
    //         for (int y = 0; y < height; y++)
    //         {

    //             float ppp = map[x, y];

    //             if (ppp > 0 && ppp < 1)
    //             {
    //                 ppp += .04f;
    //                 balancing = true;
    //                 mapTemp2[x, y] = ppp;
    //             }




    //         }
    //     }

    //     meshGenerator.GenerateMesh(map, scale);



    // }

    public void RegionGrow()
{
    if (!isMud)
    {
        return;
    }


    for (int x = 0; x < width; x++)
    {
        for (int y = 0; y < height; y++)
        {
            float currentValue = map[x, y];
            
            // Only process points that have some mass (> 0)
            if (currentValue > 0 && currentValue < 1)
            {
                // Check if this point is connected to solid terrain
                bool isConnected = false;
                
                // Check neighboring cells
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        
                        if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        {
                            if (map[nx, ny] >= 0.9f)
                            {
                                isConnected = true;
                                break;
                            }
                        }
                    }
                    if (isConnected) break;
                }

                // Only set growth target if connected to solid terrain
                if (isConnected)
                {
                    mapTemp2[x, y] = currentValue + growthAmount;
                    balancing = true;
                }
                else
                {
                    mapTemp2[x, y] = currentValue;
                }
            }
        }
    }

    meshGenerator.GenerateMesh(map, scale);
}


    public void circleCut(Vector3 circlePoint, float radius, bool meshUpdate, bool now, bool factorable, bool anti)
    {


        //if (turnSpeed != 0)
        {
            circlePoint = transform.InverseTransformPoint(circlePoint); 
        }
        circlePoint += offset;// - transform.position;

        // if (factorable)
        // {
            radius *= kFactor;
        // }


        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {


                Vector3 soilPoint = new Vector3(x, y) * scale;

               

                float dist = Vector2.Distance(circlePoint, soilPoint);

                if (dist < radius)
                {
                    float newValue = dist / radius;


                    if (anti)
                    {


                        newValue = 1 - newValue;


                        if (newValue > map[x, y])
                        {
                            map[x, y] = newValue;
                        }


                        if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                        {
                            map[x, y] = 0f;
                        }


                        
                        mapTemp2[x, y] = map[x, y];

                    }
                    else
                    {
                        if (now)
                        {

                            if (newValue < map[x, y])
                            {
                                map[x, y] = newValue;
                            }
                        }
                        else
                        {
                            if (newValue < mapTemp2[x, y])
                            {
                                balancing = true;
                                mapTemp2[x, y] = newValue;
                            }
                        }
                    }




                }


            }
        }


        if (meshUpdate)
        {
            meshGenerator.GenerateMesh(map, scale);
        }
      


    }


    public float totalArea()
    {
        return meshGenerator.totalArea();
    }




    public void rectCut(Vector3 b, Vector3 d)
    {

        b += offset - transform.position;
        d += offset - transform.position;

        float x1 = d.x;
        float y1 = d.y;

        float x2 = b.x;
        float y2 = b.y;

        // given point
        float xx = 0;
        float yy = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 soilPoint = new Vector3(x, y) * scale;

                // given point
                xx = soilPoint.x;
                yy = soilPoint.y;

                // function call
                if (FindPoint(x1, y1, x2, y2, xx, yy))
                {
                    map[x, y] = 0;
                }
            }
        }
        meshGenerator.GenerateMesh(map, scale);

    }

    public void rectCut(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        a += offset - transform.position;
        b += offset - transform.position;
        c += offset - transform.position;
        d += offset - transform.position;

        // Function to check if point (px, py) is inside the polygon defined by points
        bool IsPointInPolygon(Vector2[] polyPoints, Vector2 point)
        {
            int j = polyPoints.Length - 1;
            bool inside = false;
            for (int i = 0; i < polyPoints.Length; j = i++)
            {
                if (((polyPoints[i].y > point.y) != (polyPoints[j].y > point.y)) &&
                    (point.x < (polyPoints[j].x - polyPoints[i].x) * (point.y - polyPoints[i].y) / (polyPoints[j].y - polyPoints[i].y) + polyPoints[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        Vector2[] polyPoints = new Vector2[4] {
        new Vector2(a.x, a.y),
        new Vector2(b.x, b.y),
        new Vector2(c.x, c.y),
        new Vector2(d.x, d.y)
    };

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 soilPoint = new Vector3(x, y) * scale;
                Vector2 point = new Vector2(soilPoint.x, soilPoint.y);

                if (IsPointInPolygon(polyPoints, point))
                {
                    map[x, y] = 0;
                }
            }
        }
        meshGenerator.GenerateMesh(map, scale);
    }


    public void triangleCut(float radius, float zRotation, Transform stampTransform)
    {
        // Calculate the height of the equilateral triangle
        float height = radius * 0.42f; // sqrt(3)/2 * radius

        // Calculate the three points of the triangle
        Vector3 p1 = new Vector3(0, -height, 0);
        Vector3 p2 = new Vector3(-radius * 0.5f, height * 0.3333f, 0);
        Vector3 p3 = new Vector3(radius * 0.5f, height * 0.3333f, 0);

        Vector3 extraOffset = new Vector3(3, 7, 0);

        // Apply position offset
        p1 += stampTransform.localPosition + extraOffset;
        p2 += stampTransform.localPosition + extraOffset;
        p3 += stampTransform.localPosition + extraOffset;

        // Apply rotation
        Quaternion rotation = Quaternion.Euler(0, 0, zRotation);
        p1 = rotation * (p1 - stampTransform.localPosition) + stampTransform.localPosition;
        p2 = rotation * (p2 - stampTransform.localPosition) + stampTransform.localPosition;
        p3 = rotation * (p3 - stampTransform.localPosition) + stampTransform.localPosition;

        // Call the overloaded triangleCut method with the calculated points
        triangleCut(p1, p2, p3);
    }

    public void triangleCut(Vector3 a, Vector3 b, Vector3 c)
    {

        // Function to check if point (px, py) is inside the polygon defined by points
        bool IsPointInPolygon(Vector2[] polyPoints, Vector2 point)
        {
            int j = polyPoints.Length - 1;
            bool inside = false;
            for (int i = 0; i < polyPoints.Length; j = i++)
            {
                if (((polyPoints[i].y > point.y) != (polyPoints[j].y > point.y)) &&
                    (point.x < (polyPoints[j].x - polyPoints[i].x) * (point.y - polyPoints[i].y) / (polyPoints[j].y - polyPoints[i].y) + polyPoints[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        Vector2[] polyPoints = new Vector2[3] {
        new Vector2(a.x, a.y),
        new Vector2(b.x, b.y),
        new Vector2(c.x, c.y)
    };

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 soilPoint = new Vector3(x, y) * scale;
                Vector2 point = new Vector2(soilPoint.x, soilPoint.y);

                if (IsPointInPolygon(polyPoints, point))
                {
                    map[x, y] = 0;
                }
            }
        }
        meshGenerator.GenerateMesh(map, scale);
    }




    static bool FindPoint(float x1, float y1, float x2, float y2, float x, float y)
    {
        if (x > x1 && x < x2 && y > y1 && y < y2)
            return true;

        return false;
    }




    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;

        if (isCircle)
        {
            Gizmos.DrawWireSphere(transform.position, (width - 2) * scale * scale * 2);
        }
        else{
            Gizmos.DrawWireCube(transform.position, new Vector3((width - 2) * scale, ((height - 2) * scale), 0.01f));
        }
    }
}
