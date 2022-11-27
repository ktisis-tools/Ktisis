using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using Dalamud.Logging;
using Dalamud.Game.ClientState.Keys;

using static FFXIVClientStructs.Havok.hkaPose;

using Ktisis.Events;
using Ktisis.Overlay;
using Ktisis.Localization;
using Ktisis.Structs;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;
using Ktisis.Structs.Input;
using Ktisis.Interop.Hooks;
using Ktisis.Interface.Components;
using Ktisis.Structs.Actor.State;
using System;

namespace Ktisis.History {
	public static class HistoryManager {
		public static List<HistoryItem>? History { get; set; }
		private static int _currentIdx = 0;
		private static int _maxIdx = 0;
		private static GizmoState _currentGizmoState;
		private static TransformTableState _currentTtState;
		private static int _alternativeTimelinesCreated = 0;

		// Init & Dispose

		public unsafe static void Init() {
			EventManager.OnKeyPressed += OnInput;
			EventManager.OnGPoseChange += OnGPoseChange;
			EventManager.OnGizmoChange += OnGizmoChange;
			EventManager.OnTransformationMatrixChange += OnTransformationMatrixChange;
		}

		public unsafe static void Dispose() {
			EventManager.OnKeyPressed -= OnInput;
			EventManager.OnGPoseChange -= OnGPoseChange;
			EventManager.OnGizmoChange -= OnGizmoChange;
			EventManager.OnTransformationMatrixChange -= OnTransformationMatrixChange;
		}

		// Events

		public static bool OnInput(QueueItem input) {
			if (ControlHooks.KeyboardState!.IsKeyDown(VirtualKey.CONTROL)) {
				if (input.VirtualKey == VirtualKey.Z) {
					if (_currentIdx > 1) {
						_currentIdx--;
						PluginLog.Verbose($"Current Idx: {_currentIdx}");
						UpdateSkeleton();
						PluginLog.Verbose("CTRL+Z pressed. Undo.");
					}
					return true;
				} else if (input.VirtualKey == VirtualKey.Y) {
					if (_currentIdx < _maxIdx) {
						_currentIdx++;
						PluginLog.Verbose($"Current Idx: {_currentIdx}");
						UpdateSkeleton();
						PluginLog.Verbose("CTRL+Y pressed. Redo.");
					}
					return true;
				}
			}

			return false;
		}

		internal static void OnGPoseChange(ActorGposeState _state) {
			PluginLog.Verbose("Clearing previous history...");
			_currentIdx = 0;
			_maxIdx = 0;
			History = new List<HistoryItem>();
		}

		private unsafe static void OnGizmoChange(GizmoState state) {
			if (!PoseHooks.PosingEnabled) return;
			if (History == null) return;

			var bone = Skeleton.GetSelectedBone();
			if (bone == null) return;

			var newState = state;
			var boneTransform = bone!.AccessModelSpace(PropagateOrNot.DontPropagate);
			var matrix = Interop.Alloc.GetMatrix(boneTransform);

			if ((newState == GizmoState.EDITING) && (_currentGizmoState == GizmoState.IDLE)) {
				PluginLog.Verbose("Started Gizmo edit");
				if (_maxIdx != _currentIdx) alternativeTimelineWarning();
				HistoryItem entryToAdd = new ActorBone(matrix, bone);
				if (!entryToAdd.IsElemInHistory()) AddEntryToHistory(new ActorBone(matrix, bone));
			}
			if (newState == GizmoState.IDLE && _currentGizmoState == GizmoState.EDITING) {
				PluginLog.Verbose("Ended Gizmo edit");
				AddEntryToHistory(new ActorBone(matrix, bone));
			}
			_currentGizmoState = newState;
		}

		private unsafe static void OnTransformationMatrixChange(TransformTableState state, Matrix4x4 matrix, Bone? bone, Actor* actor) {
			if (!PoseHooks.PosingEnabled) return;
			if (History == null) return;

			var newState = state;
			if ((newState == TransformTableState.EDITING) && (_currentTtState == TransformTableState.IDLE)) {
				PluginLog.Verbose("Started TT edit");
				if (_maxIdx != _currentIdx) alternativeTimelineWarning();
				HistoryItem entryToAdd = new ActorBone(matrix, bone);
				if (!entryToAdd.IsElemInHistory()) AddEntryToHistory(new ActorBone(matrix, bone));
			}

			if ((newState == TransformTableState.IDLE) && (_currentTtState == TransformTableState.EDITING)) {
				PluginLog.Verbose("Finished TT edit");
				AddEntryToHistory(new ActorBone(matrix, bone));
			}

			_currentTtState = newState;
		}

		// Methods

		private unsafe static void AddEntryToHistory(HistoryItem historyItem) {
			if (History == null) {
				PluginLog.Warning("Attempted to add entry to uninitialised History list.");
				return;
			}
			if (historyItem == null) {
				PluginLog.Warning("Attempted to add a null entry to the history list.");
				return;
			}

			HistoryItem? historyToAdd =
				historyItem switch {
					ActorBone actorBone => new ActorBone(actorBone.TransformationMatrix, actorBone.Bone),
					_ => null,
				};
			if (historyToAdd == null) return;

			History.Insert(_maxIdx, historyToAdd);
			_currentIdx++;
			_maxIdx++;
		}

		private unsafe static void UpdateSkeleton() {
			History![_currentIdx - 1].Update();
		}

		// Debugging

		private static void PrintHistory(int until) {
			if (History == null) return;
			var str = "\n";
			for (int i = 0; i < until; i++) {
				str += $"{i + 1}: {History[i].DebugPrint()}\n";
			}
			PluginLog.Verbose(str);
		}

		private static void alternativeTimelineWarning() {
			if (History is null) return;

			_alternativeTimelinesCreated++;
			PluginLog.Verbose($"By changing the past, you've created a different future. You've created {_alternativeTimelinesCreated} different timelines.");
			createNewTimeline();
		}

		private static void createNewTimeline() {
			var newHistory = History!
							.Select(e => e.Clone())
							.ToList()
							.GetRange(0, _currentIdx);

			HistoryItem? currentElem = newHistory[_currentIdx - 1];
			var isElemInHistory = currentElem.IsElemInHistory();

			//We need to decrement by 1 if there is only one appearance of that elem in the History => It is the idle state.
			//IMPORTANT: Could this cause an issue? 
			var offset = isElemInHistory ? 1 : 0;

			var newMaxIdx = _currentIdx - offset;
			History = newHistory!.GetRange(0, newMaxIdx);
			_maxIdx = newMaxIdx;
			_currentIdx = newMaxIdx;
		}
	}
}
