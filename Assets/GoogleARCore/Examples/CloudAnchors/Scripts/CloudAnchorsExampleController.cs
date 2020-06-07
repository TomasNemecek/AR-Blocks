//-----------------------------------------------------------------------
// <copyright file="CloudAnchorsExampleController.cs" company="Google">
//
// Copyright 2018 Google LLC. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

using UnityEngine.UI;

namespace GoogleARCore.Examples.CloudAnchors
{
    using GoogleARCore;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.Networking;
    using UnityEngine.SceneManagement;

#if UNITY_EDITOR
    // Set up touch input propagation while using Instant Preview in the editor.
    using Input = GoogleARCore.InstantPreviewInput;
#endif

    /// <summary>
    /// Controller for the Cloud Anchors Example. Handles the ARCore lifecycle.
    /// See details in
    /// <a href="https://developers.google.com/ar/develop/unity/cloud-anchors/overview-unity">
    /// Share AR experiences with Cloud Anchors</a>
    /// </summary>
    public class CloudAnchorsExampleController : MonoBehaviour
    {
        [Header("ARCore")]

        /// <summary>
        /// The root for ARCore-specific GameObjects in the scene.
        /// </summary>
        public GameObject ARCoreRoot;

        /// <summary>
        /// The helper that will calculate the World Origin offset when performing a raycast or
        /// generating planes.
        /// </summary>
        public ARCoreWorldOriginHelper ARCoreWorldOriginHelper;

        [Header("UI")]

        /// <summary>
        /// The network manager UI Controller.
        /// </summary>
        public NetworkManagerUIController NetworkUIController;

        /// <summary>
        /// The Lobby Screen to see Available Rooms or create a new one.
        /// </summary>
        public GameObject LobbyScreen;

        /// <summary>
        /// The Start Screen to see help information.
        /// </summary>
        public GameObject StartScreen;

        /// <summary>
        /// The AR Screen which display the AR view, return to lobby button and room number.
        /// </summary>
        public GameObject ARScreen;

        /// <summary>
        /// The Status Screen to display the connection status and cloud anchor instructions.
        /// </summary>
        public GameObject StatusScreen;     

        /// <summary>
        /// The key name used in PlayerPrefs which indicates whether
        /// the start info has displayed at least one time.
        /// </summary>
        private const string k_HasDisplayedStartInfoKey = "HasDisplayedStartInfo";

        /// <summary>
        /// The time between room starts up and ARCore session starts resolving.
        /// </summary>
        private const float k_ResolvingPrepareTime = 3.0f;

        /// <summary>
        /// Record the time since the room started. If it passed the resolving prepare time,
        /// applications in resolving mode start resolving the anchor.
        /// </summary>
        private float m_TimeSinceStart = 0.0f;

        /// <summary>
        /// Indicates whether passes the resolving prepare time.
        /// </summary>
        private bool m_PassedResolvingPreparedTime = false;

        /// <summary>
        /// Indicates whether the Anchor was already instantiated.
        /// </summary>
        private bool m_AnchorAlreadyInstantiated = false;

        /// <summary>
        /// Indicates whether the Cloud Anchor finished hosting.
        /// </summary>
        private bool m_AnchorFinishedHosting = false;

        /// <summary>
        /// True if the app is in the process of quitting due to an ARCore connection error,
        /// otherwise false.
        /// </summary>
        private bool m_IsQuitting = false;

        /// <summary>
        /// The anchor component that defines the shared world origin.
        /// </summary>
        private Component m_WorldOriginAnchor = null;

        private bool m_InDestroyMode = false;

        /// <summary>
        /// The last pose of the hit point from AR hit test.
        /// </summary>
        private Pose? m_LastHitPose = null;

        /// <summary>
        /// The current cloud anchor mode.
        /// </summary>
        private ApplicationMode m_CurrentMode = ApplicationMode.Ready;

        /// <summary>
        /// The current active UI screen.
        /// </summary>
        private ActiveScreen m_CurrentActiveScreen = ActiveScreen.LobbyScreen;

