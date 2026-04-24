using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace OSK
{
    [DefaultExecutionOrder(-101)]
    public class RootUI : MonoBehaviour
    {
        #region Queued View
        private class QueuedView
        {
            public View view;
            public object[] data;
            public bool hidePrevView;
            public Action<View> onOpened;
        }
        #endregion

        #region Lists & Cache

        [BoxGroup("🔍 Views")] [ShowInInspector, ReadOnly]
        public List<View> ListViewInit { get; set; } = new();

        [BoxGroup("🔍 Views")] [ShowInInspector, ReadOnly]
        public List<View> ListCacheView { get; set; } = new();

        public Stack<View> ListViewHistory { get; set; } = new();

        [ShowInInspector, ReadOnly]
        private List<QueuedView> _queuedViews = new();

        private bool _isProcessingQueue = false;

        private readonly Dictionary<Type, View> _cacheByType = new();
        private readonly Dictionary<Type, View> _initByType = new();

        #endregion

        #region References

        [Title("📌 References")]
        [Required, SerializeField] private Camera _uiCamera;
        [Required, SerializeField] private Canvas _canvas;
        [Required, SerializeField] private CanvasScaler _canvasScaler;
        [SerializeField] private Transform _viewContainer;

        #endregion

        #region Settings

        [Title("⚙️ Settings")]
        [SerializeField] private bool isPortrait = true;
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool isUpdateRatioScaler = true;
 
        #endregion

        #region Properties

        public Canvas Canvas => _canvas;
        public CanvasScaler CanvasScaler => _canvasScaler;
        public Camera UICamera => _uiCamera;
        public Transform ViewContainer => _viewContainer;
        public bool IsPortrait => isPortrait;
        public bool IsDirtySort { get; set; } = true;
        #endregion

        public void Initialize()
        {
            gameObject.name = " ========== [RootUI] ==========";
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            var data = Main.Instance.configInit.data;
            if (data.listViewS0 != null)
            {
                Preload();
            }
            if (isUpdateRatioScaler)
            {
                // check if the screen is in portrait mode
                Main.UI.SetupCanvasScaleForRatio();
            }
        }

        public void SetupCanvas()
        {
            _canvas.referencePixelsPerUnit = 100;
            _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            if (isPortrait)
            {
                _canvasScaler.referenceResolution = new Vector2(1080, 1920);
                _canvasScaler.matchWidthOrHeight = 0;
            }
            else
            {
                _canvasScaler.referenceResolution = new Vector2(1920, 1080);
                _canvasScaler.matchWidthOrHeight = 1;
            }

#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this))
            {
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(_canvas);
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(_canvasScaler);

                UnityEditor.EditorUtility.SetDirty(_canvas);
                UnityEditor.EditorUtility.SetDirty(_canvasScaler);
                UnityEditor.EditorUtility.SetDirty(gameObject);
                MyLogger.Log($"[SetupCanvas] IsPortrait: {isPortrait} => Saved to prefab instance");
            }
#endif
        }


        #region Init

        private void Preload()
        {
            var listUIPopupSo = Main.Instance.configInit.data.listViewS0.Views;
            if (listUIPopupSo == null)
            {
                MyLogger.LogError("[View] is null");
                return;
            }
 
            ListViewInit.Clear();
            _initByType.Clear();

            for (int i = 0; i < listUIPopupSo.Count; i++)
            {
                var entry = listUIPopupSo[i];
                if (entry.view == null)
                {
                    MyLogger.LogError($"[View] ListViewInit[{i}] is null");
                    continue;
                }
                ListViewInit.Add(entry.view);
                _initByType[entry.view.GetType()] = entry.view;
            }

            foreach (var view in ListViewInit)
            {
                if (view.isPreloadSpawn)
                {
                    SpawnViewCache(view);
                }
            }
        }

        #endregion

        #region Spawn

        public T Spawn<T>(T view, object[] data, bool hidePrevView) where T : View
        {
            return IsExist<T>() ? Open<T>(data, hidePrevView) : SpawnViewCache(view);
        }

        public T Spawn<T>(string path, object[] data, bool cache, bool hidePrevView) where T : View
        {
            if (IsExist<T>())
            {
                return Open<T>(data, hidePrevView);
            }

            var view = SpawnFromResource<T>(path);
            if (!cache) return view;

            // FIX: was inverted (only added when already contained)
            if (!ListCacheView.Contains(view))
            {
                ListCacheView.Add(view);
                _cacheByType[typeof(T)] = view;
            }

            return view;
        }

        public T SpawnViewCache<T>(T view) where T : View
        {
            var _view = Instantiate(view, _viewContainer);
            _view.gameObject.SetActive(false);
            _view.Initialize(this);

            _view.transform.localPosition = Vector3.zero;
            _view.transform.localScale = Vector3.one;

            MyLogger.Log($"[View] Spawn view: {_view.name}");
            if (!ListCacheView.Contains(_view))
            {
                ListCacheView.Add(_view);
                _cacheByType[_view.GetType()] = _view;
                IsDirtySort = true;
            }
            return _view;
        }

        public T SpawnAlert<T>(T view, bool usePool) where T : View
        {
            T _view = usePool ? Main.Pool.Spawn<T>(KEY_POOL.KEY_UI_ALERT, view, _viewContainer) : Instantiate(view, _viewContainer);
            _view.gameObject.SetActive(true);
            _view.Initialize(this);

            _view.transform.localPosition = Vector3.zero;
            _view.transform.localScale = Vector3.one;

            MyLogger.Log($"[View] Spawn Alert view: {_view.name}");
            return _view;
        }

        #endregion

        #region Open

        public View Open(View view, object[] data = null, bool hidePrevView = false, bool checkShowing = true)
        {
            var viewType = view.GetType();

            // O(1) cached lookup instead of LINQ FirstOrDefault
            _cacheByType.TryGetValue(viewType, out var _view);

            if (hidePrevView && ListViewHistory.Count > 0)
            {
                var prevView = ListViewHistory.Peek();
                prevView.Hide();
            }

            if (_view == null)
            {
                _initByType.TryGetValue(viewType, out var viewPrefab);
                if (viewPrefab == null)
                {
                    MyLogger.LogError($"[View] Can't find view prefab for type: {viewType.Name}");
                    return null;
                }

                _view = SpawnViewCache(viewPrefab);
            }

            if (_view.IsShowing && checkShowing)
            {
                MyLogger.Log($"[View] Opened view IsShowing: {_view.name}");
                return _view;
            }

            _view.Open(data);
            ListViewHistory.Push(_view);
            MyLogger.Log($"[View] Opened view: {_view.name}");
            return _view;
        }

        public T Open<T>(object[] data = null, bool hidePrevView = false, bool checkShowing = true) where T : View
        {
            var viewType = typeof(T);

            // O(1) cached lookup instead of LINQ FirstOrDefault
            _cacheByType.TryGetValue(viewType, out var cached);
            var _view = cached as T;

            if (hidePrevView && ListViewHistory.Count > 0)
            {
                var prevView = ListViewHistory.Peek();
                prevView.Hide();
            }

            if (_view == null)
            {
                _initByType.TryGetValue(viewType, out var initPrefab);
                var viewPrefab = initPrefab as T;
                if (viewPrefab == null)
                {
                    MyLogger.LogError($"[View] Can't find view prefab for type: {typeof(T).Name}");
                    return null;
                }

                _view = SpawnViewCache(viewPrefab);
            }

            if (_view.IsShowing && checkShowing)
            {
                MyLogger.Log($"[View] Opened view: {_view.name}");
                return _view;
            }

            _view.Open(data);
            ListViewHistory.Push(_view);
            MyLogger.Log($"[View] Opened view: {_view.name}");
            return _view;
        }

        public T TryOpen<T>(object[] data = null, bool hidePrevView = false) where T : View
        {
            return Open<T>(data, hidePrevView, false);
        }
        
        public void EnqueueView(View view, object[] data = null, bool hidePrevView = false)
        {
            _queuedViews.Add(new QueuedView
            {
                view = view,
                data = data,
                hidePrevView = hidePrevView
            });

            // Sort once on insert, not in coroutine loop
            _queuedViews.Sort((a, b) => b.view.Depth.CompareTo(a.view.Depth));

            if (!_isProcessingQueue)
            {
                StartCoroutine(ProcessQueue());
            }
        }
        
        public void EnqueueView<T>(object[] data = null, bool hidePrev = false, Action<T> onOpened = null) where T : View
        {
            // O(1) cached lookup
            _cacheByType.TryGetValue(typeof(T), out var cached);
            var _view = cached as T;

            if (_view == null)
            {
                _initByType.TryGetValue(typeof(T), out var initPrefab);
                var prefab = initPrefab as T;
                if (prefab == null)
                {
                    MyLogger.LogError($"[EnqueueView<{typeof(T).Name}>] Not found view prefab for type: {typeof(T).Name}");
                    return;
                }

                _view = SpawnViewCache(prefab);
            }

            var queued = new QueuedView
            {
                view = _view,
                data = data,
                hidePrevView = hidePrev,
                onOpened = v => onOpened?.Invoke(v as T)
            };

            _queuedViews.Add(queued);
            _queuedViews.Sort((a, b) => b.view.Priority.CompareTo(a.view.Priority));

            if (!_isProcessingQueue)
                StartCoroutine(ProcessQueue());
        }

        
        private IEnumerator ProcessQueue()
        {
            _isProcessingQueue = true;

            while (_queuedViews.Count > 0)
            {
                // Already sorted — find first non-showing view (no LINQ)
                QueuedView next = null;
                for (int i = 0; i < _queuedViews.Count; i++)
                {
                    var q = _queuedViews[i];
                    if (q.view != null && !q.view.IsShowing)
                    {
                        next = q;
                        break;
                    }
                }

                if (next == null)
                {
                    yield return null;
                    continue;
                }
                 
                var openedView = Open(next.view, next.data, next.hidePrevView);
                next.onOpened?.Invoke(openedView);

                // Wait until the view is closed
                yield return new WaitUntil(() => next.view == null || !next.view.IsShowing);
                _queuedViews.Remove(next);
            }

            _isProcessingQueue = false;
        }
        
        /// <summary>
        /// Open previous view in history
        /// </summary>
        public View OpenPrevious(object[] data = null, bool isHidePrevPopup = false)
        {
            if (ListViewHistory.Count <= 1)
            {
                MyLogger.LogWarning("[View] No previous view to open");
                return null;
            }

            // Pop current view
            var currentView = ListViewHistory.Pop();

            if (isHidePrevPopup && currentView != null && !currentView.Equals(null))
            {
                try
                {
                    currentView.Hide();
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"[View] Error hiding current view: {ex.Message}");
                }
            }

            // Peek previous view
            var previousView = ListViewHistory.Peek();
            if (previousView == null || previousView.Equals(null))
            {
                MyLogger.LogWarning("[View] Previous view is null or destroyed");
                return null;
            }

            previousView.Open(data);
            MyLogger.Log($"[View] Opened previous view: {previousView.name}");
            return previousView;
        }

        /// <summary>
        /// Spawn and open alert view, destroy it when closed
        /// </summary>
        public AlertView OpenAlert<T>(AlertSetup setup) where T : AlertView
        {
            // O(1) cached lookup
            _initByType.TryGetValue(typeof(T), out var initPrefab);
            var viewPrefab = initPrefab as T;
            if (viewPrefab == null)
            {
                MyLogger.LogError($"[View] Can't find view prefab for type: {typeof(T).Name}");
                return null;
            }

            var view = SpawnAlert(viewPrefab, setup.usePool);
            view.Open(new object[] { setup });
            MyLogger.Log($"[View] Opened view: {view.name}");
            return view;
        }

        #endregion

        #region Get

        public View Get(View view, bool isInitOnScene)
        {
            var _view = GetAll(isInitOnScene).Find(x => x == view);
            if (_view == null)
            {
                MyLogger.LogError($"[View] Can't find view: {view.name}");
                return null;
            }

            if (!_view.isInitOnScene)
            {
                MyLogger.LogError($"[View] {view.name} is not init on scene");
            }

            return _view;
        }

        public T Get<T>(bool isInitOnScene = true) where T : View
        {
            // O(1) cached lookup
            if (isInitOnScene && _cacheByType.TryGetValue(typeof(T), out var cached))
                return cached as T;

            var _view = GetAll(isInitOnScene)?.Find(x => x is T) as T;
            if (_view == null)
            {
                MyLogger.LogError($"[View] Can't find view: {typeof(T).Name}");
                return null;
            }

            if (!_view.isInitOnScene)
            {
                MyLogger.LogError($"[View] {typeof(T).Name} is not init on scene");
            }

            return _view;
        }

        public View Get(View view)
        {
            var _view = GetAll(true).Find(x => x == view);
            if (_view != null)
            {
                MyLogger.Log($"[View] Found view: {_view.name} is showing {_view.IsShowing}");
                return _view;
            }

            MyLogger.LogError($"[View] Can't find view: {view.name}");
            return null;
        }

        public List<View> GetAll(bool isInitOnScene)
        {
            if (isInitOnScene) // check if the view is already initialized
                return ListCacheView;

            var views = ListViewInit.FindAll(x => x.isInitOnScene);
            if (views.Count > 0)
            {
                MyLogger.Log($"[View] Found {views.Count} views");
                return views;
            }

            MyLogger.LogError($"[View] Can't find any view");
            return null;
        }

        #endregion

        #region Hide

        public void Hide(View view)
        {
            if (view == null || !ListCacheView.Contains(view))
            {
                MyLogger.LogError($"[View] Can't hide: invalid view");
                return;
            }

            if (!view.IsShowing)
            {
                MyLogger.Log($"[View] Can't hide: {view.name} is not showing");
                return;
            }

            try
            {
                view.Hide();
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"[View] Hide failed: {view.name} - {ex.Message}");
            }
        }

        public void HideIgnore<T>() where T : View
        {
            foreach (var view in ListCacheView.ToList())
            {
                if (view == null)
                {
                    MyLogger.Log($"[View] {nameof(view)} is null in HideIgnore");
                    continue;
                }

                if (view is T) continue;
                if (!view.IsShowing) continue;

                try
                {
                    view.Hide();
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"[View] Error hiding view {view.name}: {ex.Message}");
                }
            }
        }

        public void HideIgnore<T>(T[] viewsToKeep) where T : View
        {
            HashSet<T> keepSet = viewsToKeep != null  ? new HashSet<T>(viewsToKeep) : null;

            for (int i = ListCacheView.Count - 1; i >= 0; i--)
            {
                var view = ListCacheView[i];

                if (!view)
                {
                    MyLogger.Log("[View] Null found in HideIgnore -> removing");
                    ListCacheView.RemoveAt(i);
                    continue;
                }

                if (!view.IsShowing) continue;

                if (keepSet != null && view is T tView && keepSet.Contains(tView))
                    continue;

                try
                {
                    view.Hide();
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"[View] Error hiding {view.name}: {ex}");
                }
            }
        }


        public void HideAll()
        {
            for (int i = ListCacheView.Count - 1; i >= 0; i--)
            {
                var view = ListCacheView[i];

                if (!view)
                {
                    MyLogger.LogError("[View] Null found in HideAll -> removing");
                    ListCacheView.RemoveAt(i);
                    continue;
                }
                if (!view.IsShowing) continue;
                try
                {
                    view.Hide();
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"[View] Error hiding {view.name}: {ex}");
                }
            }
        }
        
        public void CleanNull()
        {
            for (int i = ListCacheView.Count - 1; i >= 0; i--)
                if (!ListCacheView[i])
                    ListCacheView.RemoveAt(i);
        }

        #endregion

        #region Remove

        public void Remove(View view)
        {
            if (view == null || ListViewHistory.Count == 0)
                return;

            if (ListViewHistory.Peek() == view)
            {
                ListViewHistory.Pop();
                Hide(view);
            }
            else
            {
                MyLogger.LogWarning($"[View] Can't remove {view.name}: not on top of history");
            }
        }

        public void Remove(bool hidePrevView = false)
        {
            if (ListViewHistory.Count <= 0)
                return;

            var curView = ListViewHistory.Pop();
            curView.Hide();

            if (hidePrevView)
                OpenPrevious();
        }

        public void RemoveAll()
        {
            while (ListViewHistory.Count > 0)
            {
                var curView = ListViewHistory.Pop();
                if (curView == null)
                {
                    MyLogger.LogWarning($"[View] {nameof(curView)} null view");
                    continue;
                }
                
                if (!curView.IsShowing) 
                    continue;

                try
                {
                    curView.Hide();
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"[View] Error hiding popped view: {ex.Message}");
                }
            }
        }

        #endregion

        #region Delete

        public void Delete<T>(T view, Action action = null) where T : View
        {
            if (!ListCacheView.Contains(view))
                return;

            MyLogger.Log($"[View] Delete view: {view.name}");
            ListCacheView.Remove(view);
            _cacheByType.Remove(view.GetType());
            IsDirtySort = true;
            action?.Invoke();

            // Nếu từ Pool thì trả về Pool, ngược lại mới Destroy
            if (Main.Pool.IsFromPool(view))
            {
                Main.Pool.Despawn(view);
            }
            else
            {
                Destroy(view.gameObject);
            }
        }

        #endregion

        #region Private

        private T SpawnFromResource<T>(string path) where T : View
        {
            var view = Instantiate(Resources.Load<T>(path), _viewContainer);
            if (view != null)
                return SpawnViewCache(view);
            MyLogger.LogError($"[View] Can't find popup with path: {path}");
            return null;
        }

        private bool IsExist<T>() where T : View
        {
            return _cacheByType.ContainsKey(typeof(T));
        }

        #endregion

        #region Debug

        public void LogAllViews()
        {
            MyLogger.Log($"[View] Total views: {ListCacheView.Count}");
            foreach (var view in ListCacheView)
            {
                MyLogger.Log($"[View] View: {view.name} - IsShowing: {view.IsShowing}");
            }

            MyLogger.Log($"[View] Total views: {ListViewInit.Count}");
            foreach (var view in ListViewInit)
            {
                MyLogger.Log($"[View] View: {view.name} - IsShowing: {view.IsShowing}");
            }

            MyLogger.Log($"[View] Total views: {ListViewHistory.Count}");
            foreach (var view in ListViewHistory)
            {
                MyLogger.Log($"[View] View: {view.name} - IsShowing: {view.IsShowing}");
            }
        }

        #endregion
    }
}