using System;
using System.Diagnostics;

using Ktisis.Core.Attributes;
using Ktisis.Core.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.Services.Game;

namespace Ktisis.Editor.Context;

[Singleton]
public class ContextManager : IDisposable {
	private readonly GPoseService _gpose;
	private readonly ContextBuilder _builder;
	
	public IEditorContext? Current => this._context is { IsValid: true } ctx ? ctx : null;
	
	public ContextManager(
		GPoseService gpose,
		ContextBuilder builder
	) {
		this._gpose = gpose;
		this._builder = builder;
	}

	private bool _isInit;
	private IPluginContext? _plugin;
	private IEditorContext? _context;

	public void Initialize(IPluginContext context) {
		if (this._isInit)
			throw new Exception("Attempted double initialization of ContextManager.");
		this._isInit = true;
		this._plugin = context;
		this._gpose.StateChanged += this.OnGPoseEvent;
		this._gpose.Subscribe();
	}
	
	// Handlers

	private void OnGPoseEvent(object sender, bool active) {
		if (!this._isInit) return;
		this.Destroy();
		if (active) this.SetupEditor();
	}
	
	// Context setup

	private void SetupEditor() {
		if (!this._isInit || this._plugin == null)
			throw new Exception("Attempted to setup uninitialized context.");
		
		Ktisis.Log.Verbose("Creating new editor context...");

		var t = new Stopwatch();
		t.Start();

		try {
			this._context = this._builder.Create(this._plugin);
			this._context.Initialize();
			this._gpose.Update += this.Update;
		} catch (Exception err) {
			Ktisis.Log.Error($"failed to initialize editor state:\n{err}");
			this.Destroy();
		}
		
		t.Stop();
		Ktisis.Log.Debug($"Editor context initialized in {t.Elapsed.TotalMilliseconds:00.00}ms");
	}
	
	// Update handler

	private void Update() {
		if (!this._isInit) return;
		switch (this._context) {
			case { IsValid: true } context:
				context.Update();
				break;
			case { IsValid: false }:
				this.Destroy();
				break;
			default:
				return;
		}
	}
	
	// Destruction

	private void Destroy() {
		try {
			this._context?.Dispose();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to destroy editor state:\n{err}");
		} finally {
			this._context = null;
		}
		this._gpose.Update -= this.Update;
	}
	
	// Disposal

	public void Dispose() {
		this._isInit = false;
		this.Destroy();
		this._gpose.StateChanged -= this.OnGPoseEvent;
	}
}