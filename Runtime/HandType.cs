using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Input
{
    public enum HandType
    {
        Left,
        Right
    };

    [System.Flags]
    public enum HandTypeFlags
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
    }

    public static class HandTypeExtensions
    {
        //public static OVRInput.Controller ToOVRInput(this HandType handType) => handType == HandType.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;

        //public static HandType ToHandType(this OVRInput.Controller controller) => controller == OVRInput.Controller.LTouch ? HandType.Left : HandType.Right;
    }
}
