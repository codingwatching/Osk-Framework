using UnityEngine;
using UnityEngine.UI;

namespace OSK
{
    public partial class UIManager
    {
        public void SetCanvas(int sortOrder = 0, string sortingLayerName = "Default",
            RenderMode renderMode = RenderMode.ScreenSpaceOverlay, bool pixelPerfect = false,
            UnityEngine.Camera camera = null)
        {
            foreach (var canvas in RootUI.AllCanvases)
            {
                if (canvas == null) continue;
                canvas.renderMode = renderMode;
                canvas.sortingLayerName = sortingLayerName;
                canvas.pixelPerfect = pixelPerfect;
                canvas.worldCamera = camera;
                
                // Note: We don't override sortingOrder for all canvases here 
                // because Screen, Popup, Overlay need their specific gaps (0, 10, 20).
                // But if you explicitly want to shift all of them, we could add an offset.
            }
            
            // Apply base sorting order to Screen Canvas as the reference
            if (RootUI.ScreenCanvas != null)
                RootUI.ScreenCanvas.sortingOrder = sortOrder;
        }

        public void SetupCanvasScaleForRatio()
        { 
            float ratio = (float)Screen.width / Screen.height;
            float match = 0f;

            if (IsIpad())
            {
                match = 0f;
            }
            else
            {
                match = ratio > 0.65f ? 1 : 0;
            }

            foreach (var scaler in RootUI.AllScalers)
            {
                if (scaler != null)
                    scaler.matchWidthOrHeight = match;
            }
            
            string log = Mathf.Approximately(match, 1f) ? "1 (Match Width)" : "0 (Match Height)";
            MyLogger.Log($"Ratio: {ratio}. IsPad {IsIpad()} matchWidthOrHeight: {log}");
        }
         
        
        public  bool IsIpad()
        {
#if (UNITY_IOS || UNITY_IPHONE) && !UNITY_EDITOR
            if (UnityEngine.iOS.Device.generation.ToString().Contains("iPad"))
                return true;
#endif

            float w = Screen.width;
            float h = Screen.height;

            // Normalize to portrait
            if (w > h) (w, h) = (h, w);

            // Aspect ratio check (iPad thường ~ 4:3 → ~1.33)
            return (h / w) < 1.65f;
        }

        public void SetCanvasScaler(
            CanvasScaler.ScaleMode scaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize,
            float scaleFactor = 1f,
            float referencePixelsPerUnit = 100f)
        {
            foreach (var scaler in RootUI.AllScalers)
            {
                if (scaler == null) continue;
                scaler.uiScaleMode = scaleMode;
                scaler.scaleFactor = scaleFactor;
                scaler.referencePixelsPerUnit = referencePixelsPerUnit;
            }
        }

        public void SetCanvasScaler(
            CanvasScaler.ScaleMode scaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize,
            Vector2? referenceResolution = null,
            CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight,
            float matchWidthOrHeight = 0f,
            float referencePixelsPerUnit = 100f)
        {
            foreach (var scaler in RootUI.AllScalers)
            {
                if (scaler == null) continue;
                scaler.uiScaleMode = scaleMode;
                scaler.referenceResolution = referenceResolution ?? new Vector2(1920, 1080);
                scaler.screenMatchMode = screenMatchMode;
                scaler.matchWidthOrHeight = matchWidthOrHeight;
                scaler.referencePixelsPerUnit = referencePixelsPerUnit;
            }
        }

        public void SetCanvasScaler(
            CanvasScaler.ScaleMode scaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize,
            Vector2? referenceResolution = null,
            CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight,
            bool autoMatchForRatio = true,
            float referencePixelsPerUnit = 100f)
        {
            float newRatio = (float)Screen.width / Screen.height;
            SetCanvasScaler(scaleMode, referenceResolution, screenMatchMode, newRatio > 0.65f ? 1 : 0,
                referencePixelsPerUnit);
        }

        public void ShowRayCast()
        {
            foreach (var canvas in RootUI.AllCanvases)
            {
                if (canvas == null) continue;
                var graphicRayCaster = canvas.GetComponent<GraphicRaycaster>();
                if (graphicRayCaster != null)
                    graphicRayCaster.ignoreReversedGraphics = true;
            }
        }

        public void HideRayCast()
        {
            foreach (var canvas in RootUI.AllCanvases)
            {
                if (canvas == null) continue;
                var graphicRayCaster = canvas.GetComponent<GraphicRaycaster>();
                if (graphicRayCaster != null)
                    graphicRayCaster.ignoreReversedGraphics = false;
            }
        }
    }
}