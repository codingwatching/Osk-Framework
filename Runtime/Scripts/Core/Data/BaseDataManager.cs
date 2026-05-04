using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace OSK.Data
{
    /// <summary>
    /// O(1) Manager Warehouse. Any project implementing Gacha, Card Systems, or Inventory can inherit this.
    /// </summary>
    public abstract class BaseDataManager<TConfig, TSave, TRuntime> : MonoBehaviour
        where TConfig : BaseDataConfig
        where TSave : BaseSaveData, new()
        where TRuntime : BaseRuntimeData<TConfig, TSave>, new()
    {
        [Header("Master Data (Scriptable Objects)")]
        [Tooltip("Drag and drop all root configurations here (or load via Resources/Addressables via code)")]
        public List<TConfig> AllConfigs = new List<TConfig>();

        // O(1) Dictionary Structure - Completely eliminates LINQ!
        protected Dictionary<string, TConfig> _configDict = new Dictionary<string, TConfig>();
        protected Dictionary<string, TRuntime> _runtimeDict = new Dictionary<string, TRuntime>();
        
        // Temporarily store save files that have missing Configs (Prevents data loss if a Dev accidentally removes a Config)
        protected List<TSave> _orphanedSaves = new List<TSave>();

        public virtual void Awake()
        {
            // 1. Prebuild the static Dictionary as soon as the game opens
            foreach (var cfg in AllConfigs)
            {
                if (!string.IsNullOrEmpty(cfg.ID))
                {
                    _configDict[cfg.ID] = cfg;
                }
            }
        }

        // ==========================================
        // INITIALIZE FROM SAVE FILE (Load once)
        // ==========================================
        protected virtual async UniTask InitSaveDataAsync(string fileName, SaveType saveType = SaveType.File)
        {
            var root = await Main.Data.LoadAsync<DataRoot<TSave>>(saveType, fileName);
            if (root == null) root = new DataRoot<TSave>(); // If playing for the first time
            
            BuildRuntimeCache(root.Items);
        }

        private void BuildRuntimeCache(List<TSave> savedItems)
        {
            _runtimeDict.Clear();
            _orphanedSaves.Clear();

            // 1. Restore Cache from items the player already has in Save
            foreach (var saveItem in savedItems)
            {
                if (_configDict.TryGetValue(saveItem.ID, out var config))
                {
                    var runtime = new TRuntime();
                    runtime.Init(config, saveItem);
                    _runtimeDict[saveItem.ID] = runtime;
                }
                else
                {
                    // DATA PROTECTION: This card exists in the Save file but NOT in AllConfigs.
                    // DISPLAY A RED ERROR FOR DEVS TO FIX (Accidentally deleted Config). 
                    // We isolate it to prevent the player's data from being permanently lost.
                    _orphanedSaves.Add(saveItem);
                    MyLogger.LogError($"[BaseDataManager] MISSING ROOT CONFIG: Could not find ScriptableObject for ID '{saveItem.ID}'. Player's Save Data has been safely isolated!");
                }
            }

            // 2. Inject new cards (Scenario: Game Update introduces new cards not present in old Saves)
            foreach (var cfg in AllConfigs)
            {
                if (!_runtimeDict.ContainsKey(cfg.ID))
                {
                    var newSave = new TSave { ID = cfg.ID }; // Entirely new card data (Level 0)
                    var runtime = new TRuntime();
                    runtime.Init(cfg, newSave);
                    _runtimeDict[cfg.ID] = runtime;
                }
            }
        }

        // ==========================================
        // WRITE TO DISK (Save)
        // ==========================================
        public virtual async UniTask SaveDataAsync(string fileName, SaveType saveType = SaveType.File)
        {
            var root = new DataRoot<TSave>();
            
            // 1. Save all active data
            foreach (var runtime in _runtimeDict.Values)
            {
                root.Items.Add(runtime.SaveData); // Extract the Save Data core to write to disk
            }
            
            // 2. Append orphaned data back into the save file (So the player doesn't lose it)
            root.Items.AddRange(_orphanedSaves);
            
            await Main.Data.SaveAsync(saveType, fileName, root);
        }

        // ==========================================
        // O(1) OPTIMIZED API FOR OTHER MODULES
        // ==========================================
        public TRuntime GetData(string id)
        {
            return _runtimeDict.TryGetValue(id, out var data) ? data : null;
        }
        
        public IReadOnlyCollection<TRuntime> GetAllData()
        {
            return _runtimeDict.Values;
        }
    }
}
