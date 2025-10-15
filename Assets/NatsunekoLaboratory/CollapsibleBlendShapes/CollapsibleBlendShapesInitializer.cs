using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using NatsunekoLaboratory.CollapsibleBlendShapes.Interfaces;

using Refractions;

using UnityEditor;

using UnityEngine;

namespace NatsunekoLaboratory.CollapsibleBlendShapes
{
    public static class CollapsibleBlendShapesInitializer
    {
        private static Refraction<ISkinnedMeshRendererEditor> _editor;
        private static Refraction<ISkinnedMeshRendererEditor_Styles> _styles;
        private static Refraction<ISavedBool> _savedBool;
        private static Dictionary<string, Refraction<ISavedBool>> _caches = new();
        private static readonly List<string> Separators = new() { "-", "=", "#", "*" };

        [InitializeOnLoadMethod]
        public static void InitializeOnLoad()
        {
            var editorResolver = RefractionResolver.FromType<Editor>();
            var engineResolver = RefractionResolver.FromType<GameObject>();
            _editor = editorResolver.Get<ISkinnedMeshRendererEditor>("UnityEditor.SkinnedMeshRendererEditor");
            _styles = editorResolver.Get<ISkinnedMeshRendererEditor_Styles>("UnityEditor.SkinnedMeshRendererEditor+Styles");
            _savedBool = editorResolver.Get<ISavedBool>("UnityEditor.SavedBool");

            var harmony = new Harmony(nameof(CollapsibleBlendShapesInitializer));
            var t = typeof(Editor).Assembly.GetType("UnityEditor.SkinnedMeshRendererEditor");
            var mOriginal = AccessTools.Method(t, "OnBlendShapeUI");
            var mPrefix = typeof(CollapsibleBlendShapesInitializer).GetMethod(nameof(Prefix));

            harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(object __instance)
        {
            var instance = _editor.Instance(__instance);
            var renderer = (SkinnedMeshRenderer)instance.ProxyGet(w => w.target);
            var blendShapeCount = renderer.sharedMesh == null ? 0 : renderer.sharedMesh.blendShapeCount;
            if (blendShapeCount == 0)
                return false;

            var content = new GUIContent { text = "BlendShapes" };
            var weights = instance.ProxyGet(w => w.m_BlendShapeWeights);

            EditorGUILayout.PropertyField(weights, content, false);
            if (!weights.isExpanded)
                return false;

            var instanceId = renderer.sharedMesh.GetInstanceID();
            using (new EditorGUI.IndentLevelScope())
            {
                if (PlayerSettings.legacyClampBlendShapeWeights)
                    EditorGUILayout.HelpBox(_styles.ProxyGet(w => w.legacyClampBlendShapeWeightsInfo).text, MessageType.Info);

                var m = renderer.sharedMesh;
                var arraySize = weights.arraySize;
                var isGrouping = false;
                var groupName = "";

                for (var i = 0; i < blendShapeCount; i++)
                {
                    content.text = m.GetBlendShapeName(i);

                    var separator = Separators.FirstOrDefault(w => content.text.StartsWith($"{w}{w}"));
                    var isGroupingShapeKey = content.text.Length > 3 && !string.IsNullOrWhiteSpace(separator);
                    if (isGroupingShapeKey && isGrouping)
                        EditorGUI.indentLevel--;

                    if (isGroupingShapeKey)
                    {
                        Refraction<ISavedBool> b;

                        if (_caches.TryGetValue($"{instanceId}_{content.text}_b", out var sb))
                        {
                            b = sb;
                        }
                        else
                        {
                            b = _savedBool.ProxyConstruct(w => w.Constructor($"{instanceId}_{content.text}_b", true));
                            _caches.Add($"{instanceId}_{content.text}_b", b);
                        }

                        b.ProxySet(w => w.value, EditorGUILayout.Foldout(b.ProxyGet(w => w.value), content.text.Replace(separator, "")));

                        groupName = content.text;
                    }

                    var foldoutVal = true;
                    if (isGrouping)
                    {
                        var b = _caches[$"{instanceId}_{groupName}_b"];
                        foldoutVal = b.ProxyGet(w => w.value);
                    }

                    if (!isGroupingShapeKey && foldoutVal)
                    {
                        float sliderMin = 0f, sliderMax = 0f;
                        var frameCount = m.GetBlendShapeFrameCount(i);

                        for (var j = 0; j < frameCount; j++)
                        {
                            var frameWeight = m.GetBlendShapeFrameWeight(i, j);
                            sliderMin = Mathf.Min(frameWeight, sliderMin);
                            sliderMax = Mathf.Max(frameWeight, sliderMax);
                        }

                        if (i < arraySize)
                        {
                            var val = weights.GetArrayElementAtIndex(i);
                            EditorGUILayout.Slider(val, sliderMin, sliderMax, content);
                        }
                        else
                        {
                            EditorGUI.BeginChangeCheck();

                            var value = EditorGUILayout.Slider(content, 0f, sliderMax, sliderMax);
                            if (EditorGUI.EndChangeCheck())
                            {
                                weights.arraySize = blendShapeCount;
                                arraySize = blendShapeCount;
                                weights.GetArrayElementAtIndex(i).floatValue = value;
                            }
                        }
                    }

                    if (isGroupingShapeKey)
                    {
                        EditorGUI.indentLevel++;
                        isGrouping = true;
                    }
                }

                if (isGrouping)
                    EditorGUI.indentLevel--;
            }

            return false;
        }
    }
}
