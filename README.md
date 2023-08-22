# SiTAR (Situated Trajectory Analysis for AR)
This repository contains resources and research artifacts for the paper "_SiTAR: Situated Trajectory Analysis for In-the-Wild Pose Error Estimation_", to appear in Proceedings of IEEE ISMAR '23. It includes the [code required to implement SiTAR](#implementation-resources), as well as samples of the new [open-source VI-SLAM datasets](#datasets) we created to evaluate our pose error estimation method.

To create our new VI-SLAM datasets we used our previously published game engine-based emulator, **Virtual-Inertial SLAM**. For more information on this tool, implementation code and instructions, and examples of the types of projects it can support, please visit the [Virtual-Inertial SLAM GitHub repository](https://github.com/timscargill/Virtual-Inertial-SLAM/).

# SiTAR Overview
Our SiTAR system provides situated visualizations of device pose error estimates, on real AR devices (implemented here for ARCore). Our code facilitates three types of pose error visualizations, illustrated in the image below -- 1) _trajectory-only_ (left), 2) _trajectory + exclamation points_ (middle), 3) _trajectory + warning signs_ (right):

![SiTAR teaser image](https://github.com/SiTARSys/SiTAR/blob/main/SiTARTeaser.png?raw=true)

The system architecture for SiTAR is shown below. The system frontend which generates situated trajectory visualizations is implemented on the **user AR device**, and the system backend which generates pose error estimates is implemented on a **server** and **playback AR device(s)**. The system backend can be implemented using an edge or cloud server.

![SiTAR system architecture](https://github.com/SiTARSys/SiTAR/blob/main/SystemArchitecture.png?raw=true)

Below is a short demo video of our SiTAR system in action, using an edge-based architecture. A Google Pixel 7 Pro is used as the User AR device, an Apple Macbook Pro as the server, and a Google Pixel 7 as the playback AR device. The video shows the following steps: 

1) Creation of a trajectory on the user AR device ('Trajectory creation').
2) Replaying of the visual and inertial input data for that trajectory on the playback AR device to obtain multiple trajectory estimates ('Sequence playback').
3) Situated visualization of the trajectory on the user AR device before pose error estimates are added ('Trajectory visualization without error estimates').
4) Our uncertainty-based pose error estimation running on the server ('Uncertainty-based error estimation').
5) Situated visualization of the trajectory on the user AR device once pose error estimates are added, with high pose error associated with the blank wall highlighted using our 'trajectory + exclamation points' visualization ('Trajectory visualization with error estimates').

