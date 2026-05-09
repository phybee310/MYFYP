using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.EventSystems; // NEW: Required to detect which button was clicked!

public class AudioManager : MonoBehaviour
{
    [Header("Scene Navigation")]
    [SerializeField] private string _mainMenuSceneName = "mainpage";

    [Header("UI Panels")]
    public GameObject modelSelectionPanel;
    public GameObject bgmSelectionPanel;
    public GameObject timeSelectionPanel;
    public GameObject pausePanel;

    [Header("UI Buttons")]
    public GameObject startButton;
    public GameObject confirmBgmButton;
    public GameObject confirmTimeButton;

    // --- NEW: BUTTON VISUAL ARRAYS ---
    [Header("Button Visuals")]
    [SerializeField] private Color _defaultButtonColor = Color.white;
    [SerializeField] private Color _selectedOverlayColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public Button[] timeButtons;
    public Button[] bgmButtons;
    public Button[] modelButtons;

    [Header("Timer UI")]
    public GameObject timerContainer;
    public TMP_Text timerText;
    public Image progressBar;

    [Header("Audio Setup")]
    public AudioSource bgmAudioSource;
    public AudioClip[] availableBGMs;

    [Header("Meditation State Tracking")]
    private float targetMeditationSeconds = 0f;
    private float currentMeditationSeconds = 0f;
    private bool isMeditating = false;
    private bool isPaused = false;

    private void Start()
    {
        timeSelectionPanel.SetActive(true);
        bgmSelectionPanel.SetActive(false);
        modelSelectionPanel.SetActive(false);

        pausePanel.SetActive(false);
        timerContainer.SetActive(false);
        startButton.SetActive(false);
        confirmBgmButton.SetActive(false);
        confirmTimeButton.SetActive(false);
    }

    private void Update()
    {
        if (isMeditating && !isPaused)
        {
            currentMeditationSeconds += Time.deltaTime;

            if (progressBar != null)
            {
                progressBar.fillAmount = currentMeditationSeconds / targetMeditationSeconds;
            }

            UpdateTimerText();

            if (currentMeditationSeconds >= targetMeditationSeconds)
            {
                EndSession();
            }
        }
    }

    private void UpdateTimerText()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(currentMeditationSeconds / 60F);
        int seconds = Mathf.FloorToInt(currentMeditationSeconds - minutes * 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // --- BUTTON VISUAL OVERLAY HELPER ---
    private void ApplyDarkOverlay(Button[] buttonGroup)
    {
        // Find exactly which button was just clicked using Unity's EventSystem
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null) return;

        GameObject clickedButton = EventSystem.current.currentSelectedGameObject;

        foreach (Button btn in buttonGroup)
        {
            if (btn != null)
            {
                Image btnImage = btn.GetComponent<Image>();
                if (btnImage != null)
                {
                    // If this is the button we clicked, turn it grey. Otherwise, turn it white.
                    btnImage.color = (btn.gameObject == clickedButton) ? _selectedOverlayColor : _defaultButtonColor;
                }
            }
        }
    }

    // --- STEP 1: TIME SELECTION ---
    public void SelectTimeDuration(int minutes)
    {
        bgmAudioSource.Stop();
        targetMeditationSeconds = minutes * 60f;
        confirmTimeButton.SetActive(true);

        // NEW: Update the visual colors for the Time buttons
        ApplyDarkOverlay(timeButtons);
    }

    public void ConfirmTimeSelection()
    {
        timeSelectionPanel.SetActive(false);
        confirmTimeButton.SetActive(false);
        bgmSelectionPanel.SetActive(true);
    }

    // --- STEP 2: BGM SELECTION ---
    public void PreviewTrack(int trackIndex)
    {
        if (trackIndex >= 0 && trackIndex < availableBGMs.Length)
        {
            bgmAudioSource.clip = availableBGMs[trackIndex];
            bgmAudioSource.Play();
            confirmBgmButton.SetActive(true);

            // NEW: Update the visual colors for the BGM buttons
            ApplyDarkOverlay(bgmButtons);
        }
    }

    public void ConfirmBgmSelection()
    {
        bgmAudioSource.Stop();
        bgmSelectionPanel.SetActive(false);
        confirmBgmButton.SetActive(false);
        modelSelectionPanel.SetActive(true);
    }

    // --- STEP 3: MODEL SELECTION ---

    // NEW: Call this from your Model selection buttons just to update the grey overlay!
    public void SelectModelVisual()
    {
        ApplyDarkOverlay(modelButtons);
    }

    // --- START MEDITATION (Triggered after placing the AR model) ---
    public void StartExperience()
    {
        modelSelectionPanel.SetActive(false);
        startButton.SetActive(false);

        timerContainer.SetActive(true);
        bgmAudioSource.Play();

        isMeditating = true;
        isPaused = false;

        currentMeditationSeconds = 0f;
        if (progressBar != null) progressBar.fillAmount = 0f;
        UpdateTimerText();
    }

    // --- DYNAMIC BACK BUTTON LOGIC ---
    public void OnBackButtonPressed()
    {
        if (isMeditating)
        {
            isPaused = true;
            bgmAudioSource.Pause();
            pausePanel.SetActive(true);
        }
        else
        {
            SceneManager.LoadScene(_mainMenuSceneName);
        }
    }

    // --- PAUSE MENU LOGIC ---
    public void ResumeMeditation()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        bgmAudioSource.Play();
    }

    // --- END SESSION & FIREBASE SAVE LOGIC ---
    public void EndSession()
    {
        isMeditating = false;
        bgmAudioSource.Stop();

        Debug.Log($"Session Ended! Total time: {currentMeditationSeconds} seconds. Saving to cloud...");
        SaveDataToCloud(currentMeditationSeconds);
    }

    private void SaveDataToCloud(float newSessionSeconds)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;

        if (user == null)
        {
            SceneManager.LoadScene(_mainMenuSceneName);
            return;
        }

        DatabaseReference dbRef = FirebaseDatabase.GetInstance(FirebaseConfig.DatabaseURL).RootReference;
        string userId = user.UserId;

        dbRef.Child("users").Child(userId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                float existingTime = 0f;
                int existingSessions = 0;

                if (task.Result.HasChild("TotalMeditationTime"))
                {
                    existingTime = float.Parse(task.Result.Child("TotalMeditationTime").Value.ToString());
                }

                if (task.Result.HasChild("TotalMeditationSessions"))
                {
                    existingSessions = int.Parse(task.Result.Child("TotalMeditationSessions").Value.ToString());
                }

                float updatedTime = existingTime + newSessionSeconds;
                int updatedSessions = existingSessions + 1;

                dbRef.Child("users").Child(userId).Child("TotalMeditationTime").SetValueAsync(updatedTime);
                dbRef.Child("users").Child(userId).Child("TotalMeditationSessions").SetValueAsync(updatedSessions);

                Debug.Log($"Cloud Save Successful! Lifetime Time: {updatedTime}, Lifetime Sessions: {updatedSessions}");
            }
            else
            {
                Debug.LogError("Failed to save data to Firebase.");
            }

            SceneManager.LoadScene(_mainMenuSceneName);
        });
    }
}