using System;
using System.Collections.Generic;
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
    public class Device
    {        
        public IBmp280 Reader { get; set; }
        public string IotHubName { get; set; }
        public string IotHubKey { get; set; }
    }

    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        // IOT hub connection string
        private const string IotHubUri = "mohitweather.azure-devices.net";
        private readonly List<Device> _devices = new List<Device>();

        // ReSharper disable once NotAccessedField.Local
        private readonly List<Timer> _timers = new List<Timer>();

        public MainPage()
        {
            InitializeComponent();
        }

        //This method will be called by the application framework when the page is first loaded
        protected override async void OnNavigatedTo(NavigationEventArgs navArgs)
        {
            Debug.WriteLine("MainPage::OnNavigatedTo");

            var secrets = await ReadConfigAsync();
            //MakePinWebAPICall();

            _devices.Add(new Device { Reader = new MockBmp280((string)secrets["openWeather"]), IotHubName = "bellevueSensor", IotHubKey = (string)secrets["bellevueSensor"] });
#if PI
            _devices.Add(new Device { Reader = new Bmp280(), IotHubName = "myBmp280", IotHubKey = (string)secrets["myBmp280"] });
#endif

            //Create a new object for our barometric sensor class
            foreach (var device in _devices)
            {
                //InitializeAsync the sensor
                await device.Reader.InitializeAsync();

                var deviceClient = DeviceClient.Create(
                    IotHubUri,
                    new DeviceAuthenticationWithRegistrySymmetricKey(device.IotHubName, device.IotHubKey),
                    TransportType.Http1);

                //Create a constant for pressure at sea level. 
                //This is based on your local sea level pressure (Unit: Hectopascal)
                const float seaLevelPressure = 1013.25f;

                var timer = new Timer(async e =>
                {
                    float temp = await device.Reader.ReadTemperatureAsync();
                    float press = await device.Reader.ReadPreasureAsync();
                    float altitude = await device.Reader.ReadAltitudeAsync(seaLevelPressure);

                    //Convert to farenheit
                    temp = (float) (temp*1.8 + 32);

                    //Write the values to your debug console
                    Debug.WriteLine("Temperature: " + temp.ToString(CultureInfo.InvariantCulture) + " deg F");
                    Debug.WriteLine("Pressure: " + press.ToString(CultureInfo.InvariantCulture) + " Pa");
                    Debug.WriteLine("Altitude: " + altitude.ToString(CultureInfo.InvariantCulture) + " m");

                    // Don't be fooled - this really is the Pacific time zone,
                    // not just standard time...
                    var zone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

                    //Send it to IoT Hub
                    var telemetryDataPoint = new
                    {
                        deviceId = device.IotHubName,
                        temperature = temp,
                        pressure = press,
                        utcOffset = zone.BaseUtcOffset.Hours
                    };
                    var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                    var message = new Message(Encoding.ASCII.GetBytes(messageString));

                    await deviceClient.SendEventAsync(message);
                    Debug.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
                }, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromMinutes(10));
                // Necessary so timers are not garbage collected
                _timers.Add(timer);
            }            
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

        public async Task<JObject> ReadConfigAsync()
        {
            var packageFolder = Package.Current.InstalledLocation;
            var item = await packageFolder.TryGetItemAsync("secrets.json");
            if (item is StorageFile)
            {
                var contents = await FileIO.ReadTextAsync(item as StorageFile);
                return JsonConvert.DeserializeObject<JObject>(contents);
            }
            return new JObject();
        }
    }
}