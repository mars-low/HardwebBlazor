using System.Text.Json.Serialization;

namespace WebUSB
{
    public class USBConfiguration
    {
        [JsonPropertyName("configurationValue")]
        public byte ConfigurationValue { get; set; }

        [JsonPropertyName("configurationName")]
        public string ConfigurationName { get; set; }

        [JsonPropertyName("interfaces")]
        public USBInterface[] Interfaces { get; set; }
    }
}