using UnityEngine;

namespace OSK.Data
{
    /// <summary>
    /// Master Data (Layer 1): Static data that does not change during gameplay (ScriptableObject).
    /// </summary>
    public abstract class BaseDataConfig : ScriptableObject
    {
        [Tooltip("Primary foreign key (e.g., card_knight_01)")]
        public string ID;
    }
}
