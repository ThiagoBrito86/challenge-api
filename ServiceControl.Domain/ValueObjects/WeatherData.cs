namespace ServiceControl.Domain.ValueObjects;

public record WeatherData
{
    public Temperature Temperature { get; }
    public string Description { get; }
    public int Humidity { get; }
    public int Pressure { get; }
    public DateTime Timestamp { get; }
    
    public WeatherData(
        decimal temperature,
        string description,
        int humidity,
        int pressure)
    {
        Temperature = new Temperature(temperature);
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Humidity = humidity;
        Pressure = pressure;
        Timestamp = DateTime.UtcNow;
    }

    public WeatherData()
    {
    }
}