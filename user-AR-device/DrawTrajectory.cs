using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.UI;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections.LowLevel.Unsafe;
using System.Diagnostics;

#if UNITY_ANDROID
using UnityEngine.XR.ARCore;
#endif

namespace UnityEngine.XR.ARFoundation.Samples
{
    [RequireComponent(typeof(ARSession))]
    public class DrawTrajectory : MonoBehaviour
    {
        //-------------------Core SiTAR System------------------------

        // Variables for AR device camera (for pose data and camera image frame events)
        public GameObject _camera;
        [SerializeField]
        [Tooltip("The ARCameraManager which will produce frame events.")]
        ARCameraManager m_CameraManager;

        // Variables for trajectory visualization
        [SerializeField]
        GameObject m_startPrefab;
        [SerializeField]
        GameObject m_stopPrefab;
        public GameObject startPrefab
        {
            get { return m_startPrefab; }
            set { m_startPrefab = value; }
        }
        public GameObject stopPrefab
        {
            get { return m_stopPrefab; }
            set { m_stopPrefab = value; }
        }
        GameObject startObject;
        GameObject stopObject;
        public GameObject cylinderPrefab;
        public GameObject jointPrefab;
        public GameObject frustumPrefab;
        private List<GameObject> frustums = new List<GameObject>();
        private static List<string> frustum_timestamps = new List<string>();
        public GameObject errorAreaHighPrefab;
        public static List<GameObject> spawnedErrorAreasHigh = new List<GameObject>();
        public GameObject errorAreaMediumPrefab;
        public static List<GameObject> spawnedErrorAreasMedium = new List<GameObject>();
        public GameObject errorPatchHighPrefab;
        public static List<GameObject> spawnedErrorPatchesHigh = new List<GameObject>();
        public GameObject errorPatchMediumPrefab;
        public static List<GameObject> spawnedErrorPatchesMedium = new List<GameObject>();
        private static List<Vector3> errorEndpoints = new List<Vector3>();
        private static List<float> errorEndpointDistances = new List<float>();
        private List<GameObject> cylinders = new List<GameObject>();
        private List<GameObject> joints = new List<GameObject>();
        public Material errorHigh;
        public Material errorMedium;
        private Material error_material;

        // Variables for trajectory data (e.g., pose, depth for each sub-trajectory)
        private bool trajectory_started = false;
        private bool evaluation_started = false;
        private bool analysis_started = false;
        private int frame_count = 0;
        private Vector3 start_position;
        private Vector3 stop_position;
        private static List<string> trajectory_timestamps = new List<string>();
        private static List<Vector3> trajectory_positions_all = new List<Vector3>();
        private static List<Vector3> trajectory_mid_positions = new List<Vector3>();
        private static List<Quaternion> trajectory_mid_rotations = new List<Quaternion>();
        private static List<float> trajectory_mid_distances = new List<float>();
        private static List<Quaternion> trajectory_mid_orientations = new List<Quaternion>();
        private static List<Texture2D> trajectory_mid_rgbs = new List<Texture2D>();
        private int image_width;
        private int image_height;
        private int capture_count = 0;

        // Variables for sequence recording using ARCore Recording and Playback API
        ArStatus? m_SetMp4DatasetResult;
        string m_Mp4Path;
        ARSession m_Session;
        public long? frame_timestamp;
        private string original_timestamps = "";

        // Variables for accessing depth data using ARCore Depth API
        Texture2D _depthTexture;
        short[] _depthArray;
        private int depthWidth;
        private int depthHeight;
        private float Interpolation = 0.75f;
        private Vector2 ScreenPosition = new Vector2(0.5f, 0.5f);
        private const float _outlierDepthRatio = 0.2f;
        private const int _windowRadiusPixels = 2;
        private float _distance = 1.0f;
        public float Distance => _distance;

