using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;

using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Ktisis.Structs.Actor.Equip;
using Ktisis.Structs.Actor.Equip.SetSources;

namespace Ktisis.Interop
{
	public class EventsHooks
	{
		public static void Init()
		{
			Dalamud.AddonManager = new AddonManager();
			Dalamud.ClientState.Login += OnLogin;
			Dalamud.ClientState.Logout += OnLogout;
			//Dalamud.Condition.ConditionChange += ConditionChange;

			var MiragePrismMiragePlate = Dalamud.AddonManager.Get<MiragePrismMiragePlateAddon>();
			MiragePrismMiragePlate.ReceiveEvent += OnGlamourPlatesReceiveEvent;

			OnLogin(null!, null!);
		}

		public static void Dispose()
		{
			Dalamud.AddonManager.Dispose();
			Dalamud.ClientState.Logout -= OnLogout;
			Dalamud.ClientState.Login -= OnLogin;
			//Dalamud.Condition.ConditionChange -= ConditionChange;

			var MiragePrismMiragePlate = Dalamud.AddonManager.Get<MiragePrismMiragePlateAddon>();
			MiragePrismMiragePlate.ReceiveEvent -= OnGlamourPlatesReceiveEvent;

			OnLogout(null!, null!);
		}

		// Various event methods

		private static void OnLogin(object? sender, EventArgs e)
		{
			Sets.Init();
		}
		private static void OnLogout(object? sender, EventArgs e)
		{
			Sets.Dispose();
		}
		private static void ConditionChange(ConditionFlag flag, bool value)
		{
			//PluginLog.Debug($"condition changed to {flag}=>{value}");

			// TODO: find a better way to watch for OnGposeEnter, and make OnGposeLeave()
			if (flag == ConditionFlag.WatchingCutscene && value && Ktisis.IsInGPose) OnGposeEnter();
		}
		private static void OnGposeEnter()
		{
			PluginLog.Verbose($"Entered Gpose");
		}

		private static unsafe void OnGlamourPlatesReceiveEvent(object? sender, ReceiveEventArgs e)
		{
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
	internal class AddonManager : IDisposable
	{
		private readonly List<IDisposable> addons = new()
		{
			new MiragePrismMiragePlateAddon(),
		};

		public void Dispose()
		{
			foreach (var addon in addons)
			{
				addon.Dispose();
			}
		}

		public T Get<T>()
		{
			return addons.OfType<T>().First();
		}
	}
	internal unsafe class MiragePrismMiragePlateAddon : IDisposable
	{
		public event EventHandler<ReceiveEventArgs>? ReceiveEvent;
		private delegate void* AgentReceiveEvent(AgentInterface* agent, void* rawData, AtkValue* eventArgs, uint eventArgsCount, ulong sender);
		private readonly Hook<AgentReceiveEvent>? receiveEventHook;

		public MiragePrismMiragePlateAddon()
		{
			var MiragePrismMiragePlateAgentInterface = Framework.Instance()->UIModule->GetAgentModule()->GetAgentByInternalId(AgentId.MiragePrismMiragePlate);
			receiveEventHook ??= Hook<AgentReceiveEvent>.FromAddress(new IntPtr(MiragePrismMiragePlateAgentInterface->VTable->ReceiveEvent), OnReceiveEvent);

			receiveEventHook?.Enable();
		}

		public void Dispose()
		{
			receiveEventHook?.Dispose();
		}

		private void* OnReceiveEvent(AgentInterface* agent, void* rawData, AtkValue* eventArgs, uint eventArgsCount, ulong sender)
		{
			try
			{
				ReceiveEvent?.Invoke(this, new ReceiveEventArgs(agent, rawData, eventArgs, eventArgsCount, sender));
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, "Something went wrong when the MiragePrismMiragePlates Addon was opened");
			}

			return receiveEventHook!.Original(agent, rawData, eventArgs, eventArgsCount, sender);
		}
	}

	internal unsafe class ReceiveEventArgs : EventArgs
	{
		public ReceiveEventArgs(AgentInterface* agentInterface, void* rawData, AtkValue* eventArgs, uint eventArgsCount, ulong senderID)
		{
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

		public void PrintData()
		{
			PluginLog.Verbose("ReceiveEvent Argument Printout --------------");
			PluginLog.Verbose($"AgentInterface: {(IntPtr)AgentInterface:X8}");
			PluginLog.Verbose($"RawData: {(IntPtr)RawData:X8}");
			PluginLog.Verbose($"EventArgs: {(IntPtr)EventArgs:X8}");
			PluginLog.Verbose($"EventArgsCount: {EventArgsCount}");
			PluginLog.Verbose($"SenderID: {SenderID}");

			for (var i = 0; i < EventArgsCount; i++)
			{
				PluginLog.Verbose($"[{i}] {EventArgs[i].Int}, {EventArgs[i].Type}");
			}

			PluginLog.Verbose("End -----------------------------------------");
		}
	}
}
