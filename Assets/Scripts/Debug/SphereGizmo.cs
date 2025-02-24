using UnityEngine;

public class SphereGizmo : MonoBehaviour
{
    [SerializeField]
    Color _color = Color.red;

    [SerializeField]
    float _radius = .5f;

    void OnDrawGizmos()
    {
        Gizmos.color = _color;
        Gizmos.DrawSphere(transform.position, _radius);
    }
}
