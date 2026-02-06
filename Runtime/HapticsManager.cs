using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace NRVS.Input
{
    public class HapticsManager : Singleton<HapticsManager>
    {
        [Serializable]
        public class HapticsAnimation
        {
            public HandType hand;
            public AnimationCurve curve;
            public float duration;
            public Action OnComplete;
            public float intensity = 1f;

            // runtime
            internal float t;
            internal bool continuous;
            internal WrapMode postWrap;
            internal bool running;

            internal bool poolable;

            public bool isRunning => running;
        }

        // Frame accumulators (reset each Update)
        float _leftAmp, _rightAmp;

        // Active animations (packed list + index map for O(1) remove)
        readonly List<HapticsAnimation> _active = new();
        readonly Stack<HapticsAnimation> _pool = new();
        readonly Dictionary<HapticsAnimation, int> _index = new();

        protected override void OnSingletonInitialized() { }

        #region Unity Methods

        void OnDisable() => StopAllAnimations();
        void OnDestroy() => StopAllAnimations();

        void Update()
        {
            float dt = Time.deltaTime;

            for (int i = 0; i < _active.Count;)
            {
                var hapticsAnimation = _active[i];
                float inten = Mathf.Max(0f, hapticsAnimation.intensity);

                if (hapticsAnimation.continuous)
                {
                    // Continuous: sample at wrapped t
                    float sampleT = hapticsAnimation.postWrap == WrapMode.Loop
                        ? Mathf.Repeat(hapticsAnimation.t, hapticsAnimation.duration)
                        : Mathf.PingPong(hapticsAnimation.t, hapticsAnimation.duration);

                    AddAmplitude(hapticsAnimation.hand, hapticsAnimation.curve.Evaluate(sampleT) * inten);
                    hapticsAnimation.t += dt;
                    i++;
                    continue;
                }

                // Non-continuous: prevent double-add on last frame
                float nextT = hapticsAnimation.t + dt;
                if (nextT >= hapticsAnimation.duration)
                {
                    // Final sample ONLY at exact duration this frame
                    AddAmplitude(hapticsAnimation.hand, hapticsAnimation.curve.Evaluate(hapticsAnimation.duration) * inten);
                    CompleteAtIndex(i, hapticsAnimation); // removes & compacts list; do not i++
                    continue;
                }
                else
                {
                    // Normal in-range sample at current t
                    AddAmplitude(hapticsAnimation.hand, hapticsAnimation.curve.Evaluate(hapticsAnimation.t) * inten);
                    hapticsAnimation.t = nextT;
                    i++;
                }
            }

            // Send haptics once per frame (clamped)
            if (_leftAmp > 0f)
            {
                _leftAmp = Mathf.Clamp01(_leftAmp);
                InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).SendHapticImpulse(0, _leftAmp, Time.deltaTime);
                _leftAmp = 0f;
            }
            if (_rightAmp > 0f)
            {
                _rightAmp = Mathf.Clamp01(_rightAmp);
                InputDevices.GetDeviceAtXRNode(XRNode.RightHand).SendHapticImpulse(0, _rightAmp, Time.deltaTime);
                _rightAmp = 0f;
            }
        }

        #endregion

        #region Haptics API Methods

        public AnimationCurve GetImpulseCurve(float amplitude, float duration)
        {
            return AnimationCurve.Linear(0f, amplitude, duration, amplitude);
        }

        public void Impulse(HandType hand, float amplitude, float duration)
        {
            var curve = GetImpulseCurve(amplitude, duration);
            Animate(hand, curve, duration);
        }

        public void Animate(HandType hand, AnimationCurve curve, float duration = -1, Action onComplete = null)
        {
            if (curve == null || curve.length == 0) return;

            HapticsAnimation a = null;

            if (_pool.Count > 0)
            {
                a = _pool.Pop();
                a.hand = hand;
                a.curve = curve;
                a.duration = duration > 0 ? duration : curve.keys[curve.length - 1].time;
                a.OnComplete = onComplete;
                a.intensity = 1f;
                a.t = 0f;
                a.postWrap = curve.postWrapMode;
                a.continuous = (a.postWrap == WrapMode.Loop || a.postWrap == WrapMode.PingPong);
                a.running = true;
                a.poolable = true;
            }
            else
            {
                a = new HapticsAnimation
                {
                    hand = hand,
                    curve = curve,
                    duration = duration > 0 ? duration : curve.keys[curve.length - 1].time,
                    OnComplete = onComplete,
                    intensity = 1f,
                    t = 0f,
                    postWrap = curve.postWrapMode,
                    continuous = (curve.postWrapMode == WrapMode.Loop || curve.postWrapMode == WrapMode.PingPong),
                    running = true,
                    poolable = true
                };
            }

            _index[a] = _active.Count;
            _active.Add(a);
        }

        public HapticsAnimation AnimateAlloc(HandType hand, AnimationCurve curve, float duration = -1, Action onComplete = null)
        {
            if (curve == null || curve.length == 0) return null;

            var a = new HapticsAnimation
            {
                hand = hand,
                curve = curve,
                duration = duration > 0 ? duration : curve.keys[curve.length - 1].time,
                OnComplete = onComplete,
                intensity = 1f,
                t = 0f,
                postWrap = curve.postWrapMode,
            };
            a.continuous = (a.postWrap == WrapMode.Loop || a.postWrap == WrapMode.PingPong);
            a.running = true;
            a.poolable = false;

            _index[a] = _active.Count;
            _active.Add(a);
            return a;
        }

        public void RestartAnimation(HapticsAnimation hapticsAnimation)
        {
            if (hapticsAnimation == null) 
                return;

            // If already running, stop first (no OnComplete)
            if (_index.TryGetValue(hapticsAnimation, out int idx))
            {
                CompleteAtIndex(idx, hapticsAnimation, invokeComplete: false);
            }

            // Restart from beginning
            hapticsAnimation.t = 0f;
            hapticsAnimation.running = true;
            _index[hapticsAnimation] = _active.Count;
            _active.Add(hapticsAnimation);
        }

        public void StopAnimation(HapticsAnimation hapticsAnimation)
        {
            if (hapticsAnimation == null) 
                return;

            if (!_index.TryGetValue(hapticsAnimation, out int idx))
                return; // already stopped

            CompleteAtIndex(idx, hapticsAnimation, invokeComplete: false);
        }

        /// <summary>
        /// Returns an allocated animation instance to the pool for reuse. Do not use the animation reference after calling this.
        /// </summary>
        /// <param name="hapticsAnimation"></param>
        public void StoreAnimation(HapticsAnimation hapticsAnimation)
        {
            if (hapticsAnimation == null) 
                return;

            hapticsAnimation.poolable = true;

            StopAnimation(hapticsAnimation);
        }

        public void StopAllAnimations()
        {
            // No OnComplete on mass-stop; adjust if you prefer
            for (int i = 0; i < _active.Count; i++)
            {
                _active[i].running = false;
            }
            _active.Clear();
            _index.Clear();
        }

        #endregion

        #region Internal Methods

        void CompleteAtIndex(int idx, HapticsAnimation a, bool invokeComplete = true)
        {
            int last = _active.Count - 1;
            if (idx != last)
            {
                var swap = _active[last];
                _active[idx] = swap;
                _index[swap] = idx;
            }
            _active.RemoveAt(last);
            _index.Remove(a);

            a.running = false;

            if (invokeComplete) 
                a.OnComplete?.Invoke();

            if (a.poolable)
                _pool.Push(a);
        }

        void AddAmplitude(HandType hand, float amp)
        {
            if (amp <= 0f) 
                return;

            if (hand == HandType.Left) 
                _leftAmp += amp;
            else 
                _rightAmp += amp;
        }

        #endregion
    }
}