![SiTAR demo video](https://github.com/SiTARSys/SiTAR/blob/main/SiTAR.gif?raw=true)

# Implementation Resources

Our implementation code and associated resources for SiTAR are provided in three parts, for the **user AR device**, the **server** and the **playback AR device** respectively. The code for each can be found in the repository folders named '_user-AR-device_', '_server_', and '_playback-AR-device_'. The implementation resources consist of the following:

**User AR device:** A C# script _DrawTrajectory.cs_, which implements the 'Trajectory creation' and 'Trajectory visualization' modules in SiTAR. Unity prefabs for base trajectory visualization, _Start.prefab_, _Stop.prefab_, _Cylinder.prefab_, _Joint.prefab_ and _Frustum.prefab_. Unity prefabs and materials for pose error visualizations, _ErrorAreaHigh.prefab_, _ErrorAreaMedium.prefab_, _ErrorPatchHigh.prefab_, _ErrorPatchMedium.prefab_, _ErrorHigh.mat_ and _ErrorMedium.mat_.   

**Server:** a Python script _SiTAR-Server.py_, which implements the 'Sequence assignment' and 'Uncertainty-based error estimation' modules in SiTAR.

**Playback AR device:** a C# script _TrajectoryPlayback.cs_, which implements the 'Sequence playback' module in SiTAR.


# Implementation Instructions

**Prerequisites:** 2 or more Android devices running ARCore v1.3 or above; server with Python 3.8 or above and the evo (https://github.com/MichaelGrupp/evo) and FastAPI (https://fastapi.tiangolo.com/lo/) Python packages installed, and Android SDK Platform Tools installed (https://developer.android.com/tools/releases/platform-tools). For building the necessary apps to AR devices, Unity 2021.3 or later is required, with the AR Foundation framework v4.2 or later and the ARCore Extensions v1.36 or later packages installed.

Tested with Google Pixel 7 and Google Pixel 7 Pro devices running ARCore v1.31, and Apple Macbook Pro as edge server (Python 3.8).

**User AR device:** 
1) Create a Unity project with the AR Foundation template. Make sure the ARCore Extensions is fully set up by following the instructions here: https://developers.google.com/ar/develop/unity-arf/getting-started-extensions.
2) Add the _DrawTrajectory.cs_ script (in the _user-AR-device_ folder) to the AR Session Origin GameObject.
3) Drag the AR Camera GameObject to the 'Camera Manager' and 'Camera' slots in the Draw Trajectory inspector panel.
4) Add the _Start.prefab_, _Stop.prefab_, _Cylinder.prefab_, _Joint.prefab_ and _Frustum.prefab_ files (in the _user-AR-device_ folder) to your Assets folder, and drag them to the 'Start Prefab', 'Stop Prefab', 'Cylinder Prefab', 'Joint Prefab' and 'Frustum Prefab' slots in the Draw Trajectory inspector panel.
5) (Optional) If using the exclamation points or warning signs visualizations, add the _ErrorAreaHigh.prefab_, _ErrorAreaMedium.prefab_, _ErrorPatchHigh.prefab_, and _ErrorPatchMedium.prefab_ files (in the _user-AR-device_ folder) to your Assets folder, and drag them to the 'Error Area High Prefab', 'Error Area Medium Prefab', 'Error Patch High Prefab', and 'Error Patch Medium Prefab' slots in the Draw Trajectory inspector panel.
7) Add the _ErrorHigh.mat_ and _ErrorMedium.mat_ files (in the _user-AR-device_ folder) to your Assets folder, and drag them to the 'Error High' and 'Error Medium' slots in the Draw Trajectory inspector panel.
8) Add Start and Stop UI buttons, drag them to the 'Start Button' and 'Stop Button' slots in the Draw Trajectory inspector panel, and set their OnClick actions to 'DrawTrajectory.HandleStartClick' and 'DrawTrajectory.HandleStopClick' respectively.
9) Either hardcode your server IP address into line 481 of _DrawTrajectory.cs_, or add a UI panel with a text field to capture this data from the user.
10) (Optional) Add UI text objects to display SiTAR status, trajectory duration, length, average environment depth, and drag them to the 'Status', 'Trajectory Duration', 'Trajectory Length' and 'Trajectory Depth' slots in the Draw Trajectory inspector panel.
11) (Optional) Add audio clips for notifying when error estimates are ready, user captures image, and user has captured all regions, and drag them to the 'Audio Results', 'Audio Capture' and 'Audio Complete' slots in the Draw Trajectory inspector panel.
12) Set the Build platform to Android, select your device under Run device, and click Build and Run.

**Server:**
1) Create a folder on the server where SiTAR files will be located. Add an additional sub-folder named 'trajectories'.
2) Download the _server_ folder in the repository to your SiTAR folder.
3) Open the _SiTAR-Server.py_ file in the _server_ folder, complete the required configuration parameters on lines 20-29, and save.
4) In Terminal or Command Prompt, navigate to your SiTAR folder.
5) Start the server using the following command: ```uvicorn server.SiTAR-Server:app --host 0.0.0.0```

**Playback AR device:**
1) Create a Unity project with the AR Foundation template. Make sure the ARCore Extensions is fully set up by following the instructions here: https://developers.google.com/ar/develop/unity-arf/getting-started-extensions.
2) (Optional) Add the AR Plane Manager and AR Point Cloud Manager scripts (included in AR Foundation) to the AR Session Origin GameObject if you wish to visualize planes and feature points during playback.
3) Add the _TrajectoryPlayback.cs_ script (in the _playback-AR-device_ folder) to the AR Session GameObject.
4) Create a UI text object to display log messages, and drag it to the 'Log' slot in the Trajectory Playback inspector panel.
5) Drag the AR Camera GameObject to the 'Camera Manager' and 'Camera' slots in the Trajectory Playback inspector panel.
6) Set the Build platform to Android, select your device under Run device, and click Build and Run.

# Datasets

Samples of our **Hall** and **LivingRoom** VI-SLAM datasets that we created to evaluate our uncertainty-based pose error estimation method can be downloaded here: https://drive.google.com/drive/folders/1VwAgcCly0RDUmyME4MHDrcBfkXRbitpC?usp=sharing (full datasets will be released upon paper publication).

Each dataset is contained in a separate folder (e.g., _Hall.zip_), which contains sub-folders for each sequence (sequence A1 provided as sample). Each sequence folder contains the following (formatted to streamline execution in ORB-SLAM3):

1) _groundtruth_ folder, containing formatted ground truth pose for sequence (_data.csv_) plus sensor characteristics from original SenseTime dataset (_sensor.yaml_).
2) mav0 folder, containing _cam0/data_ folders with camera images, and _imu0_ folder with formatted IMU data (_data.csv_) plus sensor characteristics from original SenseTime dataset (_sensor.yaml_).
3) _sequence_name.txt_ file (e.g., _A1.txt_), containing list of camera image timestamps (format required by ORB-SLAM3).
