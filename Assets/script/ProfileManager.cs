using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.SceneManagement;

public class ProfileManager : MonoBehaviour
{
    [Header("User Information UI")]
    [SerializeField] private TMP_Text _usernameText;
    [SerializeField] private TMP_Text _emailText;

    [Header("Scene Navigation")]
    [SerializeField] private string _mainMenuSceneName = "mainpage";
    [SerializeField] private string _loginSceneName = "Login";

    [Header("Edit Profile Navigation Panels")]
    [SerializeField] private GameObject _editMenuPanel;
    [SerializeField] private GameObject _agePanel;
    [SerializeField] private GameObject _occupationPanel;
    [SerializeField] private GameObject _goalPanel;
    [SerializeField] private GameObject _tonePanel;

    [Header("Username Panel")]
    [SerializeField] private GameObject _usernamePanel;
    [SerializeField] private TMP_InputField _newUsernameInput;

    [Header("Edit Buttons (Drag your UI buttons here)")]
    [SerializeField] private Color _defaultButtonColor = Color.white;
    [SerializeField] private Color _selectedOverlayColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Button[] _ageButtons;
    [SerializeField] private Button[] _occupationButtons;
    [SerializeField] private Button[] _goalButtons;
    [SerializeField] private Button[] _toneButtons;

    [Header("Feedback UI")]
    [SerializeField] private GameObject _saveConfirmationPopup;
    [SerializeField] private TMP_Text _popupTextComponent;
    [SerializeField] private float _popupDuration = 2f;
    private Coroutine _activePopupCoroutine;

    private string _selectedAgeRange = "";
    private string _selectedOccupation = "";
    private List<string> _selectedGoals = new List<string>();
    private string _selectedTone = "";

    private DatabaseReference _dbReference;
    private string _userId;
    private UserProfileData _cachedDbData;

    [System.Serializable]
    public class UserProfileData
    {
        public string ageRange;
        public string occupation;
        public string[] mainGoal;
        public string tonePreference;
    }

    private void Start()
    {
        FirebaseUser currentUser = FirebaseAuth.DefaultInstance.CurrentUser;

        CloseEntireEditSystem();

        if (_saveConfirmationPopup != null) _saveConfirmationPopup.SetActive(false);

        if (currentUser != null)
        {
            _userId = currentUser.UserId;
            _usernameText.text = currentUser.DisplayName;
            _emailText.text = currentUser.Email;
            _dbReference = FirebaseDatabase.GetInstance(FirebaseConfig.DatabaseURL).RootReference;
        }
        else
        {
            _usernameText.text = "Guest";
            _emailText.text = "Not logged in";
        }
    }


    public void GoBackToMainMenu() => SceneManager.LoadScene(_mainMenuSceneName);

    public void LogOut()
    {
      
        FirebaseAuth.DefaultInstance.SignOut();

       
        PlayerPrefs.SetInt("RememberMePref", 0);
        PlayerPrefs.Save();

        SceneManager.LoadScene(_loginSceneName);
    }


    public void OpenEditMenu()
    {
        FetchCurrentPreferences();
        ShowOnlyPanel(_editMenuPanel);
    }

    public void GoBackToEditMenu()
    {
        RevertUnsavedChanges();
        ShowOnlyPanel(_editMenuPanel);
    }
    public void CloseEntireEditSystem() => ShowOnlyPanel(null);

    public void OpenAgePanel() => ShowOnlyPanel(_agePanel);
    public void OpenOccupationPanel() => ShowOnlyPanel(_occupationPanel);
    public void OpenGoalPanel() => ShowOnlyPanel(_goalPanel);
    public void OpenTonePanel() => ShowOnlyPanel(_tonePanel);
    public void OpenUsernamePanel() => ShowOnlyPanel(_usernamePanel);

    private void ShowOnlyPanel(GameObject panelToActivate)
    {
        if (_editMenuPanel != null) _editMenuPanel.SetActive(false);
        if (_agePanel != null) _agePanel.SetActive(false);
        if (_occupationPanel != null) _occupationPanel.SetActive(false);
        if (_goalPanel != null) _goalPanel.SetActive(false);
        if (_tonePanel != null) _tonePanel.SetActive(false);
        if (_usernamePanel != null) _usernamePanel.SetActive(false);

        if (panelToActivate != null) panelToActivate.SetActive(true);
    }


