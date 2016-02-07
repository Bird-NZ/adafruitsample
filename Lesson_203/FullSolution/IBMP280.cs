using System.Threading.Tasks;

namespace Lesson_203
{
    public interface IBmp280
    {
        Task InitializeAsync();
        Task<float> ReadTemperatureAsync();
        Task<float> ReadPreasureAsync();
        Task<float> ReadAltitudeAsync(float seaLevel);
    }
}