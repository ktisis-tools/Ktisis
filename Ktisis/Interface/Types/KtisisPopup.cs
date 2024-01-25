using System;

using Dalamud.Interface.Utility.Raii;

using GLib.Popups;

using ImGuiNET;

namespace Ktisis.Interface.Types;

public abstract class KtisisPopup(string id) : IPopup {
	private bool _isOpen;
	private bool _isOpening;
	private bool _isClosing;

	public bool IsOpen => this._isOpen || this._isOpening;

	public virtual void Open() => this._isOpening = true;
	public virtual void Close() => this._isClosing = true;
	
	public bool Draw() {
		if (this._isOpening) {
			this._isOpening = false;
			ImGui.OpenPopup(id);
		}

		this._isOpen = ImGui.IsPopupOpen(id) && !this._isClosing;
		if (!this._isOpen) return false;

		using var popup = ImRaii.Popup(id);
		if (!popup.Success) return false;
		
		try {
			this.OnDraw();
		} catch (Exception err) {
			Ktisis.Log.Error($"Error drawing popup:\n{err}");
		}
		
		return true;
	}

	protected abstract void OnDraw();
}
