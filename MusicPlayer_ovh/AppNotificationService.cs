using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer_ovh
{
    public static class AppNotificationService
    {
        public static event Action<string>? OnMessageReceived;

        public static void SendNotification(string message)
        {
            OnMessageReceived?.Invoke(message);
        }
    }
}
