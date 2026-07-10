using UnityEngine;
using DG.Tweening;

public class RadialColorReveal2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer rend;
    [SerializeField] private PolygonCollider2D polygon;

    [Header("Colors")]
    [SerializeField] private Color baseColor = Color.gray;
    [SerializeField] private Color targetColor = Color.green;

    [SerializeField] private Material finalMaterial;

    [Header("Animation")]
    [SerializeField, Range(1, 3)] private int totalPoint = 2;
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private float delay = 0f;
    [SerializeField] private float edgeSoftness = 0.08f;
    [SerializeField] private float radiusMultiplier = 2f;

    [Header("Random Points")]
    [SerializeField] private float minPointDistance = 1.5f;

    private MaterialPropertyBlock block;
    private Sequence sequence;

    private readonly Vector2[] centers = new Vector2[3];
    private readonly float[] radiuses = new float[3];

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int TargetColorID = Shader.PropertyToID("_TargetColor");

    private static readonly int RevealCenter1ID = Shader.PropertyToID("_RevealCenter1");
    private static readonly int RevealCenter2ID = Shader.PropertyToID("_RevealCenter2");
    private static readonly int RevealCenter3ID = Shader.PropertyToID("_RevealCenter3");

    private static readonly int RevealRadius1ID = Shader.PropertyToID("_RevealRadius1");
    private static readonly int RevealRadius2ID = Shader.PropertyToID("_RevealRadius2");
    private static readonly int RevealRadius3ID = Shader.PropertyToID("_RevealRadius3");

    private static readonly int EdgeSoftnessID = Shader.PropertyToID("_EdgeSoftness");

    private void Awake()
    {
        if (rend == null)
            rend = GetComponent<Renderer>();

        if (polygon == null)
            polygon = GetComponent<PolygonCollider2D>();

        block = new MaterialPropertyBlock();

        ResetReveal();
        //ApplyProperties();

        //PlayRandomPoints();
    }

    public void SetBaseColor(Color color)
    {
        baseColor = color;
                block.SetColor(BaseColorID, color);
        ApplyProperties();
    }

    public void SetTargetColor(Color color)
    {
        targetColor = color;
                block.SetColor(TargetColorID, targetColor);

    }
/*
    public void PlayRandomPoints()
    {
        sequence?.Kill();

        totalPoint = Mathf.Clamp(totalPoint, 1, 3);

        ResetReveal();

        for (int i = 0; i < totalPoint; i++)
        {
            centers[i] = GetRandomPointInsidePolygonFarFromOthers(i);
            radiuses[i] = -edgeSoftness;
        }

        ApplyProperties();

        sequence = DOTween.Sequence();

        for (int i = 0; i < totalPoint; i++)
        {
            int index = i;

            float targetRadius =
                GetNeededRadius(centers[index]) * radiusMultiplier
                + edgeSoftness;

            Tween tween = DOTween.To(
                () => radiuses[index],
                x =>
                {
                    radiuses[index] = x;
                    ApplyProperties();
                },
                targetRadius,
                duration
            )
            .SetEase(Ease.InQuad);

            sequence.Join(tween);
        }

        if (delay > 0f)
            sequence.SetDelay(delay);
    }*/

    public void PlayRandomPoints(Material matAfterDissolve)
    {
        //targetColor = overrideColor;
        finalMaterial = matAfterDissolve;
        ApplyProperties();
        PlayRandomPoints();
    }


    public void PlayRandomPoints()
    {
        sequence?.Kill();

        totalPoint = Mathf.Clamp(totalPoint, 1, 3);

        ResetReveal();
        for (int i = 0; i < totalPoint; i++)
        {
            centers[i] = GetRandomPointInsidePolygonFarFromOthers(i);
            radiuses[i] = -edgeSoftness;
        }

        ApplyProperties();

        sequence = DOTween.Sequence();

        for (int i = 0; i < totalPoint; i++)
        {
            int index = i;

            float targetRadius =
                GetNeededRadius(centers[index]) * radiusMultiplier
                + edgeSoftness;

            float pointDelay =
                delay > 0f
                    ? UnityEngine.Random.Range(0f, delay)
                    : 0f;

            Tween tween = DOTween.To(
                () => radiuses[index],
                x =>
                {
                    radiuses[index] = x;
                    ApplyProperties();
                },
                targetRadius,
                duration
            )
            .SetEase(Ease.InQuad);

            sequence.Insert(pointDelay, tween);
        }

        sequence.OnComplete(() =>
        {
            rend.material = finalMaterial;
           // Debug.Log(finalMaterial.name);
            //rend.material.color = ta("_Color", targetColor);
        });
    }



    private void ResetReveal()
    {
        Vector2 defaultCenter = transform.position;

        for (int i = 0; i < 3; i++)
        {
            centers[i] = defaultCenter;
            radiuses[i] = -edgeSoftness;
        }
    }

    private Vector2 GetRandomPointInsidePolygonFarFromOthers(int currentIndex)
    {
        Bounds bounds = polygon.bounds;

        Vector2 bestPoint = bounds.center;
        float bestMinDistance = -1f;

        for (int attempt = 0; attempt < 200; attempt++)
        {
            Vector2 point = new Vector2(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y)
            );

            if (!polygon.OverlapPoint(point))
                continue;

            if (currentIndex == 0)
                return point;

            float minDistance = float.MaxValue;

            for (int i = 0; i < currentIndex; i++)
            {
                float distance = Vector2.Distance(point, centers[i]);
                minDistance = Mathf.Min(minDistance, distance);
            }

            if (minDistance > bestMinDistance)
            {
                bestMinDistance = minDistance;
                bestPoint = point;
            }

            if (minDistance >= minPointDistance)
                return point;
        }

        return bestPoint;
    }

    private float GetNeededRadius(Vector2 centerWorld)
    {
        float maxDistance = 0f;

        foreach (Vector2 localPoint in polygon.points)
        {
            Vector3 worldPoint = polygon.transform.TransformPoint(localPoint);
            float distance = Vector2.Distance(centerWorld, worldPoint);

            if (distance > maxDistance)
                maxDistance = distance;
        }

        return maxDistance;
    }

    private void ApplyProperties()
    {
        rend.GetPropertyBlock(block);

        block.SetColor(BaseColorID, baseColor);
        block.SetColor(TargetColorID, targetColor);

        block.SetVector(RevealCenter1ID, new Vector4(centers[0].x, centers[0].y, 0f, 0f));
        block.SetVector(RevealCenter2ID, new Vector4(centers[1].x, centers[1].y, 0f, 0f));
        block.SetVector(RevealCenter3ID, new Vector4(centers[2].x, centers[2].y, 0f, 0f));

        block.SetFloat(RevealRadius1ID, radiuses[0]);
        block.SetFloat(RevealRadius2ID, radiuses[1]);
        block.SetFloat(RevealRadius3ID, radiuses[2]);

        block.SetFloat(EdgeSoftnessID, edgeSoftness);

        rend.SetPropertyBlock(block);
    }

    private void OnDestroy()
    {
        sequence?.Kill();
    }
}