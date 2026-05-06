#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace OSK
{
    public class SoundManagerWindow : EditorWindow
    {
        private ListSoundSO listSoundSo;
        private SoundData newSoundDraft = null;

        private bool showTable = true;
        private Dictionary<string, Dictionary<SoundType, bool>> soundTypeFoldoutsPerGroup = new Dictionary<string, Dictionary<SoundType, bool>>();
        private Dictionary<string, bool> groupFoldouts = new Dictionary<string, bool>();

        // Track which rows have their detail panel expanded
        private readonly HashSet<SoundData> _expandedRows = new HashSet<SoundData>();

        // --- library selection
        private string selectedGroup = null;

        // --- layout settings (change to taste)
        private const float LeftSidebarWidth = 260f;
        private const float RightPanelMinWidth = 700f; // MIN width for right panel (Option C)
        private Vector2 leftScroll;
        private Vector2 rightScroll;
        private string searchText = "";

        // Spatial blend label cache
        private static readonly string[] SpatialLabels = { "2D", "3D" };

        // Type badge colors
        private static readonly Dictionary<SoundType, Color> TypeColors = new Dictionary<SoundType, Color>
        {
            { SoundType.MUSIC, new Color(0.3f, 0.6f, 1f) },
            { SoundType.SFX, new Color(0.3f, 0.85f, 0.4f) },
            { SoundType.AMBIENCE, new Color(1f, 0.7f, 0.2f) },
            { SoundType.VOICE, new Color(0.75f, 0.45f, 1f) },
        };
        private static GUIStyle _badgeStyle;
        private static GUIStyle _clipLabelStyle;

        [MenuItem("OSK-Framework/Sound/Window")]
        public static void ShowWindow()
        {
            var w = GetWindow<SoundManagerWindow>("Window");
            w.minSize = new Vector2(LeftSidebarWidth + 400, 360);
        }

        private void OnEnable()
        {
            // Try to auto-find the first ListSoundSO in the project
            if (listSoundSo == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:ListSoundSO");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    listSoundSo = AssetDatabase.LoadAssetAtPath<ListSoundSO>(path);
                }
            }

            selectedGroup = null;
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            listSoundSo = (ListSoundSO)EditorGUILayout.ObjectField("ListSoundSO", listSoundSo, typeof(ListSoundSO), false);
            if (EditorGUI.EndChangeCheck())
            {
                soundTypeFoldoutsPerGroup.Clear();
                groupFoldouts.Clear();
            }

            if (listSoundSo == null)
            {
                EditorGUILayout.HelpBox("No ListSoundSO assigned. Drag one here or ensure a ListSoundSO exists in project (auto-loads first found).", MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(LeftSidebarWidth));
            GUILayout.Space(4);
            leftScroll = EditorGUILayout.BeginScrollView(leftScroll, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(LeftSidebarWidth), GUILayout.ExpandHeight(true));
            DrawLibrarySidebarContents();
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();

            // compute right panel width from window size but respect minimum
            float padding = 16f;
            float computedRight = Mathf.Max(RightPanelMinWidth, position.width - LeftSidebarWidth - padding);
            // if window is smaller than Left+MinRight, computedRight will equal MinRight and overall window will scroll horizontally in the editor
            GUILayout.BeginVertical(GUILayout.Width(computedRight));
            rightScroll = EditorGUILayout.BeginScrollView(rightScroll, GUILayout.Width(computedRight), GUILayout.ExpandHeight(true));

            // main right content
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            if (listSoundSo != null)
            {
                EditorGUILayout.Space(6);
                if (listSoundSo.showSoundSettings)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Sort To Type", GUILayout.Width(120)))
                        SortToType(listSoundSo);
                    if (GUILayout.Button("Set ID For Name Clip", GUILayout.Width(160)))
                        SetIDForNameClip();
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox("Please enable sound in scene game to test play sound.", MessageType.Warning);

                showTable = EditorGUILayout.Foldout(showTable, selectedGroup == null ? "Show Sound Info Table (All Groups)" : $"Show Sound Info Table (Group: {selectedGroup})", true, EditorStyles.foldoutHeader);
                if (showTable)
                {
                    EditorGUILayout.Space(6);
                    // showGroupNames = EditorGUILayout.Foldout(showGroupNames, "Show Group Names", true);
                    // if (showGroupNames)
                    //     DrawGroupNames();

                    if (listSoundSo.groupNames == null || listSoundSo.groupNames.Count == 0)
                        listSoundSo.groupNames = new List<string>() { "Default" };

                    // --- MIXER CONFIGURATION SECTION ---
                    EditorGUILayout.BeginVertical("box");
                    listSoundSo.showMixerSettings = EditorGUILayout.Foldout(listSoundSo.showMixerSettings, "Mixer Configuration", true, EditorStyles.foldoutHeader);
                    if (listSoundSo.showMixerSettings)
                    {
                        EditorGUI.indentLevel++;

                        EditorGUILayout.BeginHorizontal();
                        listSoundSo.mainMixer = (AudioMixer)EditorGUILayout.ObjectField("Main Mixer", listSoundSo.mainMixer, typeof(AudioMixer), false, GUILayout.Width(400));
                        if (listSoundSo.mainMixer != null)
                        {
                            GUI.color = Color.cyan;
                            if (GUILayout.Button("Sync Groups", GUILayout.Width(100)))
                            {
                                Undo.RecordObject(listSoundSo, "Sync Mixer Groups");
                                // Lấy tất cả các group trong mixer
                                var allGroups = listSoundSo.mainMixer.FindMatchingGroups("");
                                listSoundSo.availableMixerGroups = allGroups.ToList();
                                EditorUtility.SetDirty(listSoundSo);
                                AssetDatabase.SaveAssets();
                                MyLogger.Log($"Has synced {allGroups.Length} mixer groups from main mixer.");
                                selectedGroup = null;
                            }

                            GUI.color = Color.white;
                        }
                        EditorGUILayout.EndHorizontal();

                        if (listSoundSo.mainMixer == null)
                        {
                            EditorGUILayout.HelpBox("Vui lòng kéo Audio Mixer vào để sử dụng tính năng đồng bộ.", MessageType.Info);
                        }

                        // Hiển thị danh sách để chỉnh sửa thủ công nếu cần
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("Available Mixer Groups", EditorStyles.boldLabel);

                        for (int i = 0; i < listSoundSo.availableMixerGroups.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            listSoundSo.availableMixerGroups[i] = (AudioMixerGroup)EditorGUILayout.ObjectField($"Group {i}:", listSoundSo.availableMixerGroups[i], typeof(AudioMixerGroup), false, GUILayout.Width(500));

                            if (GUILayout.Button("X", GUILayout.Width(25)))
                            {
                                listSoundSo.availableMixerGroups.RemoveAt(i);
                                break;
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        if (GUILayout.Button("+ Add Mixer Group", GUILayout.Width(150)))
                        {
                            listSoundSo.availableMixerGroups.Add(null);
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.Space(10);

                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    GUILayout.Label("🔍 Search:", GUILayout.Width(100));
                    searchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
                    if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20))) searchText = "";
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(5);

                    var filteredMasterList = listSoundSo.ListSoundInfos.Where(s =>
                        (string.IsNullOrEmpty(selectedGroup) || s.group == selectedGroup) &&
                        (string.IsNullOrEmpty(searchText) || s.id.ToLower().Contains(searchText.ToLower()))
                    ).ToList();

                    var activeGroups = filteredMasterList
                        .Select(x => string.IsNullOrEmpty(x.group) ? "Default" : x.group)
                        .Distinct()
                        .OrderBy(x => x)
                        .ToList();

                    // ── Flat sound list (BroAudio style) ──
                    foreach (var group in activeGroups)
                    {
                        var soundsInGroup = filteredMasterList.Where(s =>
                            (string.IsNullOrEmpty(s.group) ? "Default" : s.group) == group
                        ).ToList();

                        if (soundsInGroup.Count == 0) continue;

                        if (!groupFoldouts.ContainsKey(group)) groupFoldouts[group] = true;
                        groupFoldouts[group] = EditorGUILayout.Foldout(groupFoldouts[group], $"  {group}", true, EditorStyles.boldLabel);

                        if (groupFoldouts[group])
                        {
                            for (int i = 0; i < soundsInGroup.Count; i++)
                                DrawSoundEntry(soundsInGroup[i]);
                        }
                        GUILayout.Space(4);
                    }

                    // ── Bottom Add Button ──
                    GUILayout.Space(8);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+ Add New Sound", GUILayout.Width(140), GUILayout.Height(24)))
                    {
                        newSoundDraft = new SoundData
                        {
                            audioClip = null,
                            id = "New Sound",
                            group = string.IsNullOrEmpty(selectedGroup) ? (listSoundSo.groupNames.Count > 0 ? listSoundSo.groupNames[0] : "Default") : selectedGroup,
                            type = SoundType.SFX,
                            volume = 1f,
                            mixerGroup = null,
                            pitch = new MinMaxFloat(1f, 1f),
                            loop = false,
                            priority = 128,
                            spatialBlend = 0f,
                            minDistance = 1,
                            maxDistance = 500,
                            playbackMode = PlaybackMode.Single,
                            clips = new List<AudioClip>()
                        };
                    }
                    GUILayout.Space(4);
                    EditorGUILayout.EndHorizontal();

                    if (newSoundDraft != null)
                        DrawNewSoundDraft();

                    EditorGUILayout.Space(10);
                    DrawLoadFolderSection();
                    DrawEnumGenSection();
                }
            }

            
            EditorGUILayout.EndVertical();   
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();         
            EditorGUILayout.EndHorizontal();
        }

        private void EnsureStyles()
        {
            if (_badgeStyle == null)
            {
                _badgeStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 9,
                    normal = { textColor = Color.white, background = Texture2D.whiteTexture },
                    padding = new RectOffset(4, 4, 1, 1),
                    margin = new RectOffset(2, 2, 2, 2)
                };
            }
            if (_clipLabelStyle == null)
            {
                _clipLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleLeft,
                };
            }
        }

        private void DrawSoundEntry(SoundData soundData)
        {
            EnsureStyles();
            bool isExpanded = _expandedRows.Contains(soundData);

            // ── Row ──
            Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(24));
            EditorGUI.DrawRect(rowRect, isExpanded ? new Color(0.28f, 0.28f, 0.35f, 0.6f) : new Color(0f, 0f, 0f, 0.15f));

            GUILayout.Label("≡", EditorStyles.miniLabel, GUILayout.Width(14), GUILayout.Height(22));

            string arrow = isExpanded ? "▼" : "▶";
            if (GUILayout.Button(arrow, new GUIStyle(EditorStyles.miniLabel) { fontSize = 10, alignment = TextAnchor.MiddleCenter }, GUILayout.Width(16), GUILayout.Height(22)))
            {
                if (isExpanded) _expandedRows.Remove(soundData);
                else _expandedRows.Add(soundData);
            }

            EditorGUI.BeginChangeCheck();
            GUIStyle idEditStyle = new GUIStyle(EditorStyles.textField) { 
                fontStyle = FontStyle.Bold, 
                fixedHeight = 20,
                margin = new RectOffset(0,0,2,0)
            };
            string newId = EditorGUILayout.TextField(soundData.id, idEditStyle, GUILayout.Width(200), GUILayout.Height(20));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(listSoundSo, "Rename Sound");
                soundData.id = newId;
                EditorUtility.SetDirty(listSoundSo);
            }
            GUILayout.FlexibleSpace();

            // Group badge
            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            int currentGroupIdx = listSoundSo.groupNames.IndexOf(soundData.group);
            if (currentGroupIdx < 0) currentGroupIdx = 0;
            
            EditorGUI.BeginChangeCheck();
            currentGroupIdx = EditorGUILayout.Popup(currentGroupIdx, listSoundSo.groupNames.ToArray(), _badgeStyle, GUILayout.Width(70), GUILayout.Height(18));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(listSoundSo, "Change Sound Group");
                soundData.group = listSoundSo.groupNames[currentGroupIdx];
                EditorUtility.SetDirty(listSoundSo);
            }

            // Type badge (Clickable to change)
            Color badgeColor = TypeColors.ContainsKey(soundData.type) ? TypeColors[soundData.type] : Color.gray;
            GUI.backgroundColor = badgeColor;
            
            EditorGUI.BeginChangeCheck();
            soundData.type = (SoundType)EditorGUILayout.EnumPopup(soundData.type, _badgeStyle, GUILayout.Width(80), GUILayout.Height(18));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(listSoundSo, "Change Sound Type");
                EditorUtility.SetDirty(listSoundSo);
            }
            GUI.backgroundColor = Color.white;

            // Remove button (X)
            GUILayout.Space(4);
            if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22), GUILayout.Height(18)))
            {
                if (EditorUtility.DisplayDialog("Delete", $"Remove '{soundData.id}'?", "Yes", "No"))
                {
                    Undo.RecordObject(listSoundSo, "Remove Sound");
                    listSoundSo.ListSoundInfos.Remove(soundData);
                    _expandedRows.Remove(soundData);
                    EditorUtility.SetDirty(listSoundSo);
                    GUIUtility.ExitGUI();
                }
            }
            GUILayout.Space(4);

            EditorGUILayout.EndHorizontal();

            Rect lineRect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(lineRect, new Color(0.15f, 0.15f, 0.15f));

            if (isExpanded)
                DrawExpandedPanel(soundData);
        }

        private void DrawExpandedPanel(SoundData soundData)
        {
            Rect panelRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(panelRect, new Color(0.2f, 0.2f, 0.22f, 0.8f));
            GUILayout.Space(4);
            float lw = 120f, fw = 260f, ind = 16f;

            EditorGUI.BeginChangeCheck();

            // ID (call name)
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("ID (Call Name)", GUILayout.Width(lw));
            soundData.id = EditorGUILayout.TextField(soundData.id, GUILayout.Width(fw));
            EditorGUILayout.EndHorizontal();

            // Clip (only visible if Single mode)
            if (soundData.playbackMode == PlaybackMode.Single)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(ind); GUILayout.Label("Audio Clip", GUILayout.Width(lw));
                soundData.audioClip = (AudioClip)EditorGUILayout.ObjectField(soundData.audioClip, typeof(AudioClip), false, GUILayout.Width(fw));
                if (string.IsNullOrEmpty(soundData.id)) soundData.UpdateId();
                EditorGUILayout.EndHorizontal();
            }

            // Playback Mode
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("Playback Mode", GUILayout.Width(lw));
            soundData.playbackMode = (PlaybackMode)EditorGUILayout.EnumPopup(soundData.playbackMode, GUILayout.Width(fw));
            EditorGUILayout.EndHorizontal();

            // Clips list (only visible in Sequence/Random mode)
            if (soundData.playbackMode != PlaybackMode.Single)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(ind); GUILayout.Label("Clips", EditorStyles.boldLabel, GUILayout.Width(lw));
                GUILayout.Label($"({soundData.clips.Count})", EditorStyles.miniLabel, GUILayout.Width(30));
                if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20), GUILayout.Height(16)))
                    soundData.clips.Add(null);
                if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(20), GUILayout.Height(16)))
                {
                    if (soundData.clips.Count > 0)
                        soundData.clips.RemoveAt(soundData.clips.Count - 1);
                }
                EditorGUILayout.EndHorizontal();

                for (int ci = 0; ci < soundData.clips.Count; ci++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(ind + 16);
                    GUILayout.Label("▶", EditorStyles.miniLabel, GUILayout.Width(12));
                    soundData.clips[ci] = (AudioClip)EditorGUILayout.ObjectField(soundData.clips[ci], typeof(AudioClip), false, GUILayout.Width(fw - 40));
                    if (soundData.clips[ci] != null && GUILayout.Button("♫", EditorStyles.miniButton, GUILayout.Width(22), GUILayout.Height(16)))
                        EditorAudioHelper.PlayClip(soundData.clips[ci]);
                    EditorGUILayout.EndHorizontal();
                }
                GUILayout.Space(2);
            }

            // Type
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("Type", GUILayout.Width(lw));
            soundData.type = (SoundType)EditorGUILayout.EnumPopup(soundData.type, GUILayout.Width(fw));
            EditorGUILayout.EndHorizontal();

            // Group
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("Group", GUILayout.Width(lw));
            int gi = Mathf.Max(0, listSoundSo.groupNames.IndexOf(soundData.group ?? "Default"));
            gi = EditorGUILayout.Popup(gi, listSoundSo.groupNames.ToArray(), GUILayout.Width(fw));
            soundData.group = listSoundSo.groupNames[gi];
            EditorGUILayout.EndHorizontal();

            // Mixer
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("Mixer Group", GUILayout.Width(lw));
            var mn = listSoundSo.availableMixerGroups.Select(g => g != null ? g.name : "—").ToArray();
            if (mn.Length == 0) mn = new[] { "—" };
            int mi = Mathf.Max(0, listSoundSo.availableMixerGroups.IndexOf(soundData.mixerGroup));
            int nm = EditorGUILayout.Popup(mi, mn, GUILayout.Width(fw));
            if (nm != mi && nm < listSoundSo.availableMixerGroups.Count)
            { Undo.RecordObject(listSoundSo, "Change Mixer"); soundData.mixerGroup = listSoundSo.availableMixerGroups[nm]; EditorUtility.SetDirty(listSoundSo); }
            EditorGUILayout.EndHorizontal();

            DrawPanelSeparator();

            // Volume
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("Master Volume", GUILayout.Width(lw));
            soundData.volume = EditorGUILayout.Slider(soundData.volume, 0f, 1f, GUILayout.Width(fw));
            EditorGUILayout.EndHorizontal();

            // Priority
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("Priority", GUILayout.Width(lw));
            soundData.priority = EditorGUILayout.IntSlider(soundData.priority, 0, 256, GUILayout.Width(fw));
            EditorGUILayout.EndHorizontal();

            // Looping
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("Looping", GUILayout.Width(lw));
            soundData.loop = EditorGUILayout.Toggle("Loop", soundData.loop, GUILayout.Width(fw));
            EditorGUILayout.EndHorizontal();

            // Pitch
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("Pitch Range", GUILayout.Width(lw));
            float pMin = soundData.pitch.min, pMax = soundData.pitch.max;
            pMin = EditorGUILayout.FloatField(pMin, GUILayout.Width(40));
            EditorGUILayout.MinMaxSlider(ref pMin, ref pMax, 0.1f, 2f, GUILayout.Width(160));
            pMax = EditorGUILayout.FloatField(pMax, GUILayout.Width(40));
            pMin = Mathf.Clamp(Mathf.Round(pMin * 10f) / 10f, 0.1f, pMax);
            pMax = Mathf.Clamp(Mathf.Round(pMax * 10f) / 10f, pMin, 2f);
            if (Mathf.Abs(pMin - soundData.pitch.min) > 0.01f || Mathf.Abs(pMax - soundData.pitch.max) > 0.01f)
                soundData.pitch = new MinMaxFloat(pMin, pMax);
            EditorGUILayout.EndHorizontal();

            DrawPanelSeparator();

            // Spatial
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("Spatial (3D Sound)", GUILayout.Width(lw));
            soundData.spatialBlend = EditorGUILayout.Slider(soundData.spatialBlend, 0f, 1f, GUILayout.Width(200));
            string dl = soundData.spatialBlend <= 0.01f ? "2D" : soundData.spatialBlend >= 0.99f ? "3D" : "Mix";
            GUILayout.Label(dl, EditorStyles.miniLabel, GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledGroupScope(soundData.spatialBlend <= 0f))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(ind); GUILayout.Label("Min Distance", GUILayout.Width(lw));
                soundData.minDistance = EditorGUILayout.IntField(soundData.minDistance, GUILayout.Width(60));
                GUILayout.Space(20); GUILayout.Label("Max", GUILayout.Width(30));
                soundData.maxDistance = EditorGUILayout.IntField(soundData.maxDistance, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }

            DrawPanelSeparator();

            // Play / Stop / Remove
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind);
            if (GUILayout.Button("▶ Play", GUILayout.Width(80), GUILayout.Height(20))) soundData.Play(soundData.pitch);
            if (GUILayout.Button("■ Stop", GUILayout.Width(80), GUILayout.Height(20))) soundData.Stop();
            GUILayout.FlexibleSpace();
            GUI.color = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Remove", GUILayout.Width(70), GUILayout.Height(20)))
            {
                if (EditorUtility.DisplayDialog("Delete", $"Remove '{soundData.id}'?", "Yes", "No"))
                { Undo.RecordObject(listSoundSo, "Remove Sound"); listSoundSo.ListSoundInfos.Remove(soundData); _expandedRows.Remove(soundData); EditorUtility.SetDirty(listSoundSo); }
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(listSoundSo, "Modify Sound");
                EditorUtility.SetDirty(listSoundSo);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPanelSeparator()
        {
            GUILayout.Space(3);
            Rect s = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(s, new Color(0.35f, 0.35f, 0.35f));
            GUILayout.Space(2);
        }

        private void DrawLibrarySidebarContents()
        {
            EditorGUILayout.LabelField("Library Groups", EditorStyles.boldLabel);

            var groupsInData = listSoundSo != null
                ? listSoundSo.ListSoundInfos.Select(x => string.IsNullOrEmpty(x.group) ? "Default" : x.group).Distinct().OrderBy(x => x).ToList()
                : new List<string>();

            var combined = new List<string>(listSoundSo.groupNames);
            foreach (var g in groupsInData)
                if (!combined.Contains(g))
                    combined.Add(g);

            for (int i = 0; i < combined.Count; i++)
            {
                string g = combined[i];
                EditorGUILayout.BeginHorizontal();

                GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
                btnStyle.alignment = TextAnchor.MiddleLeft;

                bool isSelected = (g == selectedGroup);

                if (isSelected)
                {
                    GUI.backgroundColor = Color.Lerp(Color.green, Color.black, 0.6f);
                }

                if (GUILayout.Button(g, btnStyle, GUILayout.Height(26), GUILayout.ExpandWidth(true)))
                {
                    if (isSelected)
                        selectedGroup = null;
                    else
                        selectedGroup = g;
                }

                GUI.backgroundColor = Color.white; // Reset màu sau khi vẽ nút
                if (listSoundSo.groupNames.Contains(g))
                {
                    if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(22)))
                    {
                        if (EditorUtility.DisplayDialog("Remove Group", $"Remove group '{g}' from list?", "Remove", "Cancel"))
                        {
                            Undo.RecordObject(listSoundSo, "Remove Group");
                            listSoundSo.groupNames.Remove(g);
                            if (selectedGroup == g) selectedGroup = null;
                            EditorUtility.SetDirty(listSoundSo);
                            GUIUtility.ExitGUI(); // Force immediate refresh
                        }
                    }

                    if (GUILayout.Button("✎", GUILayout.Width(22), GUILayout.Height(22)))
                    {
                        string oldName = g;
                        AddGroupWindow.ShowPopup((string enteredName) =>
                        {
                            RenameGroup(oldName, enteredName);
                        }, oldName, "Rename Group", "Rename");
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Group", GUILayout.Height(24)))
            {
                AddGroupWindow.ShowPopup((string enteredName) =>
                {
                    string name = enteredName.Trim();
                    if (string.IsNullOrEmpty(name)) return;
                    if (listSoundSo.groupNames.Any(g => string.Equals(g, name, StringComparison.OrdinalIgnoreCase)))
                    {
                        EditorUtility.DisplayDialog("Group exists", $"Group '{name}' already exists.", "OK");
                        return;
                    }

                    listSoundSo.groupNames.Add(name);
                    selectedGroup = name;
                    EditorUtility.SetDirty(listSoundSo); // optional
                }, "NewGroup");
            }

            if (GUILayout.Button("Clear Selection", GUILayout.Height(24)))
            {
                selectedGroup = null;
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);
            using (new EditorGUI.DisabledScope(selectedGroup == null || listSoundSo == null))
            {
                if (GUILayout.Button("Add Selected Clips to Group", GUILayout.Height(28)))
                {
                    AddSelectedClipsToGroup(selectedGroup);
                }
            }

            if (GUILayout.Button("Open Clip editor", GUILayout.Height(28)))
            {
                OSKProjectMenu.OpenInEditor(true);
            }

            GUILayout.FlexibleSpace();
        }

        private void RenameGroup(string oldName, string newName)
        {
            if (string.IsNullOrEmpty(newName) || oldName == newName) return;
            if (listSoundSo.groupNames.Contains(newName))
            {
                EditorUtility.DisplayDialog("Group exists", $"Group '{newName}' already exists.", "OK");
                return;
            }

            Undo.RecordObject(listSoundSo, "Rename Group");

            // Update the list of names
            int idx = listSoundSo.groupNames.IndexOf(oldName);
            if (idx >= 0) listSoundSo.groupNames[idx] = newName;

            // Update all sounds that use this group
            foreach (var sound in listSoundSo.ListSoundInfos)
            {
                if (sound.group == oldName || (string.IsNullOrEmpty(sound.group) && oldName == "Default"))
                    sound.group = newName;
            }

            if (selectedGroup == oldName) selectedGroup = newName;

            EditorUtility.SetDirty(listSoundSo);
        }

        #region Draw Helpers

        private void DrawRowBorder()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, Color.gray);
        }

        private void DrawGroupNames()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎵 Group Names", EditorStyles.boldLabel);
            for (int i = 0; i < listSoundSo.groupNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                listSoundSo.groupNames[i] = EditorGUILayout.TextField(listSoundSo.groupNames[i]);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    listSoundSo.groupNames.RemoveAt(i);
                    i--;
                    continue;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Group"))
                listSoundSo.groupNames.Add("NewGroup");
            EditorGUILayout.EndVertical();
        }

        private void DrawNewSoundDraft()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("➕ New Sound Draft", EditorStyles.boldLabel);
            GUILayout.Space(4);

            float lw = 100; // Label width
            float fw = 280; // Field width
            float ind = 10; // Indentation

            // ID / Name
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("ID (Auto)", GUILayout.Width(lw));
            string previewId = newSoundDraft.audioClip != null ? newSoundDraft.audioClip.name : "—";
            EditorGUILayout.LabelField(previewId, EditorStyles.textField, GUILayout.Width(fw));
            EditorGUILayout.EndHorizontal();

            // Playback Mode
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("Playback Mode", GUILayout.Width(lw));
            newSoundDraft.playbackMode = (PlaybackMode)EditorGUILayout.EnumPopup(newSoundDraft.playbackMode, GUILayout.Width(fw));
            EditorGUILayout.EndHorizontal();

            // Clip (only visible if Single mode)
            if (newSoundDraft.playbackMode == PlaybackMode.Single)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(ind); GUILayout.Label("Audio Clip", GUILayout.Width(lw));
                newSoundDraft.audioClip = (AudioClip)EditorGUILayout.ObjectField(newSoundDraft.audioClip, typeof(AudioClip), false, GUILayout.Width(fw));
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // Clips list (Sequence/Random)
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(ind); GUILayout.Label("Clips", EditorStyles.boldLabel, GUILayout.Width(lw));
                GUILayout.Label($"({newSoundDraft.clips.Count})", EditorStyles.miniLabel, GUILayout.Width(30));
                if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20), GUILayout.Height(16)))
                    newSoundDraft.clips.Add(null);
                if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(20), GUILayout.Height(16)))
                {
                    if (newSoundDraft.clips.Count > 0)
                        newSoundDraft.clips.RemoveAt(newSoundDraft.clips.Count - 1);
                }
                EditorGUILayout.EndHorizontal();

                for (int ci = 0; ci < newSoundDraft.clips.Count; ci++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(ind + 16);
                    GUILayout.Label("▶", EditorStyles.miniLabel, GUILayout.Width(12));
                    newSoundDraft.clips[ci] = (AudioClip)EditorGUILayout.ObjectField(newSoundDraft.clips[ci], typeof(AudioClip), false, GUILayout.Width(fw - 20));
                    EditorGUILayout.EndHorizontal();
                }
                if (newSoundDraft.audioClip == null && newSoundDraft.clips.Count > 0) 
                    newSoundDraft.audioClip = newSoundDraft.clips[0];
            }

            // Type
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("Type", GUILayout.Width(lw));
            newSoundDraft.type = (SoundType)EditorGUILayout.EnumPopup(newSoundDraft.type, GUILayout.Width(fw));
            EditorGUILayout.EndHorizontal();

            // Group
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("Group", GUILayout.Width(lw));
            int gi = Mathf.Max(0, listSoundSo.groupNames.IndexOf(newSoundDraft.group ?? (selectedGroup ?? "Default")));
            gi = EditorGUILayout.Popup(gi, listSoundSo.groupNames.ToArray(), GUILayout.Width(fw));
            newSoundDraft.group = listSoundSo.groupNames[gi];
            EditorGUILayout.EndHorizontal();

            // Mixer
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("Mixer Group", GUILayout.Width(lw));
            var mn = listSoundSo.availableMixerGroups.Select(g => g != null ? g.name : "—").ToArray();
            if (mn.Length == 0) mn = new[] { "—" };
            int mi = Mathf.Max(0, listSoundSo.availableMixerGroups.IndexOf(newSoundDraft.mixerGroup));
            int nm = EditorGUILayout.Popup(mi, mn, GUILayout.Width(fw));
            if (nm >= 0 && nm < listSoundSo.availableMixerGroups.Count) newSoundDraft.mixerGroup = listSoundSo.availableMixerGroups[nm];
            EditorGUILayout.EndHorizontal();

            DrawPanelSeparator();

            // Volume
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("Master Volume", GUILayout.Width(lw));
            newSoundDraft.volume = EditorGUILayout.Slider(newSoundDraft.volume, 0f, 1f, GUILayout.Width(fw));
            EditorGUILayout.EndHorizontal();

            // Priority
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("Priority", GUILayout.Width(lw));
            newSoundDraft.priority = EditorGUILayout.IntSlider(newSoundDraft.priority, 0, 256, GUILayout.Width(fw));
            EditorGUILayout.EndHorizontal();

            // Looping
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("Looping", GUILayout.Width(lw));
            newSoundDraft.loop = EditorGUILayout.Toggle(newSoundDraft.loop, GUILayout.Width(fw));
            EditorGUILayout.EndHorizontal();

            // Pitch
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("Pitch Range", GUILayout.Width(lw));
            float pMin = newSoundDraft.pitch.min, pMax = newSoundDraft.pitch.max;
            pMin = EditorGUILayout.FloatField(pMin, GUILayout.Width(40));
            EditorGUILayout.MinMaxSlider(ref pMin, ref pMax, 0.1f, 2f, GUILayout.Width(fw - 90));
            pMax = EditorGUILayout.FloatField(pMax, GUILayout.Width(40));
            newSoundDraft.pitch = new MinMaxFloat(pMin, pMax);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Spatial Settings", EditorStyles.boldLabel);
            
            // Spatial Blend
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind); GUILayout.Label("Spatial Blend", GUILayout.Width(lw));
            newSoundDraft.spatialBlend = EditorGUILayout.Slider(newSoundDraft.spatialBlend, 0f, 1f, GUILayout.Width(fw));
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledGroupScope(newSoundDraft.spatialBlend <= 0f))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(ind); GUILayout.Label("Min Distance", GUILayout.Width(lw));
                newSoundDraft.minDistance = EditorGUILayout.IntField(newSoundDraft.minDistance, GUILayout.Width(fw));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(ind); GUILayout.Label("Max Distance", GUILayout.Width(lw));
                newSoundDraft.maxDistance = EditorGUILayout.IntField(newSoundDraft.maxDistance, GUILayout.Width(fw));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ind);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Confirm Add", GUILayout.Height(24), GUILayout.Width(120)))
            {
                if (newSoundDraft.audioClip != null && listSoundSo != null)
                {
                    Undo.RecordObject(listSoundSo, "Add SoundData");
                    newSoundDraft.id = newSoundDraft.audioClip.name;
                    listSoundSo.ListSoundInfos.Add(newSoundDraft);
                    newSoundDraft = null;
                    EditorUtility.SetDirty(listSoundSo);
                }
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Cancel", GUILayout.Height(24), GUILayout.Width(80)))
                newSoundDraft = null;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }


        #endregion

        #region Load / Enum / Utilities

        private void DrawLoadFolderSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("📁 Batch Import", EditorStyles.boldLabel);

            if (GUILayout.Button("Load Folder Sounds", GUILayout.Width(150)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
                if (string.IsNullOrEmpty(path)) return;

                // Chuyển đổi đường dẫn tuyệt đối sang đường dẫn Assets của Unity
                path = "Assets" + path.Replace(Application.dataPath, "").Replace("\\", "/");

                var exts = new[] { ".wav", ".mp3", ".ogg" };
                var clips = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                    .Where(f => exts.Contains(Path.GetExtension(f).ToLower()))
                    .Select(f => AssetDatabase.LoadAssetAtPath<AudioClip>(f.Replace("\\", "/")))
                    .Where(c => c != null).ToList();

                Undo.RecordObject(listSoundSo, "Add Sounds From Folder");

                // Xác định group sẽ gán cho các clip mới (ưu tiên group đang chọn ở sidebar)
                string targetGroup = !string.IsNullOrEmpty(selectedGroup) ? selectedGroup : "Default";

                foreach (var clip in clips)
                {
                    // Kiểm tra trùng lặp để không add lại clip đã có
                    if (!listSoundSo.ListSoundInfos.Any(s => s.audioClip == clip))
                    {
                        listSoundSo.ListSoundInfos.Add(new SoundData
                        {
                            audioClip = clip,
                            id = clip.name,
                            group = targetGroup, // FIX: Gán vào group đang chọn thay vì Default
                            type = SoundType.SFX,
                            volume = 1f,
                            pitch = new MinMaxFloat(1f, 1f)
                        });
                    }
                }

                selectedGroup = null;
                EditorUtility.SetDirty(listSoundSo);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                MyLogger.Log($"Added {clips.Count} clips to group: {targetGroup}");
            }
        }

        private void DrawEnumGenSection()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Gen enum SoundID", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Open File", GUILayout.Width(120)))
            {
                string absPath = EditorUtility.OpenFilePanel("Select File (in Project)", Application.dataPath, "cs");
                if (!string.IsNullOrEmpty(absPath))
                {
                    if (absPath.StartsWith(Application.dataPath))
                    {
                        string relPath = "Assets" + absPath.Replace(Application.dataPath, "");
                        listSoundSo.filePathSoundID = relPath.Replace("\\", "/");
                        EditorUtility.SetDirty(listSoundSo);
                    }
                    else
                    {
                        string savePath = EditorUtility.SaveFilePanel("Save SoundID.cs to Project", Application.dataPath, "SoundID", "cs");
                        if (!string.IsNullOrEmpty(savePath) && savePath.StartsWith(Application.dataPath))
                        {
                            string relSave = "Assets" + savePath.Replace(Application.dataPath, "");
                            listSoundSo.filePathSoundID = relSave.Replace("\\", "/");
                            EditorUtility.SetDirty(listSoundSo);
                        }
                    }
                }
            }

            if (GUILayout.Button("Open In Windows", GUILayout.Width(140)))
            {
                if (!string.IsNullOrEmpty(listSoundSo.filePathSoundID))
                {
                    string rel = listSoundSo.filePathSoundID.Replace("/", Path.DirectorySeparatorChar.ToString());
                    string abs = Path.Combine(Application.dataPath, rel.Substring("Assets".Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                    if (File.Exists(abs))
                    {
                        EditorUtility.RevealInFinder(abs);
                        try
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = abs,
                                UseShellExecute = true
                            };
                            Process.Start(psi);
                        }
                        catch (Exception ex)
                        {
                            EditorUtility.DisplayDialog("Cannot open file", $"Failed to open file with default app: {ex.Message}", "OK");
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("File not found", $"Could not find file at '{listSoundSo.filePathSoundID}'.", "OK");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("No file selected", "Please select or save a file first.", "OK");
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("File Path:", GUILayout.Width(70));
            EditorGUILayout.LabelField(string.IsNullOrEmpty(listSoundSo.filePathSoundID) ? "<Not set>" : listSoundSo.filePathSoundID);

            EditorGUILayout.Space(6);

            if (GUILayout.Button("Generate Enum ID"))
            {
                var names = listSoundSo.ListSoundInfos
                    .Where(x => x.audioClip != null)
                    .Select(x => x.id)
                    .Distinct()
                    .ToList();

                string filePath = listSoundSo.filePathSoundID;

                if (string.IsNullOrEmpty(filePath))
                {
                    string savePath = EditorUtility.SaveFilePanel("Save SoundID.cs", Application.dataPath, "SoundID", "cs");
                    if (string.IsNullOrEmpty(savePath)) return;
                    if (!savePath.StartsWith(Application.dataPath))
                    {
                        EditorUtility.DisplayDialog("Invalid location", "Please save the file inside the project's Assets folder.", "OK");
                        return;
                    }

                    filePath = "Assets" + savePath.Replace(Application.dataPath, "");
                    listSoundSo.filePathSoundID = filePath.Replace("\\", "/");
                    EditorUtility.SetDirty(listSoundSo);
                }

                var sb = new StringBuilder();
                sb.AppendLine("// Auto-generated SoundID enum");
                sb.AppendLine("public enum SoundID");
                sb.AppendLine("{");
                foreach (var n in names)
                {
                    string safe = MakeSafeEnumName(n);
                    sb.AppendLine($"    {safe},");
                }

                sb.AppendLine("}");

                string absWritePath = Path.Combine(Application.dataPath, filePath.Substring("Assets".Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                File.WriteAllText(absWritePath, sb.ToString(), Encoding.UTF8);

                AssetDatabase.Refresh();
            }
        }

        private void SortToType(ListSoundSO so) =>
            so.ListSoundInfos.Sort((a, b) => a.type.CompareTo(b.type));

        private void SetIDForNameClip()
        {
            foreach (var sound in listSoundSo.ListSoundInfos)
                if (sound.audioClip != null)
                    sound.id = sound.audioClip.name;
            EditorUtility.SetDirty(listSoundSo);
        }

        private void AddSelectedClipsToGroup(string group)
        {
            if (listSoundSo == null || string.IsNullOrEmpty(group)) return;
            var selectedClips = Selection.GetFiltered<AudioClip>(SelectionMode.Assets);
            if (selectedClips == null || selectedClips.Length == 0)
            {
                EditorUtility.DisplayDialog("No clips selected", "Please select one or more AudioClips in the Project window.", "OK");
                return;
            }

            int added = 0;
            foreach (var clip in selectedClips)
            {
                if (!listSoundSo.ListSoundInfos.Any(s => s.audioClip == clip))
                {
                    Undo.RecordObject(listSoundSo, "Add Selected Clips");
                    listSoundSo.ListSoundInfos.Add(new SoundData
                    {
                        audioClip = clip,
                        id = clip.name,
                        group = group,
                        type = SoundType.SFX,
                        volume = 1f,
                        pitch = new MinMaxFloat(1f, 1f)
                    });
                    added++;
                }
            }

            if (added > 0)
            {
                EditorUtility.SetDirty(listSoundSo);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog("Add Selected Clips", $"Added {added} clip(s) to group '{group}'.", "OK");
        }

        // safe enum name helper
        private static string MakeSafeEnumName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "_UNKNOWN";
            var sb = new StringBuilder();
            foreach (char c in s)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
                else if (char.IsWhiteSpace(c) || c == '-' || c == '.')
                    sb.Append('_');
            }

            string outStr = sb.ToString();
            if (string.IsNullOrEmpty(outStr)) outStr = "_UNKNOWN";
            if (!char.IsLetter(outStr[0]) && outStr[0] != '_') outStr = "_" + outStr;
            return outStr;
        }

        #endregion
    }

    public class AddGroupWindow : EditorWindow
    {
        private string newName = "NewGroup";
        private Action<string> onCreate;
        private string _title = "Add Group";
        private string _btnLabel = "Create";

        public static void ShowPopup(Action<string> onCreate, string defaultName = "NewGroup", string title = "Add Group", string btnLabel = "Create")
        {
            var win = CreateInstance<AddGroupWindow>();
            win.titleContent = new GUIContent(title);
            win._title = title;
            win._btnLabel = btnLabel;
            win.newName = defaultName;
            win.onCreate = onCreate;
            win.minSize = new Vector2(360, 80);
            win.maxSize = new Vector2(360, 80);
            win.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(_title, EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            EditorGUI.BeginChangeCheck();
            newName = EditorGUILayout.TextField("Group Name", newName);
            if (EditorGUI.EndChangeCheck())
            {
                newName = newName.Trim();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(_btnLabel, GUILayout.Height(26)))
            {
                string safe = newName.Trim();
                if (string.IsNullOrEmpty(safe))
                {
                    EditorUtility.DisplayDialog("Invalid Name", "Please enter a valid group name.", "OK");
                    return;
                }

                onCreate?.Invoke(safe);
                Close();
            }

            if (GUILayout.Button("Cancel", GUILayout.Height(26)))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif