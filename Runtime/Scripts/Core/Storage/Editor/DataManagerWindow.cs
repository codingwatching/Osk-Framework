#if  UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;
 
namespace OSK
{
    public class DataManagerWindow : EditorWindow
    {
        private enum StorageDirectory
        {
            PersistentData,
            StreamingAssets,
            DataPath,
            TemporaryCache,
            Custom
        }

        private enum FileFilter
        {
            All,
            Json,
            Xml,
            Binary
        }

        // --- STATE ---
        private StorageDirectory _selectedDirectoryType = StorageDirectory.PersistentData;
        private FileFilter _currentFilter = FileFilter.All;

        private string _customPath = ""; // Lưu đường dẫn khi chọn Custom
        private string _selectedFilePath;
        private string _fileContentBuffer;
        private string _decryptedContentBuffer;
        private bool _viewDecrypted = false;
 
        private DateTime _lastFileWriteTime;
        private DataManager _dataManager;
        private DataManager Data => _dataManager;
        
        // Scroll states
        private Vector2 _scrollPosLeft;
        private Vector2 _scrollPosRight;
        private bool _isEditing = false;

        // --- PATH LOGIC ---
        private string CurrentSavePath
        {
            get
            {
                switch (_selectedDirectoryType)
                {
                    case StorageDirectory.PersistentData: return Application.persistentDataPath;
                    case StorageDirectory.StreamingAssets: return Application.streamingAssetsPath;
                    case StorageDirectory.DataPath: return Application.dataPath;
                    case StorageDirectory.TemporaryCache: return Application.temporaryCachePath;
                    case StorageDirectory.Custom: return _customPath;
                    default: return Application.persistentDataPath;
                }
            }
        }

        [MenuItem("OSK-Framework/Storage/Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<DataManagerWindow>("Window");
            window.minSize = new Vector2(800, 500);
            window.Show();
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(_customPath)) 
                _customPath = Application.persistentDataPath;
                
            RefreshDataManager();
        }

        private void OnFocus()
        {
            CheckForExternalChanges();
            RefreshDataManager();
        }

        private void Update()
        {
            // Optional: periodically check even if focused
            if (EditorApplication.timeSinceStartup % 2 < 0.05f) 
            {
                CheckForExternalChanges();
            }
        }

        private void RefreshDataManager()
        {
            if (_dataManager == null && Application.isPlaying)
            {
                _dataManager = FindObjectOfType<DataManager>();
            }
        }

        private void CheckForExternalChanges()
        {
            if (string.IsNullOrEmpty(_selectedFilePath) || !File.Exists(_selectedFilePath) || _isEditing) return;

            var currentWriteTime = File.GetLastWriteTime(_selectedFilePath);
            if (currentWriteTime > _lastFileWriteTime)
            {
                _lastFileWriteTime = currentWriteTime;
                // If viewing decrypted, refresh that too
                if (_viewDecrypted)
                {
                    RefreshDecryptedContent();
                }
                else
                {
                    _fileContentBuffer = File.ReadAllText(_selectedFilePath);
                }
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.BeginHorizontal();
            DrawSidebar();
            DrawContentArea();
            EditorGUILayout.EndHorizontal();
        }

        // --- 1. TOOLBAR ---
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Filter
            GUILayout.Label("Filter:", GUILayout.Width(40));
            _currentFilter =
                (FileFilter)EditorGUILayout.EnumPopup(_currentFilter, EditorStyles.toolbarPopup, GUILayout.Width(70));

            GUILayout.Space(10);

            // Encrypt Toggle
            if (_dataManager != null)
            {
                Color originalColor = GUI.backgroundColor;
                GUI.backgroundColor = _dataManager.isEncrypt ? new Color(0.5f, 1f, 0.5f) : originalColor;
                if (GUILayout.Button(_dataManager.isEncrypt ? "Encrypt: ON" : "Encrypt: OFF",
                        EditorStyles.toolbarButton, GUILayout.Width(90)))
                {
                    _dataManager.isEncrypt = !_dataManager.isEncrypt;
                }

                GUI.backgroundColor = originalColor;
            }

            GUILayout.FlexibleSpace();

            // Refresh Button
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                _selectedFilePath = null;
                _fileContentBuffer = "";
                GUI.FocusControl(null);
                AssetDatabase.Refresh();
            }

            EditorGUILayout.EndHorizontal();
        }

