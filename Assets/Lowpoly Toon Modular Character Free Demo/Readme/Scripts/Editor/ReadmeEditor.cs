using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Readme))]
public class ReadmeEditor : Editor
{
    private GUIStyle _headingStyle;
    private GUIStyle _bodyStyle;

    public override void OnInspectorGUI()
    {
        Readme readme = (Readme)target;
        EnsureStyles();

        if (!string.IsNullOrEmpty(readme.title))
        {
            EditorGUILayout.LabelField(readme.title, _headingStyle);
        }

        foreach (Readme.Section section in readme.sections)
        {
            if (!string.IsNullOrEmpty(section.heading))
            {
                EditorGUILayout.LabelField(section.heading, _headingStyle);
            }

            if (!string.IsNullOrEmpty(section.text))
            {
                EditorGUILayout.LabelField(section.text, _bodyStyle);
            }

            if (!string.IsNullOrEmpty(section.linkText) && GUILayout.Button(section.linkText))
            {
                Application.OpenURL(section.url);
            }

            EditorGUILayout.Space(8f);
        }
    }

    private void EnsureStyles()
    {
        if (_bodyStyle != null)
        {
            return;
        }

        _bodyStyle = new GUIStyle(EditorStyles.label)
        {
            wordWrap = true,
            fontSize = 12
        };

        _headingStyle = new GUIStyle(_bodyStyle)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 14
        };
    }
}
