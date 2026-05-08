using UnityEngine;
using UnityEngine.UI;
using OSK;
using Sirenix.OdinInspector;

public class TutorialDemo : MonoBehaviour
{
    [SerializeField] private RectTransform _mailBtn;
    [SerializeField] private RectTransform _shopBtn;

    private void Start()
    {
        SetupButton("Mail", () => Debug.Log("<color=cyan>[TutorialDemo] MAIL CLICKED!</color>"));
        SetupButton("Shop", () => Debug.Log("<color=cyan>[TutorialDemo] SHOP CLICKED!</color>"));
    }

    private void SetupButton(string name, UnityEngine.Events.UnityAction action)
    {
        var obj = GameObject.Find(name);
        if (obj != null)
        {
            var btn = obj.GetComponent<Button>();
            if (btn == null) btn = obj.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
            if (name == "Mail") _mailBtn = obj.GetComponent<RectTransform>();
            if (name == "Shop") _shopBtn = obj.GetComponent<RectTransform>();
        }
    }
 
    [Button(ButtonSizes.Medium), GUIColor(1, 0.8f, 0)]
    private void TestOutBack()
    {
        TutorialData data = new TutorialData(_shopBtn);
        data.easeType = ETutorialEase.OutBack;
        data.duration = 0.5f;
        Main.UI.ShowTutorial(data);
    }
 
    [Button(ButtonSizes.Medium)]
    private void TestLinear()
    {
        TutorialData data = new TutorialData(_shopBtn);
        data.easeType = ETutorialEase.Linear;
        data.duration = 0.4f;
        Main.UI.ShowTutorial(data);
    }

    [Button(ButtonSizes.Medium), GUIColor(0.5f, 1, 0.5f)]
    private void TestSmoothDamp()
    {
        TutorialData data = new TutorialData(_mailBtn, _shopBtn);
        data.pointerTargetIndex = 1;
        data.easeType = ETutorialEase.SmoothDamp;
        data.duration = 0.3f; // Với SmoothDamp thì duration dùng để tính độ mượt
        Main.UI.ShowTutorial(data);
    }

    [Button(ButtonSizes.Medium), GUIColor(1, 0, 0)]
    private void HideTutorial() => Main.UI.HideTutorial();
}
