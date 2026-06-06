using UnityEngine;

public class TitleSquareEffect : MonoBehaviour
{
    private float Speed = 5f;

    private Vector2 m_Direction;
    private Camera m_MainCamera;
    private Vector2 m_HalfObjectSize;
    private float m_RotationSpeed;

    private void Start()
    {
        m_MainCamera = Camera.main;
        if (m_MainCamera == null)
        {
            Debug.Log($"[TitleSquareEffect] Camera.main을 찾을 수 없어 컴포넌트를 비활성화합니다.");
            enabled = false;
            return;
        }

        m_Direction = Random.insideUnitCircle.normalized;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            m_HalfObjectSize = spriteRenderer.bounds.extents;

        Speed = Random.Range(1f, 5f);
        m_RotationSpeed = Random.Range(30f, 120f) * (Random.value > 0.5f ? 1f : -1f);
    }

    private void Update()
    {
        if(SceneManager.instance.IsSceneTransitioning == true)
            return;

        Move();
        Rotate();
        CheckBounce();
    }

    private void Rotate()
    {
        transform.Rotate(0f, 0f, m_RotationSpeed * Time.deltaTime);
    }

    private void Move()
    {
        transform.Translate(m_Direction * Speed * Time.deltaTime, Space.World);
    }

    private void CheckBounce()
    {
        if (m_MainCamera == null)
            return;

        float cameraHalfHeight = m_MainCamera.orthographicSize;
        float cameraHalfWidth  = cameraHalfHeight * m_MainCamera.aspect;
        Vector3 cameraPosition = m_MainCamera.transform.position;

        float minX = cameraPosition.x - cameraHalfWidth  + m_HalfObjectSize.x;
        float maxX = cameraPosition.x + cameraHalfWidth  - m_HalfObjectSize.x;
        float minY = cameraPosition.y - cameraHalfHeight + m_HalfObjectSize.y;
        float maxY = cameraPosition.y + cameraHalfHeight - m_HalfObjectSize.y;

        Vector3 position = transform.position;
        bool isPositionChanged = false;

        if (position.x <= minX || position.x >= maxX)
        {
            m_Direction.x = -m_Direction.x;
            position.x = Mathf.Clamp(position.x, minX, maxX);
            isPositionChanged = true;
        }

        if (position.y <= minY || position.y >= maxY)
        {
            m_Direction.y = -m_Direction.y;
            position.y = Mathf.Clamp(position.y, minY, maxY);
            isPositionChanged = true;
        }

        if (isPositionChanged == true)
            transform.position = position;
    }
}
