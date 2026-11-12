using UnityEngine;

namespace TrIVA.Agent
{
    /// <summary>
    /// Sets an integer Animator parameter to a random variant index whenever its sub-state machine is entered, so the entry transitions can pick one of several clips for the same agent state.
    /// </summary>
    public class RandomVariantPicker : StateMachineBehaviour
    {
        [SerializeField, Tooltip("Integer Animator parameter the entry transitions read.")] private string Parameter = "Variant";
        [SerializeField, Min(1), Tooltip("Number of clip variants in this sub-state machine.")] private int VariantCount = 1;
        [SerializeField, Tooltip("Avoid repeating the previous variant when more than one exists.")] private bool AvoidRepeat = true;

        private int _lastVariant = -1;

        /// <summary>
        /// Picks the variant for this pass through the sub-state machine before its entry transitions are evaluated.
        /// </summary>
        public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
        {
            int variant = Random.Range(0, VariantCount);
            if (AvoidRepeat && VariantCount > 1 && variant == _lastVariant)
                variant = (variant + Random.Range(1, VariantCount)) % VariantCount;

            _lastVariant = variant;
            animator.SetInteger(Parameter, variant);
        }
    }
}
