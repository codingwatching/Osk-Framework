using System;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace OSK
{
    /// <summary>
    /// Load ngay lập tức (Mặc định)
    /// Load rải rác mỗi frame (Đỡ lag khi start game)
    /// Khi nào dùng mới load
    /// </summary>
    public enum PreloadMode
    {
        Immediate,    
        Spread,       
        Lazy          
    }
    
    /// <summary>
    /// Đủ số lượng -> Xóa cái cũ nhất đi dùng lại (Tối ưu cho Effect/Bullet)
    /// Đủ số lượng -> Không cho sinh thêm (Dùng cho Item giới hạn)
    /// </summary>
    public enum LimitMode
    {
        RecycleOldest,  
        RejectNew       
    }

    [System.Serializable]
    public class PoolItemData
    {
        [HorizontalGroup("Split", Width = 0.3f)]
        public string GroupName { get; set; }
        
        public string Key;
        public Object Prefab;
        public Transform Parent;
        [Tooltip("Số lượng object khởi tạo ban đầu.")]
        public int Size = 10;
        [Tooltip("Giới hạn tối đa. -1 là vô hạn (tự expand).")]
        public int MaxSize = -1; 
    
        [EnumToggleButtons]
        public PreloadMode LoadMode = PreloadMode.Immediate;
        
        [EnumToggleButtons]
        public LimitMode LimitMode = LimitMode.RecycleOldest; 
    }

    [System.Serializable] 
    public class PoolGroupData
    {
        [TitleGroup("$GroupName")]
        public string GroupName;

        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = false)]
        public List<PoolItemData> Pools = new();
    }
}