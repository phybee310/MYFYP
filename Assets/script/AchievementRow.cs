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

    // NEW: The visual lock that covers the achievement
    [SerializeField] private GameObject _lockOverlay;

    // The manager calls this method to set up the row's visuals
    public void UpdateUI(string title, string description, bool isUnlocked)
    {
        _titleText.text = title;
        _descriptionText.text = description;

        if (isUnlocked)
        {
            // Unlocked: Bright colors, hide the lock overlay
            _backgroundPanel.color = Color.white;
            _iconImage.color = Color.white;
            _titleText.color = Color.black;

            // Turn off the lock image
            if (_lockOverlay != null) _lockOverlay.SetActive(false);
        }
        else
        {
            // Locked: Greyed out, show the lock overlay
            _backgroundPanel.color = new Color(0.8f, 0.8f, 0.8f);
            _iconImage.color = new Color(0.5f, 0.5f, 0.5f);
            _titleText.color = Color.gray;

            // Turn on the lock image
            if (_lockOverlay != null) _lockOverlay.SetActive(true);
        }
    }
}