        public Matrix4x4 RotationByPortriat
        {
            get
            {
                switch (Screen.orientation)
                {
                    case ScreenOrientation.Portrait:
                        return Matrix4x4.Rotate(Quaternion.identity);
                    case ScreenOrientation.LandscapeLeft:
                        return Matrix4x4.Rotate(Quaternion.Euler(0, 0, 90));
                    case ScreenOrientation.PortraitUpsideDown:
                        return Matrix4x4.Rotate(Quaternion.Euler(0, 0, 180));
                    case ScreenOrientation.LandscapeRight:
                        return Matrix4x4.Rotate(Quaternion.Euler(0, 0, 270));
                    default:
                        return Matrix4x4.Rotate(Quaternion.identity);
                }
            }
        }

        //---------------User Interaction and Additional Features-------------

        // Variables for core UI buttons
        public Button startButton;
        public Button stopButton;
        public Button captureButton;

        // Variables for entering server IP address in UI
        public GameObject introPanel;
        public InputField IPEntry;
        public string edge_ip = "";

        // Variables for displaying SiTAR status and trajectory stats on UI
        public Text status;
        private int progressPercentage = -10;
        private float trajectory_duration;
        private float trajectory_length;
        private float trajectory_mean_env_depth;
        public Text trajectoryDuration;
        public Text trajectoryLength;
        public Text trajectoryDepth;
        public Text trajectoryRegions;

        // Variables for user notification audio
        public AudioClip audioResults;
        public AudioClip audioCapture;
        public AudioClip audioComplete;

        // Variables for showing number of challenging regions identified in UI
        private int max_challenging_regions = 3;
        private int challenging_region_count = 0;
        private static List<int> trajectory_error_rendered = new List<int>();

        // Variables for timestamped folder creation for saved data
        private string timestamp = "";
        private string subtrajectoryData = "";

        // Variables for logging IMU data
        Gyroscope m_Gyro;
        private static List<float> trajectory_mid_acc_x = new List<float>();
        private static List<float> trajectory_mid_acc_y = new List<float>();
        private static List<float> trajectory_mid_acc_z = new List<float>();
        private static List<float> trajectory_mid_gyro_x = new List<float>();
        private static List<float> trajectory_mid_gyro_y = new List<float>();
        private static List<float> trajectory_mid_gyro_z = new List<float>();
        

        void Awake()
        {
            // Start AR session and set path for recorded sequence data in local storage
            m_Session = GetComponent<ARSession>();
            m_Mp4Path = Path.Combine(Application.persistentDataPath, "arcore-session.mp4");
        }

