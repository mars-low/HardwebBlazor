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

@* 
// Firmware running on Arduino Nano 33 IoT
/*
Using library WiFiNINA at version 1.8.8
Using library ArduinoOTA at version 1.0.7
Using library Adafruit_TinyUSB_Arduino at version 1.3.0
Using library ArduinoBLE at version 1.2.1
*/

/*
    This program creates a BLE peripheral with a service that contains a
    characteristic to switch to WiFi. It also contains characteristic to control
    ultrasound massager remotely.

    You can use a generic BLE central app, like LightBlue (iOS and Android) or
    nRF Connect (Android), to interact with the services and characteristics
    created in this sketch.
*/

/*
    In OTA mode it connects to an WPA encrypted WiFi network.
    Then it prints the IP address obtained and other network details.
    It then polls for sketch updates over WiFi, sketches
    can be updated by selecting a network port from within
    the Arduino IDE: Tools -> Port -> Network Ports.
*/

/* This sketch uses WebUSB to connect with Chrome browser. After successfull enumeration, Chrome will pop-up notification with URL to landing page. Click on it to test. Click ""Connect"" and select device.

    Note: On Windows 7 and prior: You need to use Zadig tool to manually bind the WebUSB interface with the WinUSB driver for Chrome to access. From windows 8 and 10, this is done automatically by firmware.
*/

/*
    It can function as an AccesPoint Server with Captive Portal

    1. Setting DNS and Gateway IUP to Access Point IP address.
    2. Checking first 16 DNS requests via UDP port 53.
    3. Send Response with redirected IP address for landing page.
    4. WebServer is a custom page to test board.
    5. <yourIP>/SECRETS endpoint sends the list of answers to the serial port.

    Works well on mobile device with Chrome. 
    I've tested it on Asus Zenfone Max Pro M1.
*/

// Store sensitive data in a separate file and do not include in version control system
#include ""arduino_secrets.h""
#include ""Adafruit_TinyUSB.h""
#include <WiFiUdp.h>
#include <SPI.h>
#include <WiFiNINA.h>
#include <ArduinoOTA.h>
#include <ArduinoBLE.h>

#define TIMER_INTERRUPT_DEBUG         0
#define _TIMERINTERRUPT_LOGLEVEL_     0

#include ""SAMDTimerInterrupt.h""

#define TIMER_INTERVAL_US 70L

SAMDTimer ITimer(TIMER_TC3);

/*
0 - continuous
1 - pulsed
2 - mixed
*/
int mode = 0;           // choose massager mode
int cycles = 0;         // count cycles in mixed mode
bool risingEdge = true; // track high and low signal level
bool idleState = false; // keep transducer switched off during period in mixed mode
int powerLevel = 0;     // control ultrasound intensity
int jump = 37;          // approx 0.12 V
int powerOne = 465;     // approx 1.5 V

void UltrasoundModeController()
{
    Serial.print(""PowerLevel:"");
    Serial.println(powerLevel);
    if(mode == 0) analogWrite(DAC0, powerLevel); // keep voltage at constant level
    else if (mode == 1) { // pulsed mode
        if(risingEdge) analogWrite(DAC0, powerLevel);
        else analogWrite(DAC0, 0);
        risingEdge = !risingEdge;
    } else if (mode == 2) { // mixed mode
        if(idleState) analogWrite(DAC0, 0);
        else {
            if(risingEdge) analogWrite(DAC0, powerLevel);
            else analogWrite(DAC0, 0);
            risingEdge = !risingEdge;
        }

        cycles = cycles + 1;
        if(cycles == 8) {
            cycles = 0;
            idleState = !idleState;
        }
    }
}

///////please enter your sensitive data in the arduino_secrets.h
/////// Wifi Settings ///////
char ssid[] = SECRET_SSID;      // your network SSID (name)
char pass[] = SECRET_PASS;   // your network password

