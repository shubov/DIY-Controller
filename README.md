# DIY-Controller
DIY Controller is made of bluetooth controller (Perfeo VR BOX 2.0 kit) and attached  to it multi target tracked by Vuforia Engine. DIYControllerProvider class is a realisation of IControllerProvider interface from GoogleVR library created to allow users to interact with XR objects and to use GoogleVR library without having a real Daydream Controller.

# Installation
1) install Vuforia Engine 8.3.x using Unity Package Manager
2) install GoogleVR
3) print cube pattern. Attach the cube to your Perfeo VR BOX 2.0 controller
4) add DIYControllerProvider.cs file to your Unity project
5) replace ControllerProviderFactory.cs file in Google VR library with the one from this repo
6) Done! GoogleVR thinks you have a Daydream controller
