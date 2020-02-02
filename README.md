# DIY-Controller
DIY Controller is made of bluetooth controller (Perfeo VR BOX 2.0 kit) and attached  to it multi target tracked by Vuforia Engine. DIYControllerProvider class is a realisation of IControllerProvider interface from GoogleVR library created to allow users to interact with XR objects and to use GoogleVR library without having a real Daydream Controller.

# Installation
1) install Vuforia Engine 8.3.x using Unity Package Manager
2) install GoogleVR
3) add DIYController.package to your project (this package contains Vuforia Multi Target database) and create a Multi Target in your scene 
4) print cube pattern. Attach the cube to your Perfeo VR BOX 2.0 controller
5) add DIYControllerProvider.cs file to your Unity project
6) replace ControllerProviderFactory.cs file in Google VR library with the one from this repo
7) Done! GoogleVR thinks you have a Daydream controller
