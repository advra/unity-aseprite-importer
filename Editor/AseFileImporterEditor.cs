using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor;
using UnityEngine.Animations;

namespace AsepriteImporter
{
    [CustomEditor(typeof(AseFileImporter)), CanEditMultipleObjects]
    public class AseFileImporterEditor : ScriptedImporterEditor
    {
        private string[] importTypes = new string[] {"Sprite", "Tileset (Grid)", "Layer To Sprite"};

        private string[] spritePivotOptions = new string[]
        {
            "Center", "Top Left", "Top", "Top Right", "Left", "Right", "Bottom Left", "Bottom", "Bottom Right", "ReadFromTag", "ReadFromLayer", "Custom"
        };

        private bool customSpritePivot = false;
        private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

        private AseFileAnimationSettings[] fileAnimationSettings;
        private List<AnimationClip> animationClips = new List<AnimationClip>();

        public override void OnEnable()
        {
            base.OnEnable();
            foldoutStates.Clear();
            animationClips.Clear();
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var importer = serializedObject.targetObject as AseFileImporter;
            var textureSettings = "textureSettings.";

            var importTypeProperty = serializedObject.FindProperty("importType");

            EditorGUILayout.LabelField("Texture Options", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;

                var importType = importTypeProperty.intValue;
                EditorGUI.BeginChangeCheck();
                importType = EditorGUILayout.Popup("Import Type", importType, importTypes);
                if (EditorGUI.EndChangeCheck())
                {
                    importTypeProperty.intValue = importType;
                }

                var transparentColorMask = serializedObject.FindProperty(textureSettings + "transparentMask");
                var transparentColor = serializedObject.FindProperty(textureSettings + "transparentColor");

                Rect lastRect = GUILayoutUtility.GetLastRect();
                Rect resetButton = new Rect(EditorGUIUtility.labelWidth + 50,
                    lastRect.y + EditorGUIUtility.singleLineHeight, 60, 18);
                if (GUI.Button(resetButton, "Reset"))
                {
                    transparentColor.colorValue = Color.magenta;
                }

                EditorGUILayout.PropertyField(transparentColorMask);
                if (transparentColorMask.boolValue)
                {
                    EditorGUILayout.PropertyField(transparentColor);
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(textureSettings + "pixelsPerUnit"));

                if (importTypeProperty.intValue == (int) AseFileImportType.Sprite)
                {
                    // Mirror
                    var mirrorProperty = serializedObject.FindProperty(textureSettings + "mirror");
                    var mirror = (MirrorOption) mirrorProperty.enumValueIndex;

                    EditorGUI.BeginChangeCheck();
                    mirror = (MirrorOption) EditorGUILayout.EnumPopup("Mirror", mirror);
                    if (EditorGUI.EndChangeCheck())
                    {
                        mirrorProperty.enumValueIndex = (int) mirror;
                    }
                }

                var meshTypeProperty = serializedObject.FindProperty(textureSettings + "meshType");
                var meshType = (SpriteMeshType) meshTypeProperty.intValue;

                EditorGUI.BeginChangeCheck();
                meshType = (SpriteMeshType) EditorGUILayout.EnumPopup("Mesh Type", meshType);
                if (EditorGUI.EndChangeCheck())
                {
                    meshTypeProperty.intValue = (int) meshType;
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(textureSettings + "extrudeEdges"));
                
                if (importTypeProperty.intValue == (int) AseFileImportType.Sprite)
                {
                    PivotPopup("Pivot");
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(textureSettings + "generatePhysics"));

                var editorBindingProperty = serializedObject.FindProperty("bindType");
                var editorBinding = (AseEditorBindType) editorBindingProperty.intValue;

                EditorGUI.BeginChangeCheck();
                editorBinding = (AseEditorBindType) EditorGUILayout.EnumPopup("Component to Bind", editorBinding);
                if (EditorGUI.EndChangeCheck())
                {
                    editorBindingProperty.intValue = (int) editorBinding;
                }

                EditorGUILayout.Space();

                importer.textureSettings.wrapMode =
                    (TextureWrapMode) EditorGUILayout.EnumPopup("Wrap Mode", importer.textureSettings.wrapMode);
                importer.textureSettings.filterMode =
                    (FilterMode) EditorGUILayout.EnumPopup("Filter Mode", importer.textureSettings.filterMode);

                EditorGUI.indentLevel--;
            }


            EditorGUILayout.Space();
            SerializedProperty animationSettingsArray = serializedObject.FindProperty("animationSettings");
            EditorGUILayout.PropertyField(serializedObject.FindProperty(textureSettings + "extrudeEdges"));
            
            if (animationSettingsArray != null)
            {
                
                int arraySize = animationSettingsArray.arraySize;
                
                if (arraySize > 0)
                {
                    EditorGUILayout.LabelField("Animation Options", EditorStyles.boldLabel);
                }

                fileAnimationSettings = new AseFileAnimationSettings[arraySize];
              
                // load each anmation at the path
                for(int i = 0; i < arraySize; i++)
                {
                    SerializedProperty animationSettings = animationSettingsArray.GetArrayElementAtIndex(i);
                    string animationName = animationSettings.FindPropertyRelative("animationName").stringValue;

                    if (!foldoutStates.ContainsKey(animationName))
                    {
                        foldoutStates.Add(animationName, false);
                    }

                    fileAnimationSettings[i] = new AseFileAnimationSettings();
                    fileAnimationSettings[i].animationName = animationName;
                    // fileAnimationSettings[i].animationClipPath = animationSettings.FindPropertyRelative("animationClipPath").stringValue;
                  

                    if(animationClips == null)
                    {
                        animationClips = new List<AnimationClip>();
                        string path = animationSettings.FindPropertyRelative("animationClipPath").stringValue;
                        object[] allAssets  = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
                        

                        foreach (var asset in allAssets)
                        {
                            if(asset is AnimationClip clip)
                            {
                               Debug.Log(clip);
                               if(!animationClips.Contains(clip))
                               {
                                   animationClips.Add(clip);
                               }
                            }
                        }
                    }
                    

                     // load each anmation at the path
                    EditorGUILayout.HelpBox(fileAnimationSettings[i].animationClipPath+ "/" + animationName, MessageType.None);
                    // Debug.Log(fileAnimationSettings[i].animationClipPath + "/" + animationName);
                    // AnimationClip animationClip = (AnimationClip) AssetDatabase.LoadAllAssetRepresentationsAtPath(fileAnimationSettings[i].animationClipPath, typeof(AnimationClip));
                    // EditorGUILayout.ObjectField(animationClip, typeof(AnimationClip), false);
                    
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUI.indentLevel++;

                    GUIStyle foldoutStyle = EditorStyles.foldout;
                    FontStyle prevoiusFontStyle = foldoutStyle.fontStyle;
                    foldoutStyle.fontStyle = FontStyle.Bold;


                    if (foldoutStates[animationName] = EditorGUILayout.Foldout(foldoutStates[animationName],
                        animationName, true, foldoutStyle))
                    {
                        EditorGUILayout.PropertyField(animationSettings.FindPropertyRelative("loopTime"));
                        EditorGUILayout.HelpBox(animationSettings.FindPropertyRelative("about").stringValue, MessageType.None);

                        // // load each anmation at the path
                        // EditorGUILayout.HelpBox(fileAnimationSettings[i].animationClipPath+ "/" + animationName, MessageType.None);
                        // Debug.Log(fileAnimationSettings[i].animationClipPath + "/" + animationName);
                        // AnimationClip animationClip = (AnimationClip) AssetDatabase.LoadAssetAtPath(fileAnimationSettings[i].animationClipPath, typeof(AnimationClip));
                        //TODO: The order is not guaranteed when reading from LoadAllAssetRepresentationsAtPath.
                        EditorGUILayout.ObjectField(animationClips[i], typeof(AnimationClip), false);
                    }

                    foldoutStyle.fontStyle = prevoiusFontStyle;

                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();

                    // animationPaths[i] = animationSettingsArray.GetArrayElementAtIndex(i).FindPropertyRelative("animationClipPath").stringValue;
                    // EditorGUILayout.HelpBox(animationPaths[i]+ "/" + animationName, MessageType.None);
                }

                // string path = animationSettings.FindPropertyRelative("animationClipPath").stringValue;
                // EditorGUILayout.HelpBox(path+ "/" + animationName, MessageType.None);
                // Debug.Log(path+ "/" + animationName);
                // AnimationClip animationClip = (AnimationClip) AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip));
                // EditorGUILayout.ObjectField(animationClip, typeof(AnimationClip), false);
                
            }

            if (importTypeProperty.intValue == (int) AseFileImportType.Tileset)
            {
                EditorGUILayout.LabelField("Tileset Options", EditorStyles.boldLabel);
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(textureSettings + "tileSize"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(textureSettings + "tilePadding"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(textureSettings + "tileOffset"));
                    PivotPopup("Tile Pivot");

                    EditorGUI.indentLevel--;
                }
            }

            serializedObject.ApplyModifiedProperties();
            base.ApplyRevertGUI();
        }


