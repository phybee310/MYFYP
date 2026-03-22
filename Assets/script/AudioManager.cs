using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject modelSelectionPanel;
    [Tooltip("Drag your new panel containing the 4 BGM buttons here")]
    public GameObject bgmSelectionPanel;   // 1. ADDED: A dedicated panel for the songs
    public GameObject startButton;
    public GameObject confirmButton;

    [Header("Audio Setup")]
    public AudioSource bgmAudioSource;
    public AudioClip[] availableBGMs;

    [Header("Achievement Tracking")]
    private float bgmPlayDuration = 0f;
    private bool isBgmPlaying = false;

    private void Start()
    {
        // 2. Start by showing ONLY the model selection panel
        modelSelectionPanel.SetActive(true);
        bgmSelectionPanel.SetActive(false);
        startButton.SetActive(false);
        confirmButton.SetActive(false);
    }

    private void Update()
    {
        if (isBgmPlaying)
        {
            bgmPlayDuration += Time.deltaTime;
        }
    }

    // 3. NEW: Call this when the user is done picking their 3D model
    public void OpenBGMSelectionPanel()
    {
        modelSelectionPanel.SetActive(false); // Hide the model menu
        bgmSelectionPanel.SetActive(true);    // Show the BGM menu with the 4 buttons
    }

    // 4. Call this from the 4 BGM choice buttons inside the BGM Panel
    public void PreviewTrack(int trackIndex)
    {
        if (trackIndex >= 0 && trackIndex < availableBGMs.Length)
        {
            // Assign the clip and play it so the user can preview it
            bgmAudioSource.clip = availableBGMs[trackIndex];
            bgmAudioSource.Play();

            // Show the confirm button now that they are listening to a track
            confirmButton.SetActive(true);
        }
    }

    // 5. Call this from your "Confirm" button
    public void ConfirmSelection()
    {
        // Stop the preview so it starts fresh from the beginning later
        bgmAudioSource.Stop();

        // Hide the BGM panel and confirm button, show the start button
        bgmSelectionPanel.SetActive(false);
        confirmButton.SetActive(false);
        startButton.SetActive(true);
    }

    // 6. Call this from your "Start" button
    public void StartExperience()
    {
        startButton.SetActive(false);
        bgmAudioSource.Play();

        isBgmPlaying = true;          // Start the achievement timer!
        bgmPlayDuration = 0f;

        Debug.Log("Experience Started! Music playing.");
    }

    // 7. Call this when the user leaves the AR view
    public void StopAndSaveAchievement()
    {
        isBgmPlaying = false;
        bgmAudioSource.Stop();

        Debug.Log($"User listened to BGM for {bgmPlayDuration} seconds.");
    }
}