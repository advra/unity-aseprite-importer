using UnityEngine;
using UnityEngine.Animations;

namespace AsepriteImporter
{
    [System.Serializable]
    public class AseFileAnimationSettings
    {

        public AseFileAnimationSettings()
        {
        }

        public AseFileAnimationSettings(string name)
        {
            animationName = name;
        }

        [SerializeField] public string animationName;
        [SerializeField] public bool loopTime = true;
        [SerializeField] public string about;
        [SerializeField] public string animationClipPath;
        [SerializeField] public AnimationEventInfo[] events;

        public override string ToString()
        {
            return animationName;
        }
    }

    [System.Serializable]
    public class AnimationEventInfo
    {
        [SerializeField] public string functionName;
        [SerializeField] public float time;
        [SerializeField] public float floatParameter;
        [SerializeField] public int intParameter;
        [SerializeField] public  string stringParameter;
    }

}