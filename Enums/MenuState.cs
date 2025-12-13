using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Enums
{
    public enum MenuState
    {
        Uninitialized,
        // Token: 0x040022F1 RID: 8945
        Menu,
        // Token: 0x040022F2 RID: 8946
        ChooseMode,
        // Token: 0x040022F3 RID: 8947
        ChooseDifficulty,
        // Token: 0x040022F4 RID: 8948
        NewMap,
        // Token: 0x040022F5 RID: 8949
        NameAndBanner,
        // Token: 0x040022F6 RID: 8950
        PauseMenu,
        // Token: 0x040022F7 RID: 8951
        SettingsMenu,
        // Token: 0x040022F8 RID: 8952
        Save,
        // Token: 0x040022F9 RID: 8953
        Load,
        // Token: 0x040022FA RID: 8954
        QuitConfirm,
        // Token: 0x040022FB RID: 8955
        ExitConfirm,
        // Token: 0x040022FC RID: 8956
        LoadError,
        // Token: 0x040022FD RID: 8957
        SendSave,
        // Token: 0x040022FE RID: 8958
        Credits,
        // Token: 0x040022FF RID: 8959
        Failure,
        // Token: 0x04002300 RID: 8960
        KeepDestroyed,
        // Token: 0x04002301 RID: 8961
        BannerSelect,
        // Token: 0x04002302 RID: 8962
        GameWorkshopUI,
        // Token: 0x04002303 RID: 8963
        RivalChoiceUI,
        // Token: 0x04002304 RID: 8964
        KingdomShareFromMenu,
        // Token: 0x04002305 RID: 8965
        KingdomShareFromGame,

        ServerBrowser,

        ServerLobby
    }
}
