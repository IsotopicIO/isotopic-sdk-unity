# Isotopic SDK - Unity
This is the official repository for the Isotopic SDK for Unity, an open-source implementation of the Isotopic APIs for use within projects that want to utilize the Isotopic services.

## About the SDK
The SDK can connect your Unity game with Isotopic, to unlock features that allow you to utilize things like User Authentication, Cloud Storage, and access to platforms and services such as Isotopic Assets.

### Features
- One-Time-Code Authentication for Isotopic Users.
- Store saves and data for Isotopic Users on the Cloud.
- Integrate Cross-Game items with Isotopic Assets.
- Decentralized functionality through ChainLink's [web3.unity](https://github.com/chainSafe/web3.unity)
- Upload builds of your game to the Isotopic Game Store directly through Unity. (Automation possible)

&nbsp;
&nbsp;
&nbsp;

# Getting Started
Follow the instructions below to download, install and setup the Isotopic Unity SDK into your project.

### Prerequisites
- Create or open a Unity project with Unity versions 2021 and above.
- Create a page for your game on the [Isotopic Game Store](https://isotopic.io/game-store). You can follow [this](https://medium.com/@isotopic.io/publishing-on-isotopic-558f9c4c6532) guide.

### Download and Install
- Head to Releases and download the latest version of the Unity Package.
- Open the package while the Unity Project is open on the editor, and import all assets.

### SDK Basic Setup
- On the Unity Editor Toolbar, click Window > Isotopic > Isotopic SDK, and click "Edit Configuration"
- An asset on the project tab will be selected, where you need to set your Isotopic App ID.
  - You can find your app id by opening your game's page on Isotopic and copying it from the URL.
  - ``https://isotopic.io/game?game=game_id`` <- ``game_id``is your ID.

### Additional Requirements
- Open Edit > Project Settings, and go to "Player".
- Under Configuration, untick "Assembly Version Validation". (This causes an issue with one of the dependencies not accepting a newer version of NewtonSoftJSON.)

&nbsp;
&nbsp;
&nbsp;

# How to use:

## > Authenticating the User
- Switch to the OTC authentication scene (found at Assets/IsotopicSDK/Scenes/Authentication) for your platform (Desktop or Mobile)
- Running this scene will create and show an OTC which the user can enter in their browser to login to their Isotopic Account.
- After user succesfully logs in, continue to your game with either:
  - Set a Scene to load on "Scene To Load After Login" on the IsotopicAuthHandler object in the authentication scene, or
  - Add a callback to be invoked through the OnIsotopicUserLoggedIn(Profile) unity event on the IsotopicAuthHandler object, or
  - If you load this scene at runtime as an additive, set the same scene to the "Scene To Unload After Login" parameter on the IsotopicAuthHandler object.

&nbsp;
&nbsp;
 
## > Accessing the User's Profile
The namespace IsotopicSDK.API grants you access to the main Isotopic object which contains high-level functionality for the Isotopic APIs.

``Isotopic.User.UserProfile`` contains some data for the logged in user.

Example:
```cs
using IsotopicSDK.API;

Texture2D userProfilePic = Isotopic.User.UserProfile.ProfileImage;
string username = Isotopic.User.UserProfile.Username;
```

&nbsp;
&nbsp;

## > Using Cloud Storage
Methods for interacting with the Cloud Storage for the logged in User can be accessed through ``Isotopic.User.CloudStore``.

### Download and sync with online data:
```cs
Isotopic.User.CloudStore.SyncFromCloud(syncResult => {
  Debug.Log("Synced Isotopic Cloud, Result: " + syncResult);
})
```

### Getting/Writing Data
CloudStore works by saving the data for the user as an encrypted file on IPFS. The user can still tamper with the file, but it is much more difficult compared to the unity's default PlayerPrefs.
The system works very similar with that of PlayerPrefs:

```cs
// Write variables
Isotopic.User.CloudStore.SetFloat("myfloat", 2.5f);
Isotopic.User.CloudStore.SetInt("playerHighScore", 10);
Isotopic.User.CloudStore.SetString("gameSave", JsonUtility.ToJson(mySaveObject)); // Can save objects by converting them to JSON and setting them as strings.

// Read variables
float? myFloat = Isotopic.User.CloudStore.GetFloat("myfloat");
if (myFloat != null) // Attempting to get a variable will not throw if it does not exist, but instead return null.
{
  actualFloat = myFloat.Value;
}

int? highScore = Isotopic.User.CloudStore.GetInt("playerHighScore");

GameSave gameSave = JsonUtility.FromJson<GameSave>(Isotopic.User.CloudStore.GetString("gameSave"));
```

### Saving Changes
Chages you make using the methods SetFloat, SetInt, and SetString, are not automatically uploaded to the cloud.
Whenever you finish setting variables, you can have them uploaded and saved by calling:

```cs
Isotopic.User.CloudStore.SaveVariablesToCloud(saveSuccesful => {
  Debug.Log("Saved to Isotopic Cloud, Result: " + saveSuccesful);
});
```

&nbsp;
&nbsp;

## > Checking Onwership of Isotopic Assets
Browse available Cross-Game assets from the Isotopic Asset Store. Collect their UUID or Asset ID from their page on Isotopic, and fill them in the Isotopic SDK Config object. 
Select "Use Testnet" for testing purposes, or untick for production.

### Example CS code (As seen on Isotopic Assets Sample):
```cs
if (Isotopic.Web3Instance == null) throw new System.Exception("Web3 Instance Uninitialized. If you are getting this error you are probably trying to perform some action that requires the Isotopic web3 Instance, but have not intialized it yet.");
if (Isotopic.User.UserProfile == null) throw new System.Exception("Isotopic User not logged in. If you are getting this error it probably means you need to first login the user via Isotopic OAuth.");


Isotopic.Network.IsotopicAssets.GetIsotopicAssetBalance(Isotopic.SDKConfig.IsotopicAssets[0], countOwned =>
{
  if (countOwned > 0)
  {
    Debug.Log("User owns " + res.ToString() + " asset(s).");

    // Do what you want, e.g. add something to the player's in-game inventory
    AddObjectToPlayersInvetory(Isotopic.SDKConfig.IsotopicAssets[0].AssetPrefab);
  }
  else
  {
    Debug.Log("Asset not owned on any linked wallet");
  }
});
```
