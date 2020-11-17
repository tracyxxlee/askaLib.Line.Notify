using askaLib.Line.Notify.Model;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace askaLib.Line.Notify
{
    public static class LineNotifyServiceExtension
    {
        public static IServiceCollection AddLineNotifyService(this IServiceCollection services, LineNotifySetting setting)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            // necessary for LineNotifyService 
            services.AddHttpClient();

            services.AddScoped(x =>
                new LineNotifyService(
                    x.GetService<IHttpClientFactory>(),
                    setting));

            return services;
        }
    }
}
