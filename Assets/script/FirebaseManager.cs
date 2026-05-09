using System;
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
    [SerializeField] private GameObject _resetPasswordPanel; // NEW: Added Reset Password Panel

    [Header("Login UI")]
    [SerializeField] private TMP_InputField _loginEmailInput;
    [SerializeField] private TMP_InputField _loginPasswordInput;

    [Header("Register UI")]
    [SerializeField] private TMP_InputField _regUserIDInput;
    [SerializeField] private TMP_InputField _regEmailInput;
    [SerializeField] private TMP_InputField _regPasswordInput;
    [SerializeField] private TMP_InputField _regRetypePasswordInput;

    // NEW: Input field for the reset password email
    [Header("Reset Password UI")]
    [SerializeField] private TMP_InputField _resetEmailInput;

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

    // NEW: Method to show the reset password panel
    public void ShowResetPasswordPanel() => TogglePanels(showLogin: false, showRegister: false, showReset: true);

    // NEW: Updated TogglePanels to handle 3 panels instead of 2
    private void TogglePanels(bool showLogin, bool showRegister, bool showReset)
    {
        if (_loginPanel != null) _loginPanel.SetActive(showLogin);
        if (_registerPanel != null) _registerPanel.SetActive(showRegister);
        if (_resetPasswordPanel != null) _resetPasswordPanel.SetActive(showReset);
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

        _auth.CreateUserWithEmailAndPasswordAsync(_regEmailInput.text.Trim(), _regPasswordInput.text).ContinueWithOnMainThread(task =>
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

            string cleanUsername = _regUserIDInput.text.Trim();
            UserProfile profile = new UserProfile { DisplayName = cleanUsername };

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

        _auth.SignInWithEmailAndPasswordAsync(_loginEmailInput.text.Trim(), _loginPasswordInput.text).ContinueWithOnMainThread(task =>
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

            OnLoginSuccess?.Invoke(_user);
        });
    }

    // --- NEW: RESET PASSWORD LOGIC ---
    public void OnResetPasswordButtonClicked()
    {
        string email = _resetEmailInput.text.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            SetWarningMessage("Please enter your email to reset your password.");
            return;
        }

        

        _auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                SetWarningMessage("Password reset was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                HandleAuthException(task.Exception);
                return;
            }

            SetWarningMessage("Password reset email sent, Check your inbox.");
            _resetEmailInput.text = ""; 
        });
    }

    // --- VALIDATION & ERROR HANDLING ---

    private bool ValidateRegistrationInputs()
    {
        string username = _regUserIDInput.text.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(_regEmailInput.text))
        {
            SetWarningMessage("Please fill in all fields.");
            return false;
        }

        if (!Regex.IsMatch(username, "^[a-zA-Z0-9_]+$"))
        {
            SetWarningMessage("Your username should contains letters,numbers and underscore only.");
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

            SetWarningMessage(errorMessage);
        }
        else
        {
            SetWarningMessage("An unexpected error occurred. Please try again.");
        }
    }
}