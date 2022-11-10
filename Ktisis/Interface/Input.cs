using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Logging;
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
			if (!Ktisis.IsInGPose) return; // TODO: when implemented move init/dispose to Gpose enter and leave instead of in Ktisis

			if (!Ktisis.Configuration.KeyBinds.Any()) Ktisis.Configuration.KeyBinds = DefaultKeys;
			GetPurposesStates();

			if (IsPurposeReleased(Purpose.SwitchToTranslate) && !ImGuizmo.IsUsing()) Ktisis.Configuration.GizmoOp = OPERATION.TRANSLATE;
			if (IsPurposeReleased(Purpose.SwitchToRotate) && !ImGuizmo.IsUsing()) Ktisis.Configuration.GizmoOp = OPERATION.ROTATE;
			if (IsPurposeReleased(Purpose.SwitchToScale) && !ImGuizmo.IsUsing()) Ktisis.Configuration.GizmoOp = OPERATION.SCALE;
			if (IsPurposeReleased(Purpose.ToggleLocalWorld) && !ImGuizmo.IsUsing()) Ktisis.Configuration.GizmoMode = Ktisis.Configuration.GizmoMode == MODE.WORLD ? MODE.LOCAL : MODE.WORLD;
			if (IsPurposeChanged(Purpose.HoldToHideSkeleton)) Skeleton.Toggle();

			PrevriousKeyStates = CurrentKeyStates!;
			CurrentKeyStates = null;
		}

		[Serializable]
		public enum Purpose {
			SwitchToTranslate,
			SwitchToRotate,
			SwitchToScale,
			ToggleLocalWorld,
			HoldToHideSkeleton,
		}

		public static readonly Dictionary<Purpose, VirtualKey> DefaultKeys = new(){
			{Purpose.SwitchToTranslate, VirtualKey.G},
			{Purpose.SwitchToRotate, VirtualKey.R},
			{Purpose.SwitchToScale, VirtualKey.T},
			{Purpose.ToggleLocalWorld, VirtualKey.X},
			{Purpose.HoldToHideSkeleton, VirtualKey.V},
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
		public const VirtualKey FallbackKey = VirtualKey.OEM_102;

		private Dictionary<Purpose, bool> PrevriousKeyStates = new();
		private Dictionary<Purpose, bool>? CurrentKeyStates = new();

		public static IEnumerable<Purpose> Purposes {
			get => Enum.GetValues<Purpose>().ToImmutableList();
		}
		private static VirtualKey PurposeToVirtualKey(Purpose purpose) {
			if (!Ktisis.Configuration.KeyBinds.TryGetValue(purpose, out VirtualKey key))
				key = VirtualKey.NO_KEY;
			return Dalamud.KeyState.IsVirtualKeyValid(key) ? key : FallbackKey;
		}
		private void GetPurposesStates() {
			CurrentKeyStates = Purposes.Select(p =>
				(purpose: p, state: Dalamud.KeyState[PurposeToVirtualKey(p)])
			).ToDictionary(kp => kp.purpose, kp => kp.state);
		}

		// Below are methods to check different kind of key state
		private bool IsPurposeChanged(Purpose purpose) {
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