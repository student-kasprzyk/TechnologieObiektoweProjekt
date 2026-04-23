using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using System.Threading.Tasks;

namespace GeoMapsPrototype.Platforms.Android
{
    [Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
    public class LocationTracker : Service
    {
        private bool isRunning;

        public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            if (!isRunning)
            {
                isRunning = true;
                Task.Run(async () => await CheckLocation());
            }

            return StartCommandResult.Sticky;
        }

        private async Task CheckLocation()
        {
            while (isRunning)
            {

                    System.Diagnostics.Debug.WriteLine("Pobieram lokacje");
                    var userLocation = await Geolocation.Default.GetLocationAsync(
                        new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10)));

                    if (userLocation != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Daje lokacje");
                        LocationProvider.UpdateLocation(userLocation);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Nie mam lokacji");
                    }
                


                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }

        /*public override void OnDestroy()
        {
            isRunning = false;
            base.OnDestroy();
        }*/
    }
}