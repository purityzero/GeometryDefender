using UnityEngine;

public class CullingObject : MonoBehaviour
{
    [SerializeField] private Renderer m_ObjectRenderer;
    [SerializeField] private RectTransform m_RectTransform;

    private Camera mainCamera;
    private bool isVisible = true;

	private bool IsInCameraView
    {
		get
		{
			if (m_ObjectRenderer != null)
			{
				Bounds bounds = m_ObjectRenderer.bounds;

				Vector3 viewportMin = mainCamera.WorldToViewportPoint(bounds.min);
				Vector3 viewportMax = mainCamera.WorldToViewportPoint(bounds.max);

				return !(viewportMax.x < 0 || viewportMax.y < 0 ||
						viewportMin.x > 1 || viewportMin.y > 1);
			}
			else if (m_RectTransform != null)
			{
				Vector3[] corners = new Vector3[4];
				m_RectTransform.GetWorldCorners(corners);

				Vector3 viewportMin = mainCamera.WorldToViewportPoint(corners[0]);
				Vector3 viewportMax = mainCamera.WorldToViewportPoint(corners[2]);

				return !(viewportMax.x < 0 || viewportMax.y < 0 ||
						viewportMin.x > 1 || viewportMin.y > 1);
			}

			return false;
		}
    }

    void Awake()
    {
        mainCamera = Camera.main;

        if (m_ObjectRenderer == null)
            m_ObjectRenderer = GetComponent<Renderer>();

        if (m_RectTransform == null)
            m_RectTransform = GetComponent<RectTransform>();

        if ( mainCamera == null)
        {
            Debug.LogWarning("Main Camera not found in the scene.");
        }

        if ( m_ObjectRenderer == null && m_RectTransform == null )
        {
            Logger.Error("Neither Renderer nor RectTransform component found on the object.");
        }
    }

    public void UpdateLogic()
    {
        if (mainCamera == null)
			return;

        bool isInView = this.IsInCameraView;

        if ( isInView != isVisible )
        {
            isVisible = isInView;
            gameObject.SetActive(isVisible);
        }

	}
}