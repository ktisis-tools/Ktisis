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
		private static int _currentIdx = -1;
		private static int _maxIdx = -1;
		private static bool _currentState;
		private static int _alternativeTimelinesCreated = 0;

		public static bool CanRedo => _currentIdx < _maxIdx;
		public static bool CanUndo => _currentIdx >= 1;

		// Init & Dispose

		public static void Init() {
			EventManager.OnKeyPressed += OnInput;
			EventManager.OnGPoseChange += OnGPoseChange;
			EventManager.OnGizmoChange += OnGizmoChange;
			EventManager.OnTransformationMatrixChange += OnGizmoChange;
		}

		public static void Dispose() {
			EventManager.OnKeyPressed -= OnInput;
			EventManager.OnGPoseChange -= OnGPoseChange;
			EventManager.OnGizmoChange -= OnGizmoChange;
			EventManager.OnTransformationMatrixChange -= OnGizmoChange;
		}

		// Events

		public static bool OnInput(QueueItem input) {
			if (EventManager.IsKeyDown(VirtualKey.CONTROL)) {
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

		private static void OnGizmoChange(bool isEditing) {
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

		private static void UpdateHistory(HistoryItemType entryType) {
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

		public static void AddEntryToHistory(HistoryItem historyItem) {
			if (History == null) {
				Logger.Warning("Attempted to add an entry to an uninitialised history list.");
				return;
			}
			History.Insert(_maxIdx, historyItem);
			_currentIdx++;
			_maxIdx++;
		}

		private static void UpdateSkeleton(bool undo) {
			History![_currentIdx - 1].Update(undo);
		}

		private static void createNewTimeline() {
			if (History is null) return;

			Logger.Verbose($"By changing the past, you've created a different future. You've created {_alternativeTimelinesCreated} different timelines.");

			var newHistory = History.Select(e => e.Clone()).ToList().GetRange(0, _currentIdx + 1);
			HistoryItem currentElem = newHistory[_currentIdx];
			var newMaxIdx = _currentIdx;
			History = newHistory.GetRange(0, newMaxIdx);
			_maxIdx = newMaxIdx;
			_currentIdx = newMaxIdx;
		}
	}
}
