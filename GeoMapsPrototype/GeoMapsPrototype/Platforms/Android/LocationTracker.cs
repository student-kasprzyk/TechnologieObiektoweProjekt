using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using System.Threading.Tasks;

namespace GeoMapsPrototype.Platforms.Android
{
    [Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
    public class LocationTracker : Service
    {
        private bool isRunning;
        private const string CHANNEL_ID = "location_notification_channel";
        private const int NOTIFICATION_ID = 1001;

        public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            RegisterNotificationChannel();
            var notification = CreateNotification();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                StartForeground(NOTIFICATION_ID, notification, global::Android.Content.PM.ForegroundService.TypeLocation);
            }
            else
            {
                StartForeground(NOTIFICATION_ID, notification);
            }

            if (!isRunning)
            {
                isRunning = true;
                Task.Run(async () => await CheckLocation());
            }

            return StartCommandResult.Sticky;
        }

        private void RegisterNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {

                var channel = new NotificationChannel(CHANNEL_ID, "Location Tracking", NotificationImportance.Low)
                {
                    Description = "Kanał powiadomień dla śledzenia lokalizacji w tle"
                };
                var manager = (NotificationManager)GetSystemService(NotificationService)!;
                manager.CreateNotificationChannel(channel);
            }
        }

        private Notification CreateNotification()
        {
            var builder = new NotificationCompat.Builder(this, CHANNEL_ID)
                .SetContentTitle("GeoMaps")
                .SetContentText("Śledzenie lokalizacji jest aktywne")
                .SetSmallIcon(global::Android.Resource.Drawable.IcMenuCompass)
                .SetOngoing(true)
                .SetPriority(NotificationCompat.PriorityLow);

            return builder.Build();
        }

        private async Task CheckLocation()
        {
            while (isRunning)
            {
                System.Diagnostics.Debug.WriteLine("[LocationTracker] Pobieram lokacje");
                try
                {
                    var userLocation = await Geolocation.Default.GetLocationAsync(
                        new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10)));

                    if (userLocation != null)
                    {
                        System.Diagnostics.Debug.WriteLine("[LocationTracker] Daje lokacje");
                        LocationProvider.UpdateLocation(userLocation);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LocationTracker] Błąd pobierania lokalizacji: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        public override IBinder? OnBind(Intent? intent) => null;

        public override void OnDestroy()
        {
            isRunning = false;
            base.OnDestroy();
        }
    }
}