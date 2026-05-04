using UnityEngine;
using UnityEditor;
using OSK.Demo;
using System.Collections.Generic;

namespace OSK.EditorTools
{
    public class CardStressTestTool
    {
        [MenuItem("OSK/Demo/Generate 1000 Test Cards")]
        public static void Generate1000Cards()
        {
            string folderPath = "Assets/OSK-Framework/Runtime/Scripts/Core/Data/DemoCards/StressTest";
            
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                // Create folder recursively
                string[] parts = folderPath.Split('/');
                string currentPath = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    if (!AssetDatabase.IsValidFolder(currentPath + "/" + parts[i]))
                    {
                        AssetDatabase.CreateFolder(currentPath, parts[i]);
                    }
                    currentPath += "/" + parts[i];
                }
            }

            var manager = Object.FindObjectOfType<DemoCardManager>();
            if (manager == null)
            {
                Debug.LogError("Vui lòng mở Scene có chứa DemoCardManager trước!");
                return;
            }

            // Xóa rác cũ
            manager.AllConfigs.Clear();
            List<DemoCardConfig> newConfigs = new List<DemoCardConfig>();

            string longText = "Đây là một đoạn text cực kỳ dài để test xem file save hay file config có bị phình to ra không. Lớp Tĩnh (Master Data) có phình to thì trên RAM cũng chỉ lưu 1 bản duy nhất nhờ Reference O(1) chứ không hề copy ra 1000 bản. Cốt truyện của nhân vật này vô cùng bi tráng và đẫm nước mắt...";

            for (int i = 0; i < 1000; i++)
            {
                DemoCardConfig card = ScriptableObject.CreateInstance<DemoCardConfig>();
                card.ID = $"test_card_{i:D4}";
                card.CardName = $"Anh Hùng Thứ {i}";
                card.Description = longText + "\nKỹ năng đặc biệt số " + i;
                card.Lore = longText + "\nTruyền thuyết đời thứ " + i;
                card.BaseATK = Random.Range(10, 100);
                card.BaseHP = Random.Range(100, 1000);
                card.ATK_GrowthPerLevel = Random.Range(1f, 10f);

                string assetPath = $"{folderPath}/Card_{i:D4}.asset";
                AssetDatabase.CreateAsset(card, assetPath);
                newConfigs.Add(card);
            }

            AssetDatabase.SaveAssets();

            // Link vào Manager
            manager.AllConfigs.AddRange(newConfigs);
            EditorUtility.SetDirty(manager);
            
            Debug.Log($"✅ Đã tạo thành công 1000 thẻ bài nặng trịch và gắn vào DemoCardManager!");
        }
    }
}
