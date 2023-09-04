namespace Ktisis.Scene.Impl;

public interface IVisibility {
	public bool Visible { get; protected set; }

	public bool SetVisible(bool visible) => this.Visible = visible;

	public bool ToggleVisible() => SetVisible(!this.Visible);
}