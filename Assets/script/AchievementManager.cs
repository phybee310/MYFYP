using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class AchievementManager : MonoBehaviour
{
    public enum AchievementType
    {
        TotalTimeInSeconds,
        TotalSessionsCount
    }

    [System.Serializable]
    public struct AchievementData
    {
        public string Title;
        public string Description;
        public AchievementType Type;
        public float RequiredValue;
        public AchievementRow RowUI;
    }

    [Header("Achievement List")]
    public AchievementData[] Achievements;

    private void Start()
    {
        FetchCloudAchievements();
    }

    private void FetchCloudAchievements()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogWarning("No user logged in. Cannot fetch cloud stats.");
            return;
        }

    
        DatabaseReference dbRef = FirebaseDatabase.GetInstance(FirebaseConfig.DatabaseURL).RootReference;

        dbRef.Child("users").Child(user.UserId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to reach Firebase Database.");
                return;
            }

            DataSnapshot snapshot = task.Result;
            float totalSeconds = 0f;
            int totalSessions = 0;

            
            if (snapshot.HasChild("TotalMeditationTime"))
            {
                totalSeconds = float.Parse(snapshot.Child("TotalMeditationTime").Value.ToString());
            }

            if (snapshot.HasChild("TotalMeditationSessions"))
            {
                totalSessions = int.Parse(snapshot.Child("TotalMeditationSessions").Value.ToString());
            }

            UpdateUI(totalSeconds, totalSessions);
        });
    }

    private void UpdateUI(float totalSeconds, int totalSessions)
    {
        
        int currentUnlockedPosition = 0;

        foreach (AchievementData achievement in Achievements)
        {
            bool isUnlocked = false;

            switch (achievement.Type)
            {
                case AchievementType.TotalTimeInSeconds:
                    isUnlocked = totalSeconds >= achievement.RequiredValue;
                    break;

                case AchievementType.TotalSessionsCount:
                    isUnlocked = totalSessions >= achievement.RequiredValue;
                    break;
            }

      
            achievement.RowUI.UpdateUI(achievement.Title, achievement.Description, isUnlocked);

           
            if (isUnlocked)
            {
        
                achievement.RowUI.transform.SetSiblingIndex(currentUnlockedPosition);

              
                currentUnlockedPosition++;
            }
            else
            {
                achievement.RowUI.transform.SetAsLastSibling();
            }
        }
    }
}