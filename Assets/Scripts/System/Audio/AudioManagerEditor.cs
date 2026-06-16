#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioManager))]
public class AudioManagerEditor : Editor
{
    private SerializedProperty musicList;
    private SerializedProperty sfxList;

    private bool showMusicList = true;
    private bool showSfxList = true;

    private const string GeneratedFolder = "Assets/Scripts/Generated";
    private const string MusicEnumPath = GeneratedFolder + "/MusicName.cs";
    private const string SfxEnumPath = GeneratedFolder + "/SFXName.cs";

    private void OnEnable()
    {
        musicList = serializedObject.FindProperty("musicList");
        sfxList = serializedObject.FindProperty("sfxList");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultPropertiesExcept("musicList", "sfxList");

        GUILayout.Space(10);
        DrawSoundList("Music List", musicList, ref showMusicList, "NewBGM");

        GUILayout.Space(10);
        DrawSoundList("SFX List", sfxList, ref showSfxList, "NewSFX");

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawDefaultPropertiesExcept(params string[] excludedProperties)
    {
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;

            bool excluded = false;
            foreach (string excludedProperty in excludedProperties)
            {
                if (property.name == excludedProperty)
                {
                    excluded = true;
                    break;
                }
            }

            if (excluded)
                continue;

            using (new EditorGUI.DisabledScope(property.name == "m_Script"))
            {
                EditorGUILayout.PropertyField(property, true);
            }
        }
    }

    private void DrawSoundList(string title, SerializedProperty list, ref bool foldout, string defaultName)
    {
        if (list == null)
        {
            EditorGUILayout.HelpBox($"{title} property not found.", MessageType.Error);
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        foldout = EditorGUILayout.Foldout(foldout, title, true);

        if (GUILayout.Button("+ Add", GUILayout.Width(75)))
        {
            AddNewSound(list, defaultName);
            GenerateEnums();
        }

        EditorGUILayout.EndHorizontal();

        if (foldout)
        {
            EditorGUI.indentLevel++;

            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);

                SerializedProperty nameProp = element.FindPropertyRelative("name");
                SerializedProperty clipProp = element.FindPropertyRelative("clip");
                SerializedProperty volumeProp = element.FindPropertyRelative("volume");
                SerializedProperty loopProp = element.FindPropertyRelative("loop");

                if (nameProp == null || clipProp == null || volumeProp == null || loopProp == null)
                {
                    EditorGUILayout.HelpBox("SoundData field mismatch. Check field names.", MessageType.Error);
                    continue;
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                element.isExpanded = EditorGUILayout.Foldout(
                    element.isExpanded,
                    string.IsNullOrEmpty(nameProp.stringValue) ? $"Element {i}" : nameProp.stringValue,
                    true
                );

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    list.DeleteArrayElementAtIndex(i);
                    GenerateEnums();
                    break;
                }

                EditorGUILayout.EndHorizontal();

                if (element.isExpanded)
                {
                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.PropertyField(nameProp);
                    EditorGUILayout.PropertyField(clipProp);
                    EditorGUILayout.Slider(volumeProp, 0f, 1f);
                    EditorGUILayout.PropertyField(loopProp);

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        GenerateEnums();
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void AddNewSound(SerializedProperty list, string defaultName)
    {
        int index = list.arraySize;
        list.InsertArrayElementAtIndex(index);

        SerializedProperty element = list.GetArrayElementAtIndex(index);

        element.FindPropertyRelative("name").stringValue = defaultName;
        element.FindPropertyRelative("clip").objectReferenceValue = null;
        element.FindPropertyRelative("volume").floatValue = 1f;
        element.FindPropertyRelative("loop").boolValue = false;

        element.isExpanded = true;

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private void GenerateEnums()
    {
        serializedObject.ApplyModifiedProperties();

        if (!Directory.Exists(GeneratedFolder))
            Directory.CreateDirectory(GeneratedFolder);

        GenerateEnumFile("MusicName", musicList, MusicEnumPath);
        GenerateEnumFile("SFXName", sfxList, SfxEnumPath);

        AssetDatabase.Refresh();
    }

    private void GenerateEnumFile(string enumName, SerializedProperty list, string path)
    {
        if (list == null)
            return;

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("// Auto-generated by AudioManagerEditor");
        sb.AppendLine("// Do not edit manually.");
        sb.AppendLine();
        sb.AppendLine("public enum " + enumName);
        sb.AppendLine("{");
        sb.AppendLine("    None,");

        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = element.FindPropertyRelative("name");

            if (nameProp == null)
                continue;

            string rawName = nameProp.stringValue;

            if (string.IsNullOrWhiteSpace(rawName))
                continue;

            string enumValue = MakeSafeEnumName(rawName);

            if (enumValue == "None")
                continue;

            sb.AppendLine("    " + enumValue + ",");
        }

        sb.AppendLine("}");

        File.WriteAllText(path, sb.ToString());
    }

    private string MakeSafeEnumName(string input)
    {
        StringBuilder sb = new StringBuilder();

        foreach (char c in input)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else
                sb.Append('_');
        }

        string result = sb.ToString();

        while (result.Contains("__"))
            result = result.Replace("__", "_");

        result = result.Trim('_');

        if (string.IsNullOrEmpty(result))
            result = "Unnamed";

        if (char.IsDigit(result[0]))
            result = "_" + result;

        return result;
    }
}
#endif