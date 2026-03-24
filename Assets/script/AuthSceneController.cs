using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Auth;

/// <summary>
/// Listens for successful authentication events and handles scene transitions.
/// </summary>
public class AuthSceneController : MonoBehaviour
{
    [Header("Scene Navigation")]
    [Tooltip("Type the exact name of your Main Menu scene here")]
    [SerializeField] private string _mainMenuSceneName = "mainpage";

    // When this script turns on, start listening for the login event
    private void OnEnable()
    {
        FirebaseManager.OnLoginSuccess += TransitionToMainMenu;
    }

    // When this script turns off or is destroyed, stop listening (CRITICAL to prevent memory leaks!)
    private void OnDisable()
    {
        FirebaseManager.OnLoginSuccess -= TransitionToMainMenu;
    }

    // This method runs automatically the exact second OnLoginSuccess is fired
    private void TransitionToMainMenu(FirebaseUser user)
    {
        Debug.Log($"Login confirmed for {user.DisplayName}. Loading Main Menu...");
        SceneManager.LoadScene(_mainMenuSceneName);
    }
}
