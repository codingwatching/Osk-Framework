namespace OSK.Data
{
    /// <summary>
    /// Save Data (Layer 2): Extremely lightweight file containing only mutable data (e.g., Level, Quantity).
    /// </summary>
    [System.Serializable]
    public abstract class BaseSaveData
    {
        public string ID; // Used to link back to the BaseDataConfig
    }
    
    /// <summary>
    /// The container wrapper holding a list of Save Data to serialize to disk.
    /// </summary>
    [System.Serializable]
    public class DataRoot<TSave> where TSave : BaseSaveData
    {
        public System.Collections.Generic.List<TSave> Items = new System.Collections.Generic.List<TSave>();
    }
}
