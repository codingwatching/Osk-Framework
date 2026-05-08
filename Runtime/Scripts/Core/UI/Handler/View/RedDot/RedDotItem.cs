using UnityEngine;

namespace OSK
{
    public class RedDotItem : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private GameObject _dotObject;

        private bool _isSubscribed = false;

        private void OnEnable()
        {
            TrySubscribe();
            Refresh();
        }

        private void Start()
        {
            TrySubscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void TrySubscribe()
        {
            if (_isSubscribed) return;
            
            if (RedDotManager.Instance != null)
            {
                RedDotManager.Instance.OnChanged += OnStateChanged;
                _isSubscribed = true;
                // Debug.Log($"[RedDotItem] {_key} subscribed successfully.");
            }
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed) return;

            if (RedDotManager.Instance != null)
            {
                RedDotManager.Instance.OnChanged -= OnStateChanged;
            }
            _isSubscribed = false;
        }

        private void OnStateChanged(string changedKey, bool state)
        { 
            Refresh();
        }

        public void Refresh()
        {
            if (RedDotManager.Instance != null && !string.IsNullOrEmpty(_key))
            {
                bool state = RedDotManager.Instance.GetStatus(_key);
                if (_dotObject != null)
                {
                    _dotObject.SetActive(state);
                }
            }
        }
    }
}
