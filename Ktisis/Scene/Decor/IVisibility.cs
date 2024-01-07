namespace Ktisis.Scene.Decor;

public interface IVisibility {
	public bool Visible { get; set; }

	public bool Toggle() => this.Visible = !this.Visible;
}
