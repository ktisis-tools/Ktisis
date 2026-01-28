namespace Ktisis.Data.Config.Props;

public record PropEntry {
	public string Item = string.Empty;
	public int Model = 0;
	public int Submodel = 0;
	public int Variant = 0;
	public string Description = string.Empty;
	// WieldsTo, SheathesTo, Notes - unsupported
}
