using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Hooking;

using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Ktisis.Events;
using Ktisis.Structs.Actor.Equip;
using Ktisis.Structs.Actor.Equip.SetSources;

namespace Ktisis.Interop.Hooks {
	public class EventsHooks {
		public static void Init() {
			Services.AddonManager = new AddonManager();
			Services.ClientState.Login += OnLogin;
			Services.ClientState.Logout += OnLogout;

			var MiragePrismMiragePlate = Services.AddonManager.Get<MiragePrismMiragePlateAddon>();
			MiragePrismMiragePlate.ReceiveEvent += OnGlamourPlatesReceiveEvent;
			
			EventManager.OnGPoseChange += OnGposeChange;
			
			OnLogin();
		}

		public static void Dispose() {
			Services.AddonManager.Dispose();
			Services.ClientState.Logout -= OnLogout;
			Services.ClientState.Login -= OnLogin;

			var MiragePrismMiragePlate = Services.AddonManager.Get<MiragePrismMiragePlateAddon>();
			MiragePrismMiragePlate.ReceiveEvent -= OnGlamourPlatesReceiveEvent;

			EventManager.OnGPoseChange -= OnGposeChange;
			
            OnLogout(0, 0);
        }

		// Various event methods
		private static void OnLogin() {
			Sets.Init();
		}
		private static void OnLogout(int type, int code) {
			Sets.Dispose();
		}

		private static void OnGposeChange(bool state) {
			if (state)
				OnGposeEnter();
			else
				OnGposeLeave();
		}
		private static void OnGposeEnter() {
			var ClickTargetAddon = Services.AddonManager.Get<ClickTargetAddon>();
			ClickTargetAddon.Enable();
		}
		private static void OnGposeLeave() {
			var ClickTargetAddon = Services.AddonManager.Get<ClickTargetAddon>();
			ClickTargetAddon.Disable();
		}

		private static unsafe void OnGlamourPlatesReceiveEvent(object? sender, ReceiveEventArgs e) {
			//Logger.Verbose($"OnGlamourPlatesReceiveEvent {e.SenderID} {e.EventArgs->Int}");

			if (
				e.SenderID == 0 && e.EventArgs->Int == 18 // used "Close" button, the (X) button, Close UI Component keybind, Cancel Keybind. NOT when using the "Glamour Plate" toggle skill to close it.
														  // || e.SenderID == 0 && e.EventArgs->Int == 17 // Change Glamour Plate Page
														  // || e.SenderID == 0 && e.EventArgs->Int == -2 // Has been closed, Plate memory has already been disposed so it's too late to read data.
				)
				GlamourDresser.PopulatePlatesData();
		}
	}


	// The classes AddonManager and ReceiveEventArgs are from DailyDuty plugin.
	// MiragePrismMiragePlateAddon class is strongly inspired from the different addons managed in DailyDuty.
	// Thank you MidoriKami <3

	// their role is to manage events for any kind of AgentInterface
	internal class AddonManager : IDisposable {
		private readonly List<IDisposable> addons = new()
		{
			new MiragePrismMiragePlateAddon(),
			new ClickTargetAddon(),
		};

		public void Dispose() {
			foreach (var addon in addons) {
				addon.Dispose();
			}
		}

		public T Get<T>() {
			return addons.OfType<T>().First();
		}
	}

	// This is Glamour Plate event hook
	// If adding new agents, it may be a good idea to move them in their own files
	internal unsafe class MiragePrismMiragePlateAddon : IDisposable {
		public event EventHandler<ReceiveEventArgs>? ReceiveEvent;
		private delegate void* AgentReceiveEvent(AgentInterface* agent, void* rawData, AtkValue* eventArgs, uint eventArgsCount, ulong sender);
		private readonly Hook<AgentReceiveEvent>? receiveEventHook;

