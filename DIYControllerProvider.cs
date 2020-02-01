//Copyright 2020 Mikhail Shubov

//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at

//    http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using Gvr;
using UnityEngine;
using Vuforia;

namespace Gvr.Internal
{
    struct DIYController
    {
        //Name of MultiTarget in the scene
        public const string MULTI_TARGET_NAME = "MultiTarget";

        //Thumbstick X axis name
        public const string AXIS_X = "Horizontal";

        //Thumbstick Y axis name
        public const string AXIS_Y = "Vertical";

#if UNITY_EDITOR
        //Name of Joystick
        public const string JOYSTICK_NAME = "Keyboard";

        /// <summary>Touchpad button</summary>
        /// <remarks>The Button under the touch pad (formerly known as Click).</remarks>
        public const string TouchPadButton = "Jump";

        /// <summary>App button</summary>
        /// <remarks>General application button.</remarks>
        public const string App = "Submit";

        /// <summary>System button.</summary>
        /// <remarks>Formerly known as Home.</remarks>
        public const string System = "Cancel";

        /// <summary>Trigger button</summary>
        /// <remarks>Primary button on the underside of the controller.</remarks>
        public const string Trigger = "";

        /// <summary>Grip button.</summary>
        /// <remarks>Secondary button on the underside of the controller.</remarks>
        public const string Grip = "";

#elif UNITY_ANDROID

        public const string JOYSTICK_NAME = "VR BOX";

        public const string TouchPadButton = "Fire3";

        public const string App = "Jump";
        
        public const string System = "Fire1";
        
        public const string Trigger = "Fire2";
        
        public const string Grip = "";

#endif // UNITY_EDITOR || UNITY_ANDROID

    }

    class DIYControllerProvider : IControllerProvider
    {
        //joystick axis values: from 0 to 1
        //                  scroll speed per second
        private const float SCROLL_SPEED = 1.0f;
        private const float HOLD_ZONE = 0.05f;
        private const float DEAD_ZONE = 0.05f;
        private const float NEUTRAL_POSITION = 0.5f;
        //                  allowed approach to the neutral position per frame
        private const float RETURN_PARAM = 0.01f;

        //                              seconds
        private const float HOLD_TIME = 0; 


        private static Vector2 neutralTouch = new Vector2(NEUTRAL_POSITION, NEUTRAL_POSITION);
        private Vector2 scaledXY = neutralTouch;

        private Vector2 holdTouchPos;
        private Vector2 scrollPos;
        private float holdTime;
        private bool isLastFrameTouchHeld;
        private bool scrollPause;

        //previous state variables
        private Vector2 lastTouchPos;
        private GvrControllerButton lastButtonsState;
        private Vector3 lastPosition;
        private Quaternion lastOrientation;

        private ControllerState state = new ControllerState();
        private static readonly ControllerState dummyState = new ControllerState();

