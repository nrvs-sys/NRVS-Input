using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NRVS.Settings;

namespace Input
{
    public class ThumbstickRotationHandler
    {
        const float baseTurnSpeed = 180f; // degrees per second

        public enum TurnStyle { Snap, Smooth, Continuous }

        TurnStyle turnStyle = TurnStyle.Snap;

        float deadzone = 0.3f;
        float turnDegrees = 45f;
        float turnSpeed = baseTurnSpeed;

        float releaseThreshold => deadzone * 0.9f;

        // for Snap/Smooth edge-detect
        bool inputLatched;
        // Smooth queue (signed degrees)
        float pendingAngle;
        // for Continuous gating
        bool continuousActive;

        public bool isSmoothTurning => turnStyle == TurnStyle.Smooth && !Mathf.Approximately(pendingAngle, 0f);

        public ThumbstickRotationHandler(SettingsBehavior turnStyleSettingsBehavior, SettingsBehavior turnDegreesSettingsBehavior, SettingsBehavior turnSpeedSettingsBehavior)
        {
            turnStyleSettingsBehavior.onIntChanged.AddListener(OnTurnStyleChanged);
            turnDegreesSettingsBehavior.onIntChanged.AddListener(OnTurnDegreesChanged);
            turnSpeedSettingsBehavior.onFloatChanged.AddListener(OnTurnSpeedChanged);

            OnTurnStyleChanged(turnStyleSettingsBehavior.GetInt());
            OnTurnDegreesChanged(turnDegreesSettingsBehavior.GetInt());
            OnTurnSpeedChanged(turnSpeedSettingsBehavior.GetFloat());
        }

        void OnTurnStyleChanged(int value)
        {
            var mode = (TurnStyle)value;

            if (turnStyle == mode) return;

            turnStyle = mode;

            // reset transitional state
            inputLatched = false;
            pendingAngle = 0f;
            continuousActive = false;
        }

        void OnTurnDegreesChanged(int value)
        {
            switch (value)
            {
                case 0:
                    turnDegrees = 30;
                    break;
                default:
                case 1:
                    turnDegrees = 45;
                    break;
                case 2:
                    turnDegrees = 60;
                    break;
                case 3:
                    turnDegrees = 90;
                    break;
            }
        }

        void OnTurnSpeedChanged(float value)
        {
            turnSpeed = baseTurnSpeed * value;
        }

        public void ProcessInput(Vector2 input, float deltaTime, out float rotationDelta, out int rotationDirection)
        {
            rotationDelta = 0f;
            rotationDirection = 0;

            float x = input.x;
            float ax = Mathf.Abs(x);

            switch (turnStyle)
            {
                case TurnStyle.Snap:
                    {
                        if (!inputLatched && ax >= deadzone)
                        {
                            inputLatched = true;
                            int dir = x > 0f ? 1 : -1;
                            rotationDelta = dir * turnDegrees;
                            rotationDirection = dir;
                        }
                        else if (inputLatched && ax <= releaseThreshold)
                        {
                            inputLatched = false;
                        }
                        break;
                    }

                case TurnStyle.Smooth:
                    {
                        if (!inputLatched && ax >= deadzone)
                        {
                            inputLatched = true;
                            pendingAngle += (x > 0f ? 1f : -1f) * turnDegrees;
                        }
                        else if (inputLatched && ax <= releaseThreshold)
                        {
                            inputLatched = false;
                        }

                        // Consume queue at turnSpeed
                        if (!Mathf.Approximately(pendingAngle, 0f))
                        {
                            int dir = pendingAngle > 0f ? 1 : -1;
                            float step = turnSpeed * deltaTime;
                            float apply = Mathf.Min(Mathf.Abs(pendingAngle), step);

                            rotationDelta = dir * apply;
                            rotationDirection = dir;

                            // signed subtraction
                            pendingAngle -= rotationDelta; 

                            if (Mathf.Abs(pendingAngle) < 0.001f)
                                pendingAngle = 0f;
                        }
                        break;
                    }

                case TurnStyle.Continuous:
                    {
                        if (!continuousActive && ax >= deadzone)
                            continuousActive = true;
                        else if (continuousActive && ax <= releaseThreshold)
                            continuousActive = false;

                        if (continuousActive)
                        {
                            rotationDelta = x * turnSpeed * deltaTime;
                            rotationDirection = (x > 0f) ? 1 : (x < 0f ? -1 : rotationDirection);
                        }

                        // no queue in continuous
                        pendingAngle = 0f;
                        break;
                    }
            }
        }
    }
}
