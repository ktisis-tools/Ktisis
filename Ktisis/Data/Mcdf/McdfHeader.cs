namespace Ktisis.Data.Mcdf;

public record McdfHeader {
	public byte Version { get; set; }
	public required string FilePath { get; set; }
	public required McdfData Data { get; set; }
}
