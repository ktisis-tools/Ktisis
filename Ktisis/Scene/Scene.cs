namespace Ktisis.Scene;

public class Scene {
	private Scene() { }

	internal static Scene Create() {
		var scene = new Scene();
		scene.Build();
		return scene;
	}

	private void Build() {

	}
}
