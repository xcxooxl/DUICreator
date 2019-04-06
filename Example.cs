using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;

namespace DuiHandler
{
    public class GameScript : BaseScript
    {
        public GameScript()
        {
            this.Tick += GameScript_Tick;
        }

        private async Task GameScript_Tick()
        {
            if (Game.IsControlJustPressed(0, Control.ReplayStartStopRecording))
            {
                var createDui = this.Exports["addon"].createDui("cinscreen", "https://www.youtube.com/watch?v=p9MnXkWLe5M");
            }

            if (Game.IsControlJustPressed(0, Control.ReplayStartStopRecordingSecondary))
            {
                this.Exports["addon"].destroyAllDui();
            }
        }
    }
}
