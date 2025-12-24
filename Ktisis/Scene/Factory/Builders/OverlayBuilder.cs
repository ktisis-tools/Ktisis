using System.IO;
using System.Linq;

using Dalamud.Utility;

using Ktisis.Scene.Entities.Utility;
using Ktisis.Scene.Factory.Types;
using Ktisis.Scene.Types;

using Lumina.Excel.Sheets;

namespace Ktisis.Scene.Factory.Builders;

public enum OverlayTypes {
	Talk,
	Balloon,
	Status
}

public interface IOverlayBuilder : IEntityBuilder<OverlayEntity, IOverlayBuilder> { }

public sealed class OverlayBuilder : EntityBuilder<OverlayEntity, IOverlayBuilder>, IOverlayBuilder {
	private OverlayTypes _type;
	public OverlayBuilder(
		ISceneManager scene,
		OverlayTypes type
	) : base(scene) {
		this._type = type;
	}

	protected override IOverlayBuilder Builder => this;

	protected override OverlayEntity Build() => this._type switch {
		OverlayTypes.Talk => this.BuildTalk(),
		OverlayTypes.Balloon => this.BuildBalloon(),
		OverlayTypes.Status => this.BuildStatus(),
		_ => this.BuildTalk()
	};

	private TalkOverlay BuildTalk() {
		if (this.Name.IsNullOrEmpty())
			this.Name = $"Dialog {this.Scene.Children.OfType<TalkOverlay>().Count() + 1}";

		return new TalkOverlay(this.Scene) {
			Name = this.Name
		};
	}

	private BalloonOverlay BuildBalloon() {
		if (this.Name.IsNullOrEmpty())
			this.Name = $"Balloon {this.Scene.Children.OfType<BalloonOverlay>().Count() + 1}";

		return new BalloonOverlay(this.Scene) {
			Name = this.Name
		};
	}

	private StatusOverlay BuildStatus() {
		if (this.Name.IsNullOrEmpty())
			this.Name = $"Status {this.Scene.Children.OfType<StatusOverlay>().Count() + 1}";

		return new StatusOverlay(this.Scene) {
			Name = this.Name
		};
	}
}
