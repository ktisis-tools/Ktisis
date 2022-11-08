using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Logging;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.History
{
    public sealed class HistoryManager : IDisposable
    {
        public static List<HistoryItem> history = new List<HistoryItem>();
        private static int currentIdx = 0;

        private HistoryManager() 
        {
            Dalamud.Framework.Update += this.Monitor;
        }


        private static HistoryManager? _instance;

        public static HistoryManager Instance()
        {
            if (_instance == null)
            {
                _instance = new HistoryManager();
            }
            return _instance;
        }

        /* 
         This is weird yes. 
         But it exists to make it obvious that it needs to be created first so it can subscribe to the Framework.Update event.
        */
        public static void Init() 
        {
            Instance();
        }

        public void Dispose()
        {
            Dalamud.Framework.Update -= this.Monitor;
        }

        public void Monitor(Framework framework)
        {
            foreach (HistoryItem item in history)
            {
                if (item.Global)
                {
                    PluginLog.Information("Global: " + item.Ttc.ToString());
                }
            }
        }
    }
}
