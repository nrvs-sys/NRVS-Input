using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisplayName("Toggle")]
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class ToggleInteraction : IInputInteraction
{
	private bool isToggled;

	static ToggleInteraction()
	{
		InputSystem.RegisterInteraction<ToggleInteraction>();
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Initialize()
	{
		// Will execute the static constructor as a side effect.
	}

	public void Process(ref InputInteractionContext context)
	{
		// On control release, perform the toggle
		bool isControlActuated = context.ControlIsActuated(0.1f);

		if (!isControlActuated)
		{
			isToggled = !isToggled;
			
			if (isToggled)
			{
				context.Started();
				context.Performed();
			}
			else
			{
				context.Canceled();
			}
		}
	}

	public void Reset()
	{
		
	}
}