using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnOrderEntry : MonoBehaviour
{
    [SerializeField] private Image turnOrderImage;
    [SerializeField] private TextMeshProUGUI turnOrderText;

    public void SetTurnOrderEntry(Sprite unitSprite, string unitName, int position)
    {
        turnOrderImage.sprite = unitSprite;
        turnOrderText.text = $"{position}";
    }
}