///////please enter your sensitive data in the arduino_secrets.h
/////// Wifi OTA hotspot credentials///////
char ssid_ota[] = SECRET_SSID_OTA;   // your network SSID (name)
char pass_ota[] = SECRET_PASS_OTA;   // your network password

int keyIndex = 0; // your network key Index number (needed only for WEP)

// Define Wifi Client parameters
IPAddress gwip(172, 128, 128, 1);     // GW fixed IP adress
IPAddress apip(172, 128, 128, 100);   // AP fixed IP adress
uint8_t apChannel = 2;                // AP wifi channel
String answerLine = """";               // data incoming from the client
String answerList1[32];               // store of answerList SSID
String answerList2[32];               // store of answerList answers
int answerCounter = 0;

// Define UDP settings for DNS
#define UDP_PACKET_SIZE 128           // MAX UDP packet size = 512
#define DNSHEADER_SIZE 12             // DNS Header
#define DNSANSWER_SIZE 16             // DNS Answer with Packet Compression
#define DNSMAXREQUESTS 16             // trigger DNS requests to redirect to webpage
byte packetBuffer[ UDP_PACKET_SIZE];  // incoming and outgoing packets
byte dnsReplyHeader[DNSHEADER_SIZE] = {
    0x00, 0x00,  // ID, to be filled in #offset 0
    0x81, 0x80,  // answer header Codes
    0x00, 0x01,  //QDCOUNT = 1 question
    0x00, 0x01,  //ANCOUNT = 1 answer
    0x00, 0x00,  //NSCOUNT / ignore
    0x00, 0x00   //ARCOUNT / ignore
};
byte dnsReplyAnswer[DNSANSWER_SIZE] = {
    0xc0, 0x0c, // pointer to pos 12 : NAME Labels
    0x00, 0x01, // TYPE
    0x00, 0x01, // CLASS
    0x00, 0x00, // TTL
    0x00, 0x3c, // TLL 1 hour
    0x00, 0x04, // RDLENGTH = 4
    0x00, 0x00, // IP adress octets to be filled #offset 12
    0x00, 0x00  // IP adress octeds to be filled
} ;
byte dnsReply[UDP_PACKET_SIZE];  // buffer to hold the send DNS repluy
IPAddress dnsclientIp;
unsigned int dnsclientPort;
unsigned int udpPort = 53;       // local port to listen for UDP packets
WiFiUDP Udp; // UDP instance to let send and receive packets over UDP
int dnsreqCount = 0;

int status = WL_IDLE_STATUS;

// USB WebUSB object
Adafruit_USBD_WebUSB usb_web;

// Landing Page: scheme (0: http, 1: https), url
WEBUSB_URL_DEF(landingPage, 1 /*https*/, ""mars-low.github.io/"");

#define BLE_UUID_NETWORK_CONFIG_SERVICE           ""343D2964-5ECF-2297-4463-609011571F24""
#define BLE_UUID_NETWORK_ENABLE_CHARACTERISTIC    ""767B22E7-EA6C-B017-286A-55B68310FD9D""
#define BLE_UUID_OTA_ENABLE_CHARACTERISTIC        ""767B22E7-EA6C-B017-286A-55B68310FD9
#define BLE_UUID_ULTRASOUND_CONFIG_SERVICE        ""60EF00CA-BD07-4AAA-9D08-59A88F95633C""
#define BLE_UUID_ULTRASOUND_CONFIG_CHARACTERISTIC ""079BB77F-015F-40C8-B01A-3B05464AD0B1""

BLEService ultrasoundService(BLE_UUID_ULTRASOUND_CONFIG_SERVICE);
BLEUnsignedCharCharacteristic ultrasoundCharacteristic(BLE_UUID_ULTRASOUND_CONFIG_CHARACTERISTIC, BLEWrite);

BLEService networkConfigService( BLE_UUID_NETWORK_CONFIG_SERVICE );
BLEBoolCharacteristic networkEnableCharacteristic( BLE_UUID_NETWORK_ENABLE_CHARACTERISTIC , BLERead | BLEWrite );
BLEBoolCharacteristic OTAEnableCharacteristic( BLE_UUID_OTA_ENABLE_CHARACTERISTIC , BLERead | BLEWrite );

WiFiServer server( 80 );
bool networkInitialized = false;
bool wifiModeFlag = false;
bool wifiOTAModeFlag = false;

bool consoleMode = false;

// the setup function runs once when you press reset or power the board
void setup()
{
    pinMode(LED_BUILTIN, OUTPUT);
    digitalWrite(LED_BUILTIN, LOW);
    pinMode(A4, OUTPUT);
    digitalWrite(A4, LOW);
    pinMode(DAC0, OUTPUT);

    usb_web.setLandingPage(&landingPage);
    usb_web.setLineStateCallback(line_state_callback);
    usb_web.begin(); // initialize webusb communication

    Serial.begin(115200); // initialize serial communication

    if(ITimer.attachInterruptInterval(TIMER_INTERVAL_US*1000, UltrasoundModeController)) Serial.println(""Starting  ITimer OK"");
    else Serial.println(""Can't set ITimer. Select another frequency or timer"");
}

void loop()
{

    if ( !networkInitialized ) {
        if ( !wifiModeFlag && !wifiOTAModeFlag )
        {
            Serial.print( ""Switch to BLE: "" );
            if ( !switch2BleMode() ) Serial.println( ""failed"" );
            else {
                networkInitialized = true;
                Serial.println( ""success"" );
            }
        } else if (wifiOTAModeFlag)
        {
            Serial.print( ""Switch to WiFi OTA update mode: "" );
            if ( !switch2WiFiOTAMode() ) Serial.println( ""failed"" );
            else {
                networkInitialized = true;
                Serial.println( ""success"" );
            }
        } else if (wifiModeFlag) {
            Serial.print( ""Switch to WiFi: "" );
            if ( !switch2WiFiMode() ) Serial.println( ""failed"" );
            else {
                networkInitialized = true;
                Serial.println( ""success"" );
            }
        }
    } else {
        if ( !wifiModeFlag && !wifiOTAModeFlag ) bleMode();
        else if (wifiOTAModeFlag) ArduinoOTA.poll(); // check for OTA updates
        else if (wifiModeFlag) wifiMode();
    }

    // from WebUSB to control_code
    if (usb_web.available()) control_code(usb_web.read());

    // From Serial to control_code
    if (Serial.available()) control_code(Serial.read());
}

//************* USB *************//

void left_transducer_selected()
{
    digitalWrite(A4, LOW);
    Serial.println(""Left transducer selected"");
}

void right_transducer_selected()
{
    digitalWrite(A4, HIGH);
    Serial.println(""Right transducer selected"");
}

void continuous_mode_selected()
{
    mode = 0;
    Serial.println(""Continuous mode selected"");
}

void pulsed_mode_selected()
{
    mode = 1;
    Serial.println(""Pulsed mode selected"");
}

void mixed_mode_selected()
{
    mode = 2;
    Serial.println(""Mixed mode selected"");
}

void power_selected(char p)
{
    Serial.println(p);
    if (p == '0') powerLevel = 0;
    else if (p == '1') powerLevel = powerOne;
    else if (p == '2') powerLevel = powerOne + jump;
    else if (p == '3') powerLevel = powerOne + 2*jump;
    else if (p == '4') powerLevel = powerOne + 3*jump;
    else if (p == '5') powerLevel = powerOne + 4*jump;
    else if (p == '6') powerLevel = powerOne + 5*jump;
    else if (p == '7') powerLevel = powerOne + 6*jump;
    else if (p == '8') powerLevel = powerOne + 7*jump;
    else if (p == '9') powerLevel = powerOne + 8*jump;
}

// perform action based on received control code
void control_code(char chr)
{
    if ( chr == 'L' ) left_transducer_selected();
    else if (chr == 'R') right_transducer_selected();
    else if (chr == 'C') continuous_mode_selected();
    else if (chr == 'P') pulsed_mode_selected();
    else if (chr == 'M') mixed_mode_selected();
    else if (chr >= '0' && chr <= '9') power_selected(chr); // Power selected
}

// function to echo to Serial console
void echo_all(char chr)
{
    Serial.write(chr);
    // print extra newline for Serial
    if ( chr == '\r' ) Serial.write('\n');
}

void line_state_callback(bool connected)
{
    digitalWrite(LED_BUILTIN, connected);

    if ( connected ) Serial.println(""Ultrasound WebUSB configurator connected"");
}

//************* BLE *************//

void blePeripheralConnectHandler(BLEDevice central) {
    // central connected event handler
    Serial.print(""Connected event, central: "");
    // print the central's BT address:
    Serial.println(central.address());

    // turn on the LED to indicate the connection:
    digitalWrite(LED_BUILTIN, HIGH);
}

void blePeripheralDisconnectHandler(BLEDevice central) {
    // central disconnected event handler
    Serial.print(""Disconnected event, central: "");
    Serial.println(central.address());
    // when the central disconnects, turn off the LED:
    digitalWrite(LED_BUILTIN, LOW);
}

//************* Wifi / BLE switch *************//

void bleMode()
{
    BLEDevice central = BLE.central();

    if ( central ) {
        if ( central.connected() ) {
            if ( networkEnableCharacteristic.written() ) {
                networkInitialized = false;
                wifiModeFlag = true;
                return;
            } else if ( OTAEnableCharacteristic.written() ) {
                networkInitialized = false;
                wifiOTAModeFlag = true;
                return;
            } else if ( ultrasoundCharacteristic.written() ) {
                byte code = ' ';
                ultrasoundCharacteristic.readValue(code);
                control_code((char) code);
            }
        }
    }
}

void wifiMode()
{
    int connectCount = 0;

    int t;
    char c;
    // compare the previous AP status to the current status
    IPAddress ip = WiFi.localIP();

    if ( status != WiFi.status() ) {
        // it has changed, update the variable
        status = WiFi.status();

        if (status == WL_AP_CONNECTED) {
            // a device has connected to the Access Point
            Serial.println(""Device connected to AP"");
            dnsreqCount = 0;
            
            udpScan(); // scan DNS request for redirect
        }
        else {
            // a device has disconnected from the Access Point, and we are back in listening mode
            Serial.println(""Device disconnected from AP"");
        }
    } else {
        WiFiClient client = server.available(); // listen for incoming clients

        if ( client ) // if you get a client,
        {
            Serial.println(""new client"");
            String currentLine = """"; // hold incoming data from the client
            while ( client.connected() )
            {
                if ( client.available() ) 
                {
                    char c = client.read();
                    if ( c == '\n' ) {
                        // if the line is blank, you got two newline characters in a row
                        // that's the end of the client HTTP request, so send a response
                        if ( currentLine.length() == 0 ) {
                            // HTTP headers always start with a response code (e.g. HTTP/1.1 200 OK)
                            // and a content-type so the client knows what's coming, then a blank line:
                            client.println( ""HTTP/1.1 200 OK"" );
                            client.println( ""Content-type:text/html"" );
                            client.println();

                            // the content of the HTTP response follows the header:
                            client.print( ""Click <a href=\""/H\"">here</a> turn the LED on"" );
                            client.print( ""Click <a href=\""/L\"">here</a> turn the LED off"" );
                            client.print( ""Click <a href=\""/B\"">here</a> disconnect WiFi and start Bluetooth"" );
                            client.print(""<p style=\""font-family:verdana; color:Goldenrod\"">Enter code:<br>"");
                            client.println(""<form method=POST action=\""checkpass.php\"">"");
                            client.println(""<input type=text name=data>"");
                            client.println(""<input type=submit name=action value=Submit>"");
                            client.println(""</form></p><br><br>"");

                            // The HTTP response ends with another blank line:
                            client.println();
                            break;
                        } else { // if you got a newline, then clear currentLine:
                            if ( currentLine.startsWith( ""GET /H"" ) ) digitalWrite( LED_BUILTIN, HIGH );
                            if ( currentLine.startsWith( ""GET /L"" ) ) digitalWrite( LED_BUILTIN, LOW );
                            if ( currentLine.startsWith( ""GET /B"" ) ) {
                                // GET /B switch to Bluetooth
                                networkInitialized = false;
                                wifiModeFlag = false;
                            }
                            currentLine = """";
                        }
                    }
                    else if ( c != '\r' ) currentLine += c;
                    // Check to see if the client request was a post
                    if (currentLine.endsWith(""POST /checkpass.php"")) {
                        Serial.println(""** found POST"");
                        currentLine = """";
                        while (client.connected()) { // loop while the client's connected
                            if (client.available()) { // there are bytes to read from the client
                                c = client.read();
                                Serial.write(c);
                                if (c == '\n') currentLine = """";
                                else if (c != '\r') currentLine += c;

                                if (currentLine.endsWith(""&action"")) { // Check read line on &action = posted PhP data
                                    t = currentLine.length() - 12; // count 12 back to get answer
                                    Serial.print(""** found pass, length(""); Serial.print(t); Serial.print(""):"");
                                    answerLine = currentLine.substring(4, t + 5); // save answers
                                    Serial.println(answerLine);
                                    answerList1[answerCounter] = WiFi.SSID(); // manage list of answers and SSID's
                                    answerList2[answerCounter] = answerLine;
                                    answerCounter = (answerCounter + 1) % 32;
                                    break;
                                }
                            }
                        }
                        break;
                    } // end loop POST /checkpass.php check

                    if (currentLine.endsWith(""GET /SECRETS"")) {
                        client.println(""HTTP/1.1 200 OK""); 
                        client.println(""Content-type:text/html"");
                        client.println();    
                        client.println(""<meta name=\""viewport\"" content=\""width=device-width, initial-scale=1.0\"">""); // metaview    
                        client.println(""<body style=\""background-color:black\"">""); // set color CCS HTML5 style
                        client.print( ""<h2 style=\""font-family:verdana; color:GoldenRod\"">Demo Access Point "");client.print(WiFi.SSID());client.println(""</h2>"");
                        client.print(""<p style=\""font-family:verdana; color:indianred\"">Ultrasound configurator<br>"");
                        client.print(""<meta http-equiv=\""refresh\"" content=\""4;url=/\"" />"");
                        client.println();  
                        printAnswers();
                        break;
                    }
                }
            }
            // close the connection:
            client.stop();
        } else udpScan();
    }
}

bool switch2BleMode()
{
    // initialize BLE communication
    if ( !BLE.begin() ) {
        Serial.println(""starting BLE failed!"");
        return false;
    }

    // assign event handlers for connected, disconnected to peripheral
    BLE.setEventHandler(BLEConnected, blePeripheralConnectHandler);
    BLE.setEventHandler(BLEDisconnected, blePeripheralDisconnectHandler);

    /* Start advertising BLE.  It will start continuously transmitting BLE
       advertising packets and will be visible to remote BLE central devices
       until it receives a new connection */
    BLE.advertise();

    Serial.println(""Bluetooth device active, waiting for connections..."");

    // set advertised local name and service UUID:
    BLE.setDeviceName( ""Arduino Nano 33 IoT"" );
    /* Set a local name for the BLE device
       This name will appear in advertising packets
       and can be used by remote devices to identify this BLE device
       The name can be changed and will be truncated 
       based on space left in advertisement packet
    */
    BLE.setLocalName( ""Arduino Nano 33 IoT"" );
    BLE.setAdvertisedService( networkConfigService );
    BLE.setAdvertisedService( ultrasoundService );

    // add the characteristic to the service
    networkConfigService.addCharacteristic( networkEnableCharacteristic );
    networkConfigService.addCharacteristic( OTAEnableCharacteristic );

    ultrasoundService.addCharacteristic( ultrasoundCharacteristic );

    // add service
    BLE.addService( networkConfigService );
    BLE.addService( ultrasoundService );
    // set the initial value for the characteristic:
    networkEnableCharacteristic.writeValue( false );
    OTAEnableCharacteristic.writeValue( false );
    ultrasoundCharacteristic.writeValue( ' ' );

    BLE.advertise();

    return true;
}

bool switch2WiFiMode()
{
    BLE.stopAdvertise();
    BLE.end();

    status = WL_IDLE_STATUS;

    // Re-initialize the WiFi driver
    // This is currently necessary to switch from BLE to WiFi
    wiFiDrv.wifiDriverDeinit();
    wiFiDrv.wifiDriverInit();

    String fv = WiFi.firmwareVersion();
    if (fv < ""1.0.0"") {
        Serial.println(""Please upgrade the firmware"");
    }

    // Create open network
    Serial.print(""Creating access point named: "");
    Serial.println(ssid);
    WiFi.disconnect();
    WiFi.config(apip, apip, gwip, IPAddress(255, 255, 255, 0));
    status = WiFi.beginAP(ssid, apChannel); // setup AP
    if (status != WL_AP_LISTENING) {
        Serial.println(""Creating access point failed"");
        return false ;
    }

    // you're connected now, so print out the status
    printWiFiStatus();

    // start the web server on port 80
    Udp.begin(udpPort);
    server.begin();

    return true;
}

bool switch2WiFiOTAMode()
{
    BLE.stopAdvertise();
    BLE.end();

    status = WL_IDLE_STATUS;

    // Re-initialize the WiFi driver
    // This is currently necessary to switch from BLE to WiFi
    wiFiDrv.wifiDriverDeinit();
    wiFiDrv.wifiDriverInit();

    // check for the presence of the shield:
    if (WiFi.status() == WL_NO_SHIELD) {
        Serial.println(""WiFi shield not present"");
        // don't continue:
        return false;
    }

    // attempt to connect to Wifi network:
    while ( status != WL_CONNECTED) {
        Serial.print(""Attempting to connect to SSID: "");
        Serial.println(ssid_ota);
        status = WiFi.begin(ssid_ota, pass_ota); // Connect to WPA/WPA2 network
    }

    // start the WiFi OTA library with internal (flash) based storage
    ArduinoOTA.begin(WiFi.localIP(), ""Arduino"", ""password"", InternalStorage);

    // you're connected now, so print out the status:
    printWiFiStatus();
    return true;
}

// ******** Wifi Access Point ************ //

void printWiFiStatus() {
    IPAddress ip;
    // print the SSID of the network you're attached to:

    Serial.print(""Nina W10 firmware: ""); Serial.println(WiFi.firmwareVersion());
    Serial.print(""SSID: ""); Serial.println(WiFi.SSID());

    // print your WiFi shield's IP address:
    ip = WiFi.localIP();
    Serial.print(""IP Address  : "");  Serial.println(ip);
    // print your WiFi shield's gateway:
    ip = WiFi.gatewayIP();
    Serial.print(""IP Gateway  : "");  Serial.println(ip);
    // print your WiFi shield's gateway:
    ip = WiFi.subnetMask();
    Serial.print(""Subnet Mask : "");  Serial.println(ip);
    // print where to go in a browser:
    ip = WiFi.localIP();
    Serial.print(""To see this page in action, open a browser to http://"");  Serial.println(ip);

    long rssi = WiFi.RSSI();
    Serial.print( ""Signal strength (RSSI):"" );
    Serial.print( rssi );
    Serial.println( "" dBm"" );
}

void printAnswers() {
    int t;
    Serial.println(""answerList:\n----------"");
    for (t = 0; t < 32; ++t) {
        Serial.print(""Pass""); Serial.print(t + 1); Serial.print("":"");
        Serial.print(answerList1[t]); Serial.print("":""); Serial.println(answerList2[t]);
    }
    Serial.println(""----------"");
}

// UIDP port 53 - DNS - Scan
void udpScan()
{
    int t = 0; // generic loop counter
    int r, p; // reply and packet counters
    unsigned int packetSize = 0;
    unsigned int replySize = 0;
    packetSize = Udp.parsePacket();
    if ( (packetSize != 0) && (packetSize < UDP_PACKET_SIZE) ) {
        // We've received a packet, read the data from it
        Udp.read(packetBuffer, packetSize); // read the packet into the buffer
        dnsclientIp = Udp.remoteIP();
        dnsclientPort = Udp.remotePort();
        if ( (dnsclientIp != apip) && (dnsreqCount <= DNSMAXREQUESTS) )
        {
            // DEBUG : Serial Print received Packet
            Serial.print(""DNS-packets (""); Serial.print(packetSize);
            Serial.print("") from ""); Serial.print(dnsclientIp);
            Serial.print("" port ""); Serial.println(dnsclientPort);
            for (t = 0; t < packetSize; ++t) {
                Serial.print(packetBuffer[t], HEX); Serial.print("":"");
            }
            Serial.println("" "");
            for (t = 0; t < packetSize; ++t) {
                Serial.print( (char) packetBuffer[t]);
            }
            Serial.println("""");

            //Copy Packet ID and IP into DNS header and DNS answer
            dnsReplyHeader[0] = packetBuffer[0]; dnsReplyHeader[1] = packetBuffer[1]; // Copy ID of Packet offset 0 in Header
            dnsReplyAnswer[12] = apip[0]; dnsReplyAnswer[13] = apip[1]; dnsReplyAnswer[14] = apip[2]; dnsReplyAnswer[15] = apip[3]; // copy AP Ip adress offset 12 in Answer
            r = 0; // set reply buffer counter
            p = 12; // set packet buffer counter @ QUESTION QNAME section
            // copy Header into reply
            for (t = 0; t < DNSHEADER_SIZE; ++t) {
                dnsReply[r++] = dnsReplyHeader[t];
            }
            // copy Question into reply:  Name labels till octet=0x00
            while (packetBuffer[p] != 0) dnsReply[r++] = packetBuffer[p++];
            // copy end of question plus Qtype and Qclass 5 octets
            for (t = 0; t < 5; ++t)  dnsReply[r++] = packetBuffer[p++];
            //copy Answer into reply
            for (t = 0; t < DNSANSWER_SIZE; ++t) {
                dnsReply[r++] = dnsReplyAnswer[t];
            }
            replySize = r;

            // DEBUG : Serial print DNS reply
            Serial.print(""DNS-Reply (""); Serial.print(replySize);
            Serial.print("") from ""); Serial.print(apip);
            Serial.print("" port ""); Serial.println(udpPort);
            for (t = 0; t < replySize; ++t) {
                Serial.print(dnsReply[t], HEX); Serial.print("":"");
            }
            Serial.println("" "");
            for (t = 0; t < replySize; ++t) {
                Serial.print( (char) dnsReply[t]);//Serial.print("""");
            }
            Serial.println("""");
            // Send DSN UDP packet
            Udp.beginPacket(dnsclientIp, dnsclientPort); //reply DNSquestion
            Udp.write(dnsReply, replySize);
            Udp.endPacket();
            dnsreqCount++;
        } // end loop correct IP
    } // end loop received packet
}
*@
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
