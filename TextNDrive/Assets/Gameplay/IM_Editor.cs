#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(IM))]
public class IM_Editor : Editor
{
    protected static bool       showSpawnTable = false;
    protected static string[]   names;
    protected static bool[]     showSounds;

    void OnEnable()
    {
        names       = Enum.GetNames(typeof(IM.Type));
        showSounds  = new bool[names.Length];
    }

    public override void OnInspectorGUI()
    {
        IM t = (IM)target;

        showSpawnTable = EditorGUILayout.Foldout(showSpawnTable, "Spawn Table");

        if (showSpawnTable)
        {
            EditorGUI.indentLevel = 1;

            if(t.SpawnTypes.Length != names.Length)
            {
                IM.InstanceData[] temp = new IM.InstanceData[names.Length];

                if(names.Length > t.SpawnTypes.Length)
                {
                    for (int i = 0; i < t.SpawnTypes.Length; ++i)
                        temp[i] = t.SpawnTypes[i];
                }
                else
                {
                    for (int i = 0; i < names.Length; ++i)
                        temp[i] = t.SpawnTypes[i];
                }

                t.SpawnTypes = temp;
            }

            for(int i = 0; i < names.Length; ++i)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 192;
                t.SpawnTypes[i].Instance = (GameObject)EditorGUILayout.ObjectField(names[i], t.SpawnTypes[i].Instance, typeof(GameObject), true);
                t.SpawnTypes[i].maxCount = EditorGUILayout.IntField(t.SpawnTypes[i].maxCount, GUILayout.Width(48));

                serializedObject.Update();
                ArrayGUI(serializedObject.FindProperty("SpawnTypes").GetArrayElementAtIndex(i).FindPropertyRelative("sounds"), ref showSounds[i]);
                serializedObject.ApplyModifiedProperties();

                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel = 0;
        }

        EditorUtility.SetDirty(t);

        DrawDefaultInspector();
    }

    private void ArrayGUI(SerializedProperty property, ref bool visible)
    {
        EditorGUILayout.BeginVertical();

        EditorGUIUtility.labelWidth = 1;
        EditorGUIUtility.fieldWidth = 64;
        visible = EditorGUILayout.Foldout(visible, property.name);
        if (visible)
        {
            EditorGUI.indentLevel++;
            SerializedProperty arraySizeProp = property.FindPropertyRelative("Array.size");
            EditorGUILayout.PropertyField(arraySizeProp);

            for (int i = 0; i < arraySizeProp.intValue; ++i)
            {
                EditorGUILayout.PropertyField(property.GetArrayElementAtIndex(i), true);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }
}
#endif
