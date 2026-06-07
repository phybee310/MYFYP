using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Auth;

public class AuthSceneController : MonoBehaviour
{
    [Header("Scene Navigation")]
    [Tooltip("Type the exact name of your Main Menu scene here")]
    [SerializeField] private string _mainMenuSceneName = "mainpage";

   
    private void OnEnable()
    {
        FirebaseManager.OnLoginSuccess += TransitionToMainMenu;
    }

    private void OnDisable()
    {
        FirebaseManager.OnLoginSuccess -= TransitionToMainMenu;
    }

  
    private void TransitionToMainMenu(FirebaseUser user)
    {
        Debug.Log($"Login confirmed for {user.DisplayName}. Loading Main Menu...");
        SceneManager.LoadScene(_mainMenuSceneName);
    }
}
