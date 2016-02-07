using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.Xaml.Navigation;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Lesson_203
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        //A class which wraps the barometric sensor
        private IBmp280 _bmp280;

        // IOT hub connection string
        private const string IotHubUri = "mohitweather.azure-devices.net";
#if PI
    // myBmp280
        private const string DeviceId = "myBmp280";
#else
        // myMockBmp280
        private const string DeviceId = "myMockBmp280";
#endif
        private string _deviceKey = "<obtained from secrets.json>";
        private string _openWeatherKey = "<obtained from secrets.json>";

        // ReSharper disable once NotAccessedField.Local
        private Timer _timer;

        public MainPage()
        {
            InitializeComponent();
        }

        //This method will be called by the application framework when the page is first loaded
        protected override async void OnNavigatedTo(NavigationEventArgs navArgs)
        {
            Debug.WriteLine("MainPage::OnNavigatedTo");

            await ReadConfigAsync();
            //MakePinWebAPICall();

            //Create a new object for our barometric sensor class
#if PI
            _bmp280 = new Bmp280();
#else
            _bmp280 = new MockBmp280(_openWeatherKey);
#endif
            //InitializeAsync the sensor
            await _bmp280.InitializeAsync();

            var deviceClient = DeviceClient.Create(
                IotHubUri,
                new DeviceAuthenticationWithRegistrySymmetricKey(DeviceId, _deviceKey),
                TransportType.Http1);

            //Create variables to store the sensor data: temperature, pressure and altitude. 
            //InitializeAsync them to 0.
            float temp;
            float press;
            float altitude;

            //Create a constant for pressure at sea level. 
            //This is based on your local sea level pressure (Unit: Hectopascal)
            const float seaLevelPressure = 1013.25f;

            _timer = new Timer(async e =>
            {
                temp = await _bmp280.ReadTemperatureAsync();
                press = await _bmp280.ReadPreasureAsync();
                altitude = await _bmp280.ReadAltitudeAsync(seaLevelPressure);

                //Write the values to your debug console
                Debug.WriteLine("Temperature: " + temp.ToString(CultureInfo.InvariantCulture) + " deg C");
                Debug.WriteLine("Pressure: " + press.ToString(CultureInfo.InvariantCulture) + " Pa");
                Debug.WriteLine("Altitude: " + altitude.ToString(CultureInfo.InvariantCulture) + " m");

                //Send it to IoT Hub
                var telemetryDataPoint = new
                {
                    deviceId = DeviceId,
                    temperature = temp,
                    pressure = press
                };
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                await deviceClient.SendEventAsync(message);
                Debug.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
            }, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromMinutes(10));
        }

        // This method will put your pin on the world map of makers using this lesson.
        // This uses imprecise location (for example, a location derived from your IP 
        // address with less precision such as at a city or postal code level). 
        // No personal information is stored.  It simply
        // collects the total count and other aggregate information.
        // http://www.microsoft.com/en-us/privacystatement/default.aspx
        // Comment out the line below to opt-out
        /// <summary>
        /// </summary>
        public void MakePinWebApiCall()
        {
            try
            {
                var client = new HttpClient();

                // Comment this line to opt out of the pin map.
                client.GetStringAsync("http://adafruitsample.azurewebsites.net/api?Lesson=203");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Web call failed: " + e.Message);
            }
        }

        public async Task ReadConfigAsync()
        {
            var packageFolder = Package.Current.InstalledLocation;
            var item = await packageFolder.TryGetItemAsync("secrets.json");
            if (item is StorageFile)
            {
                var contents = await FileIO.ReadTextAsync(item as StorageFile);
                var secrets = JsonConvert.DeserializeObject<JObject>(contents);
                _deviceKey = (string) secrets[DeviceId];
                _openWeatherKey = (string) secrets["openWeather"];
            }
        }
    }
}