using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Auth;        // Added for Cloud Save
using Firebase.Database;    // Added for Cloud Save
using Firebase.Extensions;  // Added for Cloud Save

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
        modelSelectionPanel.SetActive(true);
        bgmSelectionPanel.SetActive(false);
        timeSelectionPanel.SetActive(false);
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

    // --- STEP 1: OPEN BGM ---
    public void OpenBGMSelectionPanel()
    {
        modelSelectionPanel.SetActive(false);
        bgmSelectionPanel.SetActive(true);
    }

    public void PreviewTrack(int trackIndex)
    {
        if (trackIndex >= 0 && trackIndex < availableBGMs.Length)
        {
            bgmAudioSource.clip = availableBGMs[trackIndex];
            bgmAudioSource.Play();
            confirmBgmButton.SetActive(true);
        }
    }

    // --- STEP 2: OPEN TIME SELECTION ---
    public void ConfirmBgmSelection()
    {
        bgmAudioSource.Stop();
        bgmSelectionPanel.SetActive(false);
        confirmBgmButton.SetActive(false);

        timeSelectionPanel.SetActive(true);
    }

    // --- STEP 3: SET DURATION ---
    public void SelectTimeDuration(int minutes)
    {
        // THE BUG FIX: Force silence!
        bgmAudioSource.Stop();

        targetMeditationSeconds = minutes * 60f;
        confirmTimeButton.SetActive(true);
    }

    // --- STEP 4: CONFIRM DURATION ---
    public void ConfirmTimeSelection()
    {
        timeSelectionPanel.SetActive(false);
        confirmTimeButton.SetActive(false);
        startButton.SetActive(true);
    }

    // --- STEP 5: START MEDITATION ---
    public void StartExperience()
    {
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