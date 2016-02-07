using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

#pragma warning disable 1998

namespace Lesson_203
{
    /// <summary>
    ///     Return temparature from Bellevue, WA instead of from the sensor
    /// </summary>
    internal class MockBmp280 : IBmp280
    {
        private const int Bellevuecityid = 5786882;
        private readonly string _openWeatherKey;
        private OpenWeatherMapData _weatherData;

        public MockBmp280(string openWeatherKey)
        {
            _openWeatherKey = openWeatherKey;
        }

        public async Task InitializeAsync()
        {
            Debug.WriteLine("MockBMP280::InitializeAsync");
            var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(
                "http://api.openweathermap.org/data/2.5/weather?APPID=" + _openWeatherKey + "&units=metric&id=" +
                Bellevuecityid);
            _weatherData = JsonConvert.DeserializeObject<OpenWeatherMapData>(response);
        }

        public async Task<float> ReadAltitudeAsync(float seaLevel)
        {
            Debug.WriteLine("MockBMP280::ReadAltitudeAsync");
            // Elevation of Bellevue, WA
            return 26F;
        }

        public async Task<float> ReadPreasureAsync()
        {
            Debug.WriteLine("MockBMP280::ReadPressureAsync");
            return (float) (100*_weatherData.Main.Pressure);
        }

        public async Task<float> ReadTemperatureAsync()
        {
            Debug.WriteLine("MockBMP280::ReadTemperatureAsync");
            return (float) _weatherData.Main.Temp;
        }
    }
}