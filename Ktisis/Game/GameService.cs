using Ktisis.Core;
using Ktisis.Core.Singletons;

namespace Ktisis.Game; 

public class GameService : Service {
	public readonly XivFramework Framework;
	
	public readonly GPoseState GPoseState;
	
	// Constructor

	public GameService() {
		Framework = Services.Events.CreateProvider<XivFramework>();
		GPoseState = new GPoseState();
	}
	
	// Initialize
	
	public override void Init() {
		GPoseState.Init();
		
		Services.Events.CreateClient(GPoseState);
	}
	
	// Disposal

	public override void Dispose() {
		Services.Events.RemoveProvider(Framework);
		Services.Events.RemoveClient(GPoseState);
		
		GPoseState.Dispose();
	}
}