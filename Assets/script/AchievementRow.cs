using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementRow : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _backgroundPanel;
    [SerializeField] private GameObject _lockOverlay;

    public void UpdateUI(string title, string description, bool isUnlocked)
    {
        _titleText.text = title;
        _descriptionText.text = description;

        if (isUnlocked)
        {
           
            _backgroundPanel.color = Color.white;
            _iconImage.color = Color.white;
            _titleText.color = Color.black;

            if (_lockOverlay != null) _lockOverlay.SetActive(false);
        }
        else
        {
           
            _backgroundPanel.color = new Color(0.8f, 0.8f, 0.8f);
            _iconImage.color = new Color(0.5f, 0.5f, 0.5f);
            _titleText.color = Color.gray;

            if (_lockOverlay != null) _lockOverlay.SetActive(true);
        }
    }
}