        // --- 2. SIDEBAR (DIRECTORY SELECTOR) ---
        private void DrawSidebar()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(300), GUILayout.ExpandHeight(true));

            
            // === [PHẦN CHỌN ĐƯỜNG DẪN MỚI] ===
            EditorGUILayout.LabelField("Storage Location", EditorStyles.boldLabel);

            // 1. Enum Popup
            _selectedDirectoryType = (StorageDirectory)EditorGUILayout.EnumPopup("Type:", _selectedDirectoryType);

            // 2. Hiển thị đường dẫn (Editable nếu là Custom, Readonly nếu là Enum khác)
            EditorGUILayout.BeginHorizontal();
            if (_selectedDirectoryType == StorageDirectory.Custom)
            {
                _customPath = EditorGUILayout.TextField(_customPath);
                if (GUILayout.Button("...", GUILayout.Width(25)))
                {
                    string path = EditorUtility.OpenFolderPanel("Select Save Folder", _customPath, "");
                    if (!string.IsNullOrEmpty(path)) _customPath = path;
                }
            }
            else
            {
                // Hiển thị dạng Textfield readonly để copy được
                EditorGUILayout.TextField(CurrentSavePath);
                if (GUILayout.Button("Open", GUILayout.Width(45)))
                {
                    if (Directory.Exists(CurrentSavePath)) EditorUtility.RevealInFinder(CurrentSavePath);
                }
            }

            EditorGUILayout.EndHorizontal();
            // ==================================

            DrawSeparator(Color.gray);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Files List", EditorStyles.boldLabel);
            GUI.color = new Color(1f, 0.7f, 0.7f);
            if (GUILayout.Button("Delete All", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                DeleteAllFiles();
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            // Danh sách file
            _scrollPosLeft = EditorGUILayout.BeginScrollView(_scrollPosLeft);
            string[] files = GetFilteredFiles();

            if (files.Length == 0)
            {
                EditorGUILayout.HelpBox("No files found.", MessageType.Info);
            }
            else
            {
                foreach (string filePath in files)
                {
                    DrawFileItem(filePath);
                }
            }

            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            DrawPlayerPrefsSection();
            EditorGUILayout.EndVertical();
        }

        private void DrawFileItem(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            if (_selectedFilePath == filePath) GUI.backgroundColor = Color.cyan;

            EditorGUILayout.BeginHorizontal();

            // Icon Logic
            bool isJson = _currentFilter == FileFilter.Json || fileName.EndsWith(".json");
            var iconName = isJson ? "TextAsset Icon" : "DefaultAsset Icon";
            var icon = EditorGUIUtility.IconContent(iconName);

            if (GUILayout.Button(icon, GUILayout.Width(25), GUILayout.Height(20))) SelectFile(filePath);
            if (GUILayout.Button(fileName, EditorStyles.label, GUILayout.Height(20))) SelectFile(filePath);

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
        }

        // --- 3. CONTENT AREA ---
        private void DrawContentArea()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));

            if (string.IsNullOrEmpty(_selectedFilePath) || !File.Exists(_selectedFilePath))
            {
                EditorGUILayout.LabelField("Select a file to view content.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Path.GetFileName(_selectedFilePath), EditorStyles.boldLabel);

                // JSON Tools
                // JSON Tools
                if (_selectedFilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);
                    if (GUILayout.Button("Format", EditorStyles.miniButton, GUILayout.Width(60))) FormatJson(true);
                    if (GUILayout.Button("1-Line", EditorStyles.miniButton, GUILayout.Width(60))) FormatJson(false);
                    GUI.backgroundColor = Color.white;
                    GUILayout.Space(10);
                }

                // Decrypt Tool
                Color btnColor = _viewDecrypted ? Color.cyan : Color.white;
                GUI.backgroundColor = btnColor;
                if (GUILayout.Button(_viewDecrypted ? "View Encrypted" : "Decrypt View", EditorStyles.miniButton, GUILayout.Width(100)))
                {
                    _viewDecrypted = !_viewDecrypted;
                    if (_viewDecrypted) RefreshDecryptedContent();
                }
                GUI.backgroundColor = Color.white;
                GUILayout.Space(5);

                if (GUILayout.Button("Delete", GUILayout.Width(60))) DeleteSelectedFile();
                EditorGUILayout.EndHorizontal();

                DrawSeparator(Color.black);

                // Content Editor
                _scrollPosRight = EditorGUILayout.BeginScrollView(_scrollPosRight); 

                EditorGUI.BeginChangeCheck();
                string displayContent = _viewDecrypted ? _decryptedContentBuffer : _fileContentBuffer;
                
                GUI.enabled = !_viewDecrypted; // Disable editing if viewing decrypted
                string newContent = EditorGUILayout.TextArea(displayContent, GUILayout.ExpandHeight(true));
                GUI.enabled = true;

                if (!_viewDecrypted && EditorGUI.EndChangeCheck())
                {
                    _fileContentBuffer = newContent;
                    _isEditing = true;
                }

                EditorGUILayout.EndScrollView();

                // Save Button
                if (_isEditing)
                {
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("Save Changes", GUILayout.Height(30))) SaveChanges();
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    if (GUILayout.Button("Copy to Clipboard", GUILayout.Height(30)))
                    {
                        EditorGUIUtility.systemCopyBuffer = _fileContentBuffer;
                        ShowNotification(new GUIContent("Copied!"));
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        // --- LOGIC FUNCTIONS ---
        private string[] GetFilteredFiles()
        {
            string path = CurrentSavePath;
            if (!Directory.Exists(path) && _selectedDirectoryType != StorageDirectory.Custom) return new string[0];
            if (!Directory.Exists(path)) return new string[0];

            return Directory.GetFiles(path).Where(f =>
            {
                string ext = Path.GetExtension(f).ToLower();
                switch (_currentFilter)
                {
                    case FileFilter.Json: return ext.Contains("json");
                    case FileFilter.Xml: return ext.Contains("xml");
                    case FileFilter.Binary: return !ext.Contains("json") && !ext.Contains("xml");
                    default: return true;
                }
            }).ToArray();
        }

        private void SelectFile(string path)
        {
            if (File.Exists(path))
            {
                _selectedFilePath = path;
                _fileContentBuffer = File.ReadAllText(path);
                _lastFileWriteTime = File.GetLastWriteTime(path);
                _isEditing = false;
                _viewDecrypted = false;
                GUI.FocusControl(null);
            }
        }

        public override void SaveChanges()
        {
            try
            {
                File.WriteAllText(_selectedFilePath, _fileContentBuffer);
                _lastFileWriteTime = File.GetLastWriteTime(_selectedFilePath);
                _isEditing = false;
                ShowNotification(new GUIContent("Saved!"));
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        private void DeleteSelectedFile()
        {
            if (EditorUtility.DisplayDialog("Delete", $"Delete {Path.GetFileName(_selectedFilePath)}?", "Yes", "No"))
            {
                File.Delete(_selectedFilePath);
                _selectedFilePath = null;
                _fileContentBuffer = "";
                _viewDecrypted = false;
            }
        }

        private void DeleteAllFiles()
        {
            string[] files = GetFilteredFiles();
            if (files.Length == 0) return;

            if (EditorUtility.DisplayDialog("Delete All", $"Delete ALL {files.Length} files in this directory?\nThis cannot be undone.", "Delete All", "Cancel"))
            {
                foreach (string f in files)
                {
                    try { File.Delete(f); } catch { }
                }
                _selectedFilePath = null;
                _fileContentBuffer = "";
                _viewDecrypted = false;
                AssetDatabase.Refresh();
            }
        }

        private void RefreshDecryptedContent()
        {
            if (string.IsNullOrEmpty(_selectedFilePath) || !File.Exists(_selectedFilePath)) return;

            try
            {
                byte[] rawBytes = File.ReadAllBytes(_selectedFilePath);
                byte[] finalBytes = rawBytes.DecryptSmart(IOUtility.encryptKey);

                if (finalBytes != null)
                {
                    _decryptedContentBuffer = Encoding.UTF8.GetString(finalBytes);
                }
                else
                {
                    _decryptedContentBuffer = "❌ [Decryption Failed]";
                }
            }
            catch (Exception ex)
            {
                _decryptedContentBuffer = $"❌ [Error]: {ex.Message}";
            }
        }

        private void FormatJson(bool pretty)
        {
            try
            {
                if (_viewDecrypted)
                {
                    _decryptedContentBuffer = JsonHelper.FormatJson(_decryptedContentBuffer, pretty);
                }
                else
                {
                    _fileContentBuffer = JsonHelper.FormatJson(_fileContentBuffer, pretty);
                    _isEditing = true;
                }
                GUI.FocusControl(null);
            }
            catch { }
        }

        private void DrawPlayerPrefsSection()
        {
            DrawSeparator(Color.gray);
            EditorGUILayout.LabelField("PlayerPrefs", EditorStyles.boldLabel);
            if (GUILayout.Button("Open PlayerPrefs"))
            {
#if CustomPlayerPref
                CustomPlayerPref.PlayerPrefsEditor.PlayerPrefsEditor.Init();
#else
               Application.OpenURL("https://github.com/O-S-K/EditorTools/tree/main/Editor/Packages/PlayerPrefEditor");
#endif
            }

            if (GUILayout.Button("Delete All PlayerPrefs"))
            {
                if (EditorUtility.DisplayDialog("Warning", "Delete ALL PlayerPrefs?", "Yes", "Cancel"))
                    PlayerPrefs.DeleteAll();
            }
        }

        private void DrawSeparator(Color color)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, color);
        }
    }

    // --- JSON HELPER ---
    public static class JsonHelper
    {
        public static string FormatJson(string json, bool pretty)
        {
            if (string.IsNullOrEmpty(json)) return string.Empty;
            if (!pretty) return json.Replace("\r", "").Replace("\n", "").Replace("\t", "").Trim();
            var indent = 0;
            var quoted = false;
            var sb = new StringBuilder();
            var cleanJson = json.Replace("\r", "").Replace("\n", "").Replace("\t", "");
            for (var i = 0; i < cleanJson.Length; i++)
            {
                var ch = cleanJson[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, ++indent).ToList().ForEach(item => sb.Append("\t"));
                        }

                        break;
                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, --indent).ToList().ForEach(item => sb.Append("\t"));
                        }

                        sb.Append(ch);
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        var index = i;
                        while (index > 0 && cleanJson[--index] == '\\') escaped = !escaped;
                        if (!escaped) quoted = !quoted;
                        break;
                    case ',':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, indent).ToList().ForEach(item => sb.Append("\t"));
                        }

                        break;
                    case ':':
                        sb.Append(ch);
                        if (!quoted) sb.Append(" ");
                        break;
                    default: sb.Append(ch); break;
                }
            }

            return sb.ToString();
        }
    }
}
#endif