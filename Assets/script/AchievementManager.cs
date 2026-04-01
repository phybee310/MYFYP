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

        // 1. Using the secure config file to prevent hardcoding the URL in this script
        DatabaseReference dbRef = FirebaseDatabase.GetInstance(FirebaseConfig.DatabaseURL).RootReference;

        // 2. Fetch the user's specific data from the cloud asynchronously
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

            // 3. Extract the data if it exists
            if (snapshot.HasChild("TotalMeditationTime"))
            {
                totalSeconds = float.Parse(snapshot.Child("TotalMeditationTime").Value.ToString());
            }

            if (snapshot.HasChild("TotalMeditationSessions"))
            {
                totalSessions = int.Parse(snapshot.Child("TotalMeditationSessions").Value.ToString());
            }

            // 4. Send the downloaded data to the UI method
            UpdateUI(totalSeconds, totalSessions);
        });
    }

    private void UpdateUI(float totalSeconds, int totalSessions)
    {
        // We use this variable to keep track of where the next unlocked achievement should go
        int currentUnlockedPosition = 0;

        foreach (AchievementData achievement in Achievements)
        {
            bool isUnlocked = false;

            // 1. Check if the user meets the requirements
            switch (achievement.Type)
            {
                case AchievementType.TotalTimeInSeconds:
                    isUnlocked = totalSeconds >= achievement.RequiredValue;
                    break;

                case AchievementType.TotalSessionsCount:
                    isUnlocked = totalSessions >= achievement.RequiredValue;
                    break;
            }

            // 2. Update the colors and lock overlays visually via the AchievementRow script
            achievement.RowUI.UpdateUI(achievement.Title, achievement.Description, isUnlocked);

            // 3. Sort the UI dynamically based on unlock status
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