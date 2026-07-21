using System;

using Dalamud.Hooking;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

using Ktisis.Editor.Context;
using Ktisis.Interop.Hooking;
using Ktisis.Scene.Modules;
using Ktisis.Scene.Types;
using Ktisis.Services.Game;

namespace Ktisis.Scene;

public class CharacterHooks : SceneModule {
	private readonly ContextManager _contextManager;
	private readonly Hook<Character.Delegates.Terminate> _terminateHook;
	private readonly Hook<Character.Delegates.Dtor> _destroyHook;
	private readonly Hook<Character.Delegates.OnInitialize> _initializeHook;
	private readonly ActorService _actorService;
	private readonly IFramework _framework;
	
	public unsafe CharacterHooks(
		IHookMediator hookMediator,
		ISceneManager sceneManager,
		IFramework framework,
		IGameInteropProvider hooks,
		ContextManager ctxManager,
		ActorService actorService
	) : base(hookMediator, sceneManager) {
		_framework = framework;
		_contextManager = ctxManager;
		_actorService = actorService;
		_terminateHook = hooks.HookFromAddress<Character.Delegates.Terminate>((nint)Character.StaticVirtualTablePointer->Terminate, Terminate);
		_destroyHook = hooks.HookFromAddress<Character.Delegates.Dtor>((nint)Character.StaticVirtualTablePointer->Dtor, Destructor);
		_initializeHook = hooks.HookFromAddress<Character.Delegates.OnInitialize>((nint)Character.StaticVirtualTablePointer->OnInitialize, InitializeHook);
		Ktisis.Log.Info("CharacterHooks Setup");
	}

	public override void Setup() {
		this.EnableAll();
	}

	private unsafe void InitializeHook(Character* thisPtr) {
		Ktisis.Log.Verbose("[Initialize] New Character? {0:X}", (nint) thisPtr);
		
		try {
			_initializeHook.OriginalDisposeSafe(thisPtr);
		} catch (Exception e) {
			Ktisis.Log.Error(e, "Error on Initialize");
		}
		
		_framework.RunOnTick(() => {
			this.Add(thisPtr);
			
			if (thisPtr->ChildObject != null) {
				this.Add(thisPtr->ChildObject);
			}
		}, delayTicks: 1); //delayed to allow internal code to handle 
	}
	
	private unsafe GameObject* Destructor(Character* thisPtr, byte freeFlags) {
		Remove(thisPtr);

		try {
			return _destroyHook.OriginalDisposeSafe(thisPtr, freeFlags);
		} catch (Exception e) {
			Ktisis.Log.Error(e, "Error on dtor");
			return null;
		}
	}
	
	private unsafe void Terminate(Character* character) {
		Remove(character);
		
		try {
			_terminateHook.OriginalDisposeSafe(character);
		} catch (Exception e) {
			Ktisis.Log.Error(e, "Error on terminate");
		}
	}

	private unsafe void Remove(Character* character) {
		var current = this._contextManager.Current;
		
		if (current is null) {
			Ktisis.Log.Verbose("ContextManager current is null");
			return;
		}

		
		try {
			Ktisis.Log.Debug("Trying to remove actor {0:x}", (nint) character);
			var gameObject = this._actorService.GetAddress((nint)character);
			if (gameObject is null) {
				Ktisis.Log.Error("Unable to find gameobject for {0:X}", (nint)character);

				return;
			}

			var entity = current.Scene.GetEntityForActor(gameObject);

			if (entity is null) {
				Ktisis.Log.Error("Unable to find entity for actor {0:X}", (nint)character);

				return;
			}

			entity.Remove();
		} catch (Exception e) {
			Ktisis.Log.Error(e, "Error on Remove");
		}
	}

	private unsafe void Add(Character* character) {
		var current = this._contextManager.Current;

		if (current is null) {
			Ktisis.Log.Verbose("[Initialize] ContextManager current is null");
			return;
		}
			
		var gameObject = this._actorService.GetAddress((nint)character);
		if (gameObject is null) {
			Ktisis.Log.Error("Unable to find gameobject, or index 200/201 for {0:X}", (nint)character);

			return;
		}
			
		var entity = current.Scene.GetEntityForActor(gameObject);

		if (entity is not null) {
			return;
		}

		try {
			Ktisis.Log.Info("Trying to add actor {0:X}", (nint)character);
			current.Scene.Factory.BuildActor(gameObject).Add();
		} catch (Exception e) {
			Ktisis.Log.Error(e, "Error on Remove");
		}
	}
}
