using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuizmoNET;

using Ktisis.Overlay;
using Ktisis.Structs.Bones;

namespace Ktisis.Interface {
	public sealed class Input : IDisposable {
		// When adding a new keybind:
		//  - add the logic in Monitor
		//      (held/release/changed [+ extra conditions] and what it executes )
		//  - add the key action in Purpose enum
		//  - add the default key in DefaultKeys
		//  - add translation, handle format: Keyboard_Action_{Purpose}

		public void Monitor(Framework framework) {
			if (!Ktisis.IsInGPose || IsChatInputActive() || !Ktisis.Configuration.EnableKeybinds) return; // TODO: when implemented move init/dispose to Gpose enter and leave instead of in Ktisis.cs
			ReadPurposesStates();

			if (!ImGuizmo.IsUsing()) {
				if (IsPurposeReleased(Purpose.SwitchToTranslate))
					Ktisis.Configuration.GizmoOp = OPERATION.TRANSLATE;
				if (IsPurposeReleased(Purpose.SwitchToRotate))
					Ktisis.Configuration.GizmoOp = OPERATION.ROTATE;
				if (IsPurposeReleased(Purpose.SwitchToScale))
					Ktisis.Configuration.GizmoOp = OPERATION.SCALE;
				if (IsPurposeReleased(Purpose.SwitchToUniversal))
					Ktisis.Configuration.GizmoOp = OPERATION.UNIVERSAL;
				if (IsPurposeReleased(Purpose.ToggleLocalWorld))
					Ktisis.Configuration.GizmoMode = Ktisis.Configuration.GizmoMode == MODE.WORLD ? MODE.LOCAL : MODE.WORLD;
			}
			if (IsPurposeChanged(Purpose.HoldToHideSkeleton))
				Skeleton.Toggle();

			foreach ((var p, var c) in PurposesCategoriesHold)
				if(IsPurposeChanged(p))
					HoldCategory(c);
			foreach ((var p, var c) in PurposesCategoriesToggle)
				if(IsPurposeReleased(p))
					HoldCategory(c);

			PrevriousKeyStates = CurrentKeyStates!;
			CurrentKeyStates = null;
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
		}

		public static readonly Dictionary<Purpose, List<VirtualKey>> DefaultKeys = new(){
			{Purpose.GlobalModifierKey, new(){VirtualKey.NO_KEY}},
			{Purpose.SwitchToTranslate, new(){VirtualKey.G}},
			{Purpose.SwitchToRotate, new(){VirtualKey.R}},
			{Purpose.SwitchToScale, new(){VirtualKey.T}},
			{Purpose.ToggleLocalWorld, new(){VirtualKey.X}},
			{Purpose.HoldToHideSkeleton, new(){VirtualKey.V}},
			{Purpose.SwitchToUniversal, new(){VirtualKey.U}},
		};

		// Helpers
		public static bool IsHeld(Purpose purpose) {
			if (Instance.PrevriousKeyStates.TryGetValue(purpose, out bool shiftPressed))
				return false;
			return shiftPressed;
		}

		// Thanks to (Edited) for the intgration with the Framework Update <3
		private static Input? _instance = null;
		private Input() {
			Services.Framework.Update += Monitor;
		}
		public static Input Instance {
			get {
				_instance ??= new Input();
				return _instance!;
			}
		}
		public static void Init() {
			var _ = Instance;
		}
		public void Dispose() {
			Services.Framework.Update -= Monitor;
		}

		// Below are the methods and variables needed for Monitor to handle inputs
		public static List<VirtualKey> FallbackKey = new() { VirtualKey.NO_KEY };
		public static readonly Dictionary<Purpose, Category> PurposesCategories = new();
		public static readonly Dictionary<Purpose, Category> PurposesCategoriesHold = new();
		public static readonly Dictionary<Purpose, Category> PurposesCategoriesToggle = new();
		public const int FirstCategoryPurposeHold = 1000;
		public const int FirstCategoryPurposeToggle = 2000;
		public static List<string> CategoryOverload = new();

		private Dictionary<Purpose, bool> PrevriousKeyStates = new();
		private Dictionary<Purpose, bool>? CurrentKeyStates = new();

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
		private static void HoldCategory(Category category) {
			if (CategoryOverload.Any(s => s == category.Name))
				CategoryOverload.Remove(category.Name);
			else
				CategoryOverload.Add(category.Name);
		}
		private static List<VirtualKey> PurposeToVirtualKeys(Purpose purpose) {
			if (!Ktisis.Configuration.KeyBinds.TryGetValue(purpose, out List<VirtualKey>? keys)) {
				if (!DefaultKeys.TryGetValue(purpose, out List<VirtualKey>? defaultKeys))
					defaultKeys = FallbackKey;
				keys = defaultKeys;
			}
			if(keys == null) return FallbackKey;
			foreach (var key in keys)
				if (!Services.KeyState.IsVirtualKeyValid(key)) return FallbackKey;

			return keys;
		}
		private void ReadPurposesStates() {
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
		private unsafe bool IsChatInputActive() => ((UIModule*)Services.GameGui.GetUIModule())->GetRaptureAtkModule()->AtkModule.IsTextInputActive() == 1;

		// Below are methods to check different kind of key state
		private bool IsPurposeChanged(Purpose purpose) {
			var modifierKeys = PurposeToVirtualKeys(Purpose.GlobalModifierKey);
			if (purpose != Purpose.GlobalModifierKey && modifierKeys != FallbackKey) {
				foreach (var key in modifierKeys)
					if(!Services.KeyState[key]) return false;
			}
			if (!PrevriousKeyStates.TryGetValue(purpose, out bool previous)) return false;
			if (CurrentKeyStates == null) return false;
			if (!CurrentKeyStates.TryGetValue(purpose, out bool current)) return false;
			return previous != current;
		}
		private bool IsPurposeHeld(Purpose purpose) {
			if (CurrentKeyStates == null) return false;
			if (!CurrentKeyStates.TryGetValue(purpose, out bool current)) return false;
			return IsPurposeChanged(purpose) && current;
		}
		private bool IsPurposeReleased(Purpose purpose) {
			if (CurrentKeyStates == null) return false;
			if (!CurrentKeyStates.TryGetValue(purpose, out bool current)) return false;
			return IsPurposeChanged(purpose) && !current;
		}
	}
}