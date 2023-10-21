using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using ImGuizmoNET;

using Dalamud.Game.ClientState.Keys;

using FFXIVClientStructs.FFXIV.Client.UI;

using Ktisis.Camera;
using Ktisis.Events;
using Ktisis.Overlay;
using Ktisis.Interop.Hooks;
using Ktisis.Structs.Bones;
using Ktisis.Structs.Input;
using Ktisis.Interface.Components;

namespace Ktisis.Interface {
	public static class Input {
		// When adding a new keybind:
		//  - add the logic in Monitor
		//      (held/release/changed [+ extra conditions] and what it executes )
		//  - add the key action in Purpose enum
		//  - add the default key in DefaultKeys
		//  - add translation, handle format: Keyboard_Action_{Purpose}

		internal static bool HandleHeldPurposes(VirtualKey key) {
			foreach ((var p, var c) in PurposesCategoriesHold) {
				if (IsPurposeUsed(p, key)) {
					c.ToggleVisibilityOverload();
					return true;
				}
			}

			var purpose = GetPurposeFromInput(key);
			switch (purpose) {
				case Purpose.HoldAllCategoryVisibilityOverload:
					Category.ToggleAllVisibilityOverload();
					return true;
				case Purpose.HoldToHideSkeleton:
					Skeleton.Toggle();
					return true;
				case Purpose.NextCamera:
					CameraService.ChangeCameraIndex(1);
					break;
				case Purpose.PreviousCamera:
					CameraService.ChangeCameraIndex(-1);
					break;
				case Purpose.ToggleFreeCam:
					CameraService.ToggleFreecam();
					break;
				case Purpose.NewCamera:
					var camera = CameraService.SpawnCamera();
					CameraService.SetOverride(camera);
					break;
			}

			return false;
		}

		internal static bool OnKeyPressed(QueueItem input) {
			if (!Ktisis.Configuration.EnableKeybinds || input.Event != KeyEvent.Pressed || IsChatInputActive())
				return false;

			if (HandleHeldPurposes(input.VirtualKey))
				return true;

			// Toggled purposes
			foreach ((var p, var c) in PurposesCategoriesToggle)
				if (IsPurposeUsed(p, input.VirtualKey))
					c.ToggleVisibilityOverload();

			// Purposes
			var purpose = GetPurposeFromInput(input.VirtualKey);
			if (purpose != null) {
				var res = true;

				var isUsing = ImGuizmo.IsUsing();
				switch (purpose) {
					case Purpose.SwitchToTranslate:
						if (!isUsing) Ktisis.Configuration.GizmoOp = OPERATION.TRANSLATE;
						break;
					case Purpose.SwitchToRotate:
						if (!isUsing) Ktisis.Configuration.GizmoOp = OPERATION.ROTATE;
						break;
					case Purpose.SwitchToScale:
						if (!isUsing) Ktisis.Configuration.GizmoOp = OPERATION.SCALE;
						break;
					case Purpose.SwitchToUniversal:
						if (!isUsing) Ktisis.Configuration.GizmoOp = OPERATION.UNIVERSAL;
						break;
					case Purpose.ToggleLocalWorld:
						if (!isUsing) Ktisis.Configuration.GizmoMode = Ktisis.Configuration.GizmoMode == MODE.WORLD ? MODE.LOCAL : MODE.WORLD;
						break;
					case Purpose.ClearCategoryVisibilityOverload:
						Category.VisibilityOverload.Clear();
						break;
					case Purpose.CircleThroughSiblingLinkModes:
						ControlButtons.CircleTroughSiblingLinkModes();
						break;
					case Purpose.DeselectGizmo:
						if (OverlayWindow.GizmoOwner == null)
							res = false;
						else
							OverlayWindow.DeselectGizmo();
						break;
					case Purpose.BoneSelectionUp:
						if (Selection.Selecting)
							Selection.ScrollIndex--;
						else
							res = false;
						break;
					case Purpose.BoneSelectionDown:
						if (Selection.Selecting)
							Selection.ScrollIndex++;
						else
							res = false;
						break;
				}

				return res;
			}

			return false;
		}

		internal static void OnKeyReleased(VirtualKey key) {
			if (!Ktisis.Configuration.EnableKeybinds || IsChatInputActive())
				return;

			HandleHeldPurposes(key);
		}

		internal static Purpose? GetPurposeFromInput(VirtualKey input) {
			foreach (Purpose purpose in Purposes) {
				if (IsPurposeUsed(purpose, input))
					return purpose;
			}

			return null;
		}

		internal static bool IsPurposeUsed(Purpose purpose, VirtualKey input) {
			var keys = PurposeToVirtualKeys(purpose);

			var match = keys.Count > 0;
			foreach (var key in keys) {
				if (!Services.KeyState.IsVirtualKeyValid(key))
					return false;
				match &= key == input || ControlHooks.KeyboardState.IsKeyDown(key);
			}

			return match;
		}

		[Serializable]
		public enum Purpose {
			GlobalModifierKey,
			SwitchToTranslate,
			SwitchToRotate,
			SwitchToScale,
			ToggleLocalWorld,
			HoldToHideSkeleton,
			SwitchToUniversal,
			ClearCategoryVisibilityOverload,
			HoldAllCategoryVisibilityOverload,
			CircleThroughSiblingLinkModes,
			DeselectGizmo,
			BoneSelectionUp,
			BoneSelectionDown,
			NextCamera,
			PreviousCamera,
			ToggleFreeCam,
			NewCamera,
		}

