using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Stone))]
public class StoneEditor : Editor
{
    public int m_bisectCount = 1;

    public override void OnInspectorGUI()
    {
        Stone stone = (Stone)target;

        m_bisectCount = EditorGUILayout.IntField("Bisect count", m_bisectCount);
        if (GUILayout.Button("Generate new stone", GUILayout.Height(30f)))
        {
            stone.CleanUp();
            stone.BuildHull();
            stone.RandomBisects(m_bisectCount);
        }

        if (GUILayout.Button("Add one bisect", GUILayout.Height(30f)))
        {
            
        }
    }
}
