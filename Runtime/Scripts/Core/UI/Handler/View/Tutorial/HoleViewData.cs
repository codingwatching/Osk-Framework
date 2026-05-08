using UnityEngine;
using System;

namespace OSK
{
    public enum EHoleViewEase
    {
        Instant,
        Linear,
        SmoothDamp,
        OutSine,
        OutBack
    }

    public class HoleViewData
    {
        public RectTransform[] uiTargets;
        public Transform[] worldTargets;
        public int pointerTargetIndex = 0;
        public Vector2 pointerOffset = new Vector2(50, -50);
        public bool showPointer = true;
        public EHoleViewEase easeType = EHoleViewEase.SmoothDamp;
        public float duration = 0.3f;
        public bool freezeSize = true; // Mới: Giữ nguyên size lúc bắt đầu, không chạy theo scale animation

        public HoleViewData(params RectTransform[] targets) { this.uiTargets = targets; }
        public HoleViewData(params Transform[] worldTargets) { this.worldTargets = worldTargets; }
    }
}
