namespace SmartEnergy.Library.Measurements.Models;

public class Measurement
{
    public DateTime Timestamp { get; set; }
    public string? LocationId { get; set; }
    public Sensor Sensor { get; set; }
    public double Value { get; set; }
    public Unit Unit { get; set; }
    public double EnergyPrice { get; set; }
    public double Temperature { get; set; }
}

