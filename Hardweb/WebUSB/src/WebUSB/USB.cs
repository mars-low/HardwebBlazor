using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace WebUSB
{
    public class USB : IUSB
    {
        private static readonly List<USBDeviceFilter> _emptyFilters = new List<USBDeviceFilter>();
        private const string REGISTER_USB_METHOD = "BlazorExtensions.WebUSB.RegisterUSBEvents";
        private const string REMOVE_USB_METHOD = "BlazorExtensions.WebUSB.RemoveUSBEvents";
        private const string REQUEST_DEVICE_METHOD = "BlazorExtensions.WebUSB.RequestDevice";
        private const string GET_DEVICES_METHOD = "BlazorExtensions.WebUSB.GetDevices";
        private bool _initialized = false;
        public event Action<USBDevice> OnDisconnect;
        public event Action<USBDevice> OnConnect;
        internal IJSRuntime JSRuntime { get; private set; }

        public USB(IJSRuntime jsRuntime)
        {
            this.JSRuntime = jsRuntime;
        }

        public async Task<USBDevice[]> GetDevices()
        {
            var devices = await this.JSRuntime.InvokeAsync<USBDevice[]>(GET_DEVICES_METHOD);
            foreach (var device in devices)
            {
                device.AttachUSB(this);
            }
            return devices;
        }

        public async Task<USBDevice> RequestDevice(USBDeviceRequestOptions options = null)
        {
            try
            {
                if (options == null)
                    options = new USBDeviceRequestOptions { Filters = _emptyFilters };
                var device = await this.JSRuntime.InvokeAsync<USBDevice>(REQUEST_DEVICE_METHOD, options);
                device.AttachUSB(this);
                return device;
            }
            catch (JSException)
            {
                return null;
            }
        }

        [JSInvokable("OnConnect")]
        public Task Connected(USBDevice device)
        {
            if (this.OnConnect != null &&
                this.OnConnect.GetInvocationList().Length > 0)
            {
                this.OnConnect.Invoke(device);
            }
            return Task.CompletedTask;
        }

        [JSInvokable("OnDisconnect")]
        public Task Disconnected(USBDevice device)
        {
            if (this.OnDisconnect != null &&
                this.OnDisconnect.GetInvocationList().Length > 0)
            {
                this.OnDisconnect.Invoke(device);
            }
            return Task.CompletedTask;
        }

        // TODO: Find out a more smart way to register to global connect/disconnect events from the WebUSB API regardless if there are subscribers to the events.
        public async Task Initialize()
        {
            if (!this._initialized)
            {
                await this.JSRuntime.InvokeAsync<object>(REGISTER_USB_METHOD, DotNetObjectReference.Create(this));
                this._initialized = true;
            }
        }
    }
}