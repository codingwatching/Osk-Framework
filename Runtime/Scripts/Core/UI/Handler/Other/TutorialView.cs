using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace OSK
{
    public enum ETutorialEase
    {
        Instant,
        Linear,
        SmoothDamp,
        OutSine,
        OutBack
    }

    public class TutorialData
    {
        public RectTransform[] targets;
        public int pointerTargetIndex = 0;
        public Vector2 pointerOffset = new Vector2(50, -50);
        public bool showPointer = true;
        public ETutorialEase easeType = ETutorialEase.SmoothDamp;
        public float duration = 0.3f;

        public TutorialData(params RectTransform[] targets)
        {
            this.targets = targets;
        }
    }

    public class TutorialView : View, ICanvasRaycastFilter
    {
        [Header("References")]
        [SerializeField] private Image _maskImage;
        [SerializeField] private RectTransform _pointer; 
        
        [Header("Settings")]
        [SerializeField] private Vector2 _padding = new Vector2(20, 20);
        [SerializeField] private float _cornerRadius = 25f;
        [SerializeField] private float _showPointerThreshold = 2f;

        private Material _maskMat;
        private List<RectTransform> _targets = new List<RectTransform>();
        private RectTransform _myRectTransform;

        private ETutorialEase _currentEase = ETutorialEase.SmoothDamp;
        private float _duration = 0.3f;
        private int _pointerIndex = 0;
        private Vector2 _currentPointerOffset = new Vector2(50, -50);
        private bool _shouldShowPointer = true;
        private bool _isFirstOpen = true;

        // Persistent arrays to avoid GC allocations in Update
        private Vector2[] _startCenters = new Vector2[8];
        private Vector2[] _startSizes = new Vector2[8];
        private Vector2[] _currentCenters = new Vector2[8];
        private Vector2[] _currentSizes = new Vector2[8];
        private Vector2[] _velCenters = new Vector2[8];
        private Vector2[] _velSizes = new Vector2[8];
        
        // Shader cached arrays
        private Vector4[] _shaderCenters = new Vector4[8];
        private Vector4[] _shaderSizes = new Vector4[8];
        private float[] _shaderRadii = new float[8];

        private float _animTimer = 0f;

        protected override void OnInit()
        {
            viewType = EViewType.Overlay;
            _myRectTransform = GetComponent<RectTransform>();
            if (_maskImage != null)
            {
                _maskMat = Instantiate(_maskImage.material);
                _maskImage.material = _maskMat;
            }
            if (_pointer != null) _pointer.gameObject.SetActive(false);
            _isFirstOpen = true;
        }

        protected override void SetData(object[] data = null)
        {
            if (data == null || data.Length == 0) return;
            if (data[0] is TutorialData tutorialData) ApplyTutorialData(tutorialData);
        }

        private void Update()
        {
            if (_targets.Count == 0 || _maskMat == null) return;

            int count = Mathf.Min(_targets.Count, 8);
            _animTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_animTimer / _duration);
            bool targetReached = t >= 1f;

            for (int i = 0; i < count; i++)
            {
                var target = _targets[i];
                if (target == null) continue;

                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, target.position);
                Vector2 localPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_myRectTransform, screenPos, null, out localPos);

                Vector2 targetCenter = localPos;
                Vector2 targetSize = new Vector2(target.rect.width + _padding.x, target.rect.height + _padding.y);

                if (_currentEase == ETutorialEase.SmoothDamp)
                {
                    _currentCenters[i] = Vector2.SmoothDamp(_currentCenters[i], targetCenter, ref _velCenters[i], _duration * 0.5f);
                    _currentSizes[i] = Vector2.SmoothDamp(_currentSizes[i], targetSize, ref _velSizes[i], _duration * 0.5f);
                    targetReached = Vector2.Distance(_currentCenters[i], targetCenter) < _showPointerThreshold;
                }
                else if (_currentEase == ETutorialEase.Instant)
                {
                    _currentCenters[i] = targetCenter;
                    _currentSizes[i] = targetSize;
                    targetReached = true;
                }
                else
                {
                    float easedT = EvaluateEase(_currentEase, t);
                    _currentCenters[i] = Vector2.LerpUnclamped(_startCenters[i], targetCenter, easedT);
                    _currentSizes[i] = Vector2.LerpUnclamped(_startSizes[i], targetSize, easedT);
                }

                // Fill persistent arrays for shader
                _shaderCenters[i].x = _currentCenters[i].x;
                _shaderCenters[i].y = _currentCenters[i].y;
                _shaderSizes[i].x = _currentSizes[i].x;
                _shaderSizes[i].y = _currentSizes[i].y;
                _shaderRadii[i] = _cornerRadius;
            }

            // Push to GPU
            _maskMat.SetVectorArray("_HoleCenters", _shaderCenters);
            _maskMat.SetVectorArray("_HoleSizes", _shaderSizes);
            _maskMat.SetFloatArray("_HoleRadii", _shaderRadii);
            _maskMat.SetInt("_HoleCount", count);

            if (_pointer != null)
            {
                if (_shouldShowPointer && targetReached && _pointerIndex < count)
                {
                    if (!_pointer.gameObject.activeSelf) _pointer.gameObject.SetActive(true);
                    _pointer.anchoredPosition = _currentCenters[_pointerIndex] + _currentPointerOffset;
                }
                else if (_pointer.gameObject.activeSelf) _pointer.gameObject.SetActive(false);
            }
        }

        public void ApplyTutorialData(TutorialData data)
        {
            for(int i=0; i<8; i++)
            {
                _startCenters[i] = _currentCenters[i];
                _startSizes[i] = _currentSizes[i];
                _velCenters[i] = Vector2.zero;
                _velSizes[i] = Vector2.zero;
            }

            _targets.Clear();
            if (data.targets != null) _targets.AddRange(data.targets);
            
            _currentEase = data.easeType;
            _duration = data.duration;
            _pointerIndex = data.pointerTargetIndex;
            _currentPointerOffset = data.pointerOffset;
            _shouldShowPointer = data.showPointer;
            _animTimer = 0f;

            if (_pointer != null) _pointer.gameObject.SetActive(false);

            if (_isFirstOpen)
            {
                _currentEase = ETutorialEase.Instant;
                _isFirstOpen = false;
            }
        }

        private float EvaluateEase(ETutorialEase ease, float t)
        {
            switch (ease)
            {
                case ETutorialEase.Linear: return t;
                case ETutorialEase.OutSine: return Mathf.Sin(t * Mathf.PI * 0.5f);
                case ETutorialEase.OutBack:
                    float s = 1.70158f;
                    t -= 1.0f;
                    return t * t * ((s + 1) * t + s) + 1.0f;
                default: return t;
            }
        }

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (_targets.Count == 0) return true;
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_myRectTransform, sp, eventCamera, out localPos);
            for (int i = 0; i < Mathf.Min(_targets.Count, 8); i++)
            {
                float halfW = _currentSizes[i].x * 0.5f;
                float halfH = _currentSizes[i].y * 0.5f;
                if (localPos.x >= _currentCenters[i].x - halfW && localPos.x <= _currentCenters[i].x + halfW &&
                    localPos.y >= _currentCenters[i].y - halfH && localPos.y <= _currentCenters[i].y + halfH) return false; 
            }
            return true;
        }
    }
}
