using UnityEditor;

[CustomEditor(typeof(SurfaceMapTexture))]
[CanEditMultipleObjects]
public class SurfaceMapTextureEditor : Editor
{
    SurfaceMapTexture instance;
    SerializedProperty display;
    SerializedProperty z;

    int z_range;

    private void OnEnable()
    {
        instance = (SurfaceMapTexture)target;
        display = serializedObject.FindProperty("display");
        z = serializedObject.FindProperty("z");
        z_range = instance.GetComponentInParent<Surface>().m_num_of_chunks * instance.GetComponentInParent<Surface>().m_res;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(display);
        z.intValue = EditorGUILayout.IntSlider("z", z.intValue, 0, z_range);
        serializedObject.ApplyModifiedProperties();
    }
}
