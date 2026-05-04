using UnityEngine;
using OSK.Data;

namespace OSK.Demo
{
    public class DemoRuntimeCard : BaseRuntimeData<DemoCardConfig, DemoCardSave>
    {
        public int CurrentATK { get; private set; }
        public int CurrentHP { get; private set; }

        public override void RefreshRuntimeData()
        {
            CurrentATK = Config.BaseATK + Mathf.RoundToInt(SaveData.Level * Config.ATK_GrowthPerLevel);
            CurrentHP = Config.BaseHP + (SaveData.Level * 10);
        }

        public void LevelUp()
        {
            SaveData.Level++;
            RefreshRuntimeData();
        }
    }
}
