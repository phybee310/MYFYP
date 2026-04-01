using System; // Required for timestamps
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase.AI;
using Firebase.Database;
using Firebase.Auth;

public class ChatbotManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField userInputField;
    public Button sendButton;
    public Button backButton;
    public Transform chatContentContainer;

    [Header("Chat Bubble Prefabs")]
    public GameObject userBubblePrefab;
    public GameObject botBubblePrefab;

    [Header("Scene Settings")]
    public string mainMenuSceneName = "mainpage";

    [SerializeField] private string _loginSceneName = "Login";

    [Header("User Settings")]
    private string userId;

    // --- STATIC & DATABASE VARIABLES ---
    private static GenerativeModel _model;
    private static Chat _chatSession;
    private static List<ChatMessage> _chatHistory = new List<ChatMessage>();
    private static bool _isInitialized = false;

    private DatabaseReference dbReference;

    // Added [Serializable] so Unity can convert it to JSON for Firebase
    [System.Serializable]
    public struct ChatMessage
    {
        public string text;
        public bool isBot;
        public long timestamp;
    }

    private void Start()
    {
        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        }
        else
        {
            Debug.LogError("No user is logged in! Returning to Login Screen...");
            SceneManager.LoadScene(_loginSceneName);
            return;
        }

        sendButton.onClick.AddListener(OnSendButtonClicked);
        backButton.onClick.AddListener(ReturnToMainMenu);

        // THE FIX: Use your custom FirebaseConfig URL just like the Achievement script!
        dbReference = FirebaseDatabase.GetInstance(FirebaseConfig.DatabaseURL).RootReference;

        if (!_isInitialized)
        {
            LoadHistoryFromFirebase();
        }
        else
        {
            RebuildChatHistory();
        }
    }

    private async void LoadHistoryFromFirebase()
    {
        try
        {
            // Pull the user's chat history from Firebase
            DataSnapshot snapshot = await dbReference.Child("users").Child(userId).Child("chats").GetValueAsync();

            if (snapshot != null && snapshot.Exists)
            {
                foreach (DataSnapshot child in snapshot.Children)
                {
                    // Convert the Firebase JSON back into our struct
                    string json = child.GetRawJsonValue();
                    ChatMessage msg = JsonUtility.FromJson<ChatMessage>(json);

                    _chatHistory.Add(msg);
                    SpawnBubble(msg.text, msg.isBot);
                }
            }
            else
            {
               
                string welcomeText = "Hello! I am your AI mental health assistant. How are you feeling today?";
                SpawnBubble(welcomeText, true);
                SaveToHistory(welcomeText, true);
            }

            // Now that UI is loaded, boot up the AI
            InitializeAI();
            _isInitialized = true;

            // Push the scrollbar to the bottom
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContentContainer.GetComponent<RectTransform>());
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load Firebase history: {e.Message}");
        }
    }

    private void InitializeAI()
    {
        var ai = FirebaseAI.GetInstance(FirebaseAI.Backend.GoogleAI());
        _model = ai.GetGenerativeModel(modelName: "gemini-2.5-flash");
        _chatSession = _model.StartChat();

        Debug.Log("Google AI Initialized successfully via Firebase!");
    }

    private void RebuildChatHistory()
    {
        // Rebuild from local RAM (Used when switching back from the Main Menu)
        foreach (ChatMessage message in _chatHistory)
        {
            SpawnBubble(message.text, message.isBot);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContentContainer.GetComponent<RectTransform>());
    }

    public async void OnSendButtonClicked()
    {
        string userText = userInputField.text;
        if (string.IsNullOrWhiteSpace(userText)) return;

        userInputField.text = "";
        sendButton.interactable = false;

        // 1. Spawn user bubble and save to Firebase
        SpawnBubble(userText, false);
        SaveToHistory(userText, false);

        // 2. Send to AI
        await SendMessageToGemini(userText);
    }

    private async Task SendMessageToGemini(string userText)
    {
        GameObject botBubble = SpawnBubble("Typing...", true);
        TMP_Text botTextComponent = botBubble.GetComponentInChildren<TMP_Text>();

        try
        {
            var response = await _chatSession.SendMessageAsync(userText);

            string finalResponse = response.Text ?? "No text in response.";
            botTextComponent.text = finalResponse;

            // Save the AI's final answer to Firebase
            SaveToHistory(finalResponse, true);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"AI Error: {ex.Message}");
            botTextComponent.text = "Sorry, my connection was interrupted. Please try again.";
        }
        finally
        {
            botTextComponent.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(botBubble.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContentContainer.GetComponent<RectTransform>());

            sendButton.interactable = true;
        }
    }

    // --- HELPER METHODS ---

    private void SaveToHistory(string messageText, bool wasSentByBot)
    {
        // 1. Create the message with a timestamp
        ChatMessage newMsg = new ChatMessage
        {
            text = messageText,
            isBot = wasSentByBot,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        // 2. Save locally for fast scene switching
        _chatHistory.Add(newMsg);

        // 3. Push to Firebase Realtime Database
        string json = JsonUtility.ToJson(newMsg);
        dbReference.Child("users").Child(userId).Child("chats").Push().SetRawJsonValueAsync(json);
    }

    private GameObject SpawnBubble(string text, bool isBot)
    {
        GameObject prefabToUse = isBot ? botBubblePrefab : userBubblePrefab;
        GameObject bubble = Instantiate(prefabToUse, chatContentContainer, false);

        TMP_Text textComponent = bubble.GetComponentInChildren<TMP_Text>();
        textComponent.text = text;

        textComponent.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(bubble.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContentContainer.GetComponent<RectTransform>());

        return bubble;
    }

    private void ReturnToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}