using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using Dalamud.Logging;
using Dalamud.Game.ClientState.Keys;

using Ktisis.Events;
using Ktisis.Overlay;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;
using Ktisis.Structs.Input;
using Ktisis.Interop.Hooks;
using Ktisis.Interface.Components;
using Ktisis.Structs.Actor.State;

namespace Ktisis.History {
	public static class HistoryManager {
		public static List<HistoryItem>? History { get; set; }
		private static int _currentStateIndex;
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
			if (History == null) return false;

			if (ControlHooks.KeyboardState!.IsKeyDown(VirtualKey.CONTROL)) {
				if (input.VirtualKey == VirtualKey.Z) {
					if (_currentStateIndex > 0) {
						_currentStateIndex--;
						UpdateSkeleton();
						PluginLog.Verbose($"Current Idx: {_currentStateIndex}");
						PrintHistory(_currentStateIndex);
						PluginLog.Verbose("CTRL+Z pressed. Undo.");
					}
					return true;
				}
				else if (input.VirtualKey == VirtualKey.Y) {
					if (_currentStateIndex < (History.Count - 1)) {
						_currentStateIndex++;
						UpdateSkeleton();
						PluginLog.Verbose($"Current Idx: {_currentStateIndex}");
						PrintHistory(_currentStateIndex);
						PluginLog.Verbose("CTRL+Y pressed. Redo.");
					}
					return true;
				}
			}

			return false;
		}

		internal static void OnGPoseChange(ActorGposeState _state) {
			PluginLog.Verbose("Clearing previous history...");
			_currentStateIndex = -1;
			History = new List<HistoryItem>();
		}

		//TODO: Find a way to know what's the currently modified item to be able to add the correct entry to the history.
		private unsafe static void OnGizmoChange(GizmoState state) {
			if (!PoseHooks.PosingEnabled) return;
			if (History == null) return;

			var newState = state;
			if ((newState == GizmoState.EDITING) && (_currentGizmoState == GizmoState.IDLE)) {
				PluginLog.Verbose("Started Gizmo edit");
				if ((History.Count - 1) != _currentStateIndex) createNewTimeline();
				UpdateHistory("ActorBone");
			}
			if (newState == GizmoState.IDLE && _currentGizmoState == GizmoState.EDITING) {
				PluginLog.Verbose("Ended Gizmo edit");
				UpdateHistory("ActorBone");
			}
			_currentGizmoState = newState;
		}

		private static unsafe void UpdateHistory(string entryType) {
			try {
				HistoryItem entryToAdd = HistoryItemFactory.Create(entryType);
				AddEntryToHistory(entryToAdd);
			} catch (System.ArgumentException e) {
				PluginLog.Warning("Error in HistoryManager::UpdateHistory - " + e.Message);
				return;
			}
		}

		//TODO: Find a way to know what's the currently modified item to be able to add the correct entry to the history.
		private unsafe static void OnTransformationMatrixChange(TransformTableState state, Matrix4x4 matrix, Bone? bone, Actor* actor) {
			if (!PoseHooks.PosingEnabled) return;
			if (History == null) return;

			var newState = state;

			if ((newState == TransformTableState.EDITING) && (_currentTtState == TransformTableState.IDLE)) {
				PluginLog.Verbose("Started TT edit");
				if ((History.Count - 1) != _currentStateIndex) createNewTimeline();
				UpdateHistory("ActorBone");
			}

			if ((newState == TransformTableState.IDLE) && (_currentTtState == TransformTableState.EDITING)) {
				PluginLog.Verbose("Finished TT edit");
				UpdateHistory("ActorBone");
			}

			_currentTtState = newState;
		}

		// Methods

		public unsafe static void AddEntryToHistory(HistoryItem historyItem) {
			if (History == null) {
				PluginLog.Warning("Attempted to add an entry to an uninitialised history list.");
				return;
			}
			History.Add(historyItem);
			_currentStateIndex++;
		}

		private unsafe static void UpdateSkeleton() {
			History![_currentStateIndex].Update();
		}

		private static void alternativeTimelineWarning() {
			_alternativeTimelinesCreated++;
			PluginLog.Verbose($"By changing the past, you've created a different future. You've created {_alternativeTimelinesCreated} different timelines.");
		}

		private static void createNewTimeline() {
			if (History is null) return;

			alternativeTimelineWarning();
			History.RemoveRange(_currentStateIndex, History.Count - _currentStateIndex);
		}

		// Debugging

		private static void PrintHistory(int until) {
			if (History == null) return;
			if (until == -1) return;

			var str = "\n";
			for (int i = 0; i <= until; i++) {
				str += $"{i}: {History[i].DebugPrint()}\n";
			}
			PluginLog.Verbose(str);
		}
	}
}
