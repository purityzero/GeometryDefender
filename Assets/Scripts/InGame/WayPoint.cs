using UnityEngine;

public class WayPoint : MonoSingleton<WayPoint>
{
    public float Radius = 12f;
    public bool isAutoRadiusFromCamera = true;

    [SerializeField] private int m_GizmoSegments = 64;
    [SerializeField] private Color m_GizmoColor = Color.cyan;

    private void Start()
    {
        if (isAutoRadiusFromCamera == true)
            Radius = CalculateMinScreenRadius();
    }

    public Vector2 GetRandomWayPoint()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float xPosition = Mathf.Cos(angle) * Radius;
        float yPosition = Mathf.Sin(angle) * Radius;
        return (Vector2)transform.position + new Vector2(xPosition, yPosition);
    }

    private float CalculateMinScreenRadius()
    {
        if (Camera.main == null)
            return Radius;

        if (Camera.main.orthographic == false)
            return Radius;

        float halfHeight = Camera.main.orthographicSize;
        float halfWidth = halfHeight * Camera.main.aspect;
        return Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) + 1f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = m_GizmoColor;

        float drawRadius = (isAutoRadiusFromCamera && Camera.main != null) ? CalculateMinScreenRadius() : Radius;

        for (int i = 0; i < m_GizmoSegments; i++)
        {
            float currentAngle = (i / (float)m_GizmoSegments) * Mathf.PI * 2f;
            float nextAngle = ((i + 1) / (float)m_GizmoSegments) * Mathf.PI * 2f;

            Vector3 currentPoint = transform.position + new Vector3(Mathf.Cos(currentAngle) * drawRadius, Mathf.Sin(currentAngle) * drawRadius, 0f);
            Vector3 nextPoint = transform.position + new Vector3(Mathf.Cos(nextAngle) * drawRadius, Mathf.Sin(nextAngle) * drawRadius, 0f);

            Gizmos.DrawLine(currentPoint, nextPoint);
        }
    }
}
