using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Keys;

using Ktisis.Events;
using Ktisis.Overlay;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;
using Ktisis.Structs.Input;
using Ktisis.Interop.Hooks;
using Ktisis.Interface.Components;
using Ktisis.Structs.Actor.State;
using Dalamud.Logging;

namespace Ktisis.History {
	public static class HistoryManager {
		public static List<HistoryItem>? History { get; set; }
		private static int _currentIdx = -1;
		private static int _maxIdx = -1;
		private static GizmoState _currentGizmoState;
		private static TransformTableState _currentTtState;
		private static int _alternativeTimelinesCreated = 0;

		public static bool CanRedo => _currentIdx < _maxIdx;
		public static bool CanUndo => _currentIdx >= 1;

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
					Undo();
					return true;
				}
				if (input.VirtualKey == VirtualKey.Y) {
					Redo();
					return true;
				}
			}
			return false;
		}

		public static void Redo() {
			if (!CanRedo)
				return;

			_currentIdx++;
			UpdateSkeleton(false);
		}

		public static void Undo() {
			if (!CanUndo)
				return;

			UpdateSkeleton(true);
			_currentIdx--;
		}

		internal static void OnGPoseChange(ActorGposeState _state) {
			Logger.Verbose("Clearing previous history...");
			_currentIdx = 0;
			_maxIdx = 0;
			History = new List<HistoryItem>();
		}

		//TODO: Find a way to know what's the currently modified item to be able to add the correct entry to the history.

		public static ActorBone? CurrentBone = null;

		private unsafe static void OnGizmoChange(GizmoState state) {
			if (!PoseHooks.PosingEnabled) return;
			if (History == null) return;

			var newState = state;

			if ((newState == GizmoState.EDITING) && (_currentGizmoState == GizmoState.IDLE)) {
				if (_maxIdx != _currentIdx) createNewTimeline();
				UpdateHistory(HistoryItemType.ActorBone);
			}

			if (newState == GizmoState.IDLE && _currentGizmoState == GizmoState.EDITING) {
				var x = _currentIdx - 1;
				if (x >= 0 && x < History.Count)
					((ActorBone)History[x]).SetMatrix(false);
			}

			_currentGizmoState = newState;
		}

		private static unsafe void UpdateHistory(HistoryItemType entryType) {
			try {
				var entryToAdd = HistoryItemFactory.Create(HistoryItemType.ActorBone);
				if (entryToAdd != null)
					AddEntryToHistory(entryToAdd);
			} catch (System.ArgumentException e) {
				Logger.Fatal(e.Message);
				return;
			}
		}

		//TODO: Find a way to know what's the currently modified item to be able to add the correct entry to the history.
		private unsafe static void OnTransformationMatrixChange(TransformTableState state, Matrix4x4 matrix, Bone? bone, Actor* actor) {
			if (!PoseHooks.PosingEnabled) return;
			if (History == null) return;

			var newState = state;

			if ((newState == TransformTableState.EDITING) && (_currentTtState == TransformTableState.IDLE)) {
				Logger.Verbose("Started TT edit");
				if (_maxIdx != _currentIdx) createNewTimeline();
				UpdateHistory(HistoryItemType.ActorBone);
			}

			if ((newState == TransformTableState.IDLE) && (_currentTtState == TransformTableState.EDITING)) {
				Logger.Verbose("Finished TT edit");
				((ActorBone)History[_currentIdx - 1]).SetMatrix(false);
			}

			_currentTtState = newState;
		}

		// Methods

		public unsafe static void AddEntryToHistory(HistoryItem historyItem) {
			if (History == null) {
				Logger.Warning("Attempted to add an entry to an uninitialised history list.");
				return;
			}
			History.Insert(_maxIdx, historyItem);
			_currentIdx++;
			_maxIdx++;
		}

		private unsafe static void UpdateSkeleton(bool undo) {
			History![_currentIdx - 1].Update(undo);
		}

		private static void createNewTimeline() {
			if (History is null) return;

			Logger.Verbose($"By changing the past, you've created a different future. You've created {_alternativeTimelinesCreated} different timelines.");

			var newHistory = History.Select(e => e.Clone()).ToList().GetRange(0, _currentIdx + 1);
			HistoryItem currentElem = newHistory[_currentIdx];
			var newMaxIdx = _currentIdx;
			History = newHistory!.GetRange(0, newMaxIdx);
			_maxIdx = newMaxIdx;
			_currentIdx = newMaxIdx;
		}
	}
}