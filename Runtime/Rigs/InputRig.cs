using NRVS.Input.Rigs;
using UnityEngine;

namespace NRVS.Input
{
    public abstract class InputRig : MonoBehaviour
    {
        protected virtual void Start()
        {
            Ref.Register<InputRig>(this);
        }

        protected virtual void OnDestroy()
        {
            Ref.Unregister<InputRig>(this);
        }
    }
}
