using UnityEngine;
using DG.Tweening;

public class DissolveColor : MonoBehaviour
{
    [SerializeField] private Renderer rend;

    [SerializeField] private Color baseColor = Color.gray;
    [SerializeField] private Color targetColor = Color.green;

    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float delay = 0f;

    private MaterialPropertyBlock block;
    private float dissolve;
    private Vector2 randomOffset;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int TargetColorID = Shader.PropertyToID("_TargetColor");
    private static readonly int DissolveID = Shader.PropertyToID("_Dissolve");
    private static readonly int DissolveOffsetID = Shader.PropertyToID("_DissolveOffset");

    private void Awake()
    {
        block = new MaterialPropertyBlock();

        dissolve = 0f;

        randomOffset = new Vector2(
            UnityEngine.Random.Range(0f, 100f),
            UnityEngine.Random.Range(0f, 100f)
        );

        ApplyProperties();
        Play();
    }

    public void Play()
    {
        dissolve = 0f;

        randomOffset = new Vector2(
            UnityEngine.Random.Range(0f, 100f),
            UnityEngine.Random.Range(0f, 100f)
        );

        ApplyProperties();

        DOTween.To(
            () => dissolve,
            x =>
            {
                dissolve = x;
                ApplyProperties();
            },
            1f,
            duration
        )
        .SetDelay(1f+delay);
    }

    public void Play(Color overrideTargetColor)
    {
        targetColor = overrideTargetColor;
        Play();
    }

    private void ApplyProperties()
    {
        rend.GetPropertyBlock(block);

        block.SetColor(BaseColorID, baseColor);
        block.SetColor(TargetColorID, targetColor);
        block.SetFloat(DissolveID, dissolve);

        block.SetVector(
            DissolveOffsetID,
            new Vector4(randomOffset.x, randomOffset.y, 0f, 0f)
        );

        rend.SetPropertyBlock(block);
    }
}