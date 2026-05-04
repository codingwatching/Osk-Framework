namespace OSK.Data
{
    /// <summary>
    /// Runtime Cache (Layer 3): The bridge connecting Master Data and Save Data in RAM for O(1) access.
    /// </summary>
    public abstract class BaseRuntimeData<TConfig, TSave>
        where TConfig : BaseDataConfig
        where TSave : BaseSaveData
    {
        public TConfig Config { get; private set; }
        public TSave SaveData { get; private set; }

        public void Init(TConfig config, TSave saveData)
        {
            Config = config;
            SaveData = saveData;
            RefreshRuntimeData(); // Automatically calculate stats or process logic upon initialization
        }

        /// <summary>
        /// Triggered when the data is initially loaded or when you need to manually refresh runtime logic.
        /// Override this to calculate derived stats (e.g., current HP based on Level) or process custom logic.
        /// </summary>
        public abstract void RefreshRuntimeData();
    }
}
