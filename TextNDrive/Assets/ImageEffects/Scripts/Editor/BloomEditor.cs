using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Bloom))]
public class BloomEditor : Editor
{
	public Texture2D banner;

	SerializedObject serObj;

	SerializedProperty bloomIntensity;
	SerializedProperty lensDirtIntensity;
	SerializedProperty lensDirtTexture;

	void OnEnable()
	{
		serObj = new SerializedObject(target);
		bloomIntensity = serObj.FindProperty("bloomIntensity");
		lensDirtIntensity = serObj.FindProperty("lensDirtIntensity");
		lensDirtTexture = serObj.FindProperty("lensDirtTexture");
	}

	public override void OnInspectorGUI()
	{
		serObj.Update();

		Bloom instance = (Bloom)target;

		if (!instance.inputIsHDR)
		{
			EditorGUILayout.HelpBox("The camera is either not HDR enabled or there is an image effect before this one that converts from HDR to LDR. This image effect is dependant an HDR input to function properly.", MessageType.Warning);
		}
		EditorGUILayout.PropertyField(bloomIntensity, new GUIContent("Bloom Intensity", "The amount of light that is scattered inside the lens uniformly. Increase this value for a more drastic bloom."));
		EditorGUILayout.PropertyField(lensDirtIntensity, new GUIContent("Lens Dirt Intensity", "The amount that the lens dirt texture contributes to light scattering. Increase this value for a dirtier lens."));
		EditorGUILayout.PropertyField(lensDirtTexture, new GUIContent("Lens Dirt Texture", "The texture that controls per-channel light scattering amount. Black pixels do not affect light scattering. The brighter the pixel, the more light that is scattered."));
    	serObj.ApplyModifiedProperties();
	}
}
