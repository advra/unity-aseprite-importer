﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.Experimental.AssetImporters;
using System.IO;
using Aseprite;
using UnityEditor;
using Aseprite.Chunks;
using System.Text;
using System;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AsepriteImporter
{
    public enum AseFileImportType
    {
        Sprite,
        Tileset,
        LayerToSprite
    }

    public enum AseEditorBindType
    {
        SpriteRenderer,
        UIImage
    }

    public static class Settings
    {
        public static string FOLDERNAME = "AseAssets";
    }

    [ScriptedImporter(1, new []{ "ase", "aseprite" })]
    public class AseFileImporter : ScriptedImporter
    {
        [SerializeField] public AseFileTextureSettings textureSettings = new AseFileTextureSettings();
        [SerializeField] public AseFileAnimationSettings[] animationSettings;
        [SerializeField] public Texture2D atlas;
        [SerializeField] public AseFileImportType importType;
        [SerializeField] public AseEditorBindType bindType;

        public override void OnImportAsset(AssetImportContext ctx)
        {

            // if(!AssetDatabase.IsValidFolder(Settings.FOLDERNAME))
            // {
            //     AssetDatabase.CreateFolder("Assets", Settings.FOLDERNAME);
            // }


            name = GetFileName(ctx.assetPath);

            AseFile aseFile = ReadAseFile(ctx.assetPath);
            int frameCount = aseFile.Header.Frames;

            SpriteAtlasBuilder atlasBuilder = new SpriteAtlasBuilder(textureSettings, aseFile.Header.Width, aseFile.Header.Height);

            Texture2D[] frames = null;
            if (importType != AseFileImportType.LayerToSprite)
                frames = aseFile.GetFrames();
            else
                frames = aseFile.GetLayersAsFrames();
            
            SpriteImportData[] spriteImportData = new SpriteImportData[0];

            atlas = atlasBuilder.GenerateAtlas(frames, out spriteImportData, textureSettings.transparentMask, false);


            atlas.filterMode = textureSettings.filterMode;
            atlas.alphaIsTransparency = false;
            atlas.wrapMode = TextureWrapMode.Clamp;
            atlas.name = "Texture";

            ctx.AddObjectToAsset("Texture", atlas);
            AssetDatabase.AddObjectToAsset(atlas, "Assets/" + Settings.FOLDERNAME + name + ".spriteatlas");
            ctx.SetMainObject(atlas);

            switch (importType)
            {
                case AseFileImportType.LayerToSprite:
                case AseFileImportType.Sprite:
                    ImportSprites(ctx, aseFile, spriteImportData);
                    break;
                case AseFileImportType.Tileset:
                    ImportTileset(ctx, atlas);
                    break;
            }

            ctx.SetMainObject(atlas);
        }

        private void ImportSprites(AssetImportContext ctx, AseFile aseFile, SpriteImportData[] spriteImportData)
        {
            int spriteCount = spriteImportData.Length;
            
            
            Sprite[] sprites = new Sprite[spriteCount];

            for (int i = 0; i < spriteCount; i++)
            {
                Sprite sprite = Sprite.Create(atlas,
                    spriteImportData[i].rect,
                    spriteImportData[i].pivot, textureSettings.pixelsPerUnit, textureSettings.extrudeEdges,
                    textureSettings.meshType, spriteImportData[i].border, textureSettings.generatePhysics);
                sprite.name = string.Format("{0}_{1}", name, spriteImportData[i].name);

                ctx.AddObjectToAsset(sprite.name, sprite);
                sprites[i] = sprite;
            }

            GenerateAnimations(ctx, aseFile, sprites);
        }

        private void ImportTileset(AssetImportContext ctx, Texture2D atlas)
        {
            int cols = atlas.width / textureSettings.tileSize.x;
            int rows = atlas.height / textureSettings.tileSize.y;

            int width = textureSettings.tileSize.x;
            int height = textureSettings.tileSize.y;

            int index = 0;

            for (int y = rows - 1; y >= 0; y--)
            {
                for (int x = 0; x < cols; x++)
                {
                    Rect tileRect = new Rect(x * width, y * height, width, height);

                    Sprite sprite = Sprite.Create(atlas, tileRect, textureSettings.spritePivot,
                        textureSettings.pixelsPerUnit, textureSettings.extrudeEdges, textureSettings.meshType,
                        Vector4.zero, textureSettings.generatePhysics);
                    sprite.name = string.Format("{0}_{1}", name, index);

                    // ctx.AddObjectToAsset(sprite.name, sprite);

                    index++;
                }
            }
        }

        private string GetFileName(string assetPath)
        {
            string[] parts = assetPath.Split('/');
            string filename = parts[parts.Length - 1];

            return filename.Substring(0, filename.LastIndexOf('.'));
        }

        private static AseFile ReadAseFile(string assetPath)
        {
            FileStream fileStream = new FileStream(assetPath, FileMode.Open, FileAccess.Read);
            AseFile aseFile = new AseFile(fileStream);
            fileStream.Close();

            return aseFile;
        }

        private void GenerateAnimations(AssetImportContext ctx, AseFile aseFile, Sprite[] sprites)
        {
            if (animationSettings == null)
                animationSettings = new AseFileAnimationSettings[0];

            var animSettings = new List<AseFileAnimationSettings>(animationSettings);
            var animations = aseFile.GetAnimations();

            if (animations.Length <= 0)
                return;
            
            

            if (animationSettings != null)
                RemoveUnusedAnimationSettings(animSettings, animations);

            int index = 0;

            for(int indexAnim = 0; indexAnim < animations.Length; indexAnim ++)
            {
                FrameTag animation = animations[indexAnim];
                Console.WriteLine(animation.TagName);
                if(indexAnim == 0 && animation.TagName.Length != 0)
                {
                    if(animation.TagName.Contains("pivot:"))
                    {
                        // int pivotX;
                        // int pivotY;

                        // try
                        // {
                        //     string pivotString = animation.TagName.Split(':')[1].ToString();
                        //     string[] pivotInfo = pivotString.Split(',');
                        //     pivotX = Convert.ToInt32(pivotInfo[0]);
                        //     pivotY = Convert.ToInt32(pivotInfo[1]);
                        //     foundPivotInfo = true;
                        // }
                        // catch
                        // {
                        //     pivotX = 0;
                        //     pivotY = 0;
                        // }

                        continue;
                    }
                }
                AnimationClip animationClip = new AnimationClip();
                animationClip.name = name + "_" + animation.TagName;
                animationClip.frameRate = 25;

                AseFileAnimationSettings importSettings = GetAnimationSettingFor(animSettings, animation);

                importSettings.about = GetAnimationAbout(animation);

                EditorCurveBinding editorBinding = new EditorCurveBinding();
                editorBinding.path = "";
                editorBinding.propertyName = "m_Sprite";

                switch (bindType)
                {
                    case AseEditorBindType.SpriteRenderer:
                        editorBinding.type = typeof(SpriteRenderer);
                        break;
                    case AseEditorBindType.UIImage:
                        editorBinding.type = typeof(Image);
                        break;
                }


                int length = animation.FrameTo - animation.FrameFrom + 1;
                ObjectReferenceKeyframe[] spriteKeyFrames = new ObjectReferenceKeyframe[length + 1]; // plus last frame to keep the duration

                float time = 0;

                int from = (animation.Animation != LoopAnimation.Reverse) ? animation.FrameFrom : animation.FrameTo;
                int step = (animation.Animation != LoopAnimation.Reverse) ? 1 : -1;

                int keyIndex = from;

                for (int i = 0; i < length; i++)
                {
                    if (i >= length)
                    {
                        keyIndex = from;
                    }


                    ObjectReferenceKeyframe frame = new ObjectReferenceKeyframe();
                    frame.time = time;
                    frame.value = sprites[keyIndex];

                    time += aseFile.Frames[keyIndex].FrameDuration / 1000f;

                    keyIndex += step;
                    spriteKeyFrames[i] = frame;
                }

                float frameTime = 1f / animationClip.frameRate;

                ObjectReferenceKeyframe lastFrame = new ObjectReferenceKeyframe();
                lastFrame.time = time - frameTime;
                lastFrame.value = sprites[keyIndex - step];

                spriteKeyFrames[spriteKeyFrames.Length - 1] = lastFrame;


                AnimationUtility.SetObjectReferenceCurve(animationClip, editorBinding, spriteKeyFrames);
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(animationClip);

                switch (animation.Animation)
                {
                    case LoopAnimation.Forward:
                        animationClip.wrapMode = WrapMode.Loop;
                        settings.loopTime = true;
                        break;
                    case LoopAnimation.Reverse:
                        animationClip.wrapMode = WrapMode.Loop;
                        settings.loopTime = true;
                        break;
                    case LoopAnimation.PingPong:
                        animationClip.wrapMode = WrapMode.PingPong;
                        settings.loopTime = true;
                        break;
                }

                if (!importSettings.loopTime)
                {
                    animationClip.wrapMode = WrapMode.Once;
                    settings.loopTime = false;
                }
                
                // store our animation paths to unload later in editor
                // ctx.AddObjectToAsset(animation.TagName, animationClip);
                AnimationEvent[] oldEvents;
                // if anim file exists, save all event data
                string path = "Assets/" + animation.TagName + ".anim";
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip));
                if(obj != null)
                {
                    if(obj is AnimationClip clip)
                    {
                        oldEvents = AnimationUtility.GetAnimationEvents(clip);
                        AnimationUtility.SetAnimationEvents(animationClip, oldEvents);
                        settings = AnimationUtility.GetAnimationClipSettings(clip);
                        AssetDatabase.DeleteAsset(path);
                    }
                }

                AnimationUtility.SetAnimationClipSettings(animationClip, settings);
                AssetDatabase.CreateAsset(animationClip, path); 

                
                // add animations
                // AssetDatabase.CreateAsset(animationClip, "Assets/" + animation.TagName + ".anim");              // add animation to its own anim file (writable)
                // ctx.AddObjectToAsset(animation.TagName, animationClip);                                      // add animation files to context (read only)

                

                index++;
            }

            animationSettings = animSettings.ToArray();
        }

        private void RemoveUnusedAnimationSettings(List<AseFileAnimationSettings> animationSettings,
            FrameTag[] animations)
        {
            for (int i = 0; i < animationSettings.Count; i++)
            {
                bool found = false;
                if (animationSettings[i] != null)
                {
                    foreach (var anim in animations)
                    {
                        if (animationSettings[i].animationName == anim.TagName)
                            found = true;
                    }
                }

                if (!found)
                {
                    animationSettings.RemoveAt(i);
                    i--;
                }
            }
        }

        public AseFileAnimationSettings GetAnimationSettingFor(List<AseFileAnimationSettings> animationSettings,
            FrameTag animation)
        {
            if (animationSettings == null)
                animationSettings = new List<AseFileAnimationSettings>();

            for (int i = 0; i < animationSettings.Count; i++)
            {
                if (animationSettings[i].animationName == animation.TagName)
                    return animationSettings[i];
            }

            animationSettings.Add(new AseFileAnimationSettings(animation.TagName));
            return animationSettings[animationSettings.Count - 1];
        }

        public bool GetPivotFromFirstFrame(AssetImportContext ctx, AseFile aseFile, out Vector2Int pivot)
        {
            pivot = Vector2Int.zero;

            var animations = aseFile.GetAnimations();
            if(animations.Length <0)
            {
                return false;
            }

            try
            {
                string pivotString = animations[0].TagName.Split(':')[1].ToString();
                string[] pivotInfo = pivotString.Split(',');
                pivot.x = Convert.ToInt32(pivotInfo[0]);
                pivot.y = Convert.ToInt32(pivotInfo[1]);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetAnimationAbout(FrameTag animation)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Animation Type:\t{0}", animation.Animation.ToString());
            sb.AppendLine();
            sb.AppendFormat("Animation:\tFrom: {0}; To: {1}", animation.FrameFrom, animation.FrameTo);

            return sb.ToString();
        }
    }
}