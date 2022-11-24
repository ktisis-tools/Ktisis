using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

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

			OnGposeEnter(); // TODO: move this call on "enter gpose" event
			OnLogin(null!, null!);
		}

		public static void Dispose() {
			Services.AddonManager.Dispose();
			Services.ClientState.Logout -= OnLogout;
			Services.ClientState.Login -= OnLogin;

			var MiragePrismMiragePlate = Services.AddonManager.Get<MiragePrismMiragePlateAddon>();
			MiragePrismMiragePlate.ReceiveEvent -= OnGlamourPlatesReceiveEvent;

			OnGposeLeave();
			OnLogout(null!, null!);
		}

		// Various event methods
		private static void OnLogin(object? sender, EventArgs e) {
			Sets.Init();
		}
		private static void OnLogout(object? sender, EventArgs e) {
			Sets.Dispose();
		}
		private static void OnGposeEnter() {
			var ClickTargetAddon = Services.AddonManager.Get<ClickTargetAddon>();
			ClickTargetAddon.Enable();
		}
		private static void OnGposeLeave() {
			var ClickTargetAddon = Services.AddonManager.Get<ClickTargetAddon>();
			ClickTargetAddon.Dispose();
		}

		private static unsafe void OnGlamourPlatesReceiveEvent(object? sender, ReceiveEventArgs e) {
			//PluginLog.Verbose($"OnGlamourPlatesReceiveEvent {e.SenderID} {e.EventArgs->Int}");

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
			receiveEventHook ??= Hook<AgentReceiveEvent>.FromAddress(new IntPtr(MiragePrismMiragePlateAgentInterface->VTable->ReceiveEvent), OnReceiveEvent);

			receiveEventHook?.Enable();
		}

		public void Dispose() {
			receiveEventHook?.Dispose();
		}

		private void* OnReceiveEvent(AgentInterface* agent, void* rawData, AtkValue* eventArgs, uint eventArgsCount, ulong sender) {
			try {
				ReceiveEvent?.Invoke(this, new ReceiveEventArgs(agent, rawData, eventArgs, eventArgsCount, sender));
			} catch (Exception ex) {
				PluginLog.Error(ex, "Something went wrong when the MiragePrismMiragePlates Addon was opened");
			}

			return receiveEventHook!.Original(agent, rawData, eventArgs, eventArgsCount, sender);
		}
	}
	internal unsafe class ClickTargetAddon : IDisposable {


		private delegate void* ClickTarget(void** a1, byte* a2, bool a3);
		private readonly Hook<ClickTarget>? rightClickTargetHook;
		private readonly Hook<ClickTarget>? leftClickTargetHook;

		public ClickTargetAddon() {
			rightClickTargetHook ??= Hook<ClickTarget>.FromAddress(Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B CE E8 ?? ?? ?? ?? 48 85 C0 74 1B"), new ClickTarget(RightClickTargetDetour));
			leftClickTargetHook ??= Hook<ClickTarget>.FromAddress(Services.SigScanner.ScanText("E8 ?? ?? ?? ?? BA ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 84 C0 74 16"), new ClickTarget(LeftClickTargetDetour));
		}

		public void Enable() {
			rightClickTargetHook?.Enable();
			leftClickTargetHook?.Enable();
		}

		public void Dispose() {
			// Verify presence of hooks, in case of calls when it's already been disposed
			if (!(bool)rightClickTargetHook?.IsDisposed!) {
				if ((bool)rightClickTargetHook?.IsEnabled!)
						rightClickTargetHook?.Disable();
				rightClickTargetHook?.Dispose();
			}
			if (!(bool)leftClickTargetHook?.IsDisposed!) {
				if ((bool)leftClickTargetHook?.IsEnabled!)
						leftClickTargetHook?.Disable();
				leftClickTargetHook?.Dispose();
			}
		}


		private void* RightClickTargetDetour(void** a1, byte* a2, bool a3) =>
			ClickEvent(a1, a2, a3, ClickType.Right);
		private void* LeftClickTargetDetour(void** a1, byte* a2, bool a3) =>
			ClickEvent(a1, a2, a3, ClickType.Left);


		private void* ClickEvent(void** a1, byte* actor, bool a3, ClickType clickType) {
			if (Ktisis.IsInGPose) {
				//if (actor != null) // cast (Actor*)actor if need do something with actor

				// 1. Prevents target self when clicking somewhere else with left click
				// 2. Prevent target change with left and right clicks
				// returning null wasn't enough for 1. so we pass the current target instead
				if (Ktisis.Configuration.DisableChangeTargetOnLeftClick && clickType == ClickType.Left)
					return leftClickTargetHook!.Original(a1, (byte*)Ktisis.Target, a3);
				if (Ktisis.Configuration.DisableChangeTargetOnRightClick && clickType == ClickType.Right)
					return rightClickTargetHook!.Original(a1, (byte*)Ktisis.Target, a3);
			}

			if (clickType == ClickType.Left) leftClickTargetHook!.Original(a1, actor, a3);
			if (clickType == ClickType.Right) rightClickTargetHook!.Original(a1, actor, a3);
			return null;
		}
		internal enum ClickType {
			Left,
			Right
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
			PluginLog.Verbose("ReceiveEvent Argument Printout --------------");
			PluginLog.Verbose($"AgentInterface: {(IntPtr)AgentInterface:X8}");
			PluginLog.Verbose($"RawData: {(IntPtr)RawData:X8}");
			PluginLog.Verbose($"EventArgs: {(IntPtr)EventArgs:X8}");
			PluginLog.Verbose($"EventArgsCount: {EventArgsCount}");
			PluginLog.Verbose($"SenderID: {SenderID}");

			for (var i = 0; i < EventArgsCount; i++) {
				PluginLog.Verbose($"[{i}] {EventArgs[i].Int}, {EventArgs[i].Type}");
			}

			PluginLog.Verbose("End -----------------------------------------");
		}
	}
}
