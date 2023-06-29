using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Resto.Front.Api.UI;
using Resto.Front.Api.Data.Orders;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text;
using Resto.Front.Api.Extensions;
using Resto.Front.Api.Editors.Stubs;
using System.Runtime.CompilerServices;
using Resto.Front.Api.Editors;
using Resto.Front.Api.Data.Organization.Sections;
using Resto.Front.Api.Data.Common;
using Resto.Front.Api.Data.Brd;
using System.Reactive.Linq;

namespace Resto.Front.Api.SamplePlugin
{
    using static PluginContext;

    internal sealed class ButtonsTester : IDisposable
    {
        private readonly CompositeDisposable subscriptions;

        private Dictionary<IDeliveryOrder, DeliveryStatus> statuses = new Dictionary<IDeliveryOrder, DeliveryStatus>();
        public ButtonsTester()
        {
            subscriptions = new CompositeDisposable
            {
                //Operations.AddButtonToPluginsMenu("Delivery Status", x => ShowOkPopup(x.vm))
                Notifications.DeliveryOrderChanged.Subscribe( x => Test(x, statuses))
            };
        }
        public void Dispose()
        {
            subscriptions.Dispose();
        }
        private static void ShowNotification(string message, TimeSpan? timeout = null)
        {
            Operations.AddNotificationMessage(message, "SamplePlugin", timeout ?? TimeSpan.FromSeconds(5));
        }
        public static async void Test(EntityChangedEventArgs<IDeliveryOrder> changedEntity, Dictionary<IDeliveryOrder, DeliveryStatus> statuses)
        {
            IDeliveryOrder deliveryOrder = changedEntity.Entity;
            
            if (!statuses.ContainsKey(deliveryOrder))
            {
                statuses.Add(deliveryOrder, deliveryOrder.DeliveryStatus);    
            } 
            else
            {
                if (statuses[deliveryOrder] == deliveryOrder.DeliveryStatus)
                {
                    return;
                }
                else 
                {
                    statuses[deliveryOrder] = deliveryOrder.DeliveryStatus;
                }
            }

            PluginContext.Log.InfoFormat("Delivery order changed| Number: {0}, Status: {1}", deliveryOrder.Number, deliveryOrder.DeliveryStatus);

            string jsonResult = "";
            string curlyBrace1 = "{";
            string curlyBrace2 = "}";
            string rusStatus = "";

            switch(deliveryOrder.DeliveryStatus)
            {
                case DeliveryStatus.New:
                    rusStatus = "Готовится";
                    break;
                case DeliveryStatus.Waiting:
                    rusStatus = "Приготовлен";
                    break;
                case DeliveryStatus.OnWay:
                    rusStatus = "Отправлен";
                    break;
                case DeliveryStatus.Delivered:
                    rusStatus = "Доставлен";
                    break;
                case DeliveryStatus.Closed:
                    rusStatus = "Закрыт";
                    break;
                case DeliveryStatus.Cancelled:
                    rusStatus = "Отменен";
                    break;
                case DeliveryStatus.Unconfirmed:
                    rusStatus = "Неподтвержден";
                    break;
            }

            jsonResult = string.Format("{0}\r\n    \"body\":\"Номер:{3}, Статус:{4}\",\r\n    \"recipient\":\"{2}\"\r\n{1}", curlyBrace1, curlyBrace2, deliveryOrder.Phone, deliveryOrder.Number, rusStatus);
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://wappi.pro/api/async/message/send?profile_id=31e877b3-8fc1");
            request.Headers.Add("Authorization", "e43f33652cc3e8a3e12ecfde461b6a1899f44330");
            var content = new StringContent(jsonResult, Encoding.UTF8, "text/plain");
            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }
}