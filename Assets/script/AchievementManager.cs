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
        if (user == null) return;

        DatabaseReference dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        dbRef.Child("users").Child(user.UserId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted) return;

            DataSnapshot snapshot = task.Result;
            float totalSeconds = 0f;
            int totalSessions = 0;

            if (snapshot.HasChild("TotalMeditationTime"))
                totalSeconds = float.Parse(snapshot.Child("TotalMeditationTime").Value.ToString());

            if (snapshot.HasChild("TotalMeditationSessions"))
                totalSessions = int.Parse(snapshot.Child("TotalMeditationSessions").Value.ToString());

            UpdateUI(totalSeconds, totalSessions);
        });
    }

    private void UpdateUI(float totalSeconds, int totalSessions)
    {
        // NEW: We use this variable to keep track of where the next unlocked achievement should go
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

            // 1. Update the colors and locks visually
            achievement.RowUI.UpdateUI(achievement.Title, achievement.Description, isUnlocked);

            // 2. NEW LOGIC: Sort the UI physically in the list!
            if (isUnlocked)
            {
                // Move this unlocked row to the highest available spot at the top
                achievement.RowUI.transform.SetSiblingIndex(currentUnlockedPosition);

                // Increase the position counter so the NEXT unlocked achievement goes right below this one
                currentUnlockedPosition++;
            }
            else
            {
                // If it is locked, shove it to the very bottom of the list
                achievement.RowUI.transform.SetAsLastSibling();
            }
        }
    }
}