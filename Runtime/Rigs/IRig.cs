using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NRVS.Input.Rigs
{
    public interface IRig
    {
        public Transform transform { get; }
        public Transform origin { get; }
        public Transform head { get; }
        public Transform leftHand { get; }
        public Transform rightHand { get; }
    }

    public static class IRigExtensions
    {
        public static void CopyRigTransformsToRig(this IRig rig, IRig other)
        {
            other.origin.localPosition = rig.origin.localPosition;
            other.origin.localRotation = rig.origin.localRotation;
            other.head.localPosition = rig.head.localPosition;
            other.head.localRotation = rig.head.localRotation;
            other.leftHand.localPosition = rig.leftHand.localPosition;
            other.leftHand.localRotation = rig.leftHand.localRotation;
            other.rightHand.localPosition = rig.rightHand.localPosition;
            other.rightHand.localRotation = rig.rightHand.localRotation;
        }

        public static void CopyRigTransformsFromRig(this IRig rig, IRig other)
        {
            other.CopyRigTransformsToRig(rig);
        }

        public static void RotateRig(this IRig rig, float degrees) => rig.origin.RotateAround(rig.head.position, Vector3.up, degrees);

        public static void ResetOriginRotation(this IRig rig) => rig.origin.RotateAround(rig.head.position, Vector3.up, -rig.head.eulerAngles.y);
        //{
        //    rig.origin.position = rotation * (rig.origin.position - rig.head.position) + rig.head.position;
        //    rig.origin.rotation = rotation * rig.head.rotation;
        //}

        /// <summary>
        /// Sets position of the rig, optionally offset by the current position of the head
        /// </summary>
        public static void SetPosition(this IRig rig, Vector3 position, bool useHeadOffset = false)
        {
            var finalPosition = position;

            if (useHeadOffset)
            {
                var headOffset = rig.head.position - rig.transform.position;
                // Only apply horizontal offset
                headOffset.y = 0;

                finalPosition -= headOffset;
            }

            rig.transform.position = finalPosition;
        }

        /// <summary>
        /// TODO - determine if this even works. LOL
        /// </summary>
        /// <param name="rig"></param>
        /// <param name="position"></param>
        /// <param name="up"></param>
        public static void LookAt(this IRig rig, Vector3 position, Vector3 up) => rig.origin.LookAt(new Vector3(position.x, rig.head.position.y, position.z), up);
    }
}
