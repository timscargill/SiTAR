# SiTAR (Situated Trajectory Analysis for AR): Resources
This repository contains resources and research artifacts for the paper "_SiTAR: Situated Trajectory Analysis for In-the-Wild Pose Error Estimation_", including the [code required to implement SiTAR](#implementation-code), as well as samples of the new [open-source VI-SLAM datasets](#datasets) we created to evaluate our pose error estimation method.

# SiTAR Overview
Our SiTAR system provides situated visualizations of device pose error estimates, on real AR devices (implemented here for ARCore). Our code facilitates three types of pose error visualizations, illustrated in the image below -- 1) _trajectory-only_ (left), 2) _trajectory + exclamation points_ (middle), 3) _trajectory + warning signs_ (right):

![SiTAR teaser image](https://github.com/SiTARSys/SiTAR/blob/main/SiTARTeaser.png?raw=true)

The system architecture for SiTAR is shown below. The system frontend which generates situated trajectory visualizations is implemented on the **user AR device**, and the system backend which generates pose error estimates is implemented on a **server** and **playback AR device(s)**. The system backend can be implemented using an edge or cloud server.

![SiTAR system architecture](https://github.com/SiTARSys/SiTAR/blob/main/SystemArchitecture.png?raw=true)

Below is a short demo video of our SiTAR system in action using an edge-based architecture. A Google Pixel 7 Pro is used as the User AR device, an Apple Macbook Pro as the server, and a Google Pixel 7 as the playback AR device. The video shows the following steps: 

1) Creation of a trajectory on the user AR device ('Trajectory creation').
2) Replaying of the visual and inertial input data for that trajectory on the playback AR device to obtain multiple trajectory estimates ('Sequence playback').
3) Situated visualization of the trajectory on the user AR device before pose error estimates are added ('Trajectory visualization without error estimates').
4) Our uncertainty-based pose error estimation running on the server ('Uncertainty-based error estimation').
5) Situated visualization of the trajectory on the user AR device once pose error estimates are added, with high pose error associated with the blank wall highlighted using our 'trajectory + exclamation points' visualization ('Trajectory visualization with error estimates').

![SiTAR demo video](https://github.com/SiTARSys/SiTAR/blob/main/SiTAR.gif?raw=true)

# Implementation Code

Our implementation code for SiTAR is provided in three parts, to be run on the user AR device, the server and the playback AR device respectively. The code for each can be found in the repository folders named 'user-AR-device', 'server', and 'playback-AR-device'. The implementation code for each consists of the following:

**User AR device:** a C# script 'DrawTrajectory.cs', which implements the 'Trajectory creation' and 'Trajectory visualization' modules in SiTAR.

**Server:** a Python script 'trajectory_evaluation.py', which implements the 'Sequence assignment' and 'Uncertainty-based error estimation' modules in SiTAR.

**Playback AR device:** a C# script 'TrajectoryPlayback.cs', which implements the 'Sequence playback' module in SiTAR.


# Instructions

**Prerequisites:** 2 or more Android devices running ARCore v1.3 or above; server with Python 3.8 or above, and the evo (https://github.com/MichaelGrupp/evo) and FastAPI (https://fastapi.tiangolo.com/lo/) packages installed. For building the necessary apps to AR devices, Unity 2021.3 or later is required, with the AR Foundation framework v4.2 or later and the ARCore Extensions v1.36 or later packages installed.

Tested with Google Pixel 7 and Google Pixel 7 Pro devices running ARCore v1.31; Apple Macbook Pro as edge server (Python 3.8).

**User AR device:** 
1) Create a Unity project with the AR Foundation template. Make sure the 'ARCore Extensions' is set up (https://developers.google.com/ar/develop/unity-arf/getting-started-extensions)
2) Add the 'DrawTrajectory.cs' script to the 'AR Session Origin' GameObject.
3) Add the 'Cylinder.prefab', 'Joint.prefab' and 'Frustum.prefab' files (in the 'user-AR-device folder) to your Assets folder, and drag them to their slots in the 'Draw Trajectory' inspector panel.
4) Add the 'ErrorHigh.mat' and 'ErrorMedium.mat' files (in the 'user-AR-device folder) to your Assets folder, and drag them to their slots in the 'Draw Trajectory' inspector panel.
5) Add 'Start' and 'Stop' UI buttons, drag them to their slots in the 'Draw Trajectory' inspector panel, and set their OnClick actions to 'DrawTrajectory.HandleStartClick' and 'DrawTrajectory.HandleStopClick' respectively.
6) Either hardcode your server IP address into line 481 of DrawTrajectory.cs, or add a UI panel with a text field to capture this data from the user.
7) Set the Build platform to 'Android', select your device under 'Run device', and click 'Build and Run'.

**Server:**

**Playback AR device:**

# Datasets

Samples of our 'Hall' and 'LivingRoom' VI-SLAM datasets that we created to evaluate our uncertainty-based pose error estimation method can be downloaded here: https://drive.google.com/drive/folders/1VwAgcCly0RDUmyME4MHDrcBfkXRbitpC?usp=sharing (full datasets will be released upon paper publication).
