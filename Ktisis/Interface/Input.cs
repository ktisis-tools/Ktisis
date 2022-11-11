using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuizmoNET;

using Ktisis.Overlay;

namespace Ktisis.Interface {
	public sealed class Input : IDisposable {
		// When adding a new keybind:
		//  - add the logic in Monitor
		//      (held/release/changed [+ extra conditions] and what it executes )
		//  - add the key action in Purpose enum
		//  - add the default key in DefaultKeys

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

		public static readonly Dictionary<Purpose, VirtualKey> DefaultKeys = new(){
			{Purpose.GlobalModifierKey, VirtualKey.NO_KEY},
			{Purpose.SwitchToTranslate, VirtualKey.G},
			{Purpose.SwitchToRotate, VirtualKey.R},
			{Purpose.SwitchToScale, VirtualKey.T},
			{Purpose.ToggleLocalWorld, VirtualKey.X},
			{Purpose.HoldToHideSkeleton, VirtualKey.V},
			{Purpose.SwitchToUniversal, VirtualKey.U},
		};

		// Thanks to (Edited) for the intgration with the Framework Update <3
		private static Input? _instance = null;
		private Input() {
			Dalamud.Framework.Update += Monitor;
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
			Dalamud.Framework.Update -= Monitor;
		}

		// Below are the methods and variables needed for Monitor to handle inputs
		public const VirtualKey FallbackKey = VirtualKey.NO_KEY;

		private Dictionary<Purpose, bool> PrevriousKeyStates = new();
		private Dictionary<Purpose, bool>? CurrentKeyStates = new();

		public static IEnumerable<Purpose> Purposes {
			get => Enum.GetValues<Purpose>().ToImmutableList();
		}
		private static VirtualKey PurposeToVirtualKey(Purpose purpose) {
			if (!Ktisis.Configuration.KeyBinds.TryGetValue(purpose, out VirtualKey key)) {
				if (!DefaultKeys.TryGetValue(purpose, out VirtualKey defaultKey))
					defaultKey = FallbackKey;
				key = defaultKey;
			}
			return Dalamud.KeyState.IsVirtualKeyValid(key) ? key : FallbackKey;
		}
		private void ReadPurposesStates() {
			CurrentKeyStates = Purposes.Select(p => {
				var key = PurposeToVirtualKey(p);
				bool state;
				if (key != VirtualKey.NO_KEY) state = Dalamud.KeyState[key];
				else state = false;
				return (purpose: p, state);
			}).ToDictionary(kp => kp.purpose, kp => kp.state);
		}
		private unsafe bool IsChatInputActive() => ((UIModule*)Dalamud.GameGui.GetUIModule())->GetRaptureAtkModule()->AtkModule.IsTextInputActive() == 1;

		// Below are methods to check different kind of key state
		private bool IsPurposeChanged(Purpose purpose) {
			var modifierKey = PurposeToVirtualKey(Purpose.GlobalModifierKey);
			if (purpose != Purpose.GlobalModifierKey && modifierKey != VirtualKey.NO_KEY && !Dalamud.KeyState[modifierKey]) return false;
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