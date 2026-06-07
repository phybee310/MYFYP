using System;
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
    [Header("UI Elements (Current Chat)")]
    public TMP_InputField userInputField;
    public Button sendButton;
    public Button backButton;
    public Transform chatContentContainer;

    [Header("UI Elements (History Master List)")]
    public GameObject historyListPanel;
    public Transform historyListContainer;
    public Button viewHistoryButton;
    public Button closeHistoryListButton;

    [Header("UI Elements (History Detail View)")]
    public GameObject historyDetailPanel;
    public Transform historyDetailContainer;
    public TMP_Text historyDetailTitleText;
    public Button backToHistoryListButton;

    [Header("Keyboard Handler")]
    public RectTransform bottomInputPanel;
    public Canvas mainCanvas;
    public float keyboardAnimationSpeed = 10f;
    public float keyboardHeightRatio = 0.4f;

    [Header("Prefabs")]
    public GameObject userBubblePrefab;
    public GameObject botBubblePrefab;
    public GameObject dateButtonPrefab;

    [Header("Scene Settings")]
    public string mainMenuSceneName = "mainpage";
    [SerializeField] private string _loginSceneName = "Login";

    private string userId;
    private string userName = "User";

    private static GenerativeModel _model;
    private static Chat _chatSession;
    private static List<ChatMessage> _chatHistory = new List<ChatMessage>();
    private static bool _isInitialized = false;

    private DatabaseReference dbReference;
    private Dictionary<string, List<ChatMessage>> _groupedHistory = new Dictionary<string, List<ChatMessage>>();

    private float _defaultY;
    private float _targetY;
    private bool _keyboardVisible;

    private Dictionary<string, string> goalCategoryMap = new Dictionary<string, string>()
    {
        { "Reduce overthinking", "Emotional Regulation" },
        { "Reduce stress", "Emotional Regulation" },
        { "Improve sleep quality", "Sleep and Recovery" },
        { "Increase daily motivation", "Personal Growth" },
        { "Improve self-confidence", "Personal Growth" },
        { "Time management", "Productivity" },
        { "Feel more positive", "Mindset Shift" },
        { "Emotion management", "Stress Management" }
    };

    [System.Serializable]
    public struct ChatMessage
    {
        public string text;
        public bool isBot;
        public long timestamp;
    }

    [System.Serializable]
    public class UserProfileData
    {
        public string ageRange;
        public string occupation;
        public string[] mainGoal;
        public string tonePreference;
    }

    private void Start()
    {
        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            if (!string.IsNullOrEmpty(FirebaseAuth.DefaultInstance.CurrentUser.DisplayName))
            {
                userName = FirebaseAuth.DefaultInstance.CurrentUser.DisplayName;
            }
        }
        else
        {
            SceneManager.LoadScene(_loginSceneName);
            return;
        }

        sendButton.onClick.AddListener(OnSendButtonClicked);
        backButton.onClick.AddListener(ReturnToMainMenu);

        if (viewHistoryButton != null) viewHistoryButton.onClick.AddListener(OpenHistoryList);
        if (closeHistoryListButton != null) closeHistoryListButton.onClick.AddListener(CloseHistoryList);
        if (backToHistoryListButton != null) backToHistoryListButton.onClick.AddListener(OpenHistoryList);

        if (historyListPanel != null) historyListPanel.SetActive(false);
        if (historyDetailPanel != null) historyDetailPanel.SetActive(false);

        if (bottomInputPanel != null)
        {
            _defaultY = bottomInputPanel.anchoredPosition.y;
            _targetY = _defaultY;
        }

        dbReference = FirebaseDatabase.GetInstance(FirebaseConfig.DatabaseURL).RootReference;

        if (!_isInitialized)
        {
            LoadUserDataAndHistory();
        }
        else
        {
            StartNewConversation();
        }
    }

    private void Update()
    {
#if UNITY_ANDROID
        UpdateKeyboardState();
        UpdatePanelPosition();
#endif
    }

    private void UpdateKeyboardState()
    {
        bool visible = false;

        if (TouchScreenKeyboard.visible)
        {
            visible = true;
        }

        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            var selected = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            if (selected != null &&
                (selected.GetComponent<TMP_InputField>() != null ||
                 selected.GetComponent<InputField>() != null))
            {
                visible = true;
            }
        }

        _keyboardVisible = visible;
    }

    private float GetKeyboardHeight()
    {
        float height = TouchScreenKeyboard.area.height;

        if (height <= 0)
        {
            height = Screen.height * keyboardHeightRatio;
        }

        return height / mainCanvas.scaleFactor;
    }

    private void UpdatePanelPosition()
    {
        if (bottomInputPanel == null || mainCanvas == null) return;

        if (_keyboardVisible)
        {
            float keyboardHeight = GetKeyboardHeight();
            _targetY = _defaultY + keyboardHeight;
        }
        else
        {
            _targetY = _defaultY;
        }

        Vector2 pos = bottomInputPanel.anchoredPosition;
        pos.y = Mathf.Lerp(pos.y, _targetY, Time.deltaTime * keyboardAnimationSpeed);
        bottomInputPanel.anchoredPosition = pos;
    }

    private async void LoadUserDataAndHistory()
    {
        try
        {
            UserProfileData userProfile = null;
            DataSnapshot profileSnapshot = await dbReference.Child("users").Child(userId).Child("profileData").GetValueAsync();

            if (profileSnapshot != null && profileSnapshot.Exists)
            {
                string profileJson = profileSnapshot.GetRawJsonValue();
                userProfile = JsonUtility.FromJson<UserProfileData>(profileJson);

                Debug.Log($"[Firebase] Profile data found and loaded for user: {userName}");
            }
            else
            {
                Debug.LogWarning($"[Firebase] No profile data found for user: {userName}");
            }

            DataSnapshot chatSnapshot = await dbReference.Child("users").Child(userId).Child("chats").GetValueAsync();

            if (chatSnapshot != null && chatSnapshot.Exists)
            {
                foreach (DataSnapshot child in chatSnapshot.Children)
                {
                    string json = child.GetRawJsonValue();
                    ChatMessage msg = JsonUtility.FromJson<ChatMessage>(json);
                    _chatHistory.Add(msg);
                }
            }

            InitializeAI(userProfile);
            _isInitialized = true;
            StartNewConversation();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firebase Error] Failed to load data: {e.Message}");
        }
    }

    private void StartNewConversation()
    {
        string welcomeText = $"Hello {userName}! I am your AI mental health assistant. How are you feeling today?<sprite name=\"smile\">";
        SpawnBubble(welcomeText, true, chatContentContainer);
    }

    public void OpenHistoryList()
    {
        historyDetailPanel.SetActive(false);
        historyListPanel.SetActive(true);

        foreach (Transform child in historyListContainer)
        {
            Destroy(child.gameObject);
        }

        _chatHistory.Sort((a, b) => b.timestamp.CompareTo(a.timestamp));
        _groupedHistory.Clear();

        foreach (ChatMessage message in _chatHistory)
        {
            string dateLabel = GetDateLabelFromTimestamp(message.timestamp);

            if (!_groupedHistory.ContainsKey(dateLabel))
            {
                _groupedHistory[dateLabel] = new List<ChatMessage>();
            }
            _groupedHistory[dateLabel].Add(message);
        }

        foreach (var kvp in _groupedHistory)
        {
            string dateString = kvp.Key;
            List<ChatMessage> messagesForThatDay = kvp.Value;

            messagesForThatDay.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));

            GameObject buttonObj = Instantiate(dateButtonPrefab, historyListContainer, false);
            TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
            if (buttonText != null) buttonText.text = dateString;

            Button btn = buttonObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OpenHistoryDetail(dateString, messagesForThatDay));
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(historyListContainer.GetComponent<RectTransform>());
    }

    private void OpenHistoryDetail(string dateLabel, List<ChatMessage> messages)
    {
        historyListPanel.SetActive(false);
        historyDetailPanel.SetActive(true);

        if (historyDetailTitleText != null) historyDetailTitleText.text = dateLabel;

        foreach (Transform child in historyDetailContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (ChatMessage msg in messages)
        {
            SpawnBubble(msg.text, msg.isBot, historyDetailContainer);
        }
    }

    public void CloseHistoryList()
    {
        historyListPanel.SetActive(false);
        historyDetailPanel.SetActive(false);
    }

    private string GetDateLabelFromTimestamp(long unixSeconds)
    {
        DateTime messageDate = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToLocalTime().DateTime;
        DateTime today = DateTime.Now.Date;

        if (messageDate.Date == today) return "Today";
        else if (messageDate.Date == today.AddDays(-1)) return "Yesterday";
        else return messageDate.ToString("MMMM dd, yyyy");
    }

    private string GetBroadCategoriesForAI(string[] savedGoals)
    {
        List<string> broadCategories = new List<string>();

        if (savedGoals != null)
        {
            foreach (string goal in savedGoals)
            {
                if (goalCategoryMap.TryGetValue(goal, out string category))
                {
                    if (!broadCategories.Contains(category))
                    {
                        broadCategories.Add(category);
                    }
                }
            }
        }

        return broadCategories.Count > 0 ? string.Join(", ", broadCategories) : "General Wellness";
    }

    private void InitializeAI(UserProfileData profile)
    {
        var ai = FirebaseAI.GetInstance(FirebaseAI.Backend.GoogleAI());

        string systemInstructionText = "Role: Empathetic mental health assistant.\n" +
                                        "CRITICAL STYLE RULE: Conversational,point form, Mobile-friendly.\n" +
                                        "Safety: You are NOT a doctor. No diagnoses or prescriptions. If crisis/self-harm, gently urge professional help,provide MALAYSIA emergency hotlines.\n";

        if (profile != null)
        {
            string goalsForPrompt = GetBroadCategoriesForAI(profile.mainGoal);
            string rawGoals = profile.mainGoal != null ? string.Join(", ", profile.mainGoal) : "None";


            Debug.Log("========== GEMINI AI PROFILE CONTEXT ==========");
            Debug.Log($"Injected Age: {profile.ageRange}");
            Debug.Log($"Injected Occupation: {profile.occupation}");
            Debug.Log($"Injected Raw Goals: {rawGoals}");
            Debug.Log($"Injected Goal Categories: {goalsForPrompt}");
            Debug.Log($"Injected Tone: {profile.tonePreference}");
            Debug.Log("===============================================");

            systemInstructionText +=
                $"User Context: Age {profile.ageRange}, {profile.occupation}. Goals: {goalsForPrompt}. Tone: {profile.tonePreference}.\n" +
                "Directives:\n" +
                "- Match vocab to age (13-17: simple; 18-24: relatable; 25-50: practical; 50+: experienced).\n" +
                "- Advice must fit their occupation's routine.\n" +
                "- Listen first. Don't immediately jump to solutions.\n" +
                "- Subtly align responses with their context/goals without awkwardly repeating them.\n"+
                "- CRITICAL STYLE RULE: NEVER use regular Unicode emojis. Instead, naturally insert 1 TextMeshPro sprite tags to express emotions. Use ONLY these exact formats:\n" +
                "  * For Happy/Smiling: <sprite name=\"smile\">\n" +
                "  * For Agree/Encouragement: <sprite name=\"thumbs\">\n" +
                "  * For Sadness/Empathy: <sprite name=\"sad\">\n" +
                "  * For Care/Love/Warmth: <sprite name=\"heart\">\n" +
                "  * For Calm/Peace/Meditation: <sprite name=\"calm\">\n" +
                "  * For Progress: <sprite name=\"growth\">\n\n";
            systemInstructionText +=
            "CRITICAL SAFETY OVERRIDE:\n" +
            "IF the user expresses thoughts of self-harm, suicide, severe trauma, or physical danger, you MUST immediately halt all normal conversation. " +
            "In this specific scenario, do NOT offer advice, empathy, or follow-up questions. Your ONLY output must be exactly the text below:\n\n" +
            "\"It sounds like you are going through an incredibly difficult time right now, and you don't have to face it alone. Please reach out to these professional resources immediately:\n\n" +
            " General Emergency: 999\n" +
            "Talian Kasih (24/7): 15999 or WhatsApp 019-2615999\n" +
            "Befrienders Malaysia: 03-7627 2929\n" +
            "MIASA Crisis Helpline: 1-800-180066\n\n" +
            "Help is always available.\"";
        }

        Debug.Log($"[Gemini System Prompt Compiled]:\n{systemInstructionText}");

        float aiTemp = 0.4f;
        float aiTopP = 0.85f;
        int aiTopK = 30;

        if (profile != null && !string.IsNullOrEmpty(profile.tonePreference))
        {
            switch (profile.tonePreference.ToLower())
            {
                case "direct":
                    aiTemp = 0.2f;
                    aiTopP = 0.60f;
                    aiTopK = 20;
                    break;
                case "motivational":
                    aiTemp = 0.7f;
                    aiTopP = 0.90f;
                    aiTopK = 40;
                    break;
                case "empathetic":
                default:
                    aiTemp = 0.4f;
                    aiTopP = 0.85f;
                    aiTopK = 30;
                    break;
            }
        }

        var config = new Firebase.AI.GenerationConfig(
            temperature: aiTemp,
            topP: aiTopP,
            topK: aiTopK,
            maxOutputTokens: 200
        );

        _model = ai.GetGenerativeModel(
            modelName: "gemini-2.5-flash-lite",
            generationConfig: config,
            systemInstruction: ModelContent.Text(systemInstructionText)
        );

        List<ModelContent> pastHistory = new List<ModelContent>();

        int historyLimit = 10;
        int startIndex = Mathf.Max(0, _chatHistory.Count - historyLimit);

        for (int i = startIndex; i < _chatHistory.Count; i++)
        {
            var msg = _chatHistory[i];

            // 2. Use the correct ModelContent constructors to assign roles
            if (msg.isBot)
            {
                pastHistory.Add(new ModelContent("model", new ModelContent.TextPart(msg.text)));
            }
            else
            {
                pastHistory.Add(new ModelContent("user", new ModelContent.TextPart(msg.text)));
            }
        }

       
        _chatSession = _model.StartChat(pastHistory);
    }

    public async void OnSendButtonClicked()
    {
        string userText = userInputField.text;
        if (string.IsNullOrWhiteSpace(userText)) return;

        userInputField.text = "";
        sendButton.interactable = false;

        SpawnBubble(userText, false, chatContentContainer);
        SaveToHistory(userText, false);

        await SendMessageToGemini(userText);
    }

    private async Task SendMessageToGemini(string userText)
    {
        GameObject botBubble = SpawnBubble("Typing...", true, chatContentContainer);
        TMP_Text botTextComponent = botBubble.GetComponentInChildren<TMP_Text>();

        try
        {
            var response = await _chatSession.SendMessageAsync(userText);

            string finalResponse = response.Text ?? "No text in response.";
            botTextComponent.text = finalResponse;

            SaveToHistory(finalResponse, true);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("429") || ex.Message.Contains("Quota"))
            {
                botTextComponent.text = "I'm receiving too many messages right now. Please take a deep breath, wait about 30 seconds, and try again.";
            }
            else
            {
                botTextComponent.text = "Sorry, my connection was interrupted. Please try again.";
            }
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

    private void SaveToHistory(string messageText, bool wasSentByBot)
    {
        ChatMessage newMsg = new ChatMessage
        {
            text = messageText,
            isBot = wasSentByBot,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        _chatHistory.Add(newMsg);

        string json = JsonUtility.ToJson(newMsg);
        dbReference.Child("users").Child(userId).Child("chats").Push().SetRawJsonValueAsync(json);
    }

    private GameObject SpawnBubble(string text, bool isBot, Transform container)
    {
        if (container == null) return null;

        GameObject prefabToUse = isBot ? botBubblePrefab : userBubblePrefab;
        GameObject bubble = Instantiate(prefabToUse, container, false);

        TMP_Text textComponent = bubble.GetComponentInChildren<TMP_Text>();
        textComponent.text = text;

        textComponent.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(bubble.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());

        return bubble;
    }

    private void ReturnToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}