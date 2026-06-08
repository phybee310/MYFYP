## ⚠️ Setup Instructions for UNITY(Important)

To keep this repository lightweight, the Firebase Unity SDK and private database configuration files have not been included. If you clone this repository, you will initially see compilation errors. 

Please follow these steps to run the application successfully:
### Register Project in firebase
1. Open firebase console here: (https://console.firebase.google.com/u/0/) and create a new project.
2. Under the new project, click 'add app' and select the unity platform.
3. Select 'Register as Android app' and copy the package name by accessing **Build settings > Android > Player settings > Other settings > Bundle identifier** in the Unity IDE.
4. Download the **google-services.json** file and place it under Assets in Unity.
5. Download the latest **Firebase Unity SDK (.zip)** from the [Official Firebase Website](https://firebase.google.com/download/unity).
6. Unzip the downloaded file.
7. Open this project in Unity. (If Unity asks to enter "Safe Mode" due to compiler errors, click **Ignore**).
8. In the Unity top menu, go to **Assets > Import Package > Custom Package.**
9. Navigate to the unzipped Firebase folder and import the following Unity packages:**FirebaseAI** **FirebaseAuthentication** **FirebaseDatabase**
    
   


###  Restart and Play
Once the packages are imported and the JSON file is in place, Unity will recompile.
