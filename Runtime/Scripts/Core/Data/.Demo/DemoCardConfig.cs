using UnityEngine;
using OSK.Data;

namespace OSK.Demo
{
    [CreateAssetMenu(fileName = "NewCard", menuName = "OSK/Demo/Card Config")]
    public class DemoCardConfig : BaseDataConfig
    {
        public string CardName;
        [TextArea(3, 5)] public string Description;
        [TextArea(5, 10)] public string Lore;
        public int BaseATK;
        public int BaseHP;
        public float ATK_GrowthPerLevel;
    }
}
