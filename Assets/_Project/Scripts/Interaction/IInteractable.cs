using UnityEngine;

namespace NasconGJ.Interaction
{
    /// <summary>Anything the player can look at and press Interact on (items, guiding light, levers...).</summary>
    public interface IInteractable
    {
        /// <summary>Short prompt shown on the HUD, e.g. "Pick up torch".</summary>
        string Prompt { get; }

        bool CanInteract(GameObject interactor);

        void Interact(GameObject interactor);
    }
}
