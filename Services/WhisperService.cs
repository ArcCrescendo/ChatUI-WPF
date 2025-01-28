using NAudio.Wave;
using System.Collections.Generic;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;

namespace WpfApp1.Services
{
    public class WhisperService
    {
        private readonly string _modelPath;
        private readonly int? _deviceIndex;
        private WhisperFactory _whisperFactory;
        private WhisperProcessor _processor;
        private WaveInEvent _waveIn;
        private List<byte> _audioBuffer;
        private bool _isRecording;

        // 用于保存临时音频文件
        private string _tempWavFile;

        public event EventHandler<string> TranscriptionReceived;
        public event EventHandler<byte[]> AudioDataAvailable;

        public WhisperService(string modelPath = "ggml-base-q8_0.bin", int? deviceIndex = null)
        {
            _modelPath = modelPath;
            _deviceIndex = deviceIndex;
            _audioBuffer = new List<byte>();
            _tempWavFile = Path.Combine(Path.GetTempPath(), "whisper_temp.wav");
        }

        public static List<string> GetAudioDevices()
        {
            var devices = new List<string>();
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var capabilities = WaveIn.GetCapabilities(i);
                devices.Add(capabilities.ProductName);
            }
            return devices;
        }

        public async Task Initialize()
        {
            if (!File.Exists(_modelPath))
            {
                // 下载模型文件
                await DownloadModel();
            }

            _whisperFactory = WhisperFactory.FromPath(_modelPath);
            _processor = _whisperFactory.CreateBuilder()
                .WithLanguage("zh")
                .Build();

            InitializeAudioCapture();
        }

        private async Task DownloadModel()
        {
            using var client = new HttpClient();
            using var modelStream = await client.GetStreamAsync(
                "https://huggingface.co/datasets/ggerganov/whisper.cpp/resolve/main/ggml-base.bin");
            using var fileStream = File.Create(_modelPath);
            await modelStream.CopyToAsync(fileStream);
        }

        private void InitializeAudioCapture()
        {
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 1),
                DeviceNumber = _deviceIndex ?? 0
            };

            _waveIn.DataAvailable += (s, e) =>
            {
                if (_isRecording)
                {
                    _audioBuffer.AddRange(e.Buffer);
                    AudioDataAvailable?.Invoke(this, e.Buffer);
                }
            };
        }

        public void StartRecording()
        {
            _audioBuffer.Clear();
            _isRecording = true;
            _waveIn.StartRecording();
        }

        public async Task StopRecording()
        {
            _isRecording = false;
            _waveIn.StopRecording();

            if (_audioBuffer.Count > 0)
            {
                try
                {
                    // 将录制的音频保存为WAV文件
                    using (var writer = new WaveFileWriter(_tempWavFile, _waveIn.WaveFormat))
                    {
                        writer.Write(_audioBuffer.ToArray(), 0, _audioBuffer.Count);
                    }

                    // 读取WAV文件并进行识别
                    using (var fileStream = File.OpenRead(_tempWavFile))
                    {
                        StringBuilder textBuilder = new StringBuilder();
                        await foreach (var segment in _processor.ProcessAsync(fileStream))
                        {
                            textBuilder.Append(segment.Text);
                        }
                        TranscriptionReceived?.Invoke(this, textBuilder.ToString());
                    }
                }
                finally
                {
                    // 清理临时文件
                    if (File.Exists(_tempWavFile))
                    {
                        try { File.Delete(_tempWavFile); } catch { }
                    }
                }
            }
        }

        public void Dispose()
        {
            _waveIn?.Dispose();
            _processor?.Dispose();
            _whisperFactory?.Dispose();

            // 清理临时文件
            if (File.Exists(_tempWavFile))
            {
                try { File.Delete(_tempWavFile); } catch { }
            }
        }
    }
} 