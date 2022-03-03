using System;
using System.Threading.Tasks;

using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

namespace DownloadManager.Core {
    public static class NotificationManager {

        /// <summary>
        /// Slanje Toast obavjestenja o nezavrsenim slikama korisniku.
        /// </summary>
        /// <returns></returns>
        public static void NotifyUser() {

            try {
                ToastContentBuilder toastContentBuilder = new ToastContentBuilder();
                toastContentBuilder.AddText("You have unfinished jobs!", AdaptiveTextStyle.Title, hintMaxLines: 1);
                toastContentBuilder.SetToastScenario(ToastScenario.Reminder);


                ToastContent content = toastContentBuilder.GetToastContent();
                ToastNotification notification = new ToastNotification(content.GetXml());
                ToastNotificationManager.CreateToastNotifier().Show(notification);
            }
            catch { }
        }
    }
}
