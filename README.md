## ⚠️ Setup Instructions (Important)

To keep this repository lightweight, the Firebase Unity SDK and my private database configuration files have not been included. If you clone this repository, you will initially see compilation errors. 

Please follow these steps to run the application successfully:

### Import the Firebase Unity SDK
1. Download the latest **Firebase Unity SDK (.zip)** from the [Official Firebase Website](https://firebase.google.com/download/unity).
2. Unzip the downloaded file.
3. Open this project in Unity. (If Unity asks to enter "Safe Mode" due to compiler errors, click **Ignore**).
4. In the Unity top menu, go to **Assets > Import Package > Custom Package...**
5. Navigate to the unzipped Firebase folder and import the following Unity packages:
   * FirebaseAI
   * FirebaseAuthentication
   * FirebaseDatabase

###  Restart and Play
Once the packages are imported and the JSON file is in place, Unity will recompile. All red errors should disappear, and you can now hit Play to test the application!
