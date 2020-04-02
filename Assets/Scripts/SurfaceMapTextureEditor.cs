using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SurfaceMapTexture))]
[CanEditMultipleObjects]
public class SurfaceMapTextureEditor : Editor
{
    SurfaceMapTexture m_instance;
    SerializedProperty m_display;
    SerializedProperty drawNegative;
    SerializedProperty z;
    GameObject m_surface;

    private void OnEnable()
    {
        m_instance = (SurfaceMapTexture)target;
        m_surface = GameObject.Find("CelestialBody");
        m_display = serializedObject.FindProperty("display");
        drawNegative = serializedObject.FindProperty("drawNegative");
        z = serializedObject.FindProperty("z");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(m_display);
        EditorGUILayout.PropertyField(drawNegative);
        z.intValue = EditorGUILayout.IntSlider("z", z.intValue, 0, m_surface.GetComponent<Surface>().m_surface_res - 1);
        serializedObject.ApplyModifiedProperties();
    }
}
