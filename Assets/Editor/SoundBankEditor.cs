using UnityEditor;
using UnityEngine;

// Play, copy and paste buttons on every SoundBank entry; the JSON is the one the Retro Sound Synthesizer window copies and pastes
[CustomEditor(typeof(SoundBank))]
public class SoundBankEditor : Editor
{
    static AudioSource previewSource;

    public override void OnInspectorGUI()
    {
        var bank = (SoundBank)target;
        serializedObject.Update();
        var entries = serializedObject.FindProperty("entries");
        for (int i = 0; i < entries.arraySize; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            using (new EditorGUILayout.HorizontalScope())
            {
                entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, ((Sfx)entry.FindPropertyRelative("id").intValue).ToString(), true);
                if (GUILayout.Button("Play", GUILayout.Width(44)))
                    Preview(bank.entries[i]);
                if (GUILayout.Button("Copy JSON", GUILayout.Width(80)))
                    EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(bank.entries[i].sound, true);
                if (GUILayout.Button("Paste JSON", GUILayout.Width(80)))
                {
                    Undo.RecordObject(bank, "Paste sound JSON");
                    JsonUtility.FromJsonOverwrite(EditorGUIUtility.systemCopyBuffer, bank.entries[i].sound);
                    EditorUtility.SetDirty(bank);
                    serializedObject.Update();
                }
                if (GUILayout.Button("X", GUILayout.Width(22)))
                {
                    entries.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
            if (entry.isExpanded)
            {
                EditorGUI.indentLevel++;
                foreach (var name in new[] { "id", "volume", "variation", "minInterval", "sound" })
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative(name), true);
                EditorGUI.indentLevel--;
            }
        }

        if (GUILayout.Button("Add sound"))
            entries.InsertArrayElementAtIndex(entries.arraySize);
        serializedObject.ApplyModifiedProperties();
    }

    // The editor's own clip preview (what the AudioClip inspector uses): it needs no AudioListener in the open scene
    static readonly System.Reflection.MethodInfo playPreviewClip = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil")?
        .GetMethod("PlayPreviewClip", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
            null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);

    static void Preview(SoundBank.Entry entry)
    {
        var clip = SoundBank.Synthesize(entry, 0);
        if (clip == null) return;
        if (playPreviewClip != null && !Application.isPlaying)
        {
            playPreviewClip.Invoke(null, new object[] { clip, 0, false });
            return;
        }
        if (previewSource == null)
        {
            var host = EditorUtility.CreateGameObjectWithHideFlags("SoundBankPreview", HideFlags.HideAndDontSave, typeof(AudioSource));
            previewSource = host.GetComponent<AudioSource>();
        }
        previewSource.PlayOneShot(clip, entry.volume);
    }
}
