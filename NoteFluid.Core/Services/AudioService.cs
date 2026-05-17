using NAudio.CoreAudioApi;
using NAudio.Wave;
using NoteFluid.Core.Models;
using System.Diagnostics;
using System.IO;

namespace NoteFluid.Core.Services
{
    public class AudioService : IDisposable
    {
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private WaveOutEvent? _outputDevice;
        private bool _isInitialized = false;

        public AudioService()
        {
            _deviceEnumerator = new MMDeviceEnumerator();
        }

        /// <summary>        
        /// 获取所有音频输出设备
        /// </summary>
        public List<AudioDeviceInfo> GetOutputDevices()
        {
            var devices = new List<AudioDeviceInfo>();

            try
            {
                // 方法1：使用NAudio获取设备
                for (int i = 0; i < WaveOut.DeviceCount; i++)
                {
                    var capabilities = WaveOut.GetCapabilities(i);
                    devices.Add(new AudioDeviceInfo
                    {
                        DeviceId = i.ToString(),
                        DeviceName = capabilities.ProductName,
                        Channels = capabilities.Channels,
                        IsDefault = i == 0 // WaveOut中设备0通常是默认设备
                    });
                }

                // 方法2：使用MMDeviceEnumerator获取更详细的设备信息
                var mmDevices = _deviceEnumerator.EnumerateAudioEndPoints(
                    DataFlow.Render, DeviceState.Active);

                // 如果WaveOut没有获取到设备，使用MMDeviceEnumerator
                if (devices.Count == 0)
                {
                    foreach (var device in mmDevices)
                    {
                        devices.Add(new AudioDeviceInfo
                        {
                            DeviceId = device.ID,
                            DeviceName = device.FriendlyName,
                            DeviceFriendlyName = device.DeviceFriendlyName,
                            IsDefault = device.ID == _deviceEnumerator.GetDefaultAudioEndpoint(
                                DataFlow.Render, Role.Multimedia).ID
                        });
                    }
                }
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取音频设备失败: {ex.Message}");
            }

            return devices;
        }

        /// <summary>
        /// 获取当前默认输出设备
        /// </summary>
        public AudioDeviceInfo GetDefaultDevice()
        {
            try
            {
                var defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(
                    DataFlow.Render, Role.Multimedia);

                return new AudioDeviceInfo
                {
                    DeviceId = defaultDevice.ID,
                    DeviceName = defaultDevice.FriendlyName,
                    DeviceFriendlyName = defaultDevice.DeviceFriendlyName,
                    IsDefault = true
                };
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// 设置输出设备（通过设备索引）
        /// </summary>
        public bool SetOutputDevice(int deviceNumber)
        {
            try
            {
                CleanupOutputDevice();

                if (deviceNumber >= 0 && deviceNumber < WaveOut.DeviceCount)
                {
                    _outputDevice = new WaveOutEvent
                    {
                        DeviceNumber = deviceNumber
                    };
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置输出设备失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试当前输出设备
        /// </summary>
        public void TestOutputDevice()
        {
            try
            {
                Task.Run(async () =>
                {
                    await PlayBeepAsync(440, 220);  // A4
                    Thread.Sleep(100);
                    await PlayBeepAsync(554, 200);  // C#5
                    Thread.Sleep(100);
                    await PlayBeepAsync(659, 200);  // E5
                    Thread.Sleep(100);
                    await PlayBeepAsync(880, 400);  // A5
                });
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"测试输出设备失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 在指定的输出设备上播放Beep音
        /// </summary>
        public void PlayBeep(int frequency = 800, int duration = 200, double amplitude = 0.5)
        {
            try
            {
                if (!_isInitialized || _outputDevice == null)
                {
                    Debug.WriteLine("输出设备未初始化");
                    return;
                }

                // 确保之前的播放已停止
                _outputDevice.Stop();

                // 生成正弦波音频数据
                var samples = GenerateSineWave(frequency, duration, amplitude);

                // 转换为字节数组
                var bytes = FloatToBytes(samples);

                // 创建音频提供者
                var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1); // 单声道
                var provider = new RawSourceWaveStream(
                    new MemoryStream(bytes), waveFormat);

                // 初始化并播放
                _outputDevice.Init(provider);
                _outputDevice.Play();

                // 等待播放完成
                Thread.Sleep(duration);
                _outputDevice.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"播放Beep音失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成正弦波音频数据
        /// </summary>
        private float[] GenerateSineWave(int frequency, int durationMs, double amplitude)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * durationMs / 1000.0);
            var wave = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                // 生成正弦波
                wave[i] = (float)(amplitude * Math.Sin(
                    2 * Math.PI * frequency * i / sampleRate));

                // 添加淡入淡出效果，避免咔嗒声
                int fadeSamples = (int)(sampleRate * 0.01); // 10ms的淡入淡出
                if (i < fadeSamples)
                {
                    // 淡入
                    wave[i] *= (float)i / fadeSamples;
                }
                else if (i > samples - fadeSamples)
                {
                    // 淡出
                    wave[i] *= (float)(samples - i) / fadeSamples;
                }
            }

            return wave;
        }

        /// <summary>
        /// 异步播放Beep音（不阻塞当前线程）
        /// </summary>
        public async Task PlayBeepAsync(int frequency = 800, int duration = 200, double amplitude = 0.5)
        {
            await Task.Run(() => PlayBeep(frequency, duration, amplitude));
        }

        /// <summary>
        /// 播放音乐音符
        /// </summary>
        public void PlayNote(double frequency, int durationMs, double amplitude = 0.5)
        {
            if (frequency > 0)
            {
                PlayBeep((int)frequency, durationMs, amplitude);
            }
        }

        /// <summary>
        /// 停止当前播放
        /// </summary>
        public void StopPlayback()
        {
            try
            {
                if (_outputDevice != null &&
                    _outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    _outputDevice.Stop();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"停止播放失败: {ex.Message}");
            }
        }

        private byte[] FloatToBytes(float[] samples)
        {
            var bytes = new byte[samples.Length * 4];
            Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private void CleanupOutputDevice()
        {
            if (_outputDevice != null)
            {
                _outputDevice.Stop();
                _outputDevice.Dispose();
                _outputDevice = null;
            }
        }

        public void Dispose()
        {
            
        }
    }
}
