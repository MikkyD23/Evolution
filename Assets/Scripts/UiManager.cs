using TMPro;
using UnityEngine;

public class UiManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI HeaderText;

    float MAX_PAN = 2f;
    float PAN_SPEED = 0.3f;

    float ZOOM_SPEED = 2f;
    float MAX_ZOOM = 1f;

    private void Update()
    {
        if (Input.GetMouseButton(2))
        {
            float deltaX = Mathf.Clamp(-Input.mousePositionDelta.x, -MAX_PAN, MAX_PAN) * PAN_SPEED;
            float deltaY = Mathf.Clamp(-Input.mousePositionDelta.y, -MAX_PAN, MAX_PAN) * PAN_SPEED;
            Vector3 deltaPos = new Vector3(deltaX, deltaY, 0);
            Camera.main.transform.position += deltaPos;
        }

        float newCameraZoom = Camera.main.orthographicSize - (Input.mouseScrollDelta.y * ZOOM_SPEED);
        Camera.main.orthographicSize = Mathf.Max(newCameraZoom, MAX_ZOOM);
    }

    public void updateGenerationText(int number)
    {
        HeaderText.text = $"G: {number}";
    }
}
