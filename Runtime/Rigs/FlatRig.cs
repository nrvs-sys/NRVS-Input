using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NRVS.Input.Rigs
{
    /// <summary>
    /// Contains a Flat-Screen (non-XR) Client-Side representation (ie local inputs & camera) of the Player's Rig.
    /// </summary>
    public class FlatRig : InputRig, IRig
    {
        [Header("Components")]
        [SerializeField]
        private Transform originTransform;
        [SerializeField]
        private Transform headTransform;
        [SerializeField]
        private Transform leftHandTransform;
        [SerializeField]
        private Transform rightHandTransform;

        public Transform origin => originTransform;
        public Transform head => headTransform;
        public Transform leftHand => leftHandTransform;
        public Transform rightHand => rightHandTransform;
    }
}