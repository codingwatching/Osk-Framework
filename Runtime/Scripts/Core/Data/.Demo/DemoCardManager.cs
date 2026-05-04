using UnityEngine;
using OSK.Data;

namespace OSK.Demo
{
    public class DemoCardManager : BaseDataManager<DemoCardConfig, DemoCardSave, DemoRuntimeCard>
    {
        private async void Start()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            
            sw.Start();
            await InitSaveDataAsync("demo_cards_1000.dat", SaveType.File);
            sw.Stop();
            
            MyLogger.Log($"🚀 [PERFORMANCE TEST] Đã Build Cache O(1) cho {GetAllData().Count} thẻ bài. Thời gian load & liên kết: {sw.ElapsedMilliseconds} ms!");
        }

        [ContextMenu("Test Upgrade First Card")]
        public void TestUpgrade()
        {
            if (AllConfigs.Count == 0) return;
            string testID = AllConfigs[0].ID;

            var card = GetData(testID);
            if (card != null)
            {
                card.LevelUp();
                MyLogger.Log($"✅ Đã nâng cấp {card.Config.CardName} lên cấp {card.SaveData.Level}! Sức mạnh hiện tại: {card.CurrentATK} ATK");
            }
        }

        [ContextMenu("Test Save Game (Force Save 1000)")]
        public async void TestSave()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            
            await SaveDataAsync("demo_cards_1000.dat", SaveType.File);
            
            sw.Stop();
            MyLogger.Log($"💾 [PERFORMANCE TEST] Đã lưu {GetAllData().Count} thẻ xuống ổ cứng. Thời gian ghi nén GZip: {sw.ElapsedMilliseconds} ms!");
        }
    }
}
