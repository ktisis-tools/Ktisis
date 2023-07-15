using Ktisis.Core;
using Ktisis.Core.Singletons;
using Ktisis.Game.Engine;

namespace Ktisis.Game; 

public class GameService : Service {
	internal readonly GPoseState GPose;
	
	internal readonly XivFramework Framework;

	// Constructor

	public GameService() {
		GPose = new GPoseState(this);
		Framework = Services.Events.CreateProvider<XivFramework>();
	}
	
	// Initialize
	
	public override void Init() {
		Services.Events.CreateClient(GPose);
	}
	
	// Disposal

	public override void Dispose() {
		Services.Events.RemoveProvider(Framework);
		Services.Events.RemoveClient(GPose);
		
		GPose.Dispose();
	}
}