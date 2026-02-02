using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using NRVS.Settings;

namespace Input
{
    [CreateAssetMenu(fileName = "Input Manager_ ", menuName = "Inputs/Input Manager")]
    public class InputManager : ManagedObject
    {
        [Header("Settings")]
        public HandActionStates gunEnableMask;
        public HandActionStates gunDisableMask;

        [Space(10)]
        public HandActionStates movementEnableMask;
        public HandActionStates movementDisableMask;

        [Space(10)]
        public HandActionStates abilityEnableMask;
        public HandActionStates abilityDisableMask;

        [Space(10)]
        public HandActionStates applicationEnableMask;
        public HandActionStates applicationDisableMask;

        [Space(10)]
        public HandActionStates uiEnableMask;
        public HandActionStates uiDisableMask;

        [Space(10)]
        public HandActionStates interactionEnableMask;
        public HandActionStates interactionDisableMask;

        [Header("Binding Override Settings Behavior")]
        [Tooltip("Stores binding overrides as a JSON string. Loaded on Initialize.")]
        public SettingsBehavior bindingOverrideSettingsBehavior;


        private XRInputActions _actions;
        public XRInputActions actions
        {
            get
            {
                if (_actions == null)
                    _actions = new();

                return _actions;
            }
        }

        public event Action<bool> OnUIStateChanged;
        bool _lastIsUIActive;

        public event Action<bool> OnUIStateLeftChanged;
        bool _lastIsUILeftActive;

        public event Action<bool> OnUIStateRightChanged;
        bool _lastIsUIRightActive;

        public event Action<bool> OnMovementEnabledChanged;
        bool _lastIsMovementEnabled;

        [Flags]
        public enum HandActionStates
        {
            Nothing = 0,
            Disabled = 1,
            UI = 2,
            Shooting = 4,
            Movement = 8,
            Application = 16,
            Interaction = 32,
            Ability = 64,
        }

        public class HandActionMap
        {
            public InputActionMap actionMap;
            public HandActionStates enableMask;
            public HandActionStates disableMask;

            public void Update(HandActionStates state)
            {
                if (state.HasFlag(enableMask) && !state.HasAnyFlag(disableMask))
                    actionMap.Enable();
                else
                    actionMap.Disable();
            }
        }

        public bool isUIActive => isUILeftActive || isUIRightActive;
        public bool isUILeftActive => (leftHandActionState & HandActionStates.UI) != 0;
        public bool isUIRightActive => (rightHandActionState & HandActionStates.UI) != 0;
        public bool isMovementEnabled => actions.Movement.enabled;


        public event ResetBindingOverridesHandler OnResetBindingOverrides;
        public delegate void ResetBindingOverridesHandler();


        public const string xrControlSchemeName = "Generic XR Controller";
        public const string mouseAndKeyboardControlSchemeName = "Mouse & Keyboard";

        private HandActionStates leftHandActionState;
        private HandActionStates rightHandActionState;
        private HandActionStates generalHandActionState;

        private List<HandActionMap> leftHandActionMaps;
        private List<HandActionMap> rightHandActionMaps;
        private List<HandActionMap> generalActionMaps;