        /// <summary>
        /// The Network Manager.
        /// </summary>
        private CloudAnchorsNetworkManager m_NetworkManager;

        private SelectedObject m_SelectedObject;

        private SelectedSize m_SelectedSize = SelectedSize.Small;
        
        /// <summary>
        /// Enumerates modes the example application can be in.
        /// </summary>
        public enum ApplicationMode
        {
            /// <summary>
            /// Enume mode that indicate the example application is ready to host or resolve.
            /// </summary>
            Ready,

            /// <summary>
            /// Enume mode that indicate the example application is hosting cloud anchors.
            /// </summary>
            Hosting,

            /// <summary>
            /// Enume mode that indicate the example application is resolving cloud anchors.
            /// </summary>
            Resolving,
        }

        /// <summary>
        /// Enumerates the active UI screens the example application can be in.
        /// </summary>
        public enum ActiveScreen
        {
            /// <summary>
            /// Enume mode that indicate the example application is on lobby screen.
            /// </summary>
            LobbyScreen,

            /// <summary>
            /// Enume mode that indicate the example application is on start screen.
            /// </summary>
            StartScreen,

            /// <summary>
            /// Enume mode that indicate the example application is on AR screen.
            /// </summary>
            ARScreen,
        }
        

        /// <summary>
        /// Gets a value indicating whether the Origin of the new World Coordinate System,
        /// i.e. the Cloud Anchor was placed.
        /// </summary>
        public bool IsOriginPlaced { get; private set; }

        /// <summary>
        /// Callback handling Start Now button click event.
        /// </summary>
        public void OnStartNowButtonClicked()
        {
            _SwitchActiveScreen(ActiveScreen.ARScreen);
        }

        /// <summary>
        /// Callback handling Learn More Button click event.
        /// </summary>
        public void OnLearnMoreButtonClicked()
        {
            Application.OpenURL(
                "https://developers.google.com/ar/cloud-anchor-privacy");
        }

        /// <summary>
        /// The Unity Awake() method.
        /// </summary>
        public void Awake()
        {
            // Enable ARCore to target 60fps camera capture frame rate on supported devices.
            // Note, Application.targetFrameRate is ignored when QualitySettings.vSyncCount != 0.
            Application.targetFrameRate = 60;
        }

        /// <summary>
        /// The Unity Start() method.
        /// </summary>
        public void Start()
        {
#pragma warning disable 618
            m_NetworkManager = NetworkUIController.GetComponent<CloudAnchorsNetworkManager>();
#pragma warning restore 618
            m_NetworkManager.OnClientConnected += _OnConnectedToServer;
            m_NetworkManager.OnClientDisconnected += _OnDisconnectedFromServer;

            // A Name is provided to the Game Object so it can be found by other Scripts
            // instantiated as prefabs in the scene.
            gameObject.name = "CloudAnchorsExampleController";
            ARCoreRoot.SetActive(false);
            _ResetStatus();
        }

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            _UpdateApplicationLifecycle();

            if (m_CurrentActiveScreen != ActiveScreen.ARScreen)
            {
                return;
            }

            // If we are neither in hosting nor resolving mode then the update is complete.
            if (m_CurrentMode != ApplicationMode.Hosting &&
                m_CurrentMode != ApplicationMode.Resolving)
            {
                return;
            }

            // Give ARCore session some time to prepare for resolving and update the UI message
            // once the preparation time passed.
            if (m_CurrentMode == ApplicationMode.Resolving && !m_PassedResolvingPreparedTime)
            {
                m_TimeSinceStart += Time.deltaTime;

                if (m_TimeSinceStart > k_ResolvingPrepareTime)
                {
                    m_PassedResolvingPreparedTime = true;
                    NetworkUIController.ShowDebugMessage(
                        "Waiting for Cloud Anchor to be hosted...");
                }
            }

