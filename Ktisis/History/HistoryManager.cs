using System;
using System.Linq;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Logging;

using Ktisis.Events;
using Ktisis.Overlay;
using Ktisis.Structs.Input;
using Ktisis.Interop.Hooks;
using Ktisis.Structs.Actor.State;

namespace Ktisis.History {
	public static class HistoryManager {
		public static List<HistoryItem>? History { get; set; }
		private static int _currentIdx = -1;
		private static bool _currentState;
		private static int _alternativeTimelinesCreated = 0;

		public static bool CanRedo => History != null && _currentIdx < History.Count - 1;
		public static bool CanUndo => _currentIdx > -1;

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
			if (ControlHooks.KeyboardState.IsKeyDown(VirtualKey.CONTROL)) {
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

			PluginLog.Information($"Redo: Now on {_currentIdx}");
		}

		public static void Undo() {
			if (!CanUndo)
				return;
			
			UpdateSkeleton(true);
			_currentIdx--;
			
			PluginLog.Information($"Undo: Now on {_currentIdx}");
		}

		internal static void OnGPoseChange(bool isInGpose) {
			Logger.Verbose("Clearing previous history...");
			_currentIdx = -1;
			History = new List<HistoryItem>();
		}

		//TODO: Find a way to know what's the currently modified item to be able to add the correct entry to the history.
		
		private static ActorBone? ActiveEdit = null;
		private static void OnGizmoChange(bool isEditing) {
			if (!PoseHooks.PosingEnabled && !PoseHooks.AnamPosingEnabled) return;
			if (History == null) return;

			if (!Skeleton.BoneSelect.Active) {
				// Because history bugs out when transforming actors.
				// Just return here because this is getting rewritten anyway.
				return;
			}

			if (isEditing && !_currentState) {
				ActiveEdit = (ActorBone?)HistoryItemFactory.Create(HistoryItemType.ActorBone);
			}

			if (!isEditing && _currentState && ActiveEdit != null) {
				ActiveEdit.SetMatrix(false);
				AddEntryToHistory(ActiveEdit);
				ActiveEdit = null;
			}

			_currentState = isEditing;
		}

		// Methods

		public static void AddEntryToHistory(HistoryItem historyItem) {
			if (History == null) {
				Logger.Warning("Attempted to add an entry to an uninitialised history list.");
				return;
			}
			
			_currentIdx++;
			if (_currentIdx < History.Count)
				CreateNewTimeline();
			History.Add(historyItem);
			
		}

		private static void UpdateSkeleton(bool undo) {
			PluginLog.Information($"{undo} {_currentIdx}");
			History![_currentIdx].Update(undo);
		}

		private static void CreateNewTimeline() {
			if (History is null) return;
			History = History.GetRange(0, _currentIdx).Select(e => e.Clone()).ToList();
		}
	}
}
