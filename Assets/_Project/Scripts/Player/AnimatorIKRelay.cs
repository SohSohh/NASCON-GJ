using System;
using UnityEngine;

namespace NasconGJ.Player
{
    /// <summary>OnAnimatorIK only fires on the Animator's own GameObject; this forwards it to whoever needs it.</summary>
    [RequireComponent(typeof(Animator))]
    public class AnimatorIKRelay : MonoBehaviour
    {
        public event Action<int> IK;

        private void OnAnimatorIK(int layerIndex) => IK?.Invoke(layerIndex);
    }
}
