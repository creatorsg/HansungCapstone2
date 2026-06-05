using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FontChanger : MonoBehaviour
{
    [SerializeField] public TMP_FontAsset targetFont;

#if UNITY_EDITOR
    [ContextMenu("Change All Fonts in Scene")]
    public void ChangeAllFonts()
    {
        if (targetFont == null)
        {
            Debug.LogError("변경할 폰트 에셋을 지정해주세요.");
            return;
        }

        // 3D TextMeshPro 변경
        TextMeshPro[] text3Ds = GameObject.FindObjectsOfType<TextMeshPro>(true);
        foreach (TextMeshPro text in text3Ds)
        {
            text.font = targetFont;
        }

        // UI TextMeshProUGUI 변경
        TextMeshProUGUI[] textUIs = GameObject.FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in textUIs)
        {
            text.font = targetFont;
        }

        Debug.Log("씬 내의 모든 TMP 폰트가 교체되었습니다.");
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(FontChanger))]
public class FontChangerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FontChanger changer = (FontChanger)target;

        if (GUILayout.Button("Change Font!"))
        {
            changer.ChangeAllFonts();
        }
    }
}
#endif