		public MiragePrismMiragePlateAddon() {
			var MiragePrismMiragePlateAgentInterface = Framework.Instance()->UIModule->GetAgentModule()->GetAgentByInternalId(AgentId.MiragePrismMiragePlate);
            receiveEventHook ??= Services.Hooking.HookFromAddress<AgentReceiveEvent>(new IntPtr(MiragePrismMiragePlateAgentInterface->VirtualTable->ReceiveEvent), OnReceiveEvent);
            
			receiveEventHook?.Enable();
		}

		public void Dispose() {
			receiveEventHook?.Dispose();
		}

		private void* OnReceiveEvent(AgentInterface* agent, void* rawData, AtkValue* eventArgs, uint eventArgsCount, ulong sender) {
			try {
				ReceiveEvent?.Invoke(this, new ReceiveEventArgs(agent, rawData, eventArgs, eventArgsCount, sender));
			} catch (Exception ex) {
				Logger.Error(ex, "Something went wrong when the MiragePrismMiragePlates Addon was opened");
			}

			return receiveEventHook!.Original(agent, rawData, eventArgs, eventArgsCount, sender);
		}
	}
	internal unsafe class ClickTargetAddon : IDisposable {
		private delegate nint ProcessMouseStateDelegate(TargetSystem* targets, nint a2, nint a3);
		private readonly Hook<ProcessMouseStateDelegate>? ProcessMouseStateHook;

		public ClickTargetAddon() {
			var addr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 4C 8B BC 24 ?? ?? ?? ?? 4C 8B B4 24 ?? ?? ?? ?? 48 8B B4 24 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??");
			ProcessMouseStateHook = Services.Hooking.HookFromAddress<ProcessMouseStateDelegate>(addr, ProcessMouseStateDetour);
		}

		private nint ProcessMouseStateDetour(TargetSystem* targets, nint a2, nint a3) {
			var prev = targets->GPoseTarget;
			var exec = ProcessMouseStateHook!.Original(targets, a2, a3);

			if (Ktisis.IsInGPose && targets->GPoseTarget != prev) {
				var left = Ktisis.Configuration.DisableChangeTargetOnLeftClick && exec == 0;
				var right = !left && Ktisis.Configuration.DisableChangeTargetOnRightClick && exec == 0x10;
				if (left || right) targets->GPoseTarget = prev;
			}
			
			return exec;
		}

		public void Enable() {
			ProcessMouseStateHook?.Enable();
		}

		public void Disable() {
			ProcessMouseStateHook?.Disable();
		}

		public void Dispose() {
			if (ProcessMouseStateHook?.IsDisposed == false) {
				ProcessMouseStateHook?.Disable();
				ProcessMouseStateHook?.Dispose();
			}
		}

	}
	internal unsafe class ReceiveEventArgs : EventArgs {
		public ReceiveEventArgs(AgentInterface* agentInterface, void* rawData, AtkValue* eventArgs, uint eventArgsCount, ulong senderID) {
			AgentInterface = agentInterface;
			RawData = rawData;
			EventArgs = eventArgs;
			EventArgsCount = eventArgsCount;
			SenderID = senderID;
		}

		public AgentInterface* AgentInterface;
		public void* RawData;
		public AtkValue* EventArgs;
		public uint EventArgsCount;
		public ulong SenderID;

		public void PrintData() {
			Logger.Verbose("ReceiveEvent Argument Printout --------------");
			Logger.Verbose($"AgentInterface: {(IntPtr)AgentInterface:X8}");
			Logger.Verbose($"RawData: {(IntPtr)RawData:X8}");
			Logger.Verbose($"EventArgs: {(IntPtr)EventArgs:X8}");
			Logger.Verbose($"EventArgsCount: {EventArgsCount}");
			Logger.Verbose($"SenderID: {SenderID}");

			for (var i = 0; i < EventArgsCount; i++) {
				Logger.Verbose($"[{i}] {EventArgs[i].Int}, {EventArgs[i].Type}");
			}

			Logger.Verbose("End -----------------------------------------");
		}
	}
}
