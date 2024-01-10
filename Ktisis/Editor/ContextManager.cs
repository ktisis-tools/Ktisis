using System;
using System.Diagnostics;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Services;
using Ktisis.Editor.Context;
using Ktisis.Interface;
using Ktisis.Localization;

namespace Ktisis.Editor;

public interface IEditor {
	public IEditorContext? Context { get; }
}

[Singleton]
public class ContextManager : IEditor, IDisposable {
	private readonly ConfigManager _cfg;
	private readonly ContextBuilder _factory;
	private readonly GPoseService _gpose;
	private readonly LocaleManager _locale;
	private readonly EditorUi _ui;

	public IEditorContext? Context => this._context is { IsValid: true } ctx ? ctx : null;

	public ContextManager(
		ConfigManager cfg,
		ContextBuilder factory,
		GPoseService gpose,
		LocaleManager locale,
		EditorUi ui
	) {
		this._cfg = cfg;
		this._factory = factory;
		this._gpose = gpose;
		this._locale = locale;
		this._ui = ui;
	}

	public void Initialize() {
		this._ui.Initialize();
		this._locale.Initialize();
		this._gpose.StateChanged += this.OnStateChanged;
		this._gpose.Subscribe();
	}
	
	// Events

	private void OnStateChanged(object sender, bool state) {
		if (this.IsDisposing) return;
		this.Destroy();
		if (state) this.Setup();
		this._ui.HandleWorkspace(state);
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
			if (this._context != null)
				this._ui.OpenOverlay(this._context);
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to initialize editor context:\n{err}");
			this.Destroy();
		}
		
		t.Stop();
		Ktisis.Log.Debug($"Editor context initialized in {t.Elapsed.TotalMilliseconds:00.00}ms");
	}

	private void SetupContext() {
		var mediator = new ContextMediator(this, this._cfg.Config, this._locale);
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
		ContextManager editor,
		Configuration cfg,
		LocaleManager locale
	) : IContextMediator {
		public IEditorContext Context { get; private set; } = null!;
		
		public Configuration Config { get; } = cfg;
		public LocaleManager Locale { get; } = locale;

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
