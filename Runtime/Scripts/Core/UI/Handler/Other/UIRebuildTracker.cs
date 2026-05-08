using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace OSK
{
    [DefaultExecutionOrder(-20000)]
    public class UIRebuildTracker : MonoBehaviour
    {
        private class TrackerNode
        {
            public RectTransform rect;
            public float lastRebuildTime;
            public int totalHits;
            public float hitsPerSecond;
            public string type;
            private int _hitsThisSecond;
            private float _secondTimer;

            public void AddHit()
            {
                totalHits++;
                _hitsThisSecond++;
                lastRebuildTime = Time.time;
            }

            public void UpdatePPS()
            {
                if (Time.time > _secondTimer + 1f)
                {
                    hitsPerSecond = _hitsThisSecond;
                    _hitsThisSecond = 0;
                    _secondTimer = Time.time;
                }
            }
        }

        [Header("Appearance")]
        [Range(0.5f, 2f)] public float uiScale = 1.0f;
        public Color layoutColor = Color.magenta;
        public Color graphicColor = Color.yellow;

        private Dictionary<int, TrackerNode> _nodes = new Dictionary<int, TrackerNode>();
        private List<TrackerNode> _offenders = new List<TrackerNode>();
        private GUIStyle _headerStyle;
        private GUIStyle _itemStyle;
        private GUIStyle _tipStyle;
        private Vector2 _scrollPos;

        private void Awake()
        {
            HookAllUI();
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTMPChanged);
        }

        private void OnDestroy()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTMPChanged);
        }

        public void HookAllUI()
        {
            Graphic[] allGraphics = FindObjectsOfType<Graphic>(true);
            foreach (var g in allGraphics)
            {
                g.RegisterDirtyVerticesCallback(() => OnElementDirty(g.rectTransform, "Graphic"));
                g.RegisterDirtyLayoutCallback(() => OnElementDirty(g.rectTransform, "Layout"));
            }
        }

        private void OnTMPChanged(Object obj)
        {
            if (obj is TMP_Text tmp) OnElementDirty(tmp.rectTransform, "TMP Text");
        }

        private void OnElementDirty(RectTransform rt, string type)
        {
            if (rt == null || !Application.isPlaying) return;
            int id = rt.GetInstanceID();
            if (!_nodes.TryGetValue(id, out var node))
            {
                node = new TrackerNode { rect = rt, type = type };
                _nodes[id] = node;
            }
            node.AddHit();
        }

        private void Update()
        {
            if (Time.frameCount % 60 == 0) HookAllUI();

            _offenders.Clear();
            foreach (var node in _nodes.Values)
            {
                if (node.rect == null) continue;
                node.UpdatePPS();
                if (Time.time < node.lastRebuildTime + 2f)
                    _offenders.Add(node);
            }
            _offenders.Sort((a, b) => b.hitsPerSecond.CompareTo(a.hitsPerSecond));
        }

        private void OnGUI()
        {
            if (_headerStyle == null) InitStyles();

            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1));

            // Bảng điều khiển chính
            float panelWidth = 450;
            float panelHeight = 400;
            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            GUI.Box(new Rect(10, 10, panelWidth, panelHeight), "");
            
            GUILayout.BeginArea(new Rect(25, 20, panelWidth - 30, panelHeight - 30));
            GUILayout.Label("☢ OSK UI PERFORMANCE ANALYZER", _headerStyle);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("UI Scale: ", GUILayout.Width(60));
            uiScale = GUILayout.HorizontalSlider(uiScale, 0.5f, 2.0f, GUILayout.Width(150));
            if (GUILayout.Button("Clear Data", GUILayout.Width(80))) _nodes.Clear();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200));
            foreach (var o in _offenders)
            {
                bool isCritical = o.hitsPerSecond > 30;
                GUI.color = isCritical ? Color.red : (o.hitsPerSecond > 0 ? Color.yellow : Color.white);
                GUILayout.BeginHorizontal("box");
                GUILayout.Label($"[{o.type}] {o.rect.name}", GUILayout.Width(200));
                GUILayout.Label($"PPS: {o.hitsPerSecond:F0}", GUILayout.Width(80));
                GUILayout.Label($"Total: {o.totalHits}", GUILayout.Width(80));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(10);
            GUILayout.Label("<b>QUICK FIX ADVICE:</b>", _headerStyle);
            if (_offenders.Count > 0 && _offenders[0].hitsPerSecond > 10)
            {
                var top = _offenders[0];
                GUI.color = Color.yellow;
                if (top.type == "Layout")
                    GUILayout.Label($"- Object '{top.rect.name}' is causing Layout Churn.\n  FIX: Add a 'Canvas' component to it to isolate rebuilds.", _tipStyle);
                else
                    GUILayout.Label($"- Object '{top.rect.name}' updates too fast.\n  FIX: Only update text/sprite when value actually changes.", _tipStyle);
            }
            else
            {
                GUILayout.Label("- UI Performance is looking good!", _tipStyle);
            }

            GUILayout.EndArea();
            GUI.matrix = oldMatrix;
            GUI.color = Color.white;

            // Vẽ khung thế giới
            foreach (var node in _nodes.Values)
            {
                if (node.rect == null || Time.time > node.lastRebuildTime + 0.5f) continue;
                DrawVisual(node);
            }
        }

        private void DrawVisual(TrackerNode node)
        {
            Vector3[] corners = new Vector3[4];
            node.rect.GetWorldCorners(corners);
            Canvas canvas = node.rect.GetComponentInParent<Canvas>();
            Camera cam = (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

            Vector2 min = Vector2.one * float.MaxValue;
            Vector2 max = Vector2.one * float.MinValue;
            foreach (var corner in corners)
            {
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, corner);
                min = Vector2.Min(min, screenPos);
                max = Vector2.Max(max, screenPos);
            }

            Rect rect = new Rect(min.x, Screen.height - max.y, max.x - min.x, max.y - min.y);
            GUI.color = node.type == "Layout" ? layoutColor : graphicColor;
            if (node.hitsPerSecond > 30) GUI.color = Color.red;
            
            GUI.Box(rect, "");
            GUI.Label(new Rect(rect.x, rect.y - 20, 300, 20), $"<b>{node.rect.name} ({node.type})</b>", _itemStyle);
            GUI.color = Color.white;
        }

        private void InitStyles()
        {
            _headerStyle = new GUIStyle { richText = true, fontSize = 16, fontStyle = FontStyle.Bold };
            _headerStyle.normal.textColor = Color.cyan;

            _itemStyle = new GUIStyle { richText = true, fontSize = 12 };
            _itemStyle.normal.textColor = Color.white;

            _tipStyle = new GUIStyle { richText = true, fontSize = 13, wordWrap = true };
            _tipStyle.normal.textColor = Color.white;
        }
    }
}