        private void PivotPopup(string label)
        {
            var pivotProperty = serializedObject.FindProperty("textureSettings.spritePivot");
            var pivot = pivotProperty.vector2Value;

            EditorGUI.BeginChangeCheck();
            switch (EditorGUILayout.Popup(label, GetSpritePivotOptionIndex(pivot), spritePivotOptions))
            {
                case 0:
                    customSpritePivot = false;

                    pivot = new Vector2(0.5f, 0.5f);
                    break;
                case 1:
                    customSpritePivot = false;
                    pivot = new Vector2(0f, 1f);
                    break;
                case 2:
                    customSpritePivot = false;
                    pivot = new Vector2(0.5f, 1f);
                    break;
                case 3:
                    customSpritePivot = false;
                    pivot = new Vector2(1f, 1f);
                    break;
                case 4:
                    customSpritePivot = false;
                    pivot = new Vector2(0f, 0.5f);
                    break;
                case 5:
                    customSpritePivot = false;
                    pivot = new Vector2(1f, 0.5f);
                    break;
                case 6:
                    customSpritePivot = false;
                    pivot = new Vector2(0f, 0f);
                    break;
                case 7:
                    customSpritePivot = false;
                    pivot = new Vector2(0.5f, 0f);
                    break;
                case 8:
                    customSpritePivot = false;
                    pivot = new Vector2(1f, 0f);
                    break;
                // read from tag
                case 9:
                    break;
                default:
                    customSpritePivot = true;
                    break;
            }

            if (customSpritePivot)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("textureSettings.spritePivot"),
                    new GUIContent(label));
                EditorGUI.indentLevel--;
            }
            else if (EditorGUI.EndChangeCheck() && !customSpritePivot)
            {
                pivotProperty.vector2Value = pivot;
            }
        }

        private int GetSpritePivotOptionIndex(Vector2 spritePivot)
        {
            if (customSpritePivot)
                return spritePivotOptions.Length - 1;

            if (spritePivot.x == 0.5f && spritePivot.y == 0.5f) return 0;
            if (spritePivot.x == 0f && spritePivot.y == 1f) return 1;
            if (spritePivot.x == 0.5f && spritePivot.y == 1f) return 2;
            if (spritePivot.x == 1f && spritePivot.y == 1f) return 3;
            if (spritePivot.x == 0f && spritePivot.y == 0.5f) return 4;
            if (spritePivot.x == 1f && spritePivot.y == 0.5f) return 5;
            if (spritePivot.x == 0f && spritePivot.y == 0f) return 6;
            if (spritePivot.x == 0.5f && spritePivot.y == 0f) return 7;
            if (spritePivot.x == 1f && spritePivot.y == 0f) return 8;

            return spritePivotOptions.Length - 1; // Last one = custom
        }
    }
}
