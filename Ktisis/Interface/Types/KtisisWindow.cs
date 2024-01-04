using System;

using Dalamud.Interface.Windowing;

using ImGuiNET;

using Ktisis.Events;

namespace Ktisis.Interface.Types; 

public abstract class KtisisWindow : Window {
	public delegate void ClosedDelegate(KtisisWindow window);

	private readonly Event<Action<KtisisWindow>> _closedEvent = new();
	public event ClosedDelegate Closed {
		add => this._closedEvent.Add(value.Invoke);
		remove => this._closedEvent.Remove(value.Invoke);
	}

	protected KtisisWindow(
		string name,
		ImGuiWindowFlags flags = ImGuiWindowFlags.None,
		bool forceMainWindow = false
	) : base(name, flags, forceMainWindow) {
		
	}

	public void Open() => this.IsOpen = true;

	public void Close() {
		try {
			if (!this.IsOpen)
				this.OnClose();
		} finally {
			this.IsOpen = false;
		}
	}

	public override void OnClose() {
		this._closedEvent.Invoke(this);
	}
}
