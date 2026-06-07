using System;
using System.Collections; // NEW: Required for Coroutines
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine.InputSystem;

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
    [SerializeField] private GameObject _resetPasswordPanel;

    [Header("Login UI")]
    [SerializeField] private TMP_InputField _loginEmailInput;
    [SerializeField] private TMP_InputField _loginPasswordInput;

    [Header("Register UI")]
    [SerializeField] private TMP_InputField _regUserIDInput;
    [SerializeField] private TMP_InputField _regEmailInput;
    [SerializeField] private TMP_InputField _regPasswordInput;
    [SerializeField] private TMP_InputField _regRetypePasswordInput;

    [Header("Reset Password UI")]
    [SerializeField] private TMP_InputField _resetEmailInput;

    [Header("Popup Alert UI")]
    [SerializeField] private GameObject _popupPanel;
    [SerializeField] private TMP_Text _popupText;

    // NEW: Variable to keep track of the active popup timer
    private Coroutine _popupCoroutine;

    private void Start()
    {
        if (_popupPanel != null) _popupPanel.SetActive(false);
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogError($"Firebase Initialization Failed: {task.Exception}");
                ShowPopupMessage("Failed to connect to authentication server.");
                return;
            }

            _auth = FirebaseAuth.DefaultInstance;
            ShowLoginPanel();
        });
    }

    // --- UI STATE MANAGEMENT ---
    private void Update()
    {
        // Check the virtual keyboard for the escape key (Android Back Button)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("User pressed the back button. Exiting app...");
            Application.Quit();
        }
    }

    public void ShowLoginPanel() => TogglePanels(showLogin: true, showRegister: false, showReset: false);
    public void ShowRegisterPanel() => TogglePanels(showLogin: false, showRegister: true, showReset: false);
    public void ShowResetPasswordPanel() => TogglePanels(showLogin: false, showRegister: false, showReset: true);

    private void TogglePanels(bool showLogin, bool showRegister, bool showReset)
    {
        if (_loginPanel != null) _loginPanel.SetActive(showLogin);
        if (_registerPanel != null) _registerPanel.SetActive(showRegister);
        if (_resetPasswordPanel != null) _resetPasswordPanel.SetActive(showReset);
    }

    // --- POPUP LOGIC (UPDATED FOR AUTO-HIDE) ---

    private void ShowPopupMessage(string message)
    {
        if (_popupText != null) _popupText.text = message;
        if (_popupPanel != null) _popupPanel.SetActive(true);

        // Stop the previous timer if a new message appears quickly
        if (_popupCoroutine != null)
        {
            StopCoroutine(_popupCoroutine);
        }

        // Start a new 5-second countdown
        _popupCoroutine = StartCoroutine(HidePopupRoutine(5f));
    }

    // The coroutine that waits and then hides the panel
    private IEnumerator HidePopupRoutine(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (_popupPanel != null) _popupPanel.SetActive(false);
    }

    // --- AUTHENTICATION LOGIC ---

    public void OnRegisterButtonClicked()
    {
        if (!ValidateRegistrationInputs()) return;

        ShowPopupMessage("Registering...");

        _auth.CreateUserWithEmailAndPasswordAsync(_regEmailInput.text.Trim(), _regPasswordInput.text).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                ShowPopupMessage("Registration was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                HandleAuthException(task.Exception);
                return;
            }

            _user = task.Result.User;

            string cleanUsername = _regUserIDInput.text.Trim();
            UserProfile profile = new UserProfile { DisplayName = cleanUsername };

            _user.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(profileTask =>
            {
                ShowPopupMessage($"Successfully registered as {_user.DisplayName}!");
                ShowLoginPanel();
            });
        });
    }

    public void OnLoginButtonClicked()
    {
        if (string.IsNullOrWhiteSpace(_loginEmailInput.text) || string.IsNullOrWhiteSpace(_loginPasswordInput.text))
        {
            ShowPopupMessage("Please enter email and password.");
            return;
        }

        ShowPopupMessage("Logging in...");

        _auth.SignInWithEmailAndPasswordAsync(_loginEmailInput.text.Trim(), _loginPasswordInput.text).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                ShowPopupMessage("Login was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                HandleAuthException(task.Exception);
                return;
            }

            _user = task.Result.User;
            ShowPopupMessage("Login successful! Loading...");

            OnLoginSuccess?.Invoke(_user);
        });
    }

    // --- RESET PASSWORD LOGIC ---
    public void OnResetPasswordButtonClicked()
    {
        string email = _resetEmailInput.text.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowPopupMessage("Please enter your email to reset your password.");
            return;
        }

        ShowPopupMessage("Checking email...");

        _auth.FetchProvidersForEmailAsync(email).ContinueWithOnMainThread(fetchTask =>
        {
            if (fetchTask.IsCanceled || fetchTask.IsFaulted)
            {
                HandleAuthException(fetchTask.Exception);
                return;
            }

            var providers = fetchTask.Result;
            if (providers == null || !providers.GetEnumerator().MoveNext())
            {
                ShowPopupMessage("This email is not registered in our system.");
                return;
            }

            _auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(resetTask =>
            {
                if (resetTask.IsCanceled)
                {
                    ShowPopupMessage("Password reset was canceled.");
                    return;
                }
                if (resetTask.IsFaulted)
                {
                    HandleAuthException(resetTask.Exception);
                    return;
                }

                ShowPopupMessage("Password reset email sent! Please check your inbox or spam email.");
                _resetEmailInput.text = "";
            });
        });
    }

    // --- VALIDATION & ERROR HANDLING ---

    private bool ValidateRegistrationInputs()
    {
        string username = _regUserIDInput.text.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(_regEmailInput.text))
        {
            ShowPopupMessage("Please fill in all fields.");
            return false;
        }

        if (!Regex.IsMatch(username, "^[a-zA-Z0-9_]+$"))
        {
            ShowPopupMessage("Your username should contain letters, numbers, and underscores only.");
            return false;
        }

        if (_regPasswordInput.text != _regRetypePasswordInput.text)
        {
            ShowPopupMessage("Passwords do not match.");
            return false;
        }

        return true;
    }

    private void HandleAuthException(Exception ex)
    {
        Debug.LogWarning($"Auth Error Caught: {ex.GetBaseException().Message}");

        if (ex.GetBaseException() is FirebaseException firebaseEx)
        {
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string errorMessage = errorCode switch
            {
                AuthError.EmailAlreadyInUse => "Email is already registered.",
                AuthError.InvalidEmail => "Invalid Email Format.",
                AuthError.WeakPassword => "Password must be at least 6 characters.",
                AuthError.InvalidCredential => "Account not found or incorrect password.",
                AuthError.WrongPassword => "Incorrect Password.",
                AuthError.UserNotFound => "Account does not exist.",
                AuthError.Failure => "Account not found or incorrect password.",

                _ => "Authentication failed. Please try again."
            };

            if (firebaseEx.Message.ToLower().Contains("internal error"))
            {
                errorMessage = "Account not found or incorrect password.";
            }

            ShowPopupMessage(errorMessage);
        }
        else
        {
            ShowPopupMessage("An unexpected error occurred. Please try again.");
        }
    }
}