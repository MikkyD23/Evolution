using TMPro;
using UnityEngine;

public class UiManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI HeaderText;

    private void Update()
    {
        if (Input.GetMouseButton(2))
        {
            print("mouse button 2 held");
        }
    }

    public void updateGenerationText(int number)
    {
        HeaderText.text = $"G: {number}";
    }
}
