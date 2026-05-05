using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text _welcomeText;

    [Header("Onboarding Flow (First Time Setup)")]
    [SerializeField] private GameObject _onboardingPanel;
    [SerializeField] private GameObject[] _onboardingSteps;

    [SerializeField] private GameObject _nextButtonObject;
    [SerializeField] private TMP_Text _nextButtonText;

    [Header("Animation Settings")]
    [SerializeField] private float _fadeDuration = 0.4f;
    private bool _isTransitioning = false;

    [Header("Step 1: Age Range Buttons")]
    [SerializeField] private Button[] _ageButtons;
    private string _selectedAgeRange = "";

    [Header("Step 2: Occupation Buttons")]
    [SerializeField] private Button[] _occupationButtons;
    private string _selectedOccupation = "";

    [Header("Step 3: Goal Buttons")]
    [SerializeField] private Button[] _goalButtons;
    private List<string> _selectedGoals = new List<string>();

    [Header("Step 4: Tone Preference Buttons")]
    [SerializeField] private Button[] _toneButtons;
    private string _selectedTone = "";

    [Header("Button Selection Visuals")]
    [SerializeField] private Color _defaultButtonColor = Color.white;
    [SerializeField] private Color _selectedOverlayColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Scene Navigation")]
    [SerializeField] private string _profileSceneName = "Profile";
    [SerializeField] private string _chatbotSceneName = "Chatbot";
    [SerializeField] private string _meditationSceneName = "planedetection";

    private DatabaseReference _dbReference;
    private string _userId;
    private int _currentStepIndex = 0;

    private void Start()
    {
        FirebaseUser currentUser = FirebaseAuth.DefaultInstance.CurrentUser;

        if (_onboardingPanel != null) _onboardingPanel.SetActive(false);

        if (currentUser != null)
        {
            _userId = currentUser.UserId;
            string userName = currentUser.DisplayName;
            _welcomeText.text = $"Welcome, {userName}!";

            _dbReference = FirebaseDatabase.GetInstance(FirebaseConfig.DatabaseURL).RootReference;
            CheckIfFirstTimeUser();
        }
        else
        {
            _welcomeText.text = "Welcome, Guest!";
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("User pressed the back button. Exiting app...");
            Application.Quit();
        }
    }

    public void SelectAgeOption(int index)
    {
        if (_isTransitioning) return;
        _selectedAgeRange = GetButtonText(_ageButtons, index);
        ApplyDarkOverlay(_ageButtons, index);
        if (_nextButtonObject != null) _nextButtonObject.SetActive(true);
    }

    public void SelectOccupationOption(int index)
    {
        if (_isTransitioning) return;
        _selectedOccupation = GetButtonText(_occupationButtons, index);
        ApplyDarkOverlay(_occupationButtons, index);
        if (_nextButtonObject != null) _nextButtonObject.SetActive(true);
    }

    public void SelectGoalOption(int index)
    {
        if (_isTransitioning) return;

        string clickedText = GetButtonText(_goalButtons, index);

        if (_selectedGoals.Contains(clickedText))
        {
            _selectedGoals.Remove(clickedText);
        }
        else
        {
            if (_selectedGoals.Count >= 3)
            {
                Debug.LogWarning("You can only select up to 3 goals.");
                return;
            }
            _selectedGoals.Add(clickedText);
        }

        ApplyMultiDarkOverlay(_goalButtons, _selectedGoals);

        if (_nextButtonObject != null)
        {
            _nextButtonObject.SetActive(_selectedGoals.Count > 0);
        }
    }

    public void SelectToneOption(int index)
    {
        if (_isTransitioning) return;
        _selectedTone = GetButtonText(_toneButtons, index);
        ApplyDarkOverlay(_toneButtons, index);
        if (_nextButtonObject != null) _nextButtonObject.SetActive(true);
    }

    private string GetButtonText(Button[] buttonGroup, int index)
    {
        TMP_Text btnText = buttonGroup[index].GetComponentInChildren<TMP_Text>();
        return btnText != null ? btnText.text : "Unknown";
    }

    private void ApplyDarkOverlay(Button[] buttonGroup, int selectedIndex)
    {
        for (int i = 0; i < buttonGroup.Length; i++)
        {
            Image btnImage = buttonGroup[i].GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.color = (i == selectedIndex) ? _selectedOverlayColor : _defaultButtonColor;
            }
        }
    }

    private void ApplyMultiDarkOverlay(Button[] buttonGroup, List<string> selectedList)
    {
        for (int i = 0; i < buttonGroup.Length; i++)
        {
            Image btnImage = buttonGroup[i].GetComponent<Image>();
            string btnText = GetButtonText(buttonGroup, i);

            if (btnImage != null)
            {
                btnImage.color = selectedList.Contains(btnText) ? _selectedOverlayColor : _defaultButtonColor;
            }
        }
    }

    private void CheckIfFirstTimeUser()
    {
        _dbReference.Child("users").Child(_userId).Child("profileData").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted) return;

            DataSnapshot snapshot = task.Result;

            if (snapshot == null || !snapshot.Exists)
            {
                _onboardingPanel.SetActive(true);
                _currentStepIndex = 0;
                PrepFirstStep();
            }
            else
            {
                _onboardingPanel.SetActive(false);
            }
        });
    }

    private void PrepFirstStep()
    {
        for (int i = 0; i < _onboardingSteps.Length; i++)
        {
            _onboardingSteps[i].SetActive(i == 0);
            CanvasGroup cg = _onboardingSteps[i].GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = (i == 0) ? 1f : 0f;
        }

        if (_nextButtonObject != null) _nextButtonObject.SetActive(false);
    }

    public void OnNextButtonClicked()
    {
        if (_isTransitioning) return;

        if (_currentStepIndex == 1 && string.IsNullOrWhiteSpace(_selectedAgeRange))
        {
            Debug.LogWarning("Please select an age range.");
            return;
        }
        if (_currentStepIndex == 2 && string.IsNullOrWhiteSpace(_selectedOccupation))
        {
            Debug.LogWarning("Please select an occupation.");
            return;
        }
        if (_currentStepIndex == 3 && _selectedGoals.Count == 0)
        {
            Debug.LogWarning("Please select a goal.");
            return;
        }
        if (_currentStepIndex == 4 && string.IsNullOrWhiteSpace(_selectedTone))
        {
            Debug.LogWarning("Please select a chatbot tone preference.");
            return;
        }

        if (_currentStepIndex < _onboardingSteps.Length - 1)
        {
            int oldIndex = _currentStepIndex;
            _currentStepIndex++;
            StartCoroutine(FadeTransition(oldIndex, _currentStepIndex));
        }
        else
        {
            SaveUserProfileData();
        }
    }

    private IEnumerator FadeTransition(int oldStepIndex, int newStepIndex)
    {
        _isTransitioning = true;

        if (_nextButtonObject != null) _nextButtonObject.SetActive(false);

        CanvasGroup oldCanvasGroup = _onboardingSteps[oldStepIndex].GetComponent<CanvasGroup>();
        CanvasGroup newCanvasGroup = _onboardingSteps[newStepIndex].GetComponent<CanvasGroup>();

        if (oldCanvasGroup == null || newCanvasGroup == null)
        {
            Debug.LogWarning("Missing CanvasGroup! Skipping animation.");
            _onboardingSteps[oldStepIndex].SetActive(false);
            _onboardingSteps[newStepIndex].SetActive(true);
            FinishTransition(newStepIndex);
            yield break;
        }

        newCanvasGroup.alpha = 0f;
        _onboardingSteps[newStepIndex].SetActive(true);

        float elapsedTime = 0f;
        while (elapsedTime < _fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float lerpVal = elapsedTime / _fadeDuration;

            oldCanvasGroup.alpha = Mathf.Lerp(1f, 0f, lerpVal);
            newCanvasGroup.alpha = Mathf.Lerp(0f, 1f, lerpVal);

            yield return null;
        }

        oldCanvasGroup.alpha = 0f;
        newCanvasGroup.alpha = 1f;

        _onboardingSteps[oldStepIndex].SetActive(false);
        FinishTransition(newStepIndex);
    }

    private void FinishTransition(int newStepIndex)
    {
        if (_nextButtonText != null)
        {
            _nextButtonText.text = (newStepIndex == _onboardingSteps.Length - 1) ? "Finish" : "Next";
        }

        _isTransitioning = false;
    }

    private void SaveUserProfileData()
    {
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
                Debug.Log("Profile data saved successfully!");
                _onboardingPanel.SetActive(false);
            }
        });
    }

    public void GoToProfile() => SceneManager.LoadScene(_profileSceneName);
    public void GoToChatbot() => SceneManager.LoadScene(_chatbotSceneName);
    public void GoToMeditation() => SceneManager.LoadScene(_meditationSceneName);
}