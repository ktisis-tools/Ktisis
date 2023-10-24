using System;
using System.Collections.Generic;
using System.Threading;

using Dalamud.Logging;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Events;

namespace Ktisis.Structs.Actor.State
{
    public class ActorWetnessOverride
    {
        public static ActorWetnessOverride Instance { get; set; } = new();
        
        public Dictionary<IntPtr, (float WeatherWetness, float SwimmingWetness, float WetnessDepth)> WetnessOverrides = new();
        public Dictionary<IntPtr, bool> WetnessOverridesEnabled = new();
        
        public void Dispose() {
            Services.Framework.Update -= Monitor;
        }

        public void Init() {
            Services.Framework.Update += Monitor;
            EventManager.OnGPoseChange += OnGPoseChange;
        }
        private void OnGPoseChange(bool isingpose)
        {
            if (!isingpose) return;
            
            WetnessOverrides = new();
            WetnessOverridesEnabled = new();
        }
        
        private unsafe void Monitor(IFramework framework)
        {
            if (!Ktisis.IsInGPose) return;
            

            foreach ((var charAddress, var (weatherWetness, swimmingWetness, wetnessDepth)) in WetnessOverrides)
            {
                if (WetnessOverridesEnabled.TryGetValue(charAddress, out var enabled) && !enabled) 
                    continue;
                    
                var actor = (Actor*) charAddress;
                actor->Model->WeatherWetness = weatherWetness;
                actor->Model->SwimmingWetness = swimmingWetness;
                actor->Model->WetnessDepth = wetnessDepth;
            }
        }
    }
}