		public static readonly Dictionary<Purpose, List<VirtualKey>> DefaultKeys = new(){
			{Purpose.GlobalModifierKey, new(){VirtualKey.NO_KEY}},
			{Purpose.SwitchToTranslate, new(){VirtualKey.G}},
			{Purpose.SwitchToRotate, new(){VirtualKey.R}},
			{Purpose.SwitchToScale, new(){VirtualKey.T}},
			{Purpose.ToggleLocalWorld, new(){VirtualKey.X}},
			{Purpose.HoldToHideSkeleton, new(){VirtualKey.V}},
			{Purpose.SwitchToUniversal, new(){VirtualKey.U}},
			{Purpose.ClearCategoryVisibilityOverload, new(){VirtualKey.J}},
			{Purpose.HoldAllCategoryVisibilityOverload, new(){VirtualKey.J, VirtualKey.SHIFT}},
			{Purpose.CircleThroughSiblingLinkModes, new(){VirtualKey.C}},
			{Purpose.DeselectGizmo, new(){VirtualKey.ESCAPE}},
			{Purpose.BoneSelectionUp, new(){VirtualKey.UP}},
			{Purpose.BoneSelectionDown, new(){VirtualKey.DOWN}},
			{Purpose.NextCamera, new(){VirtualKey.OEM_6}},
			{Purpose.PreviousCamera, new(){VirtualKey.OEM_4}},
		};

		// Init & dispose

		public static void Init() {
			EventManager.OnKeyPressed += OnKeyPressed;
			EventManager.OnKeyReleased += OnKeyReleased;
		}
		public static void Dispose() {
			EventManager.OnKeyPressed -= OnKeyPressed;
			EventManager.OnKeyReleased -= OnKeyReleased;
		}

		// Below are the methods and variables needed for Monitor to handle inputs
		public static List<VirtualKey> FallbackKey = new() { VirtualKey.NO_KEY };
		public static readonly Dictionary<Purpose, Category> PurposesCategories = new();
		public static readonly Dictionary<Purpose, Category> PurposesCategoriesHold = new();
		public static readonly Dictionary<Purpose, Category> PurposesCategoriesToggle = new();
		public const int FirstCategoryPurposeHold = 1000;
		public const int FirstCategoryPurposeToggle = 2000;

		private static Dictionary<Purpose, bool> PreviousKeyStates = new();
		private static Dictionary<Purpose, bool>? CurrentKeyStates = new();

		public static IEnumerable<Purpose> Purposes {
			get => Enum.GetValues<Purpose>().ToImmutableList();
		}

		public static IEnumerable<Purpose> PurposesWithCategories {
			get {
				var purposesWithCategories = Enum.GetValues<Purpose>().ToList();

				int i = FirstCategoryPurposeHold; // start of categories in Purpose enum
				foreach (var category in Category.Categories) {
					PurposesCategories.TryAdd((Purpose)i, category.Value);
					PurposesCategoriesHold.TryAdd((Purpose)i, category.Value);
					purposesWithCategories.Add((Purpose)i);
					i++;
				}

				i = FirstCategoryPurposeToggle; // start of categories in Purpose enum
				foreach (var category in Category.Categories) {
					PurposesCategories.TryAdd((Purpose)i, category.Value);
					PurposesCategoriesToggle.TryAdd((Purpose)i, category.Value);
					purposesWithCategories.Add((Purpose)i);
					i++;
				}

				return purposesWithCategories;
			}
		}

		private static List<VirtualKey> PurposeToVirtualKeys(Purpose purpose) {
			if (!Ktisis.Configuration.KeyBinds.TryGetValue(purpose, out List<VirtualKey>? keys)) {
				if (!DefaultKeys.TryGetValue(purpose, out List<VirtualKey>? defaultKeys))
					defaultKeys = FallbackKey;
				keys = defaultKeys;
			}

			if (keys == null)
				return FallbackKey;

			foreach (var key in keys)
				if (!Services.KeyState.IsVirtualKeyValid(key))
					return FallbackKey;

			return keys;
		}

		private static void ReadPurposesStates() {
			CurrentKeyStates = PurposesWithCategories.Select(p => {
				var keys = PurposeToVirtualKeys(p);
				bool state = true;

				// check if any other key is pressed, if yes, the state is not true (e.g. to have an action with V, and another with ctrl+V)
				var allowedKeys = keys.Union(PurposeToVirtualKeys(Purpose.GlobalModifierKey));
				var otherHeld = Enum.GetValues<VirtualKey>().Any(k => Services.KeyState.IsVirtualKeyValid(k) && !allowedKeys.Any(a => k == a) && Services.KeyState[k]);

				if (keys == FallbackKey || otherHeld) state = false;
				else foreach (var key in keys)
						state &= Services.KeyState[key];

				return (purpose: p, state);
			}).ToDictionary(kp => kp.purpose, kp => kp.state);
		}
		internal unsafe static bool IsChatInputActive() => ((UIModule*)Services.GameGui.GetUIModule())->GetRaptureAtkModule()->AtkModule.IsTextInputActive();
	}
}
