using UnityEngine;

namespace AsepriteImporter
{
    [System.Serializable]
    public class AseFileAnimationSettings
    {

        public AseFileAnimationSettings()
        {
        }

        // public AseFileAnimationSettings(string name, bool loop = true)
        // {
        //     animationName = name;
        //     loopTime = loop;
        // }

        [SerializeField] public string animationName;
        [SerializeField] public bool loopTime = true;
        [SerializeField] public string about;

        public override string ToString()
        {
            return animationName;
        }
    }
}