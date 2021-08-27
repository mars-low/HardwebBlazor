using Microsoft.Extensions.DependencyInjection;

namespace WebBluetooth
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddWebBluetooth(this IServiceCollection services)
        {
            return services.AddTransient<BluetoothNavigator>();
        }
    }
}
