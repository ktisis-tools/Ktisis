using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Common.Math;

using Ktisis.Core.Attributes;
using Ktisis.Structs.Events;

namespace Ktisis.Scene.Modules.Actors;

[Transient]
public class ActorSpawnManager : IDisposable {
	private readonly IGameInteropProvider _interop;
	private readonly IFramework _framework;
	
	public ActorSpawnManager(
		IGameInteropProvider interop,
		IFramework framework
	) {
		this._interop = interop;
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

	public bool TryInitialize() {
		try {
			this._interop.InitializeFromAttributes(this);
			this.Setup();
			return true;
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to initialize spawn manager:\n{err}");
			this.Dispose();
			return false;
		}
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

	public async Task<nint> CreateActor() {
		using var source = new CancellationTokenSource();
		source.CancelAfter(10_000);
		return await this.CreateActor(source.Token);
	}

	private async Task<nint> CreateActor(
		CancellationToken token
	) {
		var index = await this._framework.RunOnFrameworkThread(() => {
			if (!this.TryDispatch(out var index))
				throw new Exception("Object table is full.");
			return index;
		});
		
		while (!token.IsCancellationRequested) {
			if (TryGetCharaAddress((ushort)index, out var result))
				return result;
			await Task.Delay(10, CancellationToken.None);
		}
		
		throw new TaskCanceledException($"Actor spawn at index {index} timed out.");
	}

	private unsafe bool TryDispatch(out uint index) {
		index = ClientObjectManager.Instance()->CalculateNextAvailableIndex();
		if (index == uint.MaxValue) return false;
		this.DispatchSpawn();
		return true;
	}

	private unsafe void DispatchSpawn() {
		if (this._hookVfTable == null)
			throw new Exception("Hook vtable is not initialized!");

		// TODO: Resolve LocalPlayer by other means.
		var player = (Character*)GameObjectManager.Instance()->ObjectList[0];
		if (player == null)
			throw new Exception("LocalPlayer is null.");
		
		// This should get freed by the event manager after handling.
		var task = (GPoseActorEvent*)IMemorySpace.GetDefaultSpace()->Malloc<GPoseActorEvent>();
		this._gPoseActorEventCtor(task, player, &player->GameObject.Position, 0x40, 30, 0, uint.MaxValue & ~0x4u & ~0x8000u, true);
		task->__vfTable = this._hookVfTable;

		// TODO: Figure out what this is.
		var handler = (nint)EventFramework.Instance() + 432 + 152;

		this._dispatchEvent(handler, task);
	}
	
	private unsafe static bool TryGetCharaAddress(ushort index, out nint address) {
		var gameObject = ClientObjectManager.Instance()->GetObjectByIndex(index);
		address = (nint)gameObject;
		if (gameObject != null && gameObject->ObjectIndex < 200)
			throw new Exception($"Index {index} resolved to non-GPose actor.");
		return address != nint.Zero;
	}
	
	// Finalize hook
	
	private unsafe delegate void FinalizeDelegate(GPoseActorEvent* a1, nint a2, nint a3);
	private static FinalizeDelegate _finalizeOriginal = null!;
	private unsafe static void FinalizeHook(GPoseActorEvent* self, nint a2, nint a3) {
		// This only prevents the new actor from being confused with the player.
		if (self->Character != null)
			self->EntityID = 0xE0000000;
		_finalizeOriginal.Invoke(self, a2, a3);
	}
	
	// Disposal
	
	public unsafe void Dispose() {
		if (this._hookVfTable != null) {
			Ktisis.Log.Verbose("Freezing hookVfTable from spawn manager");
			Marshal.FreeHGlobal((nint)this._hookVfTable);
			this._hookVfTable = null;
		}
		GC.SuppressFinalize(this);
	}
}
