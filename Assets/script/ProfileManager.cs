using UnityEngine;
using TMPro;
using Firebase.Auth;
using UnityEngine.SceneManagement;

public class ProfileManager : MonoBehaviour
{
    [Header("User Information UI")]
    [SerializeField] private TMP_Text _usernameText;
    [SerializeField] private TMP_Text _emailText;

    [Header("Scene Navigation")]
    [Tooltip("Type the exact name of your Main Menu scene here")]
    [SerializeField] private string _mainMenuSceneName = "mainpage";

    [Tooltip("Type the exact name of your Login scene here")]
    [SerializeField] private string _loginSceneName = "Login"; 

    private void Start()
    {
        LoadUserProfile();
    }

    private void LoadUserProfile()
    {
        FirebaseUser currentUser = FirebaseAuth.DefaultInstance.CurrentUser;

        if (currentUser != null)
        {
            // Display the custom UserID and the Email they registered with
            _usernameText.text = currentUser.DisplayName;
            _emailText.text = currentUser.Email;
        }
        else
        {
            _usernameText.text = "Guest";
            _emailText.text = "Not logged in";
        }
    }

    // --- BUTTON ONCLICK METHODS ---

    public void GoBackToMainMenu()
    {
        SceneManager.LoadScene(_mainMenuSceneName);
    }

    public void LogOut()
    {
        // 1. Tell Firebase to end the session
        FirebaseAuth.DefaultInstance.SignOut();

        // 2. Send the user back to the login screen
        SceneManager.LoadScene(_loginSceneName);
    }
}