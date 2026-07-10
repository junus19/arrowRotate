using UnityEngine;

public class Stamp : MonoBehaviour
{
    public enum CutterType
    {
        Circle,
        Rectangle,
        Triangle,
        AntiCircle
    }
    public CutterType cutterType;

    [ConditionalHide(nameof(cutterType), 1)]
    public float radius = 1f;

    public float rotation = 0f;
    //[ConditionalHide(nameof(cutterType), 1)]
    //public float radius2 = 2f;


    [ConditionalHide(nameof(cutterType), 0)]
    public Vector2 size = Vector2.one;


    //public bool reverse = false;


    //Vector3 leftTop => transform.position + ((-transform.right * size.x * 0.5f) + (transform.up * size.y * 0.5f)) * 0.7f;
    //Vector3 rightTop => transform.position + ((transform.right * size.x * 0.5f) + (transform.up * size.y * 0.5f)) * 0.7f;
    //Vector3 rightBottom => transform.position + ((transform.right * size.x * 0.5f) + (-transform.up * size.y * 0.5f)) * 0.7f;
    //Vector3 leftBottom => transform.position + ((-transform.right * size.x * 0.5f) + (-transform.up * size.y * 0.5f)) * 0.7f;

    public Vector3 leftTop => transform.position + ((-transform.right * size.x * 0.5f) + (transform.up * size.y * 0.5f)) * 0.7f;
    public Vector3 rightTop => transform.position + ((transform.right * size.x * 0.5f) + (transform.up * size.y * 0.5f)) * 0.7f;
    public Vector3 rightBottom => transform.position + ((transform.right * size.x * 0.5f) + (-transform.up * size.y * 0.5f)) * 0.7f;
    public Vector3 leftBottom => transform.position + ((-transform.right * size.x * 0.5f) + (-transform.up * size.y * 0.5f)) * 0.7f;


    // calculate triangle points using radius
    public Vector3 leftTop2 => transform.position + ((-transform.right * radius * 0.5f) + (transform.up * radius * 0.5f)) * 0.7f;
    public Vector3 rightTop2 => transform.position + ((transform.right * radius * 0.5f) + (transform.up * radius * 0.5f)) * 0.7f;
    public Vector3 rightBottom2 => transform.position + ((transform.right * radius * 0.5f) + (-transform.up * radius * 0.5f)) * 0.7f;
    /*
    public void stampCut(RegionGenerator myRegion)
    {


        
        if (cutterType == CutterType.Circle)
        {
            if (!reverse)
            {
                myRegion.circleCut(transform.position, radius);
            }
            else
            {
                myRegion.circleCut2(transform.position, radius, radius2);
            }
            
        }
        else
        {
            //myRegion.rectCut(leftTop, rightTop, rightBottom, leftBottom);
            myRegion.rectCut(rightTop, leftBottom);
        }
    }
    */



#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (cutterType == CutterType.Circle || cutterType == CutterType.AntiCircle)
        {
            /*
            if (!reverse)
            {
                UnityEditor.Handles.color = Color.red;

                UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.back, radius * 0.7f);
            }
            else
            {
                UnityEditor.Handles.color = Color.blue;

                UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.back, radius * 0.7f);
            }
            */
            UnityEditor.Handles.color = Color.red;

            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.back, radius * 0.7f);

        }
        else
        {
            Gizmos.color = Color.red;

            Gizmos.DrawLine(leftTop, rightTop);
            Gizmos.DrawLine(rightTop, rightBottom);
            Gizmos.DrawLine(rightBottom, leftBottom);
            Gizmos.DrawLine(leftBottom, leftTop);
        }
    }
#endif
}
