using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.UI;

#if UNITY_ANDROID
using UnityEngine.XR.ARCore;
#endif

namespace UnityEngine.XR.ARFoundation.Samples
{
    [RequireComponent(typeof(ARSession))]
    public class TrajectoryPlayback : MonoBehaviour
    {
        private int frame_counter = 0;
        string m_Mp4Path;
        ARSession m_Session;
        public Text log;

        [SerializeField]
        [Tooltip("The ARCameraManager which will produce frame events.")]
        ARCameraManager m_CameraManager;

        public GameObject _camera;
        public long? frame_timestamp;
        private bool trajectory_written = false;
        private string devicePose = "";
        public int num_playbacks = 5;
        private int playback_count = 0;

        // Start AR session and set path for recorded sequence data in local storage
        void Awake()
        {
            m_Session = GetComponent<ARSession>();
            m_Mp4Path = Path.Combine(Application.persistentDataPath, "arcore-session.mp4");
        }

        void Start()
        {

        }

        void OnEnable()
        {
            if (m_CameraManager == null)
            {
                Debug.LogException(new NullReferenceException(
                    $"Serialized properties were not initialized on {name}'s {nameof(CpuImageSample)} component."), this);
                return;
            }

            m_CameraManager.frameReceived += OnCameraFrameReceived;
        }

        void OnDisable()
        {
            if (m_CameraManager != null)
                m_CameraManager.frameReceived -= OnCameraFrameReceived;
        }

        void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
        {
            frame_timestamp = eventArgs.timestampNs;

            frame_counter += 1;

            // Once app is started and started to load camera frames, check AR system is running
            if (m_Session.subsystem is ARCoreSessionSubsystem subsystem)
            {
                var playbackStatus = subsystem.playbackStatus;

                // After 60 frames, check AR session is valid and for saved sequence in local storage
                if (frame_counter == 60)
                {
                    var session = subsystem.session;
                    if (session == null)
                    {
                        log.text = "No AR session found";
                        return;
                    }

                    // If sequence found, start sequence playback
                    if (File.Exists(m_Mp4Path))
                    {
                        var status = subsystem.StartPlayback(m_Mp4Path);
                    }
                    else
                    {
                        log.text = "No sequence found";
                    }
                }

                // During playback, record device pose every frame
                if (playbackStatus.Playing())
                {
                    devicePose = devicePose + frame_timestamp.ToString() + " " + _camera.transform.position.x.ToString("F4") + " " + _camera.transform.position.y.ToString("F4") + " " + _camera.transform.position.z.ToString("F4") + " " + _camera.transform.rotation.x.ToString("F4") + " " + _camera.transform.rotation.y.ToString("F4") + " " + _camera.transform.rotation.z.ToString("F4") + " " + _camera.transform.rotation.w.ToString("F4") + "\n";

                    // As soon as new trajectory starts recording, indicate that it needs to be written out to local storage
                    if (trajectory_written == true)
                    {
                        trajectory_written = false;
                    }
                }

                // When finished, write estimated trajectory to local storage if hasn't already
                if ((playbackStatus == ArPlaybackStatus.Finished) && (trajectory_written == false))
                {
                    string filepath_pose = Application.persistentDataPath + "/trajectory_" + (playback_count + 1).ToString() + ".txt";
                    using (StreamWriter sw = new StreamWriter(filepath_pose))
                    {
                        sw.WriteLine(devicePose);
                    }
                    trajectory_written = true;
                    devicePose = "";
                    playback_count += 1;

                    // Keep playing back until we have the required number of trajectories
                    if (playback_count < num_playbacks)
                    {
                        var status = subsystem.StartPlayback(m_Mp4Path);
                        log.text = "Playback " + (playback_count + 1).ToString() + " started";
                    }
                    // When we have completed required number of playbacks, reset count to 0
                    else
                    {
                        playback_count = 0;
                        log.text = "Playback finished";
                    }
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}