        protected override void Initialize()
        {
            leftHandActionMaps = new List<HandActionMap>()
            {
                new HandActionMap() { actionMap = actions.Guns_LeftHand,      enableMask = gunEnableMask,          disableMask = gunDisableMask },
                new HandActionMap() { actionMap = actions.UI_LeftHand,        enableMask = uiEnableMask,           disableMask = uiDisableMask },
            };

            rightHandActionMaps = new List<HandActionMap>()
            {
                new HandActionMap() { actionMap = actions.Guns_RightHand,      enableMask = gunEnableMask,         disableMask = gunDisableMask },
                new HandActionMap() { actionMap = actions.UI_RightHand,        enableMask = uiEnableMask,          disableMask = uiDisableMask },
            };

            generalActionMaps = new List<HandActionMap>()
            {
                new HandActionMap() { actionMap = actions.Movement,            enableMask = movementEnableMask,    disableMask = movementDisableMask},
                new HandActionMap() { actionMap = actions.Application,         enableMask = applicationEnableMask, disableMask = applicationDisableMask },
                new HandActionMap() { actionMap = actions.Interaction,         enableMask = interactionEnableMask, disableMask = interactionDisableMask },
                new HandActionMap() { actionMap = actions.Ability,            enableMask = abilityEnableMask,     disableMask = abilityDisableMask },
            };

            // Enable all actions first
            actions.asset.Enable();

            // Initialize action maps
            UpdateLeftHandActionMaps();
            UpdateRightHandActionMaps();
            UpdateGeneralActionMaps();

            // Set the default player state
            SetShooting(true);
            SetMovement(true);
            SetAbility(true);
            SetApplication(true);
            SetInteraction(false);

            // Lock the cursor in flat mode
            if (ApplicationInfo.applicationMode == ApplicationInfo.ApplicationMode.Flat)
                Cursor.lockState = CursorLockMode.Locked;

            // Set binding overrides
            var bindingOverridesJson = bindingOverrideSettingsBehavior.GetString();
            if (!string.IsNullOrEmpty(bindingOverridesJson))
                actions.LoadBindingOverridesFromJson(bindingOverridesJson);
        }

        protected override void Cleanup()
        {
            _actions?.Disable();

            leftHandActionState = HandActionStates.Nothing;
            rightHandActionState = HandActionStates.Nothing;
        }


        public void SetLeftHandDisabled(bool active) => SetHandActionState(HandType.Left, HandActionStates.Disabled, active);
        public void SetRightHandDisabled(bool active) => SetHandActionState(HandType.Right, HandActionStates.Disabled, active);
        public void SetDisabled(bool active)
        {
            SetLeftHandDisabled(active);
            SetRightHandDisabled(active);
        }

        public void SetLeftHandShooting(bool active) => SetHandActionState(HandType.Left, HandActionStates.Shooting, active);
        public void SetRightHandShooting(bool active) => SetHandActionState(HandType.Right, HandActionStates.Shooting, active);
        public void SetShooting(bool active)
        {
            SetLeftHandShooting(active);
            SetRightHandShooting(active);
        }

        public void SetMovement(bool active)
        {
            SetHandActionState(HandType.Left, HandActionStates.Movement, active);
            SetHandActionState(HandType.Right, HandActionStates.Movement, active);
        }

        public void SetAbility(bool active)
        {
            SetHandActionState(HandType.Left, HandActionStates.Ability, active);
            SetHandActionState(HandType.Right, HandActionStates.Ability, active);
        }

        public void SetLeftHandUI(bool active) => SetHandActionState(HandType.Left, HandActionStates.UI, active);
        public void SetRightHandUI(bool active) => SetHandActionState(HandType.Right, HandActionStates.UI, active);
        public void SetUI(bool active)
        {
            SetLeftHandUI(active);
            SetRightHandUI(active);

            if (ApplicationInfo.applicationMode == ApplicationInfo.ApplicationMode.Flat)
                Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
        }

        public void SetApplication(bool active)
        {
            SetHandActionState(HandType.Left, HandActionStates.Application, active);
            SetHandActionState(HandType.Right, HandActionStates.Application, active);
        }

        public void SetInteraction(bool active)
		{
            SetHandActionState(HandType.Left, HandActionStates.Interaction, active);
            SetHandActionState(HandType.Right, HandActionStates.Interaction, active);
        }

