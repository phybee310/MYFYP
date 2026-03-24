using UnityEngine;
using TMPro;
using Firebase.Auth;
using UnityEngine.SceneManagement; // REQUIRED for loading scenes

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Drag the TextMeshPro text where you want the user's name to appear")]
    [SerializeField] private TMP_Text _welcomeText;

    [Header("Scene Navigation")]
    [Tooltip("Type the exact name of your Profile scene here")]
    [SerializeField] private string _profileSceneName = "Profile";

    [Tooltip("Type the exact name of your Login scene here")]
    [SerializeField] private string _loginSceneName = "Login";

    private void Start()
    {
        // 1. Ask Firebase who is currently logged in
        FirebaseUser currentUser = FirebaseAuth.DefaultInstance.CurrentUser;

        // 2. If someone is logged in, grab their DisplayName
        if (currentUser != null)
        {
            string userName = currentUser.DisplayName;
            _welcomeText.text = $"Welcome, {userName}!";
        }
        else
        {
            // Fallback just in case they reached this scene without logging in
            _welcomeText.text = "Welcome, Guest!";
        }
    }

    // --- BUTTON ONCLICK METHODS ---

    // Hook this up to your new "Profile" button!
    public void GoToProfile()
    {
        SceneManager.LoadScene(_profileSceneName);
    }

   
}