    private void FetchCurrentPreferences()
    {
        _dbReference.Child("users").Child(_userId).Child("profileData").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot != null && snapshot.Exists)
                {
                    string json = snapshot.GetRawJsonValue();
                    _cachedDbData = JsonUtility.FromJson<UserProfileData>(json);
                    RevertUnsavedChanges();
                }
            }
        });
    }

    public void SelectAgeOption(int index) { _selectedAgeRange = GetButtonText(_ageButtons, index); RefreshAllButtonVisuals(); }
    public void SelectOccupationOption(int index) { _selectedOccupation = GetButtonText(_occupationButtons, index); RefreshAllButtonVisuals(); }
    public void SelectGoalOption(int index)
    {
        string clickedText = GetButtonText(_goalButtons, index);
        if (_selectedGoals.Contains(clickedText)) _selectedGoals.Remove(clickedText);
        else if (_selectedGoals.Count < 3) _selectedGoals.Add(clickedText);
        RefreshAllButtonVisuals();
    }
    public void SelectToneOption(int index) { _selectedTone = GetButtonText(_toneButtons, index); RefreshAllButtonVisuals(); }

    private string GetButtonText(Button[] buttonGroup, int index)
    {
        TMP_Text btnText = buttonGroup[index].GetComponentInChildren<TMP_Text>();
        return btnText != null ? btnText.text : "Unknown";
    }

    private void RefreshAllButtonVisuals()
    {
        ApplyDarkOverlay(_ageButtons, _selectedAgeRange);
        ApplyDarkOverlay(_occupationButtons, _selectedOccupation);
        ApplyDarkOverlay(_toneButtons, _selectedTone);

        for (int i = 0; i < _goalButtons.Length; i++)
        {
            Image btnImage = _goalButtons[i].GetComponent<Image>();
            string btnText = GetButtonText(_goalButtons, i);
            if (btnImage != null) btnImage.color = _selectedGoals.Contains(btnText) ? _selectedOverlayColor : _defaultButtonColor;
        }
    }

    private void ApplyDarkOverlay(Button[] buttonGroup, string selectedText)
    {
        for (int i = 0; i < buttonGroup.Length; i++)
        {
            Image btnImage = buttonGroup[i].GetComponent<Image>();
            string btnText = GetButtonText(buttonGroup, i);
            if (btnImage != null) btnImage.color = (btnText == selectedText) ? _selectedOverlayColor : _defaultButtonColor;
        }
    }

    private bool HasProfileChanged()
    {
        if (_cachedDbData == null) return true;

        if (_selectedAgeRange != _cachedDbData.ageRange) return true;
        if (_selectedOccupation != _cachedDbData.occupation) return true;
        if (_selectedTone != _cachedDbData.tonePreference) return true;

        if (_cachedDbData.mainGoal == null && _selectedGoals.Count > 0) return true;
        if (_cachedDbData.mainGoal != null && _selectedGoals.Count != _cachedDbData.mainGoal.Length) return true;

        if (_cachedDbData.mainGoal != null)
        {
            foreach (string goal in _cachedDbData.mainGoal)
            {
                if (!_selectedGoals.Contains(goal)) return true;
            }
        }

        return false;
    }


    public void SaveUpdatedPreferences()
    {

        if (_selectedGoals.Count == 0)
        {
            TriggerPopup("You must choose at least 1 goal");
            return; 
        }

        if (!HasProfileChanged())
        {
            TriggerPopup("No changes were made, please choose again");
            return;
        }

        string formattedGoalsArray = "[\"" + string.Join("\", \"", _selectedGoals) + "\"]";

        string json = $@"{{
            ""ageRange"": ""{_selectedAgeRange}"",
            ""occupation"": ""{_selectedOccupation}"",
            ""mainGoal"": {formattedGoalsArray},
            ""tonePreference"": ""{_selectedTone}""
        }}";

        _dbReference.Child("users").Child(_userId).Child("profileData").SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                if (_cachedDbData == null) _cachedDbData = new UserProfileData();
                _cachedDbData.ageRange = _selectedAgeRange;
                _cachedDbData.occupation = _selectedOccupation;
                _cachedDbData.tonePreference = _selectedTone;
                _cachedDbData.mainGoal = _selectedGoals.ToArray();

                GoBackToEditMenu();
                TriggerPopup("Preferences updated");
            }
        });
    }

    private void RevertUnsavedChanges()
    {
        if (_cachedDbData != null)
        {
            _selectedAgeRange = _cachedDbData.ageRange;
            _selectedOccupation = _cachedDbData.occupation;
            _selectedTone = _cachedDbData.tonePreference;

            _selectedGoals.Clear();
            if (_cachedDbData.mainGoal != null) _selectedGoals.AddRange(_cachedDbData.mainGoal);

            RefreshAllButtonVisuals();
        }
    }

    public void SaveNewUsername()
    {
        string newName = _newUsernameInput.text.Trim();

        if (string.IsNullOrWhiteSpace(newName))
        {
            TriggerPopup("Username cannot be empty");
            return;
        }

        if (!Regex.IsMatch(newName, "^[a-zA-Z0-9_]+$"))
        {
            TriggerPopup("Your name should contain letters, numbers and underscores only");
            return;
        }

        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null)
        {
            if (newName == user.DisplayName)
            {
                TriggerPopup("Please enter a different username");
                return;
            }

            UserProfile profileUpdate = new UserProfile { DisplayName = newName };

            user.UpdateUserProfileAsync(profileUpdate).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    _usernameText.text = newName;
                    _newUsernameInput.text = "";

                    GoBackToEditMenu();
                    TriggerPopup("Username updated");
                }
                else
                {
                    TriggerPopup("Failed to update username");
                }
            });
        }
    }

    private void TriggerPopup(string message)
    {
        if (_saveConfirmationPopup == null) return;

        if (_activePopupCoroutine != null)
        {
            StopCoroutine(_activePopupCoroutine);
        }

        _activePopupCoroutine = StartCoroutine(ShowPopupRoutine(message));
    }

    private IEnumerator ShowPopupRoutine(string message)
    {
        if (_popupTextComponent != null) _popupTextComponent.text = message;

        _saveConfirmationPopup.SetActive(true);
        yield return new WaitForSeconds(_popupDuration);
        _saveConfirmationPopup.SetActive(false);

        _activePopupCoroutine = null;
    }
}