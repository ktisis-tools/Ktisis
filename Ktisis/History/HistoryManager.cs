using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.IoC;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.UI;
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
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static FFXIVClientStructs.Havok.hkaPose;

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

        private void alternativeTimelineWarning()
        {
            _alternativeTimelinesCreated++;
            PluginLog.Information($"By changing the past, you've created a different future. You've created {_alternativeTimelinesCreated} different timelines.");
        }

        private unsafe HistoryManager() 
        {
            Dalamud.Framework.Update += this.Monitor;
            EventManager.OnTransformationMatrixChange += this.OnTransformationMatrixChange;
            EventManager.OnGizmoChange += this.OnGizmoChange;
        }

        private unsafe void OnTransformationMatrixChange(Matrix4x4 matrix, Bone? bone, Actor* actor)
        {
            if (_maxIdx != _currentIdx)
            {
                alternativeTimelineWarning();
            }
            _maxIdx = _currentIdx;
            AddEntryToHistory(matrix, bone);
        }

        private unsafe void AddEntryToHistory(Matrix4x4 tt, Bone? bone)
        {
            History!.Insert(_maxIdx, new(tt, bone, (Actor*)Ktisis.GPoseTarget!.Address));
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
                str += $"Transform Matrix: {entry.TransformationMatrix} | Bone {Locale.GetBoneName(entry.Bone!.HkaBone.Name.String)}\n";
            }
            PluginLog.Information(str);
        }

        private unsafe void OnGizmoChange(GizmoState state)
        {
            var newState = state;
            if ((newState == GizmoState.IDLE) && (_currentState == GizmoState.EDITING))
            {
                var bone = Skeleton.GetSelectedBone(EditActor.Target->Model->Skeleton);
                var boneTransform = bone!.AccessModelSpace(PropagateOrNot.DontPropagate);
                AddEntryToHistory(Interop.Alloc.GetMatrix(boneTransform), Skeleton.GetSelectedBone(EditActor.Target->Model->Skeleton));
                _maxIdx = _currentIdx; //Discarding everything contained after _currentIdx because the user won't need it anymore.
                if (_maxIdx != _currentIdx)
                {
                    alternativeTimelineWarning();
                }
                //printHistory();
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

        public unsafe void Dispose()
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
            var transformToRollbackTo = historyToUndo.TransformationMatrix;
            var bone = historyToUndo.Bone;
            var actor = historyToUndo.Actor;
            hkQsTransformf* boneTransform = bone!.AccessModelSpace(PropagateOrNot.DontPropagate);
            Interop.Alloc.SetMatrix(boneTransform, transformToRollbackTo);  
        }
    }
}
