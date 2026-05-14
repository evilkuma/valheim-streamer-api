using UnityEngine;

namespace ValheimStreamerApi
{
    public class FollowerTameable : Tameable, Interactable
    {
        public new bool Interact(Humanoid character, bool hold, bool alt)
        {
            if (alt)
            {
                var container = GetComponent<Container>();
                if (container != null)
                    return container.Interact(character, hold, false);
            }

            return base.Interact(character, hold, alt);
        }
    }
}
