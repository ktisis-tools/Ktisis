using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.IoC;
using Dalamud.Logging;
using ImGuiNET;
using Ktisis.Events;
using Ktisis.Interface.Components;
using Ktisis.Interface.Windows;
using Ktisis.Interface.Windows.ActorEdit;
using Ktisis.Localization;
using Ktisis.Overlay;
using Ktisis.Structs.Bones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.History
{
    public sealed class HistoryManager : IDisposable
    {
        public List<HistoryItem> history { get; set; } = new List<HistoryItem>();
        private int currentIdx = 0;
        private GizmoState _currentState;

        private HistoryManager() 
        {
            Dalamud.Framework.Update += this.Monitor;
            EventManager.OnTransformationMatrixChange += this.OnTransformationMatrixChange;
            EventManager.OnGizmoChange += this.OnGizmoChange;
        }

        private void OnTransformationMatrixChange(TransformTable sender, Bone? bone)
        {
            if (bone is null)
            {
                PluginLog.Information("The actor had its global skeleton modified. Saving...");
                return;
            }
            PluginLog.Information($"The actor had its { Locale.GetBoneName(bone.HkaBone.Name.String)} modified. Saving...");
        }

        private unsafe void OnGizmoChange(GizmoState state)
        {
            var newState = state;
            if ((newState == GizmoState.IDLE) && (_currentState == GizmoState.EDITING))
            {
                Bone bone = Skeleton.GetSelectedBone(EditActor.Target->Model->Skeleton)!;
                TransformTable tt = Workspace.Transform;
                PluginLog.Information($"The actor had its {Locale.GetBoneName(bone!.HkaBone.Name.String)} modified via gizmo. Saving...");
            }

            _currentState = newState;
        }


        private static HistoryManager? _instance;

        public static HistoryManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new HistoryManager();
                }
                return _instance!;
            }
        }

        /* 
         This is weird yes. 
         But it exists to make it obvious that it needs to be created first so it can subscribe to the Framework.Update event.
        */
        public static void Init() 
        {
            _ = Instance;
        }

        public void Dispose()
        {
            Dalamud.Framework.Update -= this.Monitor;
            EventManager.OnTransformationMatrixChange -= this.OnTransformationMatrixChange;
            EventManager.OnGizmoChange -= this.OnGizmoChange;
        }

        public void Monitor(Framework framework)
        {
            //PluginLog.Information(Dalamud.Keys[VirtualKey.G].ToString());
        }
    }
}
