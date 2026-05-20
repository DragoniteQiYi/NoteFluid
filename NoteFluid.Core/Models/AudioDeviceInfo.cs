namespace NoteFluid.Core.Models
{
    /// <summary>
    /// 音频设备信息
    /// </summary>
    public class AudioDeviceInfo
    {
        public string? DeviceId { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceFriendlyName { get; set; }
        public int Channels { get; set; }
        public bool IsDefault { get; set; }
        public bool IsSelected { get; set; }

        public override string ToString()
        {
            return $"{DeviceName} {(IsDefault ? "(默认)" : "")}";
        }
    }
}
