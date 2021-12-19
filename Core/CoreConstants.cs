namespace BlazorRepl.Core
{
    public static class CoreConstants
    {
        public const string MainComponentFilePath = "__Main.razor";
        public const string MainComponentDefaultFileContent = @"
<!-- Web Bluetooth manager -->
@inject BluetoothNavigator ble
<!-- Web USB manager -->
@inject IUSB usb
<!-- Floating notification -->
@inject ISnackbar Snackbar
@using System.Text

<MudGrid>
    <MudItem xs=""12"">
        <MudToolBar>
            <MudIconButton Icon=""@usbStateIcon"" Color=""Color.Inherit"" OnClick=""ToggleUsbState"" Class=""mr-3"" />
            <MudIconButton Icon=""@bleStateIcon"" Color=""Color.Inherit"" OnClick=""ToggleBleState"" Class=""mr-3"" />
        </MudToolBar>
    </MudItem>
    <MudItem xs=""12"">
        <MudContainer MaxWidth=""MaxWidth.Small"">
            <MudTimePicker @ref=""_picker"" @bind-Time=""time"" Label=""Timer"" OpenTo=""OpenTo.Minutes"" TimeFormat=""HH:mm:ss"">
                <PickerActions>
                    <MudButton Class=""mr-auto align-self-start"" OnClick=""@(() => _picker.Clear())"">Clear</MudButton>
                    <MudButton OnClick=""@(() => _picker.Close(false))"">Cancel</MudButton>
                    <MudButton Color=""Color.Primary"" OnClick=""@(() => _picker.Close())"">Ok</MudButton>
                </PickerActions>
            </MudTimePicker>
        </MudContainer>
    </MudItem>
    <MudItem xs=""12"">
        <MudContainer MaxWidth=""MaxWidth.Small"">
            <MudSlider @bind-Value=""Power"" Min=""0"" Max=""9"" Step=""1"" Color=""Color.Info"">Power: @Power.ToString()</MudSlider>
        </MudContainer>
    </MudItem>
    <MudItem xs=""6"">
        <MudContainer MaxWidth=""MaxWidth.Small"">
            <MudRadioGroup @bind-SelectedOption=""LeftTransducer"">
                <MudRadio Option=""true"" Color=""Color.Primary"" Size=""Size.Large"">L</MudRadio>
                <MudRadio Option=""false"" Color=""Color.Secondary"" Size=""Size.Large"">R</MudRadio>
            </MudRadioGroup>
        </MudContainer>
    </MudItem>
    <MudItem xs=""6"">
        <MudContainer MaxWidth=""MaxWidth.Small"">
            <MudRadioGroup @bind-SelectedOption=""UltrasoundMode"">
                <MudRadio Option=""1"" Color=""Color.Tertiary"" Size=""Size.Large"">Continuous</MudRadio>
                <MudRadio Option=""2"" Color=""Color.Tertiary"" Size=""Size.Large"">Pulsed</MudRadio>
                <MudRadio Option=""3"" Color=""Color.Tertiary"" Size=""Size.Large"">Mixed</MudRadio>
            </MudRadioGroup>
        </MudContainer>
    </MudItem>
</MudGrid>

@code { 
    // Connection state
    private string usbStateIcon = Icons.Material.Outlined.UsbOff; //Icons.Material.Outlined.Usb
    private string bleStateIcon = Icons.Material.Outlined.BluetoothDisabled; //Icons.Material.Outlined.BluetoothConnected

    public bool bleState { get; set; }
    public bool usbState { get; set; }

    // Timer
    private MudTimePicker _picker;
    private TimeSpan? time = new TimeSpan(00, 15, 00);

    // Transducer selector
    private bool _leftTransducer = true;
    public bool LeftTransducer 
    { 
        get { return _leftTransducer; } 
        set
        {
            _leftTransducer = value;
            OnTransducerChanged(value);
        } 
    }

    // Mode selector
    private int _ultrasoundMode = 1;
    public int UltrasoundMode 
    { 
        get { return _ultrasoundMode; } 
        set
        {
            _ultrasoundMode = value;
            OnUltrasoundModeChanged(value);
        } 
    }

    // Ultrasound power slider
    private int _power = 0;
    public int Power
    {
        get { return _power; }
        set
        {
            _power = value;
            OnPowerChanged(value);
        }
    }

    private Device bleDevice;
    private string ultrasoundServiceId = ""60ef00ca-bd07-4aaa-9d08-59a88f95633c"";
    private string ultrasoundCharacteristic= ""079bb77f-015f-40c8-b01a-3b05464ad0b1"";

    private USBDevice device = null;
    private bool usbDeviceInitialized = false;
    private string productId { get; set; }
    private string vendorId { get; set; }

    private bool isTimerRunning = false;

    private async Task ResumeTimer()
    {
        isTimerRunning = true;
        while (isTimerRunning & time.Value > new TimeSpan())
        {
            await Task.Delay(1000);
            time = time.Value.Subtract(new TimeSpan(0,0,1));
            if (time.Value == new TimeSpan())
            {
                Console.WriteLine(""Timer stopped"");
                isTimerRunning = false;
                Power = 0;
                ResetTimer();
                Snackbar.Add(""Timer elapsed. Massager stopped."");
            }
            StateHasChanged();
        }
    }

    private void PauseTimer()
    {
        isTimerRunning = false;
    }

    private void ResetTimer()
    {
        time = new TimeSpan(00,15,00);
    }

    private async Task OnTransducerChanged(bool left)
    {
        char code = left ? 'L' : 'R';
        await SendControlCode(code);
    }

    private async Task OnUltrasoundModeChanged(int mode)
    {
        char code = 'C';
        if (mode == 1)
        {
            code = 'C'; // Continuous
        } else if (mode == 2)
        {
            code = 'P'; // Pulsed
        } else if (mode ==3)
        {
            code = 'M'; // Mixed
        }
        
        await SendControlCode(code);
    }

    private async Task OnPowerChanged(int powerDigit)
    {
        char code = (char)(powerDigit + '0');
        if(powerDigit > 0)
        {
            if (!isTimerRunning)
            {
                await ResumeTimer();
            }
        } else {
            PauseTimer();
        }

        await SendControlCode(code);
    }

    private async Task SendControlCode(char code)
    {
        if (usbState)
        {
            await SendUSBControlCode(code);
        }
        if (bleState)
        {
            await SendBLEControlCode(code);
        }
    }

    private async Task SendUSBControlCode(char code)
    {
        if (device != null && device.Opened)
        {
            var outResult = await device.TransferOut(2, new byte[] {(byte) code});
        }
    }

    private async Task SendBLEControlCode(char code)
    {
        if (bleDevice != null)
        {
            await ble.WriteValueAsync(bleDevice.Id, ultrasoundServiceId, ultrasoundCharacteristic, new byte[] { (byte) code });
        }
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        ble.OnDeviceDisconnected += OnBleDeviceDisconnected;
        if (!usbDeviceInitialized)
        {
            usb.OnConnect += OnUSBDeviceConnect;
            usb.OnDisconnect += OnUSBDeviceDisconnect;
            usbDeviceInitialized = true;
            return usb.Initialize();
        }
        return Task.CompletedTask;
    }

    private void OnBleDeviceDisconnected(string deviceId)
    {
        Snackbar.Add(""BLE device disconnected"");
        bleState = false;
        bleStateIcon = Icons.Material.Outlined.BluetoothDisabled;
    }

    private async void OnUSBDeviceConnect(USBDevice device)
    {
        await SetupUSB();
        Snackbar.Add(""USB Device connected"");
    }

    private async void OnUSBDeviceDisconnect(USBDevice device)
    {
        Snackbar.Add(""USB Device disconnected"");
        if (device != null)
        {
            await CloseUSB();
        }
    }

    public async Task ToggleBleState()
    {
        // Because variable is not two-way bound, we need to update it ourself
        if (!bleState)
        {
            var q = new RequestDeviceQuery();
            q.AcceptAllDevices = true;
            q.OptionalServices.Add(this.ultrasoundServiceId);

            try 
            {
                bleDevice = await ble.RequestDeviceAsync(q);
                bleState = true;
                bleStateIcon = Icons.Material.Outlined.BluetoothConnected;
                var msg = String.Format(""{0} connected"", bleDevice.Name);
                Snackbar.Add(msg, Severity.Normal, config => { config.VisibleStateDuration  = 2000; });
            } catch (JSException e) 
            {
                Console.WriteLine(e.Message);
                Snackbar.Add(""BLE Device not connected"");
                bleState = false;
                bleStateIcon = Icons.Material.Outlined.BluetoothDisabled;
            }
        } else 
        {
            if (bleDevice != null)
            {
                await ble.DisconnectAsync(bleDevice.Id);
                bleState = false;
                bleStateIcon = Icons.Material.Outlined.BluetoothDisabled;
                Snackbar.Add(""BLE device disconnected"");
            }
        }
    }

    private async Task SetupUSB()
    {
        if (device != null)
        {
            device = await device.Open();
            device = await device.SelectConfiguration(1);
            device = await device.ClaimInterface(2);
            device = await device.SelectAlternateInterface(2, 0);
            // The vendor-specific interface provided by a device using this
            // Arduino library is a copy of the normal Arduino USB CDC-ACM
            // interface implementation and so reuses some requests defined by
            // that specification. This request sets the DTR (data terminal
            // ready) signal high to indicate to the device that the host is
            // ready to send and receive data.
            await device.ControlTransferOut(new USBControlTransferParameters {
                RequestType = USBRequestType.Class,
                Recipient = USBRecipient.Interface,
                Request = (byte) 0x22,
                Value = 1,
                Index = 2
            }, new byte[] {});
            usbState = true;
            usbStateIcon = Icons.Material.Outlined.Usb;
        }
    }

    private async Task CloseUSB()
    {
        // This request sets the DTR (data terminal ready) signal low to
        // indicate to the device that the host has disconnected.
        await device.ControlTransferOut(new USBControlTransferParameters {
            RequestType = USBRequestType.Class,
            Recipient = USBRecipient.Interface,
            Request = (byte) 0x22,
            Value = 0,
            Index = 2
        }, new byte[] {});
        await device.Close();
        usbState = false;
        usbStateIcon = Icons.Material.Outlined.UsbOff;
    }

    public async Task ToggleUsbState()
    {
        // Because variable is not two-way bound, we need to update it ourself

        if (!usbState)
        {
            if (string.IsNullOrEmpty(productId) && string.IsNullOrEmpty(vendorId))
            {
                device = await usb.RequestDevice();
            }
            else
            {
                device = await usb.RequestDevice(new USBDeviceRequestOptions
                {
                    Filters = new List<USBDeviceFilter>
                    {
                        new USBDeviceFilter { VendorId = Convert.ToUInt16(vendorId, 16) , ProductId = Convert.ToUInt16(productId, 16) }
                    }
                });
            }
            
            await SetupUSB();

        } else
        {
            if (device != null)
            {
                await CloseUSB();
            }
        }
    }
}
";

        public const string DefaultUserComponentsAssemblyBytes =
            "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ACIAIABiAHUAdAB0AG8AbgAgAG8AcgAgAHAAcgBlAHMAcwBpAG4AZwAgAEMAdAByAGwAKwBTAC4APAAvAHAAPgAKAAoAAD08AGgAMgA+AFMAaABhAHIAZQAgAHkAbwB1AHIAIABzAG4AaQBwAHAAZQB0AHMAIQA8AC8AaAAyAD4ACgAAczwAcAA+AFMAaABhAHIAZQAgAHkAbwB1AHIAIABzAG4AaQBwAHAAZQB0ACAAZQBhAHMAaQBsAHkAIABiAHkAIABmAG8AbABsAG8AdwBpAG4AZwAgAHQAaABlACAAcwB0AGUAcABzADoAPAAvAHAAPgAKAACBTTwAdQBsAD4APABsAGkAPgBDAGwAaQBjAGsAIAB0AGgAZQAgACIAUwBBAFYARQAiACAAYgB1AHQAdABvAG4APAAvAGwAaQA+AAoAIAAgACAAIAA8AGwAaQA+AEMAbwBuAGYAaQByAG0AIAB0AGgAYQB0ACAAeQBvAHUAIABhAGcAcgBlAGUAIAB3AGkAdABoACAAdABoAGUAIAB0AGUAcgBtAHMAPAAvAGwAaQA+AAoAIAAgACAAIAA8AGwAaQA+AEMAbwBwAHkAIAB0AGgAZQAgAFUAUgBMACAAbwBmACAAdABoAGUAIABzAG4AaQBwAHAAZQB0ACAAYQBuAGQAIABwAGEAcwB0AGUAIABpAHQAIAB3AGgAZQByAGUAdgBlAHIAIAB5AG8AdQAgAG4AZQBlAGQAPAAvAGwAaQA+ADwALwB1AGwAPgAKAAoAAFM8AGgAMgA+AFcAaABhAHQAIABhAHIAZQAgAHQAaABlACAAZQBkAGkAdABvAHIAJwBzACAAZgBlAGEAdAB1AHIAZQBzAD8APAAvAGgAMgA+AAoAAYLHPABwAD4AVwBlACAAYQByAGUAIAB1AHMAaQBuAGcAIABNAGkAYwByAG8AcwBvAGYAdAAnAHMAIAA8AGEAIAB0AGEAcgBnAGUAdAA9ACIAXwBiAGwAYQBuAGsAIgAgAGgAcgBlAGYAPQAiAGgAdAB0AHAAcwA6AC8ALwBtAGkAYwByAG8AcwBvAGYAdAAuAGcAaQB0AGgAdQBiAC4AaQBvAC8AbQBvAG4AYQBjAG8ALQBlAGQAaQB0AG8AcgAvACIAPgBNAG8AbgBhAGMAbwAgAEUAZABpAHQAbwByADwALwBhAD4ALgAgAEkAdAAgAGkAcwAgAHQAaABlACAAYwBvAGQAZQAgAGUAZABpAHQAbwByACAAdABoAGEAdAAgAHAAbwB3AGUAcgBzACAAVgBTACAAQwBvAGQAZQAuACAAWQBvAHUAIABjAGEAbgAgAGEAYwBjAGUAcwBzACAAaQB0AHMAIABDAG8AbQBtAGEAbgBkACAAUABhAGwAZQB0AHQAZQAgAGIAeQAgAGYAbwBjAHUAcwBpAG4AZwAgAG8AbgAgAHQAaABlACAAZQBkAGkAdABvAHIAIABhAG4AZAAgAGMAbABpAGMAawBpAG4AZwAgAEYAMQAgAGIAdQB0AHQAbwBuACAAbwBuACAAeQBvAHUAcgAgAGsAZQB5AGIAbwBhAHIAZAAuACAAWQBvAHUAIAB3AGkAbABsACAAcwBlAGUAIAB0AGgAZQAgAGwAaQBzAHQAIABvAGYAIABhAGwAbAAgAGEAdgBhAGkAbABhAGIAbABlACAAYwBvAG0AbQBhAG4AZABzAC4AIABZAG8AdQAgAGMAYQBuACAAdQBzAGUAIAB0AGgAZQAgAGMAbwBtAG0AYQBuAGQAcwAnACAAcwBoAG8AcgB0AGMAdQB0AHMAIAB0AG8AbwAuADwALwBwAD4ACgABaTwAcAA+AFMAbwBtAGUAIABvAGYAIAB0AGgAZQAgAG0AbwBzAHQAIABjAG8AbQBtAG8AbgBsAHkAIAB1AHMAZQBkACAAYwBvAG0AbQBhAG4AZABzACAAYQByAGUAOgA8AC8AcAA+AAoAAIFrPAB1AGwAPgA8AGwAaQA+AEMAdAByAGwAKwBLACAAQwB0AHIAbAArAEMAIABjAG8AbQBtAGUAbgB0AHMAIABvAHUAdAAgAHQAaABlACAAYwB1AHIAcgBlAG4AdAAgAGwAaQBuAGUAPAAvAGwAaQA+AAoAIAAgACAAIAA8AGwAaQA+AEMAdAByAGwAKwBTAGgAaQBmAHQAKwBLACAAZABlAGwAZQB0AGUAcwAgAGEAIABsAGkAbgBlADwALwBsAGkAPgAKACAAIAAgACAAPABsAGkAPgBDAG8AbQBtAGEAbgBkACAAUABhAGwAZQB0AHQAZQAgAC0APgAgAEUAZABpAHQAbwByACAARgBvAG4AdAAgAFoAbwBvAG0AIABJAG4ALwBPAHUAdAAgAGMAaABhAG4AZwBlAHMAIAB0AGgAZQAgAGYAbwBuAHQAIABzAGkAegBlADwALwBsAGkAPgA8AC8AdQBsAD4ACgABgWs8AHAAPgBJAGYAIAB5AG8AdQAgAHcAYQBuAHQAIAB0AG8AIABkAGkAZwAgAGEAIABsAGkAdAB0AGwAZQAgAGQAZQBlAHAAZQByACAAaQBuAHQAbwAgAE0AbwBuAGEAYwBvACAARQBkAGkAdABvAHIAJwBzACAAZgBlAGEAdAB1AHIAZQBzACwAIAB5AG8AdQAgAGMAYQBuACAAZABvACAAcwBvACAAPABhACAAdABhAHIAZwBlAHQAPQAiAF8AYgBsAGEAbgBrACIAIABoAHIAZQBmAD0AIgBoAHQAdABwAHMAOgAvAC8AYwBvAGQAZQAuAHYAaQBzAHUAYQBsAHMAdAB1AGQAaQBvAC4AYwBvAG0ALwBkAG8AYwBzAC8AZQBkAGkAdABvAHIALwBlAGQAaQB0AGkAbgBnAGUAdgBvAGwAdgBlAGQAIgA+AGgAZQByAGUAPAAvAGEAPgAuADwALwBwAD4ACgAKAAExPABoADIAPgBFAG4AagBvAHkAIABjAHIAZQBhAHQAaQBuAGcAIQA8AC8AaAAyAD4AAAAA85G1IggFbUyMt2Jcs4oKpgAEIAEBCAMgAAEFIAEBEREEIAEBDgUgAgEIDgh87IXXvqd5jgituXk4Kd2uYAUgAQESHQgBAAgAAAAAAB4BAAEAVAIWV3JhcE5vbkV4Y2VwdGlvblRocm93cwEIAQACAAAAAAAMAQAHL19fbWFpbgAAAAAAkC4AAAAAAAAAAAAAqi4AAAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJwuAAAAAAAAAAAAAAAAX0NvckRsbE1haW4AbXNjb3JlZS5kbGwAAAAAAP8lACAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAAAwAAAC8PgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

        public const string DefaultRazorFileContentFormat = "<h1>{0}</h1>";

        public static readonly string DefaultCSharpFileContentFormat =
            @$"namespace {CompilationService.DefaultRootNamespace}
{{{{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class {{0}}
    {{{{
    }}}}
}}}}
";
    }
}
