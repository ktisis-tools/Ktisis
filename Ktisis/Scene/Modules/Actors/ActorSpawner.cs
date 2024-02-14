using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

using Ktisis.Interop.Hooking;
using Ktisis.Structs.Events;

namespace Ktisis.Scene.Modules.Actors;

public class ActorSpawner : HookModule {
	private const ushort Start = 200;
	private const ushort SoftCap = 30;
	private const ushort HardCap = SoftCap + 8;
	
	private readonly IObjectTable _objectTable;
	private readonly IFramework _framework;
	
	public ActorSpawner(
		IHookMediator hook,
		IObjectTable objectTable,
		IFramework framework
	) : base(hook) {
		this._objectTable = objectTable;
		this._framework = framework;
	}
	
	// Signatures

	private const int VfSize = 9;

	[Signature("48 8D 05 ?? ?? ?? ?? 48 89 01 45 33 D2 C7 41 ?? ?? ?? ?? ??", ScanType = ScanType.StaticAddress, Fallibility = Fallibility.Fallible)]
	private unsafe nint* _eventVfTable = null;

	[Signature("E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 48 8B D0 E8 ?? ?? ?? ?? EB 67")]
	private GPoseActorEventCtorDelegate _gPoseActorEventCtor = null!;
	private unsafe delegate nint GPoseActorEventCtorDelegate(GPoseActorEvent* self, Character* target, Vector3* position, uint a4, int a5, int a6, uint a7, bool a8);

	[Signature("E8 ?? ?? ?? ?? 0F 28 74 24 ?? B0 01 48 8B 74 24 ??")]
	private DispatchEventDelegate _dispatchEvent = null!;
	private unsafe delegate nint DispatchEventDelegate(nint handler, GPoseActorEvent* task);
	
	// Initialization
	
	public void TryInitialize() {
		try {
			this.Initialize();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to initialize actor spawner:\n{err}");
		}
	}

	protected override bool OnInitialize() {
		this.Setup();
		return true;
	}
	
	// Virtual Functions Setup

	private unsafe nint* _hookVfTable = null;

	private unsafe void Setup() {
		var vf = (nint*)Marshal.AllocHGlobal(sizeof(nint) * VfSize);
		for (var i = 0; i < VfSize; i++) {
			var original = this._eventVfTable[i];
			if (i == 2) {
				_finalizeOriginal = Marshal.GetDelegateForFunctionPointer<FinalizeDelegate>(original);
				vf[i] = Marshal.GetFunctionPointerForDelegate<FinalizeDelegate>(FinalizeHook);
			} else {
				vf[i] = original;
			}
		}
		this._hookVfTable = vf;
	}
	
	// Creation

	public async Task<nint> CreateActor(GameObject original) {
		using var source = new CancellationTokenSource();
		source.CancelAfter(10_000);
		return await this.CreateActor(original, source.Token);
	}

	private async Task<nint> CreateActor(
		GameObject original,
		CancellationToken token
	) {
		var index = await this._framework.RunOnFrameworkThread(() => {
			if (!this.TryDispatch(original, out var index))
				throw new Exception("Object table is full.");
			return index;
		});
		
		while (!token.IsCancellationRequested) {
			if (this._objectTable[(int)index] is { } actor)
				return actor.Address;
			await Task.Delay(10, CancellationToken.None);
		}
		
		throw new TaskCanceledException($"Actor spawn at index {index} timed out.");
	}

	private bool TryDispatch(GameObject original, out uint index) {
		index = this.CalculateNextIndex();
		if (index == ushort.MaxValue) return false;
		Ktisis.Log.Info($"Dispatching, expecting spawn on {index}");
		this.DispatchSpawn(original);
		return true;
	}

	private unsafe void DispatchSpawn(GameObject original) {
		if (this._hookVfTable == null)
			throw new Exception("Hook vtable is not initialized!");
		
		var player = (Character*)original.Address;
		if (player == null || !player->GameObject.IsCharacter())
			throw new Exception($"Original object '{original.Name}' ({original.ObjectIndex}) is invalid.");
		
		// This gets freed by the event manager after handling.
		var task = (GPoseActorEvent*)IMemorySpace.GetDefaultSpace()->Malloc<GPoseActorEvent>();
		this._gPoseActorEventCtor(task, player, &player->GameObject.Position, 0x40, 30, 0, uint.MaxValue & ~0x4u & ~0x8000u, true);
		task->__vfTable = this._hookVfTable;

		// TODO: Map this struct out.
		var handler = (nint)EventFramework.Instance() + 432 + 152;
		this._dispatchEvent(handler, task);
	}

	private ushort CalculateNextIndex() {
		for (var i = Start; i <= Start + HardCap; i++) {
			var actor = this._objectTable[i];
			if (actor == null) return i;
		}
		return ushort.MaxValue;
	}
	
	// Finalize hook
	
	private unsafe delegate void FinalizeDelegate(GPoseActorEvent* a1, nint a2, nint a3);
	private static FinalizeDelegate _finalizeOriginal = null!;
	private unsafe static void FinalizeHook(GPoseActorEvent* self, nint a2, nint a3) {
		// This prevents the new actor from being confused with the player.
		if (self->Character != null)
			self->EntityID = 0xE0000000;
		_finalizeOriginal.Invoke(self, a2, a3);
	}
	
	// Disposal
	
	public unsafe override void Dispose() {
		base.Dispose();
		Ktisis.Log.Verbose("Disposing actor spawn manager...");
		if (this._hookVfTable != null) {
			Ktisis.Log.Verbose("Freeing hookVfTable from spawn manager");
			Marshal.FreeHGlobal((nint)this._hookVfTable);
			this._hookVfTable = null;
		}
		GC.SuppressFinalize(this);
	}
}
