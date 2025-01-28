namespace WpfApp1.Models
{
    public class AppSettings
    {
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; }
        public string Model { get; set; }
        public bool UseCustomModel { get; set; }
        public string CustomModel { get; set; }
        public int? SelectedAudioDeviceIndex { get; set; }
        public string WhisperModel { get; set; }

        public AppSettings()
        {
            BaseUrl = "https://api.openai.com/v1";
            Model = "gpt-3.5-turbo";
            WhisperModel = "ggml-base.bin";
            SelectedAudioDeviceIndex = null;
        }
    }
} 