        void Start()
        {
            // Use smooth Depth API data so we have values for every pixel in depth image
            DepthSource.SwitchToRawDepth(false);
            // Update UI with instructions
            status.text = "Press Start to record";
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


        //--------------------------Trajectory Creation Module---------------------------
        //-------------------------------------------------------------------------------

        // Co-routine that provides depth magnitude and normal through ARCore Depth API
        IEnumerator GetDepth()
        {
            try
            {
                float distance = ComputeCenterScreenDistance();

                if (distance > DepthSource.InvalidDepthValue)
                {
                    Vector3 translation = DepthSource.ARCamera.ScreenToWorldPoint(new Vector3(
                        Screen.width * ScreenPosition.x,
                        Screen.height * ScreenPosition.y,
                        distance));

                    // Add distance to environment surface to sub-trajectory list
                    trajectory_mid_distances.Add(distance);
                }

                Quaternion? orientation = ComputeCenterScreenOrientation();
                if (orientation != null)
                {
                    // Add environment surface normal to sub-trajectory list
                    trajectory_mid_orientations.Add(orientation.Value);
                }
            }
            catch (InvalidOperationException)
            {
                // Intentional pitfall, depth values were invalid
            }
            yield return null;
        }

        private float ComputeCenterScreenDistance()
        {
            Vector2 depthMapPoint = ScreenPosition;

            if (!DepthSource.Initialized)
            {
                throw new InvalidOperationException("Depth source is not initialized");
            }

            short[] depthMap = DepthSource.DepthArray;
            float depthM = DepthSource.GetDepthFromUV(depthMapPoint, depthMap);

            if (depthM <= DepthSource.InvalidDepthValue)
            {
                throw new InvalidOperationException("Invalid depth value");
            }

            Vector3 viewspacePoint = DepthSource.ComputeVertex(depthMapPoint, depthM);
            return viewspacePoint.magnitude;
        }

        private Quaternion? ComputeCenterScreenOrientation()
        {
            Vector3 normal = RotationByPortriat *
                ComputeNormalMapFromDepthWeightedMeanGradient(ScreenPosition);

            // Transforms normal to the world space.
            normal = DepthSource.ARCamera.transform.TransformDirection(normal);

            Vector3 right = Vector3.right;
            if (normal != Vector3.up)
            {
                right = Vector3.Cross(normal, Vector3.up);
            }

            Vector3 forward = Vector3.Cross(normal, right);
            Quaternion orientation = Quaternion.identity;
            orientation.SetLookRotation(forward, normal);

            return orientation;
        }

        // ARCore Depth API method that computes depth normal from depth map
        private Vector3 ComputeNormalMapFromDepthWeightedMeanGradient(Vector2 screenUV)
        {
            short[] depthMap = DepthSource.DepthArray;
            Vector2 depthUV = screenUV;
            Vector2Int depthXY = DepthSource.DepthUVtoXY(depthUV);
            float depth_m = DepthSource.GetDepthFromUV(depthUV, depthMap);

            if (depth_m == DepthSource.InvalidDepthValue)
            {
                throw new InvalidOperationException("Invalid depth value");
            }

            // Iterates over neighbors to compute normal vector.
            float neighbor_corr_x = 0.0f;
            float neighbor_corr_y = 0.0f;
            float outlier_distance_m = _outlierDepthRatio * depth_m;
            int radius = _windowRadiusPixels;
            float neighbor_sum_confidences_x = 0.0f;
            float neighbor_sum_confidences_y = 0.0f;
            for (int dy = -radius; dy <= radius; ++dy)
            {
                for (int dx = -radius; dx <= radius; ++dx)
                {
                    // Self isn't a neighbor.
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    Vector2Int offset = new Vector2Int(dx, dy);
                    int currentX = depthXY.x + offset.x;
                    int currentY = depthXY.y + offset.y;

                    // Retrieves neighbor value.
                    float neighbor_depth_m = DepthSource.GetDepthFromXY(currentX, currentY, depthMap);

                    // Confidence is not currently being packed yet, so for now this hardcoded.
                    float neighbor_confidence = 1.0f;
                    if (neighbor_depth_m == 0.0)
                    {
                        continue; // Neighbor does not exist.
                    }

                    float neighbor_distance_m = neighbor_depth_m - depth_m;

                    // Checks for outliers.
                    if (neighbor_confidence == 0.0f ||
                        Mathf.Abs(neighbor_distance_m) > outlier_distance_m)
                    {
                        continue;
                    }

                    // Updates correlations in each dimension.
                    if (dx != 0)
                    {
                        neighbor_sum_confidences_x += neighbor_confidence;
                        neighbor_corr_x += neighbor_confidence * neighbor_distance_m / dx;
                    }

                    if (dy != 0)
                    {
                        neighbor_sum_confidences_y += neighbor_confidence;
                        neighbor_corr_y += neighbor_confidence * neighbor_distance_m / dy;
                    }
                }
            }

            if (neighbor_sum_confidences_x == 0 && neighbor_sum_confidences_y == 0)
            {
                throw new InvalidOperationException("Invalid confidence value.");
            }

            // Computes estimate of normal vector by finding weighted averages of
            // the surface gradient in x and y.
            float pixel_width_m = depth_m / DepthSource.FocalLength.x;
            float slope_x = neighbor_corr_x / (pixel_width_m * neighbor_sum_confidences_x);
            float slope_y = neighbor_corr_y / (pixel_width_m * neighbor_sum_confidences_y);

            // Negatives convert the normal to Unity's coordinate system.
            Vector3 normal = new Vector3(-slope_y, -slope_x, -1.0f);
            normal.Normalize();

            return normal;
        }

        // Method that runs on every received camera frame to log trajectory data
        unsafe void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
        {
            frame_timestamp = eventArgs.timestampNs;

            if (m_Session.subsystem is ARCoreSessionSubsystem subsystem)
            {
                var recordingStatus = subsystem.recordingStatus;

                if (recordingStatus.Recording())
                {
                    original_timestamps = original_timestamps + frame_timestamp.ToString() + "\n";
                }
            }

            // If recording has started, log trajectory data
            if (trajectory_started == true)
            {
                frame_count += 1;
                // Log the following data at the midpoints of each 10-frame sub-trajectory
                if ((frame_count % 5 == 0) && (frame_count % 10 != 0))
                {
                    // Pose
                    trajectory_positions_all.Add(_camera.transform.position);
                    trajectory_mid_positions.Add(_camera.transform.position);
                    trajectory_mid_rotations.Add(_camera.transform.rotation);
                    // Depth
                    StartCoroutine(GetDepth());
                    // IMU
                    trajectory_mid_acc_x.Add(Input.acceleration.x);
                    trajectory_mid_acc_y.Add(Input.acceleration.y);
                    trajectory_mid_acc_z.Add(Input.acceleration.z);
                    trajectory_mid_gyro_x.Add(Input.gyro.rotationRate.x);
                    trajectory_mid_gyro_y.Add(Input.gyro.rotationRate.y);
                    trajectory_mid_gyro_z.Add(Input.gyro.rotationRate.z);
                    // RGB camera images
                    if (m_CameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
                    {
                        var format = TextureFormat.RGBA32;
                        image_width = image.width;
                        image_height = image.height;
                        if (m_CameraTexture == null || m_CameraTexture.width != image_width || m_CameraTexture.height != image_height)
                        {
                            m_CameraTexture = new Texture2D(image_width, image_height, format, false);
                        }
                        var conversionParams = new XRCpuImage.ConversionParams(image, format, XRCpuImage.Transformation.MirrorY);
                        var rawTextureData = m_CameraTexture.GetRawTextureData<byte>();
                        try
                        {
                            image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
                        }
                        finally
                        {
                            image.Dispose();
                        }
                        m_CameraTexture.Apply();
                        Texture2D _rgbTexture = new Texture2D(image_width, image_height, format, false);
                        Graphics.CopyTexture(m_CameraTexture, _rgbTexture);
                        trajectory_mid_rgbs.Add(_rgbTexture);
                    }
                }
                // Log the following data at the endpoints of each 10-frame sub-trajectory
                if (frame_count % 10 == 0)
                {
                    trajectory_positions_all.Add(_camera.transform.position);
                    trajectory_timestamps.Add(frame_timestamp.ToString());
                }
            }
            if (analysis_started == true)
            {
                // Get RGB camera images that can be saved if user presses 'Capture'
                if (m_CameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
                {
                    var format = TextureFormat.RGBA32;
                    if (m_CameraTexture == null || m_CameraTexture.width != image_width || m_CameraTexture.height != image_height)
                    {
                        m_CameraTexture = new Texture2D(image_width, image_height, format, false);
                    }
                    var conversionParams = new XRCpuImage.ConversionParams(image, format, XRCpuImage.Transformation.MirrorY);
                    var rawTextureData = m_CameraTexture.GetRawTextureData<byte>();
                    try
                    {
                        image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
                    }
                    finally
                    {
                        image.Dispose();
                    }
                    m_CameraTexture.Apply();
                }
            }
        }

        void Update()
        {
            
        }

        static int GetRotation() => Screen.orientation switch
        {
            ScreenOrientation.Portrait => 0,
            ScreenOrientation.LandscapeLeft => 90,
            ScreenOrientation.PortraitUpsideDown => 180,
            ScreenOrientation.LandscapeRight => 270,
            _ => 0
        };

        // Method called when user presses 'Submit' button after entering IP address
        public void HandleSubmitClick()
        {
            edge_ip = IPEntry.text;
            introPanel.gameObject.SetActive(false);
            
            //Needed to access gyroscope data on Android
#if UNITY_ANDROID
            m_Gyro = Input.gyro;
            m_Gyro.enabled = true;
#endif
        }

        // Method called when user presses 'Start' button to start recording trajectory
        public void HandleStartClick()
        {
            startButton.interactable = false;
            stopButton.interactable = true;

            if (m_Session.subsystem is ARCoreSessionSubsystem subsystem)
            {
                var session = subsystem.session;
                if (session == null)
                {
                    return;
                }
                using (var config = new ArRecordingConfig(session))
                {
                    config.SetMp4DatasetFilePath(session, m_Mp4Path);
                    config.SetRecordingRotation(session, GetRotation());
                    var status = subsystem.StartRecording(config);
                }
            }
            trajectory_started = true;
            start_position = _camera.transform.position;
            startObject = Instantiate(m_startPrefab, start_position, Quaternion.identity);
            status.text = "Recording trajectory";
        }

        // Method called when user presses 'Stop' button to stop recording trajectory
        public void HandleStopClick()
        {
            stopButton.interactable = false;

            if (m_Session.subsystem is ARCoreSessionSubsystem subsystem)
            {
                var status = subsystem.StopRecording();
            }
            trajectory_started = false;

            // Folder creation for saved data
            timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm");
            Directory.CreateDirectory(Application.persistentDataPath + "/" + timestamp);
            string filepath_ft = Application.persistentDataPath + "/" + timestamp + "/original_timestamps.txt";
            using (StreamWriter sw = new StreamWriter(filepath_ft))
            {
                sw.WriteLine(original_timestamps);
            }

            //write trajectory timestamps
            string filepath_tt = Application.persistentDataPath + "/" + timestamp + "/trajectory_timestamps.txt";
            using (StreamWriter sw = new StreamWriter(filepath_tt))
            {
                foreach (var ts in trajectory_timestamps)
                {
                    sw.WriteLine(ts);
                }
            }

            stop_position = _camera.transform.position;
            stopObject = Instantiate(m_stopPrefab, stop_position, Quaternion.identity);
            DrawInitialTrajectory();
            StartCoroutine(UploadTimestamps());
            StartCoroutine(UploadRecording());
        }

        // Upload sub-trajectory timestamps to server for logging (optional)
        IEnumerator UploadTimestamps()
        {
            WWWForm form = new WWWForm();
            form.AddBinaryData("timestamps", File.ReadAllBytes(Application.persistentDataPath + "/" + timestamp + "/original_timestamps.txt"), "original_timestamps.txt", "text/csv");
            string url_timestamps = "http://" + edge_ip + ":8000/timestamps";
            UnityWebRequest www = UnityWebRequest.Post(url_timestamps, form);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
                status.text = "Network error";
            }
            else
            {
                evaluation_started = true;
                float expected_time = trajectory_duration*5 + 17.0f;
                float progress_increment_time = expected_time / 10.0f;
                StartCoroutine(ProgressUpdate(progress_increment_time));
            }
        }

        // Update UI progress indicator
        IEnumerator ProgressUpdate(float increment)
        {
            while (evaluation_started == true)
            {
                progressPercentage += 10;
                status.text = "Evaluation in progress: " + progressPercentage.ToString() + "%";
                yield return new WaitForSeconds(increment);
            }
        }

        // Upload visual and inertial input data ('recording') to server
        IEnumerator UploadRecording()
        {
            WWWForm form = new WWWForm();
            form.AddBinaryData("recording", File.ReadAllBytes(Application.persistentDataPath + "/arcore-session.mp4"), "arcore-session.mp4", "video/mp4");
            string url_recording = "http://" + edge_ip + ":8000/recording";
            UnityWebRequest www = UnityWebRequest.Post(url_recording, form);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
                status.text = "Network error";
            }
            else
            {
                status.text = "Evaluation complete";
                evaluation_started = false;
                StartCoroutine(GetResults());
            }
        }


        //--------------------------Trajectory Visualization Module---------------------------
        //------------------------------------------------------------------------------------

        // Create situated visualization of initial trajectory estimate
        void DrawInitialTrajectory()
        {
            // Disable occlusion manager so situated visualizations are not occluded by real world objects (optional)
            var occlusionManager = GetComponentInChildren<AROcclusionManager>();
            occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Disabled;

            float length = 0.0f;
            for (var i = 0; i < (trajectory_positions_all.Count); i++)
            {
                if (i == 0)
                {
                    var offset = trajectory_positions_all[i] - start_position;
                    var scale = new Vector3(0.03f, offset.magnitude / 2.0f, 0.03f);
                    var position = start_position + (offset / 2.0f);
                    var cylinder = Instantiate(cylinderPrefab, position, Quaternion.identity);
                    cylinder.transform.up = offset;
                    cylinder.transform.localScale = scale;
                    cylinders.Add(cylinder);
                    length = length + offset.magnitude;
                }
                else
                {
                    var offset = trajectory_positions_all[i] - trajectory_positions_all[i-1];
                    var scale = new Vector3(0.03f, offset.magnitude / 2.0f, 0.03f);
                    var position = trajectory_positions_all[i-1] + (offset / 2.0f);
                    var cylinder = Instantiate(cylinderPrefab, position, Quaternion.identity);
                    cylinder.transform.up = offset;
                    cylinder.transform.localScale = scale;
                    cylinders.Add(cylinder);
                    length = length + offset.magnitude;
                }
                var joint = Instantiate(jointPrefab, trajectory_positions_all[i], Quaternion.identity);
                joints.Add(joint);

                // Every 0.5m of trajectory draw a camera frustum
                if (length > 0.5f)
                {
                    if (i % 2 == 0)
                    {
                        var frustum = Instantiate(frustumPrefab, trajectory_mid_positions[i / 2] + new Vector3(0.0f, 0.0f, 0.0f), trajectory_mid_rotations[i / 2] * Quaternion.Euler(0, 0, 0));
                        frustums.Add(frustum);
                        frustum_timestamps.Add(trajectory_timestamps[i / 2]);
                    }
                    else
                    {
                        var frustum = Instantiate(frustumPrefab, trajectory_mid_positions[(i - 1) / 2] + new Vector3(0.0f, 0.0f, 0.0f), trajectory_mid_rotations[(i - 1) / 2] * Quaternion.Euler(0, 0, 0));
                        frustums.Add(frustum);
                        frustum_timestamps.Add(trajectory_timestamps[(i - 1) / 2]);
                    }
                    length = 0.0f;
                }
            }
            trajectory_duration = 0.033333f * frame_count * 2;
            trajectory_length = 0.5f * frustums.Count;
            float sum = 0;
            for (var i = 0; i < (trajectory_mid_distances.Count); i++)
            {
                sum += trajectory_mid_distances[i];
            }
            trajectory_mean_env_depth = sum / trajectory_mid_distances.Count;
            trajectoryDuration.text = "Trajectory duration: " + trajectory_duration.ToString("F1") + "s";
            trajectoryLength.text = "Trajectory length: " + trajectory_length.ToString("F1") + "m";
            trajectoryDepth.text = "Average environment depth: " + trajectory_mean_env_depth.ToString("F1") + "m";
        }

        // Handle receipt of sub-trajectory pose error estimates from system backend and save to local storage
        IEnumerator GetResults()
        {
            string url_results = "http://" + edge_ip + ":8000/results.csv";
            using (UnityWebRequest www = UnityWebRequest.Get(url_results))
            {
                yield return www.SendWebRequest();
                if (www.isNetworkError || www.isHttpError)
                {
                    status.text = "Network error";
                }
                else
                {
                    string savePath = string.Format("{0}/{1}.csv", Application.persistentDataPath, "results");
                    System.IO.File.WriteAllText(savePath, www.downloadHandler.text);
                }
            }
            UpdateTrajectory();
        }

        // Update situated trajectory visualization with pose error estimates
        void UpdateTrajectory()
        {
            // Create dictionary with .csv of timestamped estimates sent from server
            var subtrajectories = new Dictionary<string, string>();
            var lines = File.ReadAllLines(Application.persistentDataPath + "/results.csv");
            foreach (var l in lines)
            {
                var lsplit = l.Split(',');
                if (lsplit.Length > 1)
                {
                    var newkey = lsplit[0];
                    var newval = lsplit[1];
                    subtrajectories[newkey] = newval;
                }
            }

            // Write out dictionary for logging (optional)
            string filepath_sd = Application.persistentDataPath + "/subtrajectory_dict.txt";
            using (StreamWriter sw = new StreamWriter(filepath_sd))
            {
                foreach (KeyValuePair<string, string> sub in subtrajectories)
                {
                    sw.WriteLine(sub.Key + "," + sub.Value);
                }
            }

            // Set all sub-trajectory rendered indicators to zero to indicate none have been rendered yet
            for (var i = 0; i < trajectory_timestamps.Count; i++)
            {
                trajectory_error_rendered.Add(0);
            }

            // Set class boundaries for error
            var error_boundaries = new List<float> { 0.1f, 0.05f, 0.02f, 0.01f};

            // Loop through each class boundary and check for sub-trajectories where error is greater than it
            foreach (var error_boundary in error_boundaries)
            {
                // Continue if number of challenging regions found is less than max number set
                if (challenging_region_count < max_challenging_regions)
                {
                    // Set materials (colors) for visualizing different error magnitudes
                    if (error_boundary == 0.1f)
                    {
                        error_material = errorHigh;
                    }
                    else
                    {
                        error_material = errorMedium;
                    }

                    // Loop through saved trajectory timestamps and render visualizations where appropriate
                    for (var i = 0; i < trajectory_timestamps.Count; i++)
                    {
                        if (subtrajectories.ContainsKey(trajectory_timestamps[i]) && challenging_region_count < max_challenging_regions)
                        {
                            // Check error magnitude
                            float sub_error = (float)Convert.ToDouble(subtrajectories[trajectory_timestamps[i]]);
                            if (sub_error > error_boundary)
                            {
                                // Calculate 3D point on environment surface that device camera was facing
                                Vector3 endpoint = trajectory_mid_positions[i] + (trajectory_mid_rotations[i] * Vector3.forward * trajectory_mid_distances[i]);
                                if (errorEndpoints.Count != 0)
                                {
                                    foreach (Vector3 point in errorEndpoints)
                                    {
                                        errorEndpointDistances.Add(Vector3.Distance(endpoint, point));
                                    }
                                }
                                // instantiate if only endpoint, or not within 0.5m of another error endpoint (value can be adjusted to preference)
                                if (errorEndpoints.Count == 0 || (errorEndpoints.Count != 0) && (errorEndpointDistances.Min() > 0.5))
                                {
                                    errorEndpoints.Add(endpoint);
                                    // Get meshes and materials for this sub-trajectory (and frustums) 
                                    MeshRenderer cylinder1_mesh = cylinders[i * 2].GetComponent<MeshRenderer>();
                                    MeshRenderer joint1_mesh = joints[i * 2].GetComponent<MeshRenderer>();
                                    MeshRenderer cylinder2_mesh = cylinders[(i * 2) + 1].GetComponent<MeshRenderer>();
                                    MeshRenderer joint2_mesh = joints[(i * 2) + 1].GetComponent<MeshRenderer>();
                                    cylinder1_mesh.material = error_material;
                                    joint1_mesh.material = error_material;
                                    cylinder2_mesh.material = error_material;
                                    joint2_mesh.material = error_material;
                                    if (frustum_timestamps.Contains(trajectory_timestamps[i]))
                                    {
                                        int frustum_index = frustum_timestamps.IndexOf(trajectory_timestamps[i]);
                                        MeshRenderer frustum_mesh = frustums[frustum_index].GetComponent<MeshRenderer>();
                                        frustum_mesh.material = error_material;
                                    }

                                    // Additional code for rendering exclamation points or warning signs (optional)
                                    if (error_boundary == 0.1f)
                                    {
                                        //Exclamation points
                                        //------------------
                                        //GameObject errorAreaHigh = Instantiate(errorAreaHighPrefab, endpoint, Quaternion.identity);
                                        //spawnedErrorAreasHigh.Add(errorAreaHigh);
                                        //Warning signs
                                        //------------------
                                        //GameObject errorPatchHigh = Instantiate(errorPatchHighPrefab, endpoint, trajectory_mid_orientations[i]);
                                        //spawnedErrorPatchesHigh.Add(errorPatchHigh);
                                    }
                                    else
                                    {
                                        //Exclamation points
                                        //------------------
                                        //GameObject errorAreaMedium = Instantiate(errorAreaMediumPrefab, endpoint, Quaternion.identity);
                                        //spawnedErrorAreasMedium.Add(errorAreaMedium);
                                        //Warning signs
                                        //------------------
                                        //GameObject errorPatchMedium = Instantiate(errorPatchMediumPrefab, endpoint, trajectory_mid_orientations[i]);
                                        //spawnedErrorPatchesMedium.Add(errorPatchMedium);
                                    }

                                    // Increment challenging regions count and render indicator
                                    challenging_region_count += 1;
                                    trajectory_error_rendered[i] = 1;
                                }
                                errorEndpointDistances.Clear();
                            }
                        }
                    }
                }
            }

            // Update UI with number of challenging environment regions identified 
            for (var i = 0; i < trajectory_timestamps.Count; i++)
            {
                if (subtrajectories.ContainsKey(trajectory_timestamps[i]))
                {
                    float sub_error = (float)Convert.ToDouble(subtrajectories[trajectory_timestamps[i]]);
                    subtrajectoryData += trajectory_timestamps[i].ToString() + "," + trajectory_mid_acc_x[i].ToString() + "," + trajectory_mid_acc_y[i].ToString() + "," + trajectory_mid_acc_z[i].ToString() + "," + trajectory_mid_gyro_x[i].ToString() + "," + trajectory_mid_gyro_y[i].ToString() + "," + trajectory_mid_gyro_z[i].ToString() + "," + trajectory_mid_distances[i].ToString() + "," + sub_error.ToString() + "," + trajectory_error_rendered[i].ToString() + "\r\n";
                }
            }
            trajectoryRegions.text = "Challenging regions to capture: " + challenging_region_count.ToString();
            status.text = "Results loaded";

            if (challenging_region_count == max_challenging_regions)
            {
                captureButton.interactable = true;
                AudioSource.PlayClipAtPoint(audioResults, _camera.transform.position);
            }

            // Save sub-trajectory data to local storage
            string filepath_st = Application.persistentDataPath + "/" + timestamp + "/subtrajectories.txt";
            using (StreamWriter sw = new StreamWriter(filepath_st))
            {
                sw.WriteLine(subtrajectoryData);
            }
            for (var i = 0; i < trajectory_mid_rgbs.Count; i++)
            {
                var bytes = trajectory_mid_rgbs[i].EncodeToPNG();
                string filepath_rgb = Application.persistentDataPath + "/" + timestamp + "/" + trajectory_timestamps[i].ToString() + ".png";
                File.WriteAllBytes(filepath_rgb, bytes);
            }

            // Enable analysis mode in which user captures images of challenging regions
            analysis_started = true;
            if (challenging_region_count > 0)
            {
                captureButton.interactable = true;
            }
        }

        //--------------------------Additional methods for user interaction---------------------------
        //--------------------------------------------------------------------------------------------

        // Save current camera image when user presses 'Capture' button
        public void HandleCaptureClick()
        {
            capture_count += 1;
            captureButton.interactable = false;
            AudioSource.PlayClipAtPoint(audioCapture, _camera.transform.position);
            Texture2D _rgbTexture = new Texture2D(image_width, image_height, TextureFormat.RGBA32, false);
            Graphics.CopyTexture(m_CameraTexture, _rgbTexture);
            var bytes = _rgbTexture.EncodeToPNG();
            string filepath_rgb = Application.persistentDataPath + "/" + timestamp + "/capture_" + capture_count.ToString() + ".png";
            File.WriteAllBytes(filepath_rgb, bytes);
            trajectoryRegions.text = "Challenging regions to capture: " + (challenging_region_count - capture_count).ToString();
            if (capture_count == challenging_region_count)
            {
                captureButton.interactable = false;
                trajectoryRegions.text = "Complete!";
                StartCoroutine(playComplete());
            }
            else
            {
                captureButton.interactable = true;
            }
        }

        // Save audio notification when all challenging regions are captured
        IEnumerator playComplete()
        {
            yield return new WaitForSeconds(1);
            AudioSource.PlayClipAtPoint(audioComplete, _camera.transform.position);
        }

        Texture2D m_CameraTexture;
    }
}
