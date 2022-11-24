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

			var newState = state;
			var bone = Skeleton.GetSelectedBone();
			if (bone == null) return;

			var boneTransform = bone!.AccessModelSpace(PropagateOrNot.DontPropagate);
			var matrix = Interop.Alloc.GetMatrix(boneTransform);

			if ((newState == GizmoState.EDITING) && (_currentGizmoState == GizmoState.IDLE)) {
				PluginLog.Verbose("Started Gizmo edit");
				if (_maxIdx != _currentIdx) alternativeTimelineWarning();
				var isBoneInHistory = History?.FirstOrDefault(historyItem => historyItem.Bone?.UniqueName == bone?.UniqueName) != null;
				if (!isBoneInHistory) AddEntryToHistory(matrix, bone);
			}
			if (newState == GizmoState.IDLE && _currentGizmoState == GizmoState.EDITING) {
				PluginLog.Verbose("Ended Gizmo edit");
				AddEntryToHistory(matrix, bone);
			}
			_currentGizmoState = newState;
		}

		private unsafe static void OnTransformationMatrixChange(TransformTableState state, Matrix4x4 matrix, Bone? bone, Actor* actor) {
			if (!PoseHooks.PosingEnabled) return;
			var newState = state;
			if ((newState == TransformTableState.EDITING) && (_currentTtState == TransformTableState.IDLE)) {
				PluginLog.Verbose("Started TT edit");
				if (_maxIdx != _currentIdx) alternativeTimelineWarning();
				var isBoneInHistory = History?.FirstOrDefault(historyItem => historyItem.Bone?.UniqueName == bone?.UniqueName) != null;
				if (!isBoneInHistory) AddEntryToHistory(matrix, bone);
			}

			if ((newState == TransformTableState.IDLE) && (_currentTtState == TransformTableState.EDITING)) {
				PluginLog.Verbose("Finished TT edit");
				AddEntryToHistory(matrix, bone);
			}

			_currentTtState = newState;
		}

		// Methods

		private unsafe static void AddEntryToHistory(Matrix4x4 tt, Bone? bone) {
			if (History == null) {
				PluginLog.Warning("Attempted to add entry to uninitialised History list.");
				return;
			}

			History!.Insert(_maxIdx, new(tt, bone, (Actor*)Ktisis.GPoseTarget!.Address));
			_currentIdx++;
			_maxIdx++;
			PluginLog.Verbose($"Current Idx: {_currentIdx} - Max Idx: {_maxIdx}");
			printHistory(_maxIdx);
		}

		//Thanks Emyka for the help on the bone undo/redo!
		private unsafe static void UpdateSkeleton() {
			var historyToUndo = History![_currentIdx - 1];
			var transformToRollbackTo = historyToUndo.TransformationMatrix;
			var historyBone = historyToUndo.Bone!;
			var isGlobalRotation = historyBone is null;
			var model = historyToUndo.Actor->Model;

			if (model is null) return;
			if (isGlobalRotation) { //There is no bone if you have a global rotation.
				Interop.Alloc.SetMatrix(&model->Transform, transformToRollbackTo);
				return;
			}

			var bone = model->Skeleton->GetBone(historyBone!.Partial, historyBone!.Index);
			var boneTransform = bone!.AccessModelSpace(PropagateOrNot.DontPropagate);

			// Write our updated matrix to memory.
			var initialRot = boneTransform->Rotation.ToQuat();
			var initialPos = boneTransform->Translation.ToVector3();

			Interop.Alloc.SetMatrix(boneTransform, transformToRollbackTo);

			// Bone parenting
			// Adapted from Anamnesis Studio code shared by Yuki - thank you!

			var sourcePos = boneTransform->Translation.ToVector3();
			var deltaRot = boneTransform->Rotation.ToQuat() / initialRot;
			var deltaPos = sourcePos - initialPos;

			UpdateChildren(bone, sourcePos, deltaRot, deltaPos);
		}

		private static unsafe void UpdateChildren(Bone bone, Vector3 sourcePos, Quaternion deltaRot, Vector3 deltaPos) {
			Matrix4x4 matrix;
			var descendants = bone!.GetDescendants();
			foreach (var child in descendants) {
				var access = child.AccessModelSpace(PropagateOrNot.DontPropagate);

				var offset = access->Translation.ToVector3() - sourcePos;
				offset = Vector3.Transform(offset, deltaRot);

				matrix = Interop.Alloc.GetMatrix(access);
				matrix *= Matrix4x4.CreateFromQuaternion(deltaRot);
				matrix.Translation = deltaPos + sourcePos + offset;
				Interop.Alloc.SetMatrix(access, matrix);
			}
		}

		// Debugging

		private static void printHistory(int until) {
			var str = "\n";
			for (int i = 0; i < until; i++) {
				str += $"{i + 1}: ";
				var entry = History![i];
				if (entry.Bone is null) str += $"Bone Global";
				else str += $"Bone {Locale.GetBoneName(entry.Bone!.HkaBone.Name.String)}";
				str += "\n";
			}
			PluginLog.Verbose(str);
		}

		private static void alternativeTimelineWarning() {
			if (History is null) return;

			_alternativeTimelinesCreated++;
			PluginLog.Verbose($"By changing the past, you've created a different future. You've created {_alternativeTimelinesCreated} different timelines.");

			var newHistory = History!
				.Select(e => e.Clone())
				.ToList()
				.GetRange(0, _currentIdx);

			printHistory(_currentIdx - 1);
			var currBone = newHistory[_currentIdx - 1].Bone;
			var isBoneInHistory = newHistory?.FirstOrDefault(historyItem => historyItem.Bone?.UniqueName == currBone?.UniqueName) != null;

			//We need to decrement by 1 if there is only one appearance of that bone in the History => It is the idle state.
			var offset = isBoneInHistory ? 1 : 0;

			var newMaxIdx = _currentIdx - offset;
			History = newHistory!.GetRange(0, newMaxIdx);
			_maxIdx = newMaxIdx;
			_currentIdx = newMaxIdx;
		}
	}
}
