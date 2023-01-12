using System;

using Dalamud.Hooking;

using FFXIVClientStructs.FFXIV.Client.Game;

using Ktisis.Events;
using Ktisis.Structs.Actor;
using Ktisis.Interface.Windows.Workspace;
using Ktisis.Structs.Actor.State;

namespace Ktisis.Interop.Hooks {
	internal static class ActorHooks {
		// Control actor gaze
		// a1 = Actor + 0xC20

		internal delegate IntPtr ControlGazeDelegate(IntPtr a1);
		internal static Hook<ControlGazeDelegate> ControlGazeHook = null!;
		
		internal delegate IntPtr EnforceGlobalSpeedDelegate(IntPtr a1);
		internal static Hook<EnforceGlobalSpeedDelegate> EnforceGlobalSpeedHook = null!;

		public delegate void SetSlotSpeedDelegate(IntPtr a1, uint slot, float speed);
		internal static Hook<SetSlotSpeedDelegate> SetSlotSpeedHook = null!;

		public static SpeedControlModes SpeedControlMode { get; set; }

		internal unsafe static IntPtr ControlGaze(IntPtr a1) {
			var actor = (Actor*)(a1 - 0xC30);
			EditGaze.Apply(actor);
			return ControlGazeHook.Original(a1);
		}

		// Init & Dispose

		internal static void Init() {
			var controlGaze = Services.SigScanner.ScanText("40 53 41 54 41 55 48 81 EC ?? ?? ?? ?? 48 8B D9");
			ControlGazeHook = Hook<ControlGazeDelegate>.FromAddress(controlGaze, ControlGaze);
			ControlGazeHook.Enable();
			
			var globalSpeed = Services.SigScanner.ScanText("40 53 48 83 EC 30 48 8B D9 0F 29 74 24 20 48 8B 49 08 ?? ?? ?? ?? ?? 0F 28 F0 0F 57");
			EnforceGlobalSpeedHook = Hook<EnforceGlobalSpeedDelegate>.FromAddress(globalSpeed, EnforceGlobalSpeedDetour);
			EnforceGlobalSpeedHook.Enable();

			SetSlotSpeedHook = Hook<SetSlotSpeedDelegate>.FromAddress((nint)ActionTimelineDriver.Addresses.SetSlotSpeed.Value, SetSlotSpeedDetour);
			SetSlotSpeedHook.Enable();

			EventManager.OnGPoseChange += OnGPoseChange;
		}
		
		internal static void OnGPoseChange(ActorGposeState _state) {
			if (_state == ActorGposeState.OFF) {
				SpeedControlMode = SpeedControlModes.Manual;
			}
		}
		
		internal static IntPtr EnforceGlobalSpeedDetour(IntPtr a1) {
			if (SpeedControlMode != SpeedControlModes.Global)
				return EnforceGlobalSpeedHook.Original(a1);

			return IntPtr.Zero;
		}

		internal static void SetSlotSpeedDetour(IntPtr a1, uint slot, float speed) {
			if (SpeedControlMode != SpeedControlModes.Slot)
				SetSlotSpeedHook.Original(a1, slot, speed);
		}

		internal static void Dispose() {
			EventManager.OnGPoseChange -= OnGPoseChange;
			ControlGazeHook.Disable();
			ControlGazeHook.Dispose();
			EnforceGlobalSpeedHook.Disable();
			EnforceGlobalSpeedHook.Dispose();
			SetSlotSpeedHook.Disable();
			SetSlotSpeedHook.Dispose();
		}
		
		public enum SpeedControlModes : int {
			Manual = 0,
			Global = 1,
			Slot = 2,
		}
	}
}
