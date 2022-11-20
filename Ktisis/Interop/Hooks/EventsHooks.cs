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

			OnLogin(null!, null!);
		}

		public static void Dispose() {
			Services.AddonManager.Dispose();
			Services.ClientState.Logout -= OnLogout;
			Services.ClientState.Login -= OnLogin;

			var MiragePrismMiragePlate = Services.AddonManager.Get<MiragePrismMiragePlateAddon>();
			MiragePrismMiragePlate.ReceiveEvent -= OnGlamourPlatesReceiveEvent;

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
			PluginLog.Verbose($"Entered Gpose");
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
