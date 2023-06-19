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
3) Situated visualization of the trajectory on the user AR device before pose error estimates are added.
4) Our uncertainty-based pose error estimation running on the server ('Uncertainty-based error estimation').
5) Situated visualization of the trajectory on the user AR device once pose error estimates are added, with high pose error associated with the blank wall highlighted using our 'trajectory + exclamation points' visualization.   

![GIF of SiTAR in action](https://github.com/SiTARSys/SiTAR/blob/main/SiTAR.gif?raw=true)

# Implementation Code



# Instructions

Prerequisites: 2 or more Android devices running ARCore v1.3 or above; server with Python 3.8 or above, .

Tested with Google Pixel 7 and Google Pixel 7 Pro devices running ARCore v1.31; Apple Macbook Pro as edge server (Python 3.8). 

# Datasets

Samples of our 'Hall' and 'LivingRoom' VI-SLAM datasets that we created to evaluate our uncertainty-based pose error estimation method can be downloaded here: https://drive.google.com/drive/folders/1VwAgcCly0RDUmyME4MHDrcBfkXRbitpC?usp=sharing (full datasets will be released upon paper publication).
