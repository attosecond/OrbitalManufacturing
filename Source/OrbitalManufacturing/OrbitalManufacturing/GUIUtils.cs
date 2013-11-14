using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OrbitalManufacturing
{
    public class GuiUtils
    {
        public static GUIStyle _basicbutton;
        public enum SkinType { Default, MechJeb1 }
        public static GUISkin skin;
        public static GUISkin defaultSkin;
   
        public static GUIStyle basicbutton
        {
            get
            {
                if (_basicbutton == null)
                {
                    _basicbutton = new GUIStyle(GUI.skin.button);
                    _basicbutton.normal.textColor = _basicbutton.focused.textColor = Color.white;
                    _basicbutton.onActive.textColor = _basicbutton.onFocused.textColor = _basicbutton.onHover.textColor = _basicbutton.onNormal.textColor = Color.green;
                }
                return _basicbutton;
            }
        }

        public static void CopyDefaultSkin()
        {
            GUI.skin = null;
            defaultSkin = (GUISkin)GameObject.Instantiate(GUI.skin);
        }

        public static void LoadSkin(SkinType skinType)
        {
            switch (skinType)
            {
                case SkinType.Default:
                    if (defaultSkin == null) CopyDefaultSkin();
                    skin = defaultSkin;
                    break;

                case SkinType.MechJeb1:
                    skin = AssetBase.GetGUISkin("KSP window 2");
                    break;
            }
        }

        public static void SimpleLabel(string leftLabel, string rightLabel = "")
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(leftLabel, GUILayout.ExpandWidth(true));
            GUILayout.Label(rightLabel, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
    }
}
