namespace Ktisis.History {

	//Used as a null check. Assigning null to variables will inevitably cause issues. This will not cause any NullPointerException.
	internal class DummyItem : HistoryItem {
		public override HistoryItem Clone() {
			Dalamud.Logging.PluginLog.Fatal("Attempted to call Clone() on a dummy item.");
			return new DummyItem();
		}

		public override void Update() {
			Dalamud.Logging.PluginLog.Fatal("Attempted to call Update() on a dummy item.");
			return;
		}

		public override string DebugPrint() {
			Dalamud.Logging.PluginLog.Fatal("Attempted to call DebugPrint() on a dummy item.");
			return "I'm a dummy item.";
		}

	}
}