        public void SetHandActionState(HandType hand, HandActionStates state, bool active)
        {
            if (hand == HandType.Left)
            {
                if (active)
                    leftHandActionState = leftHandActionState.Add(state);
                else
                    leftHandActionState = leftHandActionState.Remove(state);

                UpdateLeftHandActionMaps();
            }
            else
            {
                if (active)
                    rightHandActionState = rightHandActionState.Add(state);
                else
                    rightHandActionState = rightHandActionState.Remove(state);

                UpdateRightHandActionMaps();
            }

            //Debug.Log($"Set {hand} Hand Action State: {state} to {(active ? "Active" : "Inactive")}");


            generalHandActionState = leftHandActionState.Union(rightHandActionState);

            //Debug.Log($"General Hand Action State: {generalHandActionState}");

            UpdateGeneralActionMaps();


            // Fire input state change events
            bool newIsUIActive = isUIActive;
            if (newIsUIActive != _lastIsUIActive)
            {
                _lastIsUIActive = newIsUIActive;
                OnUIStateChanged?.Invoke(newIsUIActive);
            }

            bool newIsUILeftActive = isUILeftActive;
            if (newIsUILeftActive != _lastIsUILeftActive)
            {
                _lastIsUILeftActive = newIsUILeftActive;
                OnUIStateLeftChanged?.Invoke(newIsUILeftActive);
            }

            bool newIsUIRightActive = isUIRightActive;
            if (newIsUIRightActive != _lastIsUIRightActive)
            {
                _lastIsUIRightActive = newIsUIRightActive;
                OnUIStateRightChanged?.Invoke(newIsUIRightActive);
            }

            bool newIsMovementEnabled = isMovementEnabled;
            if (newIsMovementEnabled != _lastIsMovementEnabled)
            {
                _lastIsMovementEnabled = newIsMovementEnabled;
                OnMovementEnabledChanged?.Invoke(newIsMovementEnabled);
            }
        }

        /// <summary>
        /// Applies a binding override to change the interactions on the Guns_RightHand.Combine action
        /// Note: this is called from the Input Settings object in the Core scene
        /// </summary>
        /// <param name="value">a 1 based index of the combine mode</param>
        public void SetCombineMode(int value)
		{
            // The combine mode is a 1 based index of modes
            // 1: Hold (default)
            // 2: Toggle
            string interactions = value == 2 ? "toggle()" : "";
            
            actions.Guns_RightHand.Combine.ApplyBindingOverride(new InputBinding
			{
				overrideInteractions = interactions
            });
		}

        private void UpdateLeftHandActionMaps() => leftHandActionMaps?.ForEach(m => m?.Update(leftHandActionState));
        private void UpdateRightHandActionMaps() => rightHandActionMaps?.ForEach(m => m?.Update(rightHandActionState));
        private void UpdateGeneralActionMaps() => generalActionMaps?.ForEach(m => m?.Update(generalHandActionState));

        public string GetActiveControlSchemeName()
		{
			switch (ApplicationInfo.applicationMode)
			{
				case ApplicationInfo.ApplicationMode.XR:
					return xrControlSchemeName;
				case ApplicationInfo.ApplicationMode.Flat:
				case ApplicationInfo.ApplicationMode.Server:
				default:
					return mouseAndKeyboardControlSchemeName;
			}
		}


		#region Input Rebinding


		private InputActionRebindingExtensions.RebindingOperation _op;
        public void BeginRebind(InputActionReference inputActionReference, Action<bool, string> OnComplete = null)
		{
            // Get the action
            var action = actions.FindAction(inputActionReference.action.id.ToString());
            var scheme = actions.MouseKeyboardScheme;
            var bindingGroup = scheme.bindingGroup;
            var bindingIndex = GetBindingIndexForScheme(action, scheme.name);
            if (bindingIndex < 0)
            {
                Debug.LogError($"Input Manager: no binding for '{action.name}' under scheme '{scheme.name}'");
                return;
            }

            Rebind(action, bindingGroup, bindingIndex, OnComplete);
        }

        public void BeginRebindForComposite(InputActionReference inputActionReference, string compositeName, string partName, Action<bool, string> OnComplete = null)
		{
            // Get the action
            var action = actions.FindAction(inputActionReference.action.id.ToString());
            var scheme = actions.MouseKeyboardScheme;
            var bindingGroup = scheme.bindingGroup;
            var bindingIndex = GetCompositePartIndex(action, bindingGroup, compositeName, partName);
            if (bindingIndex < 0)
            {
                Debug.LogError($"Input Manager: no binding for '{action.name}' under scheme '{scheme.name}'");
                return;
            }

            Rebind(action, bindingGroup, bindingIndex, OnComplete);
        }