        private bool IsJoystickConnected
        {
            get
            {
                foreach(string name in Input.GetJoystickNames())
                {
                    if (name == DIYController.JOYSTICK_NAME)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private GameObject GetControllerTarget
        {
            get
            {
                return GameObject.Find(DIYController.MULTI_TARGET_NAME);
            }
        }

        private bool IsControllerTracked
        {
            get
            {
                var trackable = GetControllerTarget.GetComponent<TrackableBehaviour>();
                var status = trackable.CurrentStatus;
                return status == TrackableBehaviour.Status.TRACKED;
            }
        }

        private static bool IsTouchPadButtonPressed
        {
            get
            {
                if (DIYController.TouchPadButton != "")
                {
                    return Input.GetAxis(DIYController.TouchPadButton) == 1;
                }
                return false;
            }
        }

        private static bool IsGripButtonPressed
        {
            get
            {
                if (DIYController.Grip != "")
                {
                    return Input.GetAxis(DIYController.Grip) == 1;
                }
                return false;
            }
        }

        private static bool IsTriggerButtonPressed
        {
            get
            {
                if (DIYController.Trigger != "")
                {
                    return Input.GetAxis(DIYController.Trigger) == 1;
                }
                return false;
            }
        }

        private static bool IsAppButtonPressed
        {
            get
            {
                if (DIYController.App != "")
                {
                    return Input.GetAxis(DIYController.App) == 1;
                }
                return false;
            }
        }

        private static bool IsSystemButtonPressed
        {
            get
            {
                if (DIYController.System != "")
                {
                    return Input.GetAxis(DIYController.System) == 1;
                }
                return false;
            }
        }

        private bool IsTouching
        {
            get
            {
                Vector2 XY = new Vector2(Input.GetAxis(DIYController.AXIS_X),
                    Input.GetAxis(DIYController.AXIS_Y));

                if (XY != Vector2.zero)
                {
#if UNITY_EDITOR
                    scaledXY.Set(Mathf.Clamp01(XY.y * 100 - 99),
                        Mathf.Clamp01(XY.x * 100 + 100));
                    return !IsTouchMovingToNeutralPos && !IsTouchInDeadZone;
#elif UNITY_ANDROID
                    scaledXY.Set(Mathf.Clamp01(XY.y / 2.0f + 0.5f),
                         Mathf.Clamp01(XY.x / 2.0f + 0.5f));
                    return !IsTouchMovingToNeutralPos;
#endif
                }
                else
                {
                    return IsTouchPadButtonPressed;
                }
            }
        }

        private bool IsTouchInDeadZone
        {
            get
            {
                return (scaledXY - neutralTouch).magnitude < DEAD_ZONE;
            }
            
        }

        private bool IsTouchMovingToNeutralPos
        {
            get
            {
                bool res = (scaledXY - neutralTouch).magnitude < (lastTouchPos - neutralTouch).magnitude - RETURN_PARAM;
                lastTouchPos = scaledXY;
                return res;
            }
        }

        private bool IsTouchHeld
        {
            get
            {
                Vector2 delta = scaledXY - holdTouchPos;
                
                if (delta.magnitude < HOLD_ZONE)
                {
                    holdTime += Time.unscaledDeltaTime;
                    return holdTime >= HOLD_TIME ? true : false;
                }
                
                holdTime = 0;
                holdTouchPos = scaledXY;
                return false;
            }
        }

        public bool SupportsBatteryStatus
        {
            get { return false; }
        }

        public int MaxControllerCount
        {
            get { return 1; }
        }

        internal DIYControllerProvider()
        {
        }

        public void Dispose()
        {
        }

        public void ReadState(ControllerState outState, int controller_id)
        {
            if (controller_id != 0)
            {
                outState.CopyFrom(dummyState);
                return;
            }

            lock (state)
            {
                UpdateState();

                outState.CopyFrom(state);
            }

            state.ClearTransientState();
        }

        public void OnPause()
        {
        }

        public void OnResume()
        {
        }

        private void UpdateState()
        {
            if (!IsControllerTracked || !IsJoystickConnected)
            {
                string err = !IsJoystickConnected ? "Cannot find Joystick '" + DIYController.JOYSTICK_NAME + "' " : "";
                err += !IsControllerTracked ? "Cannot recognise MultiTarget '" + DIYController.MULTI_TARGET_NAME + "'" : "";
                Debug.Log(err);

                ClearState();
                return;
            }

            state.is6DoF = true;
            state.connectionState = GvrConnectionState.Connected;
            state.apiStatus = GvrControllerApiStatus.Ok;
            state.isCharging = false;
            state.batteryLevel = GvrControllerBatteryLevel.Full;
            
            UpdateButtonStates();

            if (IsTouchHeld && scrollPause)
            {
                ClearTouchPos();
                scrollPause = false;
            }

            if (0 != (state.buttonsState & GvrControllerButton.TouchPadTouch))
            {
                if (IsTouchHeld)
                {
                    if (isLastFrameTouchHeld)
                    {
                        Vector2 direction = holdTouchPos - neutralTouch;
                        direction.Normalize();

                        scrollPos += SCROLL_SPEED * direction * Time.unscaledDeltaTime;

                        bool isHoldPointReached = (holdTouchPos - neutralTouch).magnitude
                            <= (scrollPos - neutralTouch).magnitude;

                        if (isHoldPointReached)
                        {
                            scrollPos = neutralTouch;
                            scrollPause = true;
                        }
                        else
                        {
                            UpdateTouchPos(scrollPos);
                        }
                    }
                    else
                    {
                        scrollPos = neutralTouch;
                        scrollPause = true;
                        isLastFrameTouchHeld = true;
                    }
                }   
                else
                {
                    isLastFrameTouchHeld = false;
                    scrollPos = neutralTouch;
                    UpdateTouchPos(scaledXY);
                }    
            }

            UpdatePosition();
            UpdateAccel();

            UpdateOrientation();
            UpdateGyro(); 
        }

        private void UpdateTouchPos(Vector2 vec)
        {
            state.touchPos = vec;
        }

        private void UpdatePosition()
        {
            var joystickPosition = new Vector3(0.05196152422f, -0.0378193887f, 0.0661036599f);
            state.position = GetControllerTarget.transform.TransformPoint(joystickPosition);
        }

        private void UpdateAccel()
        {
            state.accel = new Vector3(0, -9.8f, 0);
            state.accel += (state.position - lastPosition) 
                / Mathf.Pow(Time.unscaledDeltaTime, 2);
            lastPosition = state.position;
        }

        private void UpdateOrientation()
        {
            Quaternion rotation = Quaternion.Euler(-35.26439f, -135.0f, 0);
            state.orientation = GetControllerTarget.transform.rotation * rotation;
        }

        private void UpdateGyro()
        {
            Vector3 deltaDegrees = state.orientation.eulerAngles - lastOrientation.eulerAngles;

            state.gyro = deltaDegrees * (Mathf.Deg2Rad / Time.unscaledDeltaTime);

            lastOrientation = state.orientation;
        }

        private void UpdateButtonStates()
        {
            state.buttonsState = 0;
            if (IsTouchPadButtonPressed)
            {
                state.buttonsState |= GvrControllerButton.TouchPadButton;
            }

            if (IsGripButtonPressed)
            {
                state.buttonsState |= GvrControllerButton.Grip;
            }

            if (IsTriggerButtonPressed)
            {
                state.buttonsState |= GvrControllerButton.Trigger;
            }

            if (IsAppButtonPressed)
            {
                state.buttonsState |= GvrControllerButton.App;
            }

            if (IsSystemButtonPressed)
            {
                state.buttonsState |= GvrControllerButton.System;
            }

            if (IsTouching & !scrollPause)
            {
                state.buttonsState |= GvrControllerButton.TouchPadTouch;
            }

            state.SetButtonsUpDownFromPrevious(lastButtonsState);
            lastButtonsState = state.buttonsState;

            if (0 != (state.buttonsUp & GvrControllerButton.TouchPadTouch))
            {
                ClearTouchPos();
            }

            if (0 != (state.buttonsUp & GvrControllerButton.System))
            {
                Recenter();
            }
        }

        private void Recenter()
        {
            state.recentered = true;
        }

        private void ClearTouchPos()
        {
            state.touchPos = neutralTouch;
        }

        private void ClearState()
        {
            state.connectionState = GvrConnectionState.Disconnected;
            state.buttonsState = 0;
            state.buttonsDown = 0;
            state.buttonsUp = 0;
            state.orientation = Quaternion.identity;
            state.position = Vector3.zero;
            state.gyro = Vector3.zero;
            state.accel = Vector3.zero;

            ClearTouchPos();
        }
    }
}
