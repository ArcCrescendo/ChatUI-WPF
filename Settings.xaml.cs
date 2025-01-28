using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Text.Json;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1
{
    /// <summary>
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class Settings : Window
    {
        private AppSettings _currentSettings;
        public event Action<AppSettings> SettingsSaved;

        public Settings()
        {
            InitializeComponent();
            LoadCurrentSettings();
            LoadAudioDevices();
        }

        private void LoadCurrentSettings()
        {
            _currentSettings = LoadSettingsFromFile();
            
            // 填充已有设置
            ApiKeyInput.Text = _currentSettings.ApiKey;
            BaseUrlInput.Text = _currentSettings.BaseUrl;
            
            // 设置模型选择
            if (_currentSettings.UseCustomModel)
            {
                UseCustomModelCheckbox.IsChecked = true;
                CustomModelInput.Text = _currentSettings.CustomModel;
            }
            else
            {
                foreach (RadioButton rb in ModelOptions.Children.OfType<RadioButton>())
                {
                    if (rb.Content.ToString() == _currentSettings.Model)
                    {
                        rb.IsChecked = true;
                        break;
                    }
                }
            }
            
            // 设置音频设备
            if (_currentSettings.SelectedAudioDeviceIndex.HasValue)
            {
                AudioDeviceComboBox.SelectedIndex = _currentSettings.SelectedAudioDeviceIndex.Value;
            }

            // 设置语音模型选择
            if (_currentSettings.WhisperModel == "ggml-base.en.bin")
            {
                WhisperModelFull.IsChecked = true;
            }
            else
            {
                WhisperModelBase.IsChecked = true;
            }
        }

        private void ModelOption_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.IsChecked == true)
            {
                UseCustomModelCheckbox.IsChecked = false;
            }
        }

        private void UseCustomModel_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (UseCustomModelCheckbox.IsChecked == true)
            {
                foreach (RadioButton rb in ModelOptions.Children.OfType<RadioButton>())
                {
                    rb.IsChecked = false;
                }
            }
        }

        private void WhisperModel_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.IsChecked == true)
            {
                string newModel = rb == WhisperModelFull ? "ggml-base.en.bin" : "ggml-base.bin";
                
                if (_currentSettings.WhisperModel != newModel)
                {
                    _currentSettings.WhisperModel = newModel;
                    SaveSettingsToFile(_currentSettings);
                    SettingsSaved?.Invoke(_currentSettings);
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = new AppSettings
            {
                ApiKey = ApiKeyInput.Text,
                BaseUrl = BaseUrlInput.Text,
                UseCustomModel = UseCustomModelCheckbox.IsChecked ?? false,
                CustomModel = CustomModelInput.Text,
                SelectedAudioDeviceIndex = AudioDeviceComboBox.SelectedIndex >= 0 ? 
                    AudioDeviceComboBox.SelectedIndex : null,
                WhisperModel = WhisperModelFull.IsChecked == true ? 
                    "ggml-base.en.bin" : "ggml-base.bin"
            };

            if (settings.UseCustomModel)
            {
                settings.Model = settings.CustomModel;
            }
            else
            {
                var selectedModel = ModelOptions.Children.OfType<RadioButton>()
                    .FirstOrDefault(rb => rb.IsChecked == true);
                settings.Model = selectedModel?.Content.ToString() ?? "gpt-3.5-turbo";
            }

            SaveSettingsToFile(settings);
            SettingsSaved?.Invoke(settings);
            Close();
        }

        private void LoadAudioDevices()
        {
            var devices = WhisperService.GetAudioDevices();
            AudioDeviceComboBox.ItemsSource = devices;
        }

        private AppSettings LoadSettingsFromFile()
        {
            const string SETTINGS_FILE = "appsettings.json";
            if (File.Exists(SETTINGS_FILE))
            {
                string json = File.ReadAllText(SETTINGS_FILE);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            return new AppSettings();
        }

        private void SaveSettingsToFile(AppSettings settings)
        {
            const string SETTINGS_FILE = "appsettings.json";
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SETTINGS_FILE, json);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AudioDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 这里可以添加音频设备选择变更的处理逻辑
        }
    }
}
