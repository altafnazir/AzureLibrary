namespace DummyAPI
{
    public class WeatherForecastV2
    {
        public DateOnly Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string Unit { get; set; } = "Celsius";

        public string? Summary { get; set; }

        public string Test { get; set; } = "V2";
    }
}
