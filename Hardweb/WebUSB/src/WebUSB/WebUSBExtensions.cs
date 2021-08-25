using Microsoft.Extensions.DependencyInjection;

namespace WebUSB
{
    public static class WebUSBExtensions
    {
        public static IServiceCollection UseWebUSB(this IServiceCollection services) => services.AddSingleton<IUSB, USB>();
    }
}