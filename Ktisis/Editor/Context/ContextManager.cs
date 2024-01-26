using System;
using System.Diagnostics;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Interop.Ipc;
using Ktisis.Localization;
using Ktisis.Services;
using Ktisis.Services.Game;

namespace Ktisis.Editor.Context;

public interface IContextManager {
	public IEditorContext? Context { get; }
}

[Singleton]
public class ContextManager : IContextManager, IDisposable {
	private readonly ConfigManager _cfg;
	private readonly ContextBuilder _factory;
	private readonly GPoseService _gpose;
	private readonly LocaleManager _locale;
	private readonly IpcManager _ipc;

	public IEditorContext? Context => this._context is { IsValid: true } ctx ? ctx : null;

	public ContextManager(
		ConfigManager cfg,
		ContextBuilder factory,
		GPoseService gpose,
		LocaleManager locale,
		IpcManager ipc
	) {
		this._cfg = cfg;
		this._factory = factory;
		this._gpose = gpose;
		this._locale = locale;
		this._ipc = ipc;
	}

	public void Initialize() {
		this._locale.Initialize();
		this._gpose.StateChanged += this.OnStateChanged;
		this._gpose.Subscribe();
	}
	
	// Events

	private void OnStateChanged(object sender, bool state) {
		if (this.IsDisposing) return;
		this.Destroy();
		if (state) this.Setup();
	}
	
	// Initialization
	
	private IEditorContext? _context;

	private void Setup() {
		if (this.IsDisposing)
			throw new Exception("Attempted to initialize context while disposed.");
		
		Ktisis.Log.Verbose("Initializing new editor context...");

		var t = new Stopwatch();
		t.Start();

		try {
			this._gpose.Update += this.Update;
			this.SetupContext();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to initialize editor context:\n{err}");
			this.Destroy();
		}
		
		t.Stop();
		Ktisis.Log.Debug($"Editor context initialized in {t.Elapsed.TotalMilliseconds:00.00}ms");
	}

	private void SetupContext() {
		var mediator = new ContextMediator(this);
		this._context = this._factory.Initialize(mediator);
	}
	
	// Update handler

	private void Update() {
		if (this.IsDisposing) return;
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
	
	// Destruction handler

	private void Destroy() {
		try {
			this._context?.Dispose();
			this._context = null;
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to destroy context:\n{err}");
		} finally {
			this._gpose.Update -= this.Update;
		}
	}
	
	// Mediator

	private class ContextMediator(
		ContextManager editor
	) : IContextMediator {
		public IEditorContext Context { get; private set; } = null!;
		
		public Configuration Config { get; } = editor._cfg.Config;
		public LocaleManager Locale { get; } = editor._locale;
		public IpcManager Ipc { get; } = editor._ipc;

		public bool IsGPosing => editor._gpose.IsGPosing;

		public void Initialize(IEditorContext context) {
			this.Context = context;
			this.Context.Initialize();
		}

		public void Destroy() => this.Context = null!;
	}
	
	// Disposal

	private bool IsDisposing;

	public void Dispose() {
		this.IsDisposing = true;
		this.Destroy();
		this._gpose.StateChanged -= this.OnStateChanged;
	}
}
