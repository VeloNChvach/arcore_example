//-----------------------------------------------------------------------
// <copyright file="HelloARController.cs" company="Google">
//
// Copyright 2017 Google Inc. All Rights Reserved.
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

namespace GoogleARCore.HelloAR
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;
    using GoogleARCore;
    using UnityEngine.UI;
    using System.Net.Mail;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.IO;
    using System.Net;

    /// <summary>
    /// Controlls the HelloAR example.
    /// </summary>
    public class HelloARController : MonoBehaviour
    {
        /// <summary>
        /// The first-person camera being used to render the passthrough camera.
        /// </summary>
        public Camera m_firstPersonCamera;

        /// <summary>
        /// A prefab for tracking and visualizing detected planes.
        /// </summary>
        public GameObject m_trackedPlanePrefab;

        /// <summary>
        /// A model to place when a raycast from a user touch hits a plane.
        /// </summary>
        public GameObject m_andyAndroidPrefab;
        public GameObject m_andyAndroidPrefab_UI;

        /// <summary>
        /// A gameobject parenting UI for displaying the "searching for planes" snackbar.
        /// </summary>
        public GameObject m_searchingForPlaneUI;

        private List<TrackedPlane> m_newPlanes = new List<TrackedPlane>();

        private List<TrackedPlane> m_allPlanes = new List<TrackedPlane>();

        // Raycast for UI
        private RaycastHit myhit = new RaycastHit();
        private Ray myray = new Ray();
        private bool alreadySend = false;

        private Color[] m_planeColors = new Color[] {
            new Color(1.0f, 1.0f, 1.0f),
            new Color(0.956f, 0.262f, 0.211f),
            new Color(0.913f, 0.117f, 0.388f),
            new Color(0.611f, 0.152f, 0.654f),
            new Color(0.403f, 0.227f, 0.717f),
            new Color(0.247f, 0.317f, 0.709f),
            new Color(0.129f, 0.588f, 0.952f),
            new Color(0.011f, 0.662f, 0.956f),
            new Color(0f, 0.737f, 0.831f),
            new Color(0f, 0.588f, 0.533f),
            new Color(0.298f, 0.686f, 0.313f),
            new Color(0.545f, 0.764f, 0.290f),
            new Color(0.803f, 0.862f, 0.223f),
            new Color(1.0f, 0.921f, 0.231f),
            new Color(1.0f, 0.756f, 0.027f)
        };

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            _QuitOnConnectionErrors();

            // The tracking state must be FrameTrackingState.Tracking in order to access the Frame.
            if (Frame.TrackingState != FrameTrackingState.Tracking)
            {
                const int LOST_TRACKING_SLEEP_TIMEOUT = 15;
                Screen.sleepTimeout = LOST_TRACKING_SLEEP_TIMEOUT;
                return;
            }

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Frame.GetNewPlanes(ref m_newPlanes);

            // Iterate over planes found in this frame and instantiate corresponding GameObjects to visualize them.
            for (int i = 0; i < m_newPlanes.Count; i++)
            {
                // Instantiate a plane visualization prefab and set it to track the new plane. The transform is set to
                // the origin with an identity rotation since the mesh for our prefab is updated in Unity World
                // coordinates.
                GameObject planeObject = Instantiate(m_trackedPlanePrefab, Vector3.zero, Quaternion.identity,
                    transform);
                planeObject.GetComponent<TrackedPlaneVisualizer>().SetTrackedPlane(m_newPlanes[i]);

                // Apply a random color and grid rotation.
                planeObject.GetComponent<Renderer>().material.SetColor("_GridColor", m_planeColors[Random.Range(0,
                    m_planeColors.Length - 1)]);
                planeObject.GetComponent<Renderer>().material.SetFloat("_UvRotation", Random.Range(0.0f, 360.0f));
            }

            // Disable the snackbar UI when no planes are valid.
            bool showSearchingUI = true;
            Frame.GetAllPlanes(ref m_allPlanes);
            for (int i = 0; i < m_allPlanes.Count; i++)
            {
                if (m_allPlanes[i].IsValid)
                {
                    showSearchingUI = false;
                    break;
                }
            }

            m_searchingForPlaneUI.SetActive(showSearchingUI);

            Touch touch;
            if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
            {
                return;
            }

            TrackableHit hit;
            TrackableHitFlag raycastFilter = TrackableHitFlag.PlaneWithinBounds | TrackableHitFlag.PlaneWithinPolygon;

            if (Session.Raycast(m_firstPersonCamera.ScreenPointToRay(touch.position), raycastFilter, out hit))
            {
                // Create an anchor to allow ARCore to track the hitpoint as understanding of the physical
                // world evolves.
                var anchor = Session.CreateAnchor(hit.Point, Quaternion.identity);

                // Intanstiate an Andy Android object as a child of the anchor; it's transform will now benefit
                // from the anchor's tracking.
                var andyObject = Instantiate(m_andyAndroidPrefab, hit.Point, Quaternion.AngleAxis(90, Vector3.left),
                    anchor.transform);

                // Add UIHouse
                GameObject UIHouse = Instantiate(m_andyAndroidPrefab_UI, new Vector3(0f, 0.62f, 0f), Quaternion.identity);
                UIHouse.transform.SetParent(andyObject.transform);
                UIHouse.transform.LookAt(m_firstPersonCamera.transform);

                // Andy should look at the camera but still be flush with the plane.
                andyObject.transform.LookAt(m_firstPersonCamera.transform);
                andyObject.transform.rotation = Quaternion.Euler(0.0f,
                    andyObject.transform.rotation.eulerAngles.y, andyObject.transform.rotation.z);
                andyObject.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

                // Use a plane attachment component to maintain Andy's y-offset from the plane
                // (occurs after anchor updates).
                andyObject.GetComponent<PlaneAttachment>().Attach(hit.Plane);
            }

            //Session.Raycast(m_firstPersonCamera.ScreenPointToRay(touch.position),TrackableHitFlag.None

            myray = Camera.main.ScreenPointToRay(touch.position);
            if (Physics.Raycast(myray, out myhit))
                controllerUI(myhit);

        }

        private void controllerUI(RaycastHit myhit)
        {
            var name = myhit.transform.name;
            var pointLight = myhit.transform.parent.gameObject.transform.Find("Door").Find("Point_light").gameObject;
            var canvasHelp = myhit.transform.parent.gameObject.transform.Find("Canvas_help");
            var upUI = canvasHelp.Find("UpUI").gameObject;
            var downUI = canvasHelp.Find("DownUI").gameObject;

            if (name == "Up")
            {
                if (upUI.activeSelf == true)
                {
                    upUI.SetActive(false);
                    upUI.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
                }
                else
                {
                    upUI.SetActive(true);
                    upUI.GetComponentInChildren<Button>().onClick.AddListener(ClickSend);
                }
            }

            if (name == "Down")
            {
                if (downUI.activeSelf == true)
                {
                    downUI.SetActive(false);
                    downUI.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
                }
                else
                {
                    downUI.SetActive(true);
                    downUI.GetComponentInChildren<Button>().onClick.AddListener(ClickGo);
                }
            }

            if (name == "Door")
            {
                if (pointLight.activeSelf == true)
                {
                    pointLight.SetActive(false);
                }
                else
                {
                    pointLight.SetActive(true);
                    pointLight.GetComponent<Light>().color = new Color(Random.Range(0, 2) * 255f,
                        Random.Range(0, 2) * 255f, Random.Range(0, 2) * 255f, Random.Range(0, 2) * 255f);
                }
            }
        }

        private void ClickSend()
        {
            if (alreadySend)
                return;

            Debug.Log("Send...");

            //ScreenCapture.CaptureScreenshot("Assets/screenshot.png");

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("valerychvach@gmail.com");
            mail.To.Add("valerychvach@gmail.com");
            mail.Subject = "Test Mail";
            mail.Body = "This is for testing SMTP mail from GMAIL";
            //mail.Attachments.Add(new Attachment("Assets/screenshot.png"));
            SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");
            smtpServer.Port = 587;
            smtpServer.Credentials = new System.Net.NetworkCredential("valerychvach@gmail.com", "Velachvach") as ICredentialsByHost;
            smtpServer.EnableSsl = true;
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                { return true; };
            smtpServer.Send(mail);
            alreadySend = true;

            Debug.Log("Complete!");
        }

        private void ClickGo()
        {
            Application.OpenURL("http://tsukat.com/");
        }


        /// <summary>
        /// Quit the application if there was a connection error for the ARCore session.
        /// </summary>
        private void _QuitOnConnectionErrors()
        {
            // Do not update if ARCore is not tracking.
            if (Session.ConnectionState == SessionConnectionState.DeviceNotSupported)
            {
                _ShowAndroidToastMessage("This device does not support ARCore.");
                Application.Quit();
            }
            else if (Session.ConnectionState == SessionConnectionState.UserRejectedNeededPermission)
            {
                _ShowAndroidToastMessage("Camera permission is needed to run this application.");
                Application.Quit();
            }
            else if (Session.ConnectionState == SessionConnectionState.ConnectToServiceFailed)
            {
                _ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
                Application.Quit();
            }
        }

        /// <summary>
        /// Show an Android toast message.
        /// </summary>
        /// <param name="message">Message string to show in the toast.</param>
        /// <param name="length">Toast message time length.</param>
        private static void _ShowAndroidToastMessage(string message)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (unityActivity != null)
            {
                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
                unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
                        message, 0);
                    toastObject.Call("show");
                }));
            }
        }
    }
}
