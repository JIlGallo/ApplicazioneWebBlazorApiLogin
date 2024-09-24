using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace ApplicazioneWebBlazorLogin
{
    public class GeolocationService
    {
        private readonly IJSRuntime _jsRuntime;

        public GeolocationService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<GeolocationCoordinates> GetCurrentPosition()
        {
            return await _jsRuntime.InvokeAsync<GeolocationCoordinates>("geolocation.getCurrentPosition");
        }
    }
}
public class GeolocationCoordinates
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
