using System.IO;
using System.Linq;

using Dalamud.Utility;

using Ktisis.Scene.Entities.Utility;
using Ktisis.Scene.Factory.Types;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Factory.Builders;

public interface IOverlayBuilder : IEntityBuilder<TalkOverlay, IOverlayBuilder> { }

public sealed class OverlayBuilder : EntityBuilder<TalkOverlay, IOverlayBuilder>, IOverlayBuilder {
	public OverlayBuilder(
		ISceneManager scene
	) : base(scene) { }

	protected override IOverlayBuilder Builder => this;

	protected override TalkOverlay Build() {
		if (this.Name.IsNullOrEmpty())
			this.Name = $"Dialog {this.Scene.Children.OfType<TalkOverlay>().Count() + 1}";

		return new TalkOverlay(this.Scene) {
			Name = this.Name
		};
	}
}
