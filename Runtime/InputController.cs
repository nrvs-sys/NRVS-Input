using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Input
{
    public class InputController : MonoBehaviour
    {
		[Header("Settings")]
		public List<InputActionEvent> inputActionEvents;

		[Header("Events")]
		public UnityEvent<bool> onUIStateChanged;
		public UnityEvent<bool> onUIStateLeftChanged;
		public UnityEvent<bool> onUIStateRightChanged;
		public UnityEvent<bool> onMovementEnabledChanged;

		[Header("Dependencies")]
		public InputManager inputManager;


		[Serializable]
		public class InputActionEvent
        {
			public InputActionReference actionReference;
			public UnityEvent onActionStarted;
			public UnityEvent onActionPerformed;
			public UnityEvent onActionCancelled;

			[HideInInspector]
			public InputAction action;
		}


		void OnEnable()
		{
			foreach (InputActionEvent inputActionEvent in inputActionEvents)
			{
				InputAction action = inputManager.actions.FindAction(inputActionEvent.actionReference.action.id.ToString());

                action.started   += Action_started;
				action.performed += Action_performed;
                action.canceled  += Action_canceled;

				inputActionEvent.action = action;
			}

			inputManager.OnUIStateChanged += OnUIActiveChanged;
			inputManager.OnUIStateLeftChanged += OnUILeftActiveChanged;
			inputManager.OnUIStateRightChanged += OnUIRightActiveChanged;
			inputManager.OnMovementEnabledChanged += OnMovementStateChanged;

			// initial event states
			onUIStateChanged?.Invoke(inputManager.isUIActive);
			onUIStateLeftChanged?.Invoke(inputManager.isUILeftActive);
			onUIStateRightChanged?.Invoke(inputManager.isUIRightActive);
			onMovementEnabledChanged?.Invoke(inputManager.isMovementEnabled);
		}

        private void OnDisable()
		{
			foreach (InputActionEvent inputActionEvent in inputActionEvents)
			{
				InputAction action = inputManager.actions.FindAction(inputActionEvent.actionReference.action.id.ToString());

				action.started   -= Action_started;
				action.performed -= Action_performed;
				action.canceled  -= Action_canceled;

				inputActionEvent.action = null;
			}

			inputManager.OnUIStateChanged -= OnUIActiveChanged;
			inputManager.OnUIStateLeftChanged -= OnUILeftActiveChanged;
			inputManager.OnUIStateRightChanged -= OnUIRightActiveChanged;
			inputManager.OnMovementEnabledChanged -= OnMovementStateChanged;
		}


		private void Action_started(InputAction.CallbackContext callbackContext)   => inputActionEvents.Find(e => e.action == callbackContext.action)?.onActionStarted?.Invoke();
		private void Action_performed(InputAction.CallbackContext callbackContext) => inputActionEvents.Find(e => e.action == callbackContext.action)?.onActionPerformed?.Invoke();
		private void Action_canceled(InputAction.CallbackContext callbackContext)  => inputActionEvents.Find(e => e.action == callbackContext.action)?.onActionCancelled?.Invoke();


		void OnUIActiveChanged(bool uiActive)
		{
			onUIStateChanged?.Invoke(uiActive);
		}

		void OnUILeftActiveChanged(bool uiLeftActive)
		{
			onUIStateLeftChanged?.Invoke(uiLeftActive);
		}

		void OnUIRightActiveChanged(bool uiRightActive)
		{
			onUIStateRightChanged?.Invoke(uiRightActive);
		}

		void OnMovementStateChanged(bool enabled)
		{
			onMovementEnabledChanged?.Invoke(enabled);
		}
	}
}
