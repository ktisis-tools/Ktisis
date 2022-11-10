using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.IoC;
using Dalamud.Logging;
using FFXIVClientStructs.Havok;
using ImGuiNET;
using Ktisis.Events;
using Ktisis.Helpers;
using Ktisis.Interface.Components;
using Ktisis.Interface.Windows;
using Ktisis.Interface.Windows.ActorEdit;
using Ktisis.Localization;
using Ktisis.Overlay;
using Ktisis.Structs;
using Ktisis.Structs.Actor;
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
        public List<HistoryItem>? History { get; set; }
        private int _currentIdx = 0;
        private int _maxIdx = 0;
        private GizmoState _currentState;
        private bool _isInGpose = false;
        private bool _undoIsPressed;
        private bool _redoIsPressed;
        private int _alternativeTimelinesCreated = 0;

        private HistoryManager() 
        {
            Dalamud.Framework.Update += this.Monitor;
            EventManager.OnTransformationMatrixChange += this.OnTransformationMatrixChange;
            EventManager.OnGizmoChange += this.OnGizmoChange;
        }

        private void OnTransformationMatrixChange(TransformTable tt, Bone? bone)
        {
            if (_maxIdx != _currentIdx)
            {
                alternativeTimelineWarning();
            }
            _maxIdx = _currentIdx;
            AddEntryToHistory(tt, bone);
            printHistory();
        }

        private unsafe void AddEntryToHistory(TransformTable tt, Bone? bone)
        {
            History!.Insert(_maxIdx, new(tt.Clone(), bone, (Actor*)Ktisis.GPoseTarget!.Address));
            _currentIdx++;
            _maxIdx++;
            PluginLog.Information($"Current Idx: {_currentIdx} - Max Idx: {_maxIdx}");
        }

        private void printHistory()
        {
            var str = "\n";
            for (int i = 0; i < _maxIdx; i++)
            {
                str += $"{i + 1}: ";
                var entry = History![i];
                if (entry.IsGlobalTransform)
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
                AddEntryToHistory(Workspace.Transform.Clone(), Skeleton.GetSelectedBone(EditActor.Target->Model->Skeleton));
                _maxIdx = _currentIdx; //Discarding everything contained after _currentIdx because the user won't need it anymore.
                if (_maxIdx != _currentIdx)
                {
                    alternativeTimelineWarning();
                }
                printHistory();
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

        public unsafe void Monitor(Framework framework)
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
                _currentIdx = 0;
                _maxIdx = 0;
                History = new List<HistoryItem>();
            }

            if (newUndoIsPressed != _undoIsPressed)
            {
                //Without this check, anything inside  'if (newUndoIsPressed != _undoIsPressed)' gets executed twice.
                //The first time when CTRL and Z are pressed together.
                //The second time when either CTRL or Z is released.
                if (newUndoIsPressed) 
                {
                    if (_currentIdx > 1)
                    {
                        _currentIdx--;
                        UpdateSkeleton();
                        PluginLog.Information($"CTRL+Z pressed. Undo.");
                    }


                }
            }

            if (newRedoIsPressed != _redoIsPressed)
            {
                if (newRedoIsPressed)
                {
                    if (_currentIdx < _maxIdx)
                    {
                        _currentIdx++;
                        UpdateSkeleton();
                        PluginLog.Information("CTRL+Y pressed. Redo.");
                    }
                    
                }
            }

            _isInGpose = newIsInGpose;
            _undoIsPressed = newUndoIsPressed;
            _redoIsPressed = newRedoIsPressed;
        }

        private unsafe void UpdateSkeleton()
        {
            var historyToUndo = History![_currentIdx - 1];
            var damnQuaternion = MathHelpers.ToQuaternion(historyToUndo.Tt.Rotation);
            var transformToRollbackTo = historyToUndo.Tt;
            var bone = historyToUndo.Bone;
            var actor = historyToUndo.Actor;
            if (historyToUndo.IsGlobalTransform)
            {
                actor->Model->Position = transformToRollbackTo.Position;
                actor->Model->Rotation = damnQuaternion;
                actor->Model->Scale = transformToRollbackTo.Scale;
            }
            else
            {
                hkVector4f bonePos = new();
                hkVector4f boneScale = new();
                hkQuaternionf boneRot = new();
                var boneTransform = bone!.Transform;
                bonePos = bonePos.SetFromVector3(transformToRollbackTo.Position);
                var rad = MathHelpers.ToRadians(transformToRollbackTo.Rotation);
                boneRot.setFromEulerAngles1(rad.X, rad.Y, rad.Z);
                boneScale = boneScale.SetFromVector3(transformToRollbackTo.Scale);

                boneTransform.Translation = bonePos;
                boneTransform.Rotation = boneRot;
                boneTransform.Scale = boneScale;
            }
        }

        private void alternativeTimelineWarning()
        {
            _alternativeTimelinesCreated++;
            PluginLog.Information($"By changing the past, you've created a different future. You've created {_alternativeTimelinesCreated} different timelines.");
        }
    }
}
