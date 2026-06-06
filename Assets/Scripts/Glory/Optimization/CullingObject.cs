using UnityEngine;

public class CullingObject : MonoBehaviour
{
    private Camera mainCamera;
    private Renderer objectRenderer;
    private RectTransform rectTransform;
    private bool isVisible = true;

	private bool IsInCameraView
    {
		get
		{
			if (objectRenderer != null)
			{
				Bounds bounds = objectRenderer.bounds;

				Vector3 viewportMin = mainCamera.WorldToViewportPoint(bounds.min);
				Vector3 viewportMax = mainCamera.WorldToViewportPoint(bounds.max);

				return !(viewportMax.x < 0 || viewportMax.y < 0 ||
						viewportMin.x > 1 || viewportMin.y > 1);
			}
			else if (rectTransform != null)
			{
				Vector3[] corners = new Vector3[4];
				rectTransform.GetWorldCorners(corners);

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
        objectRenderer = GetComponent<Renderer>();
        rectTransform = GetComponent<RectTransform>();

        if ( mainCamera == null)
        {
            Debug.LogWarning("Main Camera not found in the scene.");
        }

        if ( objectRenderer == null && rectTransform == null )
        {
            Debug.LogError("Neither Renderer nor RectTransform component found on the object.");
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