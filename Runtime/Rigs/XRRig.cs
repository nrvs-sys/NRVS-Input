using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;
using NRVS.Settings;

namespace NRVS.Input.Rigs
{
    /// <summary>
    /// Contains the Client-Side representation (ie local inputs & camera) of the Player's XR Rig.
    /// </summary>
    public class XRRig : InputRig, IRig
    {
        [Header("Dependencies")]
        [SerializeField]
        private InputManager inputManager;

        [SerializeField]
        SettingsBehavior xrTurnStyleSettingsBehavior;
        [SerializeField]
        SettingsBehavior xrTurnDegreesSettingsBehavior;
        [SerializeField]
        SettingsBehavior xrTurnSpeedSettingsBehavior;

        [Header("Components")]
        [SerializeField]
        private Transform headTransform;
        [SerializeField]
        private Transform leftHandTransform;
        [SerializeField]
        private Transform rightHandTransform;

        [Header("Renderers")]
        [SerializeField]
        private List<GameObject> handVisuals;

        [Header("Settings")]

        [SerializeField]
        Vector3 recenterForward = Vector3.forward;

        [Header("Events")]
        public UnityEvent onRecentered;

        [Header("Debug")]
        public GameObject moveDebugObject;


        // Properties

        public Transform origin => xrOrigin.transform;
        public Transform head => headTransform;
        public Transform leftHand => leftHandTransform;
        public Transform rightHand => rightHandTransform;

        public XROrigin xrOrigin { get; private set; }

        XRInputSubsystem xrInput;
        bool subscribedToInput;

        private ThumbstickRotationHandler thumbstickRotationHandler;

        public bool isMoveDebugEnabled { get; set; }


        private void Awake()
        {
            xrOrigin = GetComponentInChildren<XROrigin>();

            // Start TunnelingMobile from black
            //tunnellingMobile.forceVignetteValue = 1f;

#if UNITY_EDITOR
            // If this is a ParrelSync client, turn off visuals so its easier to see through the game's camera
            //if (ParrelSyncManager.type == ParrelSyncManager.ParrelInstanceType.Client)
            //{
            //	foreach (var renderer in GetComponentsInChildren<Renderer>())
            //		renderer.enabled = false;
            //}
#endif
        }

        protected override void Start()
        {
            base.Start();

            thumbstickRotationHandler = new(
                xrTurnStyleSettingsBehavior,
                xrTurnDegreesSettingsBehavior,
                xrTurnSpeedSettingsBehavior
        );

            OnTrackingOriginUpdated(null);
        }

        private void OnEnable()
        {
            var inputs = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(inputs);
            xrInput = inputs.Count > 0 ? inputs[0] : null;

            if (xrInput != null)
            {
                xrInput.trackingOriginUpdated += OnTrackingOriginUpdated;
                subscribedToInput = true;
            }
        }

        protected override void OnDestroy()
        {
            if (subscribedToInput && xrInput != null)
                xrInput.trackingOriginUpdated -= OnTrackingOriginUpdated;
            subscribedToInput = false;

            base.OnDestroy();
        }

        private void Update()
        {
            if (inputManager == null)
                return;

            var rightThumbstick = inputManager.isUIRightActive ? new() : inputManager.actions.RightHand.Thumbstick.ReadValue<Vector2>();

            // Thumbstick rotations
            thumbstickRotationHandler.ProcessInput(rightThumbstick,
                Time.deltaTime,
                out float rotationDegress,
                out int rotationDirection
                );

            if (rotationDegress != 0)
            {
                this.RotateRig(rotationDegress);
            }
        }

        public void SetHandsVisibility(bool visible)
        {
            foreach (var hand in handVisuals)
                hand?.SetActive(visible);
        }

        public void SetMoveDebugVisible(bool visible)
        {
            moveDebugObject?.SetActive(isMoveDebugEnabled && visible);
        }

        // Called by Unity AFTER the runtime (Quest) has moved the origin during a recenter.
        void OnTrackingOriginUpdated(XRInputSubsystem _)
        {
            var desiredForward = Vector3.ProjectOnPlane(recenterForward, Vector3.up);
            if (desiredForward.sqrMagnitude < 1e-4f) desiredForward = Vector3.forward; // safety
            xrOrigin.MatchOriginUpCameraForward(Vector3.up, desiredForward.normalized);

            onRecentered?.Invoke();
        }

        public void ManualRecenter()
        {
            // Will succeed/fail depending on tracking origin support
            xrInput?.TryRecenter();
        }
    }
}
