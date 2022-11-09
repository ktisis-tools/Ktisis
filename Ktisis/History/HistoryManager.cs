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
        public List<HistoryItem> History { get; set; }
        private int _currentIdx = 0;
        private GizmoState _currentState;
        private bool _isInGpose = false;
        private bool _undoIsPressed;
        private bool _redoIsPressed;

        private HistoryManager() 
        {
            Dalamud.Framework.Update += this.Monitor;
            EventManager.OnTransformationMatrixChange += this.OnTransformationMatrixChange;
            EventManager.OnGizmoChange += this.OnGizmoChange;
        }

        private void OnTransformationMatrixChange(TransformTable tt, Bone? bone)
        {
            AddEntryToHistory(tt, bone);
        }

        private void AddEntryToHistory(TransformTable tt, Bone? bone)
        {
            if (bone is null)
            {
                History.Add(new(tt.Clone(), null));
            } else
            {
                History.Add(new(tt.Clone(), bone));
            }
            _currentIdx++;
            printHistory();
        }

        private void printHistory()
        {
            var str = "";
            foreach(HistoryItem entry in History)
            {
                if (entry.Bone is null)
                {
                    str += $"Pos: {entry.Tt.Position} - Rot: {entry.Tt.Rotation} - Scale: {entry.Tt.Scale} | Bone Global \n";
                    continue;
                }
                str += $"Pos: {entry.Tt.Position} - Rot: {entry.Tt.Rotation} - Scale: {entry.Tt.Scale} | Bone {Locale.GetBoneName(entry.Bone!.HkaBone.Name.String)}\n";
            }
            PluginLog.Information(str);
        }

        private unsafe void OnGizmoChange(GizmoState state)
        {
            var newState = state;
            if ((newState == GizmoState.IDLE) && (_currentState == GizmoState.EDITING))
            {
                Bone bone = Skeleton.GetSelectedBone(EditActor.Target->Model->Skeleton)!;
                TransformTable tt = Workspace.Transform;
                AddEntryToHistory(tt.Clone(), bone);
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
            if (!Ktisis.IsInGPose)
            {
                _isInGpose = Ktisis.IsInGPose;
                return;
            }

            var newIsInGpose = Ktisis.IsInGPose;
            var newUndoIsPressed = Dalamud.Keys[VirtualKey.CONTROL] && Dalamud.Keys[VirtualKey.Z];
            var newRedoIsPressed = Dalamud.Keys[VirtualKey.CONTROL] && Dalamud.Keys[VirtualKey.Y];

            if (newIsInGpose != _isInGpose)
            {
                PluginLog.Information("Clearing previous history...");
                History = new List<HistoryItem>();
            }

            if (newUndoIsPressed != _undoIsPressed)
            {
                //Without this check, anything inside  'if (newUndoIsPressed != _undoIsPressed)' gets executed twice.
                //The first time when CTRL and Z are pressed together.
                //The second time when either CTRL or Z is released.
                if (newUndoIsPressed) 
                {
                    PluginLog.Information($"CTRL+Z pressed. Undo.");
                }
            }

            if (newRedoIsPressed != _redoIsPressed)
            {
                if (newRedoIsPressed)
                {
                    PluginLog.Information("CTRL+Y pressed. Redo.");
                }
            }

            _isInGpose = newIsInGpose;
            _undoIsPressed = newUndoIsPressed;
            _redoIsPressed = newRedoIsPressed;
        }
    }
}