            // If the origin anchor has not been placed yet, then update in resolving mode is
            // complete.
            if (m_CurrentMode == ApplicationMode.Resolving && !IsOriginPlaced)
            {
                return;
            }

            // If the player has not touched the screen then the update is complete.
            Touch touch;
            if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
            {
                return;
            }

            // Ignore the touch if it's pointing on UI objects.
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                return;
            }

            TrackableHit arcoreHitResult = new TrackableHit();
            m_LastHitPose = null;

            
            Ray raycast = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
            
            // We are in destroy mode
            if (m_InDestroyMode)
            {

                RaycastHit hitInfo;
                if (ARCoreWorldOriginHelper.DetectBlock(raycast, out hitInfo))
                {
                    _DestroyBlock(hitInfo.collider.gameObject);
                }


                return;
            }
            //Detect block
            Pose buildablePose;
            if (IsOriginPlaced && ARCoreWorldOriginHelper.RaycastWithDetection(raycast, m_SelectedSize, out buildablePose ))
            {  
                m_LastHitPose = buildablePose;

            }
            
            //Detect plane          
            else if (ARCoreWorldOriginHelper.Raycast(touch.position.x, touch.position.y,
                    TrackableHitFlags.PlaneWithinPolygon, out arcoreHitResult))
            {
                //If origin is already placed, change the y value so that gameobject is not clipping through the plane
                m_LastHitPose = arcoreHitResult.Pose;
                if (IsOriginPlaced)
                {
                    m_LastHitPose = new Pose(new Vector3(m_LastHitPose.Value.position.x, m_LastHitPose.Value.position.y + CalcAdjustedHeight(), m_LastHitPose.Value.position.z), m_LastHitPose.Value.rotation);    
                }
            }
           

