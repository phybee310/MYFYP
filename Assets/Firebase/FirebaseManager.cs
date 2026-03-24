using System;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;

/// <summary>
/// Handles Firebase Authentication including user registration, login, and UI state management.
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    // Broadcasts successful login to other scripts (like AuthSceneController)
    public static event Action<FirebaseUser> OnLoginSuccess;

    [Header("Firebase State")]
    private FirebaseAuth _auth;
    private FirebaseUser _user;

    [Header("UI Panels")]
    [SerializeField] private GameObject _loginPanel;
    [SerializeField] private GameObject _registerPanel;

    [Header("Login UI")]
    [SerializeField] private TMP_InputField _loginEmailInput;
    [SerializeField] private TMP_InputField _loginPasswordInput;

    [Header("Register UI")]
    [SerializeField] private TMP_InputField _regUserIDInput;
    [SerializeField] private TMP_InputField _regEmailInput;
    [SerializeField] private TMP_InputField _regPasswordInput;
    [SerializeField] private TMP_InputField _regRetypePasswordInput;

    [Header("Alerts")]
    [SerializeField] private TMP_Text _warningText;

    private void Start()
    {
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogError($"Firebase Initialization Failed: {task.Exception}");
                SetWarningMessage("Failed to connect to authentication server.");
                return;
            }

            _auth = FirebaseAuth.DefaultInstance;
            ShowLoginPanel();
        });
    }

    // --- UI STATE MANAGEMENT ---

    public void ShowLoginPanel() => TogglePanels(showLogin: true);
    public void ShowRegisterPanel() => TogglePanels(showLogin: false);

    private void TogglePanels(bool showLogin)
    {
        _loginPanel.SetActive(showLogin);
        _registerPanel.SetActive(!showLogin);
        SetWarningMessage(string.Empty);
    }

    private void SetWarningMessage(string message)
    {
        if (_warningText != null)
            _warningText.text = message;
    }

    // --- AUTHENTICATION LOGIC ---

    public void OnRegisterButtonClicked()
    {
        if (!ValidateRegistrationInputs()) return;

        SetWarningMessage("Registering...");

        _auth.CreateUserWithEmailAndPasswordAsync(_regEmailInput.text, _regPasswordInput.text).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                SetWarningMessage("Registration was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                HandleAuthException(task.Exception);
                return;
            }

            _user = task.Result.User;

            UserProfile profile = new UserProfile { DisplayName = _regUserIDInput.text };
            _user.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(profileTask =>
            {
                SetWarningMessage($"Successfully registered as {_user.DisplayName}!");
                ShowLoginPanel();
            });
        });
    }

    public void OnLoginButtonClicked()
    {
        if (string.IsNullOrWhiteSpace(_loginEmailInput.text) || string.IsNullOrWhiteSpace(_loginPasswordInput.text))
        {
            SetWarningMessage("Please enter email and password.");
            return;
        }

        SetWarningMessage("Logging in...");

        _auth.SignInWithEmailAndPasswordAsync(_loginEmailInput.text, _loginPasswordInput.text).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                SetWarningMessage("Login was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                HandleAuthException(task.Exception);
                return;
            }

            _user = task.Result.User;
            SetWarningMessage("Login successful! Loading...");

            // Fire the event to tell AuthSceneController to load the Main Menu
            OnLoginSuccess?.Invoke(_user);
        });
    }

    // --- VALIDATION & ERROR HANDLING ---

    private bool ValidateRegistrationInputs()
    {
        if (string.IsNullOrWhiteSpace(_regUserIDInput.text) || string.IsNullOrWhiteSpace(_regEmailInput.text))
        {
            SetWarningMessage("Please fill in all fields.");
            return false;
        }

        if (_regPasswordInput.text != _regRetypePasswordInput.text)
        {
            SetWarningMessage("Passwords do not match.");
            return false;
        }

        return true;
    }

    private void HandleAuthException(Exception ex)
    {
        Debug.LogException(ex);

        if (ex.GetBaseException() is FirebaseException firebaseEx)
        {
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string errorMessage = errorCode switch
            {
                AuthError.EmailAlreadyInUse => "Email is already registered.",
                AuthError.InvalidEmail => "Invalid Email Format.",
                AuthError.WrongPassword => "Incorrect Password.",
                AuthError.UserNotFound => "Account does not exist.",
                AuthError.WeakPassword => "Password must be at least 6 characters.",
                _ => "Authentication failed. Please try again."
            };

            SetWarningMessage(errorMessage);
        }
        else
        {
            SetWarningMessage("An unexpected error occurred.");
        }
    }
}