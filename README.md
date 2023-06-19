# SiTAR (Situated Trajectory Analysis for AR): Resources
This repository contains resources and research artifacts for the paper "_SiTAR: Situated Trajectory Analysis for In-the-Wild Pose Error Estimation_", including the code required to implement SiTAR, and samples of the new open-source VI-SLAM datasets we created to evaluate our pose error estimation method.

# SiTAR Overview
Our SiTAR system provides situated visualizations of device pose error estimates, on real AR devices (implemented here for ARCore). Our current code facilitates three types of pose error visualizations, illustrated in the image below -- 1) trajectory-only (left), 2) trajectory + exclamation points (middle), 3) trajectory + warning signs (right):

![SiTAR teaser image](https://github.com/SiTARSys/SiTAR/blob/main/SiTARTeaser.png?raw=true)

Below is a video of our SiTAR system in action 

![GIF of SiTAR in action](https://github.com/SiTARSys/SiTAR/blob/main/SiTAR.gif?raw=true)

# Implementation Code

The system architecture for SiTAR is shown below. Our code

# Instructions

Prerequisites: 2 or more Android devices running ARCore v1.3 or above; server with Python 3.8 or above, .

Tested with Google Pixel 7 and Google Pixel 7 Pro devices running ARCore v1.31; Apple Macbook Pro as edge server (Python 3.8). 

# Datasets

Samples of our 'Hall' and 'LivingRoom' VI-SLAM datasets that we created to evaluate our uncertainty-based pose error estimation method can be downloaded here: https://drive.google.com/drive/folders/1VwAgcCly0RDUmyME4MHDrcBfkXRbitpC?usp=sharing (full datasets will be released upon paper publication).