        // This is used for rebinding the mirrored input for double tap weapon switching
        public void RebindForMirrorInput(InputActionReference inputActionReference, string newPath)
		{
            var action = actions.FindAction(inputActionReference.action.id.ToString());

            // The binding index is explicitly set to 1, matching the setup in the input actions asset
            action.ApplyBindingOverride(1, newPath);

            // Update saved binding overrides
            SaveBindingOverrides();

            Debug.LogError($"Input Manager: Set binding override for mirror input '{action.name}' to '{newPath}'");
        }

        public void ResetBindingOverrides()
		{
            actions.RemoveAllBindingOverrides();

            bindingOverrideSettingsBehavior.DeleteValue();
            bindingOverrideSettingsBehavior.Save();

            OnResetBindingOverrides?.Invoke();
        }

        private void Rebind(InputAction action, string bindingGroup, int bindingIndex, Action<bool, string> OnComplete = null)
		{
            // Start a rebinding operation (action.PerformInteractiveRebinding)
            string prevOverridePath = action.bindings[bindingIndex].overridePath;
            string prevEffectivePath = action.bindings[bindingIndex].effectivePath;

            action.Disable();
            
            _op = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsHavingToMatchPath(LayoutFilterForGroup(bindingGroup))
                .WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(op =>
                {
                    op.Dispose();
                    action.Enable();
                    Debug.LogError($"Input Manager: binding for '{action.name}' cancelled!");
                    OnComplete?.Invoke(false, string.Empty);
                })
                .OnComplete(op =>
                {
                    string newPath = action.bindings[bindingIndex].effectivePath;
                    op.Dispose();
                    action.Enable();
                    Debug.LogError($"Input Manager: binding for '{action.name}' set to key '{newPath}'");

                    // Save the new binding override
                    SaveBindingOverrides();

                    OnComplete?.Invoke(true, newPath);
                })
                .Start();

            Debug.LogError($"Input Manager: listening for input to rebind action'{action.name}'...");
        }

        private void SaveBindingOverrides()
		{
            var bindingOverridesJson = actions.SaveBindingOverridesAsJson();
            bindingOverrideSettingsBehavior.SetValue(bindingOverridesJson);
            bindingOverrideSettingsBehavior.Save();
        }

        public static int GetBindingIndexForScheme(InputAction action, string scheme)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var b = action.bindings[i];
                if (b.isComposite || b.isPartOfComposite) continue;
                if (BindingBelongsToScheme(b, scheme) && b.action == action.name)
                {
                    return i;
                }
            }
            return -1;
        }

        private static bool BindingBelongsToScheme(InputBinding b, string scheme)
        {
            if (string.IsNullOrEmpty(b.groups)) return false;
            var groups = b.groups.Split(';');
            for (int i = 0; i < groups.Length; i++)
                if (groups[i].Trim() == scheme) return true;
            return false;
        }

        private static string LayoutFilterForGroup(string bindingGroup)
        {
            // map bindingGroup → device layout filter; adjust if your group names differ
            if (bindingGroup.Contains("XR")) return "<XRController>";
            if (bindingGroup.Contains("Gamepad")) return "<Gamepad>";
            if (bindingGroup.Contains("Mouse") ||
                bindingGroup.Contains("Keyboard")) return "<Keyboard>"; // broaden if you want to allow mouse too
            return "*"; // no filter
        }

        public static int GetCompositePartIndex(InputAction action, string bindingGroup, string compositeNameOrNull, string partName)
        {
            partName = partName.ToLowerInvariant();

            for (int i = 0; i < action.bindings.Count; i++)
            {
                var b = action.bindings[i];

                // part belongs to same group?
                bool inGroup = false;
                if (!string.IsNullOrEmpty(b.groups))
                    foreach (var g in b.groups.Split(';'))
                        if (g.Trim() == bindingGroup) { inGroup = true; break; }
                if (!inGroup) continue;

                if (b.name.ToLowerInvariant() == partName)
                    return i;
            }
            return -1;
        }


		#endregion
	}
}
