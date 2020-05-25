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

        public override string ToString()
        {
            return animationName;
        }
    }

}