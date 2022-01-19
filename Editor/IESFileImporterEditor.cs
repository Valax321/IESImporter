using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Valax321.IESImporter
{
    [CustomEditor(typeof(IESFileImporter))]
    public class IESFileImporterEditor : ScriptedImporterEditor
    {
        private static Dictionary<int, string> s_TextureSizeNames = new Dictionary<int, string>()
        {
            { 16, "16x16" },
            { 32, "32x32" },
            { 64, "64x64" },
            { 128, "128x128" },
            { 256, "256x256" },
            { 512, "512x512" },
            { 1024, "1024x1024" },
            { 2048, "2048x2048" },
            { 4096, "4096x4096" },
            { 8192, "8192x8192" },
        };

        private SerializedProperty m_CookieType;
        private SerializedProperty m_TextureSize;
        
        public override void OnEnable()
        {
            base.OnEnable();
            m_CookieType = serializedObject.FindProperty(nameof(m_CookieType));
            m_TextureSize = serializedObject.FindProperty(nameof(m_TextureSize));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(m_CookieType);

            var selectedIndex = EditorGUILayout.Popup(new GUIContent(m_TextureSize.displayName, m_TextureSize.tooltip),
                s_TextureSizeNames.Keys.IndexOf(m_TextureSize.intValue, 0), s_TextureSizeNames.Select(x => new GUIContent(x.Value)).ToArray());
            m_TextureSize.intValue = s_TextureSizeNames.Keys.ElementAt(selectedIndex);
            
            serializedObject.ApplyModifiedProperties();
            
            ApplyRevertGUI();
        }
    }
}