            // If there was an anchor placed, then instantiate the corresponding object.
            if (m_LastHitPose != null)
            {
                // The first touch on the Hosting mode will instantiate the origin anchor. Any
                // subsequent touch will instantiate a block, both in Hosting and Resolving modes.
                if (_CanPlaceBlocks())
                {
//                    _InstantiateBlock();
                    _InstantiateObject();
                }
                else if (!IsOriginPlaced && m_CurrentMode == ApplicationMode.Hosting)
                {
                    
                    m_WorldOriginAnchor =
                        arcoreHitResult.Trackable.CreateAnchor(arcoreHitResult.Pose);
                    

                    SetWorldOrigin(m_WorldOriginAnchor.transform);
                    _InstantiateAnchor();
                    OnAnchorInstantiated(true);
                }
            }
        }
        
        public void OnBuildModePressed()
        {
            m_InDestroyMode = true;
            NetworkUIController.EnableDestroyButton();
        }
        
        public void OnDestroyPressed()
        {
            m_InDestroyMode = false;
            NetworkUIController.EnableBuildButton();
        }

        /// <summary>
        /// Indicates whether the resolving prepare time has passed so the AnchorController
        /// can start to resolve the anchor.
        /// </summary>
        /// <returns><c>true</c>, if resolving prepare time passed, otherwise returns <c>false</c>.
        /// </returns>
        public bool IsResolvingPrepareTimePassed()
        {
            return m_CurrentMode != ApplicationMode.Ready && m_TimeSinceStart > k_ResolvingPrepareTime;
        }

        /// <summary>
        /// Sets the apparent world origin so that the Origin of Unity's World Coordinate System
        /// coincides with the Anchor. This function needs to be called once the Cloud Anchor is
        /// either hosted or resolved.
        /// </summary>
        /// <param name="anchorTransform">Transform of the Cloud Anchor.</param>
        public void SetWorldOrigin(Transform anchorTransform)
        {
            if (IsOriginPlaced)
            {
                Debug.LogWarning("The World Origin can be set only once.");
                return;
            }

            IsOriginPlaced = true;
            ARCoreWorldOriginHelper.SetWorldOrigin(anchorTransform);
           
        }

        /// <summary>
        /// Callback called when the lobby screen's visibility is changed.
        /// </summary>
        /// <param name="visible">If set to <c>true</c> visible.</param>
        public void OnLobbyVisibilityChanged(bool visible)
        {
            if (visible)
            {
                _SwitchActiveScreen(ActiveScreen.LobbyScreen);
            }
            else if (PlayerPrefs.HasKey(k_HasDisplayedStartInfoKey))
            {
                _SwitchActiveScreen(ActiveScreen.ARScreen);
            }
            else
            {
                _SwitchActiveScreen(ActiveScreen.StartScreen);
            }
        }

        /// <summary>
        /// Callback called when the resolving timeout is passed.
        /// </summary>
        public void OnResolvingTimeoutPassed()
        {
            if (m_CurrentMode != ApplicationMode.Resolving)
            {
                Debug.LogWarning("OnResolvingTimeoutPassed shouldn't be called" +
                    "when the application is not in resolving mode.");
                return;
            }

            NetworkUIController.ShowDebugMessage("Still resolving the anchor." +
                "Please make sure you're looking at where the Cloud Anchor was hosted." +
                "Or, try to re-join the room.");
        }

        /// <summary>
        /// Handles user intent to enter a mode where they can place an anchor to host or to exit
        /// this mode if already in it.
        /// </summary>
        public void OnEnterHostingModeClick()
        {
            if (m_CurrentMode == ApplicationMode.Hosting)
            {
                m_CurrentMode = ApplicationMode.Ready;
                _ResetStatus();
                Debug.Log("Reset ApplicationMode from Hosting to Ready.");
            }

            m_CurrentMode = ApplicationMode.Hosting;
        }

        /// <summary>
        /// Handles a user intent to enter a mode where they can input an anchor to be resolved or
        /// exit this mode if already in it.
        /// </summary>
        public void OnEnterResolvingModeClick()
        {
            if (m_CurrentMode == ApplicationMode.Resolving)
            {
                m_CurrentMode = ApplicationMode.Ready;
                _ResetStatus();
                Debug.Log("Reset ApplicationMode from Resolving to Ready.");
            }

            m_CurrentMode = ApplicationMode.Resolving;
        }

        /// <summary>
        /// Callback indicating that the Cloud Anchor was instantiated and the host request was
        /// made.
        /// </summary>
        /// <param name="isHost">Indicates whether this player is the host.</param>
        public void OnAnchorInstantiated(bool isHost)
        {
            if (m_AnchorAlreadyInstantiated)
            {
                return;
            }

            m_AnchorAlreadyInstantiated = true;
            NetworkUIController.OnAnchorInstantiated(isHost);
        }

        /// <summary>
        /// Callback indicating that the Cloud Anchor was hosted.
        /// </summary>
        /// <param name="success">If set to <c>true</c> indicates the Cloud Anchor was hosted
        /// successfully.</param>
        /// <param name="response">The response string received.</param>
        public void OnAnchorHosted(bool success, string response)
        {
            m_AnchorFinishedHosting = success;
            NetworkUIController.OnAnchorHosted(success, response);
        }

        /// <summary>
        /// Callback indicating that the Cloud Anchor was resolved.
        /// </summary>
        /// <param name="success">If set to <c>true</c> indicates the Cloud Anchor was resolved
        /// successfully.</param>
        /// <param name="response">The response string received.</param>
        public void OnAnchorResolved(bool success, string response)
        {
            NetworkUIController.OnAnchorResolved(success, response);
        }

        /// <summary>
        /// Callback that happens when the client successfully connected to the server.
        /// </summary>
        private void _OnConnectedToServer()
        {
            if (m_CurrentMode == ApplicationMode.Hosting)
            {
                NetworkUIController.ShowDebugMessage(
                    "Find a plane, tap to create a Cloud Anchor.");
            }
            else if (m_CurrentMode == ApplicationMode.Resolving)
            {
                NetworkUIController.ShowDebugMessage(
                    "Look at the same scene as the hosting phone.");
            }
            else
            {
                _ReturnToLobbyWithReason(
                    "Connected to server with neither Hosting nor Resolving" +
                    "mode. Please start the app again.");
            }
        }

        /// <summary>
        /// Callback that happens when the client disconnected from the server.
        /// </summary>
        private void _OnDisconnectedFromServer()
        {
            _ReturnToLobbyWithReason("Network session disconnected! " +
                "Please start the app again and try another room.");
        }

        /// <summary>
        /// Instantiates the anchor object at the pose of the m_LastPlacedAnchor Anchor. This will
        /// host the Cloud Anchor.
        /// </summary>
        private void _InstantiateAnchor()
        {
            // The anchor will be spawned by the host, so no networking Command is needed.
            GameObject.Find("LocalPlayer").GetComponent<LocalPlayerController>()
                .SpawnAnchor(Vector3.zero, Quaternion.identity, m_WorldOriginAnchor);
        }
       
        private void _InstantiateObject()
        {
            // Star must be spawned in the server so a networking Command is used.
            GameObject.Find("LocalPlayer").GetComponent<LocalPlayerController>()
                .SpawnObject(m_SelectedObject, m_SelectedSize, m_LastHitPose.Value.position, m_LastHitPose.Value.rotation);
        }
        
        private void _DestroyBlock(GameObject block)
        {
            // Star must be spawned in the server so a networking Command is used.
//            GameObject.Find("LocalPlayer").GetComponent<LocalPlayerController>()
//                .CmdDestroyBlock(block);

            if (m_CurrentMode == ApplicationMode.Hosting)
            {
                GameObject.Find("LocalPlayer").GetComponent<LocalPlayerController>()
                    .OnDestroyBlockForClient(block);   
            }
            else
            {
                GameObject.Find("LocalPlayer").GetComponent<LocalPlayerController>()
                    .OnDestroyBlockForHost(block);       
            }
        }

        /// <summary>
        /// Sets the corresponding platform active.
        /// </summary>
        private void _SetPlatformActive()
        {  
            ARCoreRoot.SetActive(true);           
        }

        /// <summary>
        /// Indicates whether a star can be placed.
        /// </summary>
        /// <returns><c>true</c>, if stars can be placed, <c>false</c> otherwise.</returns>
        private bool _CanPlaceBlocks()
        {
            if (m_CurrentMode == ApplicationMode.Resolving)
            {
                return IsOriginPlaced;
            }

            if (m_CurrentMode == ApplicationMode.Hosting)
            {
                return IsOriginPlaced && m_AnchorFinishedHosting;
            }

            return false;
        }

        /// <summary>
        /// Resets the internal status.
        /// </summary>
        private void _ResetStatus()
        {
            // Reset internal status.
            m_CurrentMode = ApplicationMode.Ready;
            if (m_WorldOriginAnchor != null)
            {
                Destroy(m_WorldOriginAnchor.gameObject);
            }

            IsOriginPlaced = false;
            m_WorldOriginAnchor = null;
        }

        private void _SwitchActiveScreen(ActiveScreen activeScreen)
        {
            LobbyScreen.SetActive(activeScreen == ActiveScreen.LobbyScreen);
            StatusScreen.SetActive(activeScreen != ActiveScreen.StartScreen);
            StartScreen.SetActive(activeScreen == ActiveScreen.StartScreen);
            ARScreen.SetActive(activeScreen == ActiveScreen.ARScreen);
            m_CurrentActiveScreen = activeScreen;

            if (m_CurrentActiveScreen == ActiveScreen.StartScreen)
            {
                PlayerPrefs.SetInt(k_HasDisplayedStartInfoKey, 1);
            }

            if (m_CurrentActiveScreen == ActiveScreen.ARScreen)
            {
                // Set platform active when switching to AR Screen so the camera permission only
                // shows after Start Screen.
                m_TimeSinceStart = 0.0f;
                _SetPlatformActive();
            }
        }

        /// <summary>
        /// Check and update the application lifecycle.
        /// </summary>
        private void _UpdateApplicationLifecycle()
        {
            // Exit the app when the 'back' button is pressed.
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

            var sleepTimeout = SleepTimeout.NeverSleep;

#if !UNITY_IOS
            // Only allow the screen to sleep when not tracking.
            if (Session.Status != SessionStatus.Tracking)
            {
                sleepTimeout = SleepTimeout.SystemSetting;
            }
#endif

            Screen.sleepTimeout = sleepTimeout;

            if (m_IsQuitting)
            {
                return;
            }

            // Quit if ARCore is in error status.
            if (Session.Status == SessionStatus.ErrorPermissionNotGranted)
            {
                _ReturnToLobbyWithReason("Camera permission is needed to run this application.");
            }
            else if (Session.Status.IsError())
            {
                _QuitWithReason("ARCore encountered a problem connecting. " +
                    "Please start the app again.");
            }
        }

        /// <summary>
        /// Quits the application after 5 seconds for the toast to appear.
        /// </summary>
        /// <param name="reason">The reason of quitting the application.</param>
        private void _QuitWithReason(string reason)
        {
            if (m_IsQuitting)
            {
                return;
            }

            NetworkUIController.ShowDebugMessage(reason);
            m_IsQuitting = true;
            Invoke("_DoQuit", 5.0f);
        }

        /// <summary>
        /// Returns to lobby after 3 seconds for the reason message to appear.
        /// </summary>
        /// <param name="reason">The reason of returning to lobby.</param>
        private void _ReturnToLobbyWithReason(string reason)
        {
            // No need to return if the application is currently quitting.
            if (m_IsQuitting)
            {
                return;
            }

            NetworkUIController.ShowDebugMessage(reason);
            Invoke("_DoReturnToLobby", 3.0f);
        }

        /// <summary>
        /// Actually quit the application.
        /// </summary>
        private void _DoQuit()
        {
            Application.Quit();
        }

        /// <summary>
        /// Actually return to lobby scene.
        /// </summary>
        private void _DoReturnToLobby()
        {
            NetworkManager.Shutdown();
            SceneManager.LoadScene("CloudAnchors");
        }

        
        //Object selection
        public void OnCubeClicked()
        {
            m_SelectedObject = SelectedObject.Block;
        }

        public void OnCubesClicked()
        {
            m_SelectedObject = SelectedObject.Blocks;    
        }

        public void OnCubesVerticalClicked()
        {
            m_SelectedObject = SelectedObject.BlocksVertical;   
                        
        }
        
        public void OnCubesCornerClicked()
        {
            m_SelectedObject = SelectedObject.BlocksCorner;  
        }
        
        
        //Size Selection
        public void OnMiniClicked()
        {
            m_SelectedSize = SelectedSize.Mini;
        }
        
        public void OnSmallClicked()
        {
            m_SelectedSize = SelectedSize.Small;
        }
        
        public void OnMediumClicked()
        {
            m_SelectedSize = SelectedSize.Medium;
        }
        
        public void OnLargeClicked()
        {
            m_SelectedSize = SelectedSize.Large;
        }

        

        private void _ActiveteButton()
        {
            GameObject btn = EventSystem.current.currentSelectedGameObject;
            _ShowAndroidToastMessage(btn.name);
            btn.GetComponent<Image>().color = Color.black;          
            btn.GetComponent<Text>().color = Color.white;
        }
        
        
        private void _ShowAndroidToastMessage(string message)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity =
                unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (unityActivity != null)
            {
                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
                unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject toastObject =
                        toastClass.CallStatic<AndroidJavaObject>(
                            "makeText", unityActivity, message, 0);
                    toastObject.Call("show");
                }));
            }
        }

        private float CalcAdjustedHeight()
        {
            switch (m_SelectedSize)
            {
                case SelectedSize.Mini:
                    return 0.25f / 4; 
                case SelectedSize.Small:
                    return 0.25f/2;
                case SelectedSize.Large:
                    return 0.5f;
                default:
                    return 0.25f;
            }
        }
    }
}
