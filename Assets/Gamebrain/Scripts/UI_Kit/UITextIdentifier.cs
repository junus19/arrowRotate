using UnityEngine;

namespace GameBrain.Casual
{    
    public enum UITextType
    {
        Default = 0,
        Bold = 1,
        Alternative_1 = 2,
        Alternative_2 = 3,
        Alternative_3 = 4,
    }

    public class UITextIdentifier : UIIdentifier
    {
        [SerializeField] UITextType _UITextType;
        public UITextType UITextType => _UITextType;
    }
}
