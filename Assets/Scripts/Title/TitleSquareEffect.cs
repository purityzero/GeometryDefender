using UnityEngine;

public class TitleSquareEffect : UpdatableBehaviour
{
    [SerializeField] private SpriteRenderer m_SpriteRenderer;

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
            // 등록 자체는 OnEnable(UpdatableBehaviour)에서 이미 끝난 뒤라 UpdateLogic()은 계속 불림 — 여기서 리턴해도 m_Direction/m_RotationSpeed가 기본값(0)이라 Move/Rotate가 사실상 무해한 공회전만 함(CheckBounce는 카메라 null 가드 있음)
            Logger.Log($"[TitleSquareEffect] Camera.main을 찾을 수 없어 초기화를 건너뜁니다.");
            return;
        }

        m_Direction = Random.insideUnitCircle.normalized;

        if (m_SpriteRenderer == null)
            m_SpriteRenderer = GetComponent<SpriteRenderer>();

        if (m_SpriteRenderer != null)
        {
            // 오브젝트가 계속 회전하므로 축 정렬 경계(extents)를 그대로 쓰면 45도 부근에서 대각선만큼 실제 경계보다 작게 잡혀 화면 밖으로 삐져나감 — 회전각과 무관하게 항상 안전한 대각선 반지름으로 고정
            float diagonalHalfExtent = m_SpriteRenderer.bounds.extents.magnitude;
            m_HalfObjectSize = new Vector2(diagonalHalfExtent, diagonalHalfExtent);
        }

        Speed = Random.Range(1f, 5f);
        m_RotationSpeed = Random.Range(30f, 120f) * (Random.value > 0.5f ? 1f : -1f);

        SetRandomPosition();
    }

    private void SetRandomPosition()
    {
        Rect moveArea = GetMoveArea();
        Vector3 position = transform.position;
        position.x = Random.Range(moveArea.xMin, moveArea.xMax);
        position.y = Random.Range(moveArea.yMin, moveArea.yMax);
        transform.position = position;
    }

    private Rect GetMoveArea()
    {
        float cameraHalfHeight = m_MainCamera.orthographicSize;
        float cameraHalfWidth  = cameraHalfHeight * m_MainCamera.aspect;
        Vector3 cameraPosition = m_MainCamera.transform.position;

        float minX = cameraPosition.x - cameraHalfWidth  + m_HalfObjectSize.x;
        float maxX = cameraPosition.x + cameraHalfWidth  - m_HalfObjectSize.x;
        float minY = cameraPosition.y - cameraHalfHeight + m_HalfObjectSize.y;
        float maxY = cameraPosition.y + cameraHalfHeight - m_HalfObjectSize.y;

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    public override void UpdateLogic()
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
        // 씬 전환(InGame→Title 등) 중 캐싱된 카메라가 파괴되면 == null이 되어 여기서 계속 조기 리턴하게 되는데,
        // Move()는 카메라와 무관하게 계속 실행되므로 반사 판정만 영구히 멈춰 사각형이 경계 밖으로 계속 나가버린다.
        // CullingObject.UpdateLogic()과 동일한 패턴으로 null이면 재조회해 다음 프레임부터 자연 복구되게 한다.
        if (m_MainCamera == null)
            m_MainCamera = Camera.main;

        if (m_MainCamera == null)
            return;

        Rect moveArea = GetMoveArea();

        Vector3 position = transform.position;
        bool isPositionChanged = false;

        if (position.x <= moveArea.xMin || position.x >= moveArea.xMax)
        {
            m_Direction.x = -m_Direction.x;
            position.x = Mathf.Clamp(position.x, moveArea.xMin, moveArea.xMax);
            isPositionChanged = true;
        }

        if (position.y <= moveArea.yMin || position.y >= moveArea.yMax)
        {
            m_Direction.y = -m_Direction.y;
            position.y = Mathf.Clamp(position.y, moveArea.yMin, moveArea.yMax);
            isPositionChanged = true;
        }

        if (isPositionChanged == true)
            transform.position = position;
    }
}
