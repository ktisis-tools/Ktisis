using System.Linq;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Keys;

using Ktisis.Events;
using Ktisis.Structs.Input;
using Ktisis.Interop.Hooks;
using Ktisis.Structs.Actor.State;

namespace Ktisis.History {
	public static class HistoryManager {
		public static List<HistoryItem>? History { get; set; }
		private static int _currentStateIndex;
		private static GizmoState _currentGizmoState;
		private static TransformTableState _currentTtState;
		private static bool _currentState;
		private static int _alternativeTimelinesCreated = 0;

		public static bool CanRedo => _currentIdx < _maxIdx;
		public static bool CanUndo => _currentIdx >= 1;

		// Init & Dispose

		public unsafe static void Init() {
			EventManager.OnKeyPressed += OnInput;
			EventManager.OnGPoseChange += OnGPoseChange;
			EventManager.OnGizmoChange += OnGizmoChange;
			EventManager.OnTransformationMatrixChange += OnGizmoChange;
		}

		public unsafe static void Dispose() {
			EventManager.OnKeyPressed -= OnInput;
			EventManager.OnGPoseChange -= OnGPoseChange;
			EventManager.OnGizmoChange -= OnGizmoChange;
			EventManager.OnTransformationMatrixChange -= OnGizmoChange;
		}

		// Events

		public static bool OnInput(QueueItem input) {
			if (History == null) return false;

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

			_currentStateIndex++;
			UpdateSkeleton(false);
		}

		public static void Undo() {
			if (!CanUndo)
				return;

			UpdateSkeleton(true);
			_currentStateIndex--;
		}

		internal static void OnGPoseChange(ActorGposeState _state) {
			Logger.Verbose("Clearing previous history...");
			_currentStateIndex = -1;
			History = new List<HistoryItem>();
		}

		//TODO: Find a way to know what's the currently modified item to be able to add the correct entry to the history.

		public static ActorBone? CurrentBone = null;

		private unsafe static void OnGizmoChange(bool isEditing) {
			if (!PoseHooks.PosingEnabled && !PoseHooks.AnamPosingEnabled) return;
			if (History == null) return;

			if (isEditing && !_currentState) {
				if (_maxIdx != _currentIdx) createNewTimeline();
				UpdateHistory(HistoryItemType.ActorBone);
			}

			if (!isEditing && _currentState) {
				var x = _currentIdx - 1;
				if (x >= 0 && x < History.Count)
					((ActorBone)History[x]).SetMatrix(false);
			}

			_currentState = isEditing;
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

		// Methods

		public unsafe static void AddEntryToHistory(HistoryItem historyItem) {
			if (History == null) {
				Logger.Warning("Attempted to add an entry to an uninitialised history list.");
				return;
			}
			History.Add(historyItem);
			_currentStateIndex++;
		}

		private unsafe static void UpdateSkeleton(bool undo) {
			History![_currentStateIndex].Update(undo);
		}

		private static void createNewTimeline() {
			if (History is null) return;

			Logger.Verbose($"By changing the past, you've created a different future. You've created {_alternativeTimelinesCreated} different timelines.");

			History.RemoveRange(_currentStateIndex, History.Count - _currentStateIndex);
		}
	}
}