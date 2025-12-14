using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KCM
{
    /// <summary>
    ///     Kingdoms and Castles "Contants" (World instances, etc)
    ///     
    ///     _T is a Trasnform
    ///     _O is a GameObject
    /// </summary>
    public static class Constants
    {
        public static readonly MainMenuMode MainMenuMode = GameState.inst.mainMenuMode;
        public static readonly PlayingMode PlayingMode = GameState.inst.playingMode;
        public static readonly World World = GameState.inst.world;

        #region "UI"
        public static readonly Transform MainMenuUI_T = MainMenuMode.mainMenuUI.transform;
        public static readonly GameObject MainMenuUI_O = MainMenuMode.mainMenuUI;

       /* public static readonly Transform TopLevelUI_T = MainMenuUI_T.parent;
        public static readonly GameObject TopLevelUI_O = MainMenuUI_T.parent.gameObject;*/

        public static readonly Transform ChooseModeUI_T = MainMenuMode.chooseModeUI.transform;
        public static readonly GameObject ChooseModeUI_O = MainMenuMode.chooseModeUI;
        #endregion

    }
}
