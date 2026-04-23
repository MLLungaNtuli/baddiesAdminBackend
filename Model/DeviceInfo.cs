public class DeviceInfo
{
    public string UserAgent { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string OS { get; set; } = string.Empty;
    public string Device { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string DeviceFingerprint { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
}