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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using WpfApp1.Models;
using System.Text.Json;
using System.IO;
using WpfApp1.Services;
using System.Windows.Media.Animation;
using System.Windows;  // Clipboard is part of System.Windows
using WpfApp1.Converters;

namespace WpfApp1
{
    /// <summary>
    /// chatUI.xaml 的交互逻辑
    /// </summary>
    public partial class ChatUI : UserControl
    {
        private ObservableCollection<ChatMessage> messages;
        private Settings settingsWindow;
        private OpenAIService _openAIService;
        private List<ChatHistory> _chatHistories;
        private ChatHistory _currentChat;
        private bool _isHistoryPanelOpen;
        private DatabaseService _databaseService;
        private WhisperService _whisperService;
        private bool _isRecording;

        public ChatUI()
        {
            InitializeComponent();
            messages = new ObservableCollection<ChatMessage>();
            ChatMessages.ItemsSource = messages;
            _chatHistories = new List<ChatHistory>();
            _databaseService = new DatabaseService();
            
            var settings = LoadCurrentSettings();
            _openAIService = new OpenAIService(settings);
            
            InitializeChat();
            
            // 绑定事件处理器
            btnSettings.Click += BtnSettings_Click;
            SendButton.Click += SendButton_Click;
            MessageInput.KeyDown += MessageInput_KeyDown;
            btnNewChat.Click += BtnNewChat_Click;
            btnClearContext.Click += BtnClearContext_Click;
            ChatHistoryControl.OnChatSelected += ChatHistoryControl_OnChatSelected;
            ChatHistoryControl.OnChatDeleted += ChatHistoryControl_OnChatDeleted;
        }

        private void InitializeChat()
        {
            try
            {
                var loadedHistories = _databaseService.LoadChatHistories();
                if (loadedHistories != null)
                {
                    _chatHistories.Clear();
                    _chatHistories.AddRange(loadedHistories);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化聊天历史时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            StartNewChat();
            
            // 添加返回按钮事件处理
            ChatHistoryControl.OnBackButtonClicked += () => ToggleHistoryPanel();
        }

        private async Task StartNewChat()
        {
            if (_chatHistories == null)
            {
                _chatHistories = new List<ChatHistory>();
            }
            
            _currentChat = new ChatHistory { Title = "新对话" };
            messages.Clear();
            UpdateHistoryList();
        }

        private async void BtnNewChat_Click(object sender, RoutedEventArgs e)
        {
            await StartNewChat();
        }

        private async void BtnClearContext_Click(object sender, RoutedEventArgs e)
        {
            messages.Clear();
            _currentChat.Messages.Clear();
            await _databaseService.SaveChatHistory(_currentChat);
        }

        private void BtnHistory_Click(object sender, RoutedEventArgs e)
        {
            ToggleHistoryPanel();
        }

        private void ToggleHistoryPanel()
        {
            if (_isHistoryPanelOpen)
            {
                var animation = new DoubleAnimation(300, 0, TimeSpan.FromSeconds(0.3));
                HistoryPanel.BeginAnimation(WidthProperty, animation);
            }
            else
            {
                var animation = new DoubleAnimation(0, 300, TimeSpan.FromSeconds(0.3));
                HistoryPanel.BeginAnimation(WidthProperty, animation);
            }
            _isHistoryPanelOpen = !_isHistoryPanelOpen;
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        private async void MessageInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && !e.KeyboardDevice.IsKeyDown(System.Windows.Input.Key.LeftShift))
            {
                e.Handled = true;
                await SendMessage();
            }
        }

        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(MessageInput.Text))
                return;

            // 检查API设置
            if (!CheckAPISettings())
            {
                MessageBox.Show("请先在设置中配置OpenAI API密钥", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                OpenSettings();
                return;
            }

            string userMessage = MessageInput.Text.Trim();
            await SendMessage(userMessage);
        }

        private async Task SendMessage(string userMessage)
        {
            try
            {
                MessageInput.IsEnabled = false;
                SendButton.IsEnabled = false;
                LoadingIndicator.Visibility = Visibility.Visible;

                // 添加用户消息
                var userChatMessage = new ChatMessage(userMessage, true);
                messages.Add(userChatMessage);
                _currentChat.Messages.Add(userChatMessage);

                // 添加AI消息
                var aiMessage = new ChatMessage("", false);
                messages.Add(aiMessage);
                _currentChat.Messages.Add(aiMessage);

                // 如果这是第一条消息，将对话添加到历史记录中并保存
                if (_currentChat.Messages.Count == 2 && !_chatHistories.Contains(_currentChat))
                {
                    _chatHistories.Add(_currentChat);
                    await _databaseService.SaveChatHistory(_currentChat);
                }

                // 更新对话标题
                if (_currentChat.Messages.Count == 2)
                {
                    _currentChat.Title = userMessage.Length > 20 
                        ? userMessage.Substring(0, 20) + "..." 
                        : userMessage;
                }

                StringBuilder responseBuilder = new StringBuilder();
                var typingDelay = new Random();
                
                var streamTask = _openAIService.GetCompletionStream(_currentChat.Messages);
                
                await foreach (var chunk in streamTask)
                {
                    responseBuilder.Append(chunk);
                    await Dispatcher.InvokeAsync(() =>
                    {
                        aiMessage.Content = responseBuilder.ToString();
                        var currentIndex = messages.IndexOf(aiMessage);
                        if (currentIndex != -1)
                        {
                            messages[currentIndex] = aiMessage;
                        }
                        ChatScrollViewer.ScrollToBottom();
                    });
                    await Task.Delay(typingDelay.Next(20, 50));
                }
                
                // 只在消息发送成功后保存对话历史
                await _databaseService.SaveChatHistory(_currentChat);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送消息时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                // 移除失败的消息
                messages.RemoveAt(messages.Count - 1);
                _currentChat.Messages.RemoveAt(_currentChat.Messages.Count - 1);
                if (_currentChat.Messages.Count == 0)
                {
                    _chatHistories.Remove(_currentChat);
                }
            }
            finally
            {
                MessageInput.IsEnabled = true;
                SendButton.IsEnabled = true;
                LoadingIndicator.Visibility = Visibility.Collapsed;
                MessageInput.Focus();
            }
        }

        private bool CheckAPISettings()
        {
            try
            {
                var settings = LoadCurrentSettings();
                return !string.IsNullOrWhiteSpace(settings.ApiKey);
            }
            catch
            {
                return false;
            }
        }

        private AppSettings LoadCurrentSettings()
        {
            const string SETTINGS_FILE = "appsettings.json";
            if (File.Exists(SETTINGS_FILE))
            {
                string json = File.ReadAllText(SETTINGS_FILE);
                return JsonSerializer.Deserialize<AppSettings>(json);
            }
            return new AppSettings();
        }

        private void ChatHistoryControl_OnChatSelected(ChatHistory selectedChat)
        {
            _currentChat = selectedChat;
            messages.Clear();
            foreach (var message in selectedChat.Messages)
            {
                messages.Add(message);
            }
            ToggleHistoryPanel();
        }

        private void UpdateHistoryList()
        {
            ChatHistoryControl.UpdateHistories(_chatHistories);
        }

        private void CopyMessage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ChatMessage message)
            {
                Clipboard.SetText(message.Content);
            }
        }

        private async void ChatHistoryControl_OnChatDeleted(ChatHistory chatHistory)
        {
            try
            {
                await _databaseService.DeleteChatHistory(chatHistory.Id);
                _chatHistories.Remove(chatHistory);
                
                if (_currentChat.Id == chatHistory.Id)
                {
                    await StartNewChat();
                }
                else
                {
                    UpdateHistoryList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除历史记录时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenSettings()
        {
            if (settingsWindow != null)
            {
                settingsWindow.Close();
            }
            settingsWindow = new Settings();
            settingsWindow.SettingsSaved += SettingsWindow_SettingsSaved;
            settingsWindow.ShowDialog();
        }

        private void SettingsWindow_SettingsSaved(AppSettings newSettings)
        {
            // 更新OpenAIService
            _openAIService = new OpenAIService(newSettings);
            
            // 如果语音模型发生变化，重新初始化WhisperService
            var currentSettings = LoadCurrentSettings();
            if (currentSettings.WhisperModel != newSettings.WhisperModel ||
                currentSettings.SelectedAudioDeviceIndex != newSettings.SelectedAudioDeviceIndex)
            {
                _whisperService?.Dispose();
                _whisperService = null;
            }
        }

        private async void RegenerateMessage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && 
                button.DataContext is ChatMessage message && 
                !message.IsUser)
            {
                int index = messages.IndexOf(message);
                if (index > 0)
                {
                    // 移除当前AI回复
                    messages.RemoveAt(index);
                    _currentChat.Messages.RemoveAt(_currentChat.Messages.Count - 1);
                    
                    MessageInput.IsEnabled = false;
                    SendButton.IsEnabled = false;
                    LoadingIndicator.Visibility = Visibility.Visible;

                    try
                    {
                        var aiMessage = new ChatMessage("", false);
                        messages.Add(aiMessage);
                        _currentChat.Messages.Add(aiMessage);

                        StringBuilder responseBuilder = new StringBuilder();
                        var typingDelay = new Random();
                        
                        // 传递完整的对话历史
                        var streamTask = _openAIService.GetCompletionStream(_currentChat.Messages);
                        
                        await foreach (var chunk in streamTask)
                        {
                            responseBuilder.Append(chunk);
                            await Dispatcher.InvokeAsync(() =>
                            {
                                aiMessage.Content = responseBuilder.ToString();
                                var currentIndex = messages.IndexOf(aiMessage);
                                if (currentIndex != -1)
                                {
                                    messages[currentIndex] = aiMessage;
                                }
                                ChatScrollViewer.ScrollToBottom();
                            });
                            await Task.Delay(typingDelay.Next(20, 50));
                        }
                        
                        await _databaseService.SaveChatHistory(_currentChat);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"重新生成回复时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        MessageInput.IsEnabled = true;
                        SendButton.IsEnabled = true;
                        LoadingIndicator.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private async void RetryMessage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && 
                button.DataContext is ChatMessage message && 
                message.IsUser)
            {
                // 移除当前消息之后的所有消息
                int index = messages.IndexOf(message);
                while (messages.Count > index + 1)
                {
                    messages.RemoveAt(messages.Count - 1);
                    _currentChat.Messages.RemoveAt(_currentChat.Messages.Count - 1);
                }
                
                // 重新发送消息
                MessageInput.Text = message.Content;
                await SendMessage();
            }
        }

        public async Task InitializeWhisperService()
        {
            var settings = LoadCurrentSettings();
            if (_whisperService != null)
            {
                _whisperService.Dispose();
                _whisperService = null;
            }

            _whisperService = new WhisperService(
                modelPath: settings.WhisperModel,
                deviceIndex: settings.SelectedAudioDeviceIndex
            );
            _whisperService.TranscriptionReceived += OnTranscriptionReceived;
            _whisperService.AudioDataAvailable += OnAudioDataAvailable;
            await _whisperService.Initialize();
        }

        private void OnTranscriptionReceived(object sender, string transcription)
        {
            Dispatcher.Invoke(() =>
            {
                MessageInput.Text += transcription;
            });
        }

        private void OnAudioDataAvailable(object sender, byte[] buffer)
        {
            AudioWaveform.UpdateWaveform(buffer);
        }

        private async void VoiceInputButton_Click(object sender, RoutedEventArgs e)
        {
            Storyboard processingAnimation = VoiceInputButton.Resources["ProcessingAnimation"] as Storyboard;

            // 检查是否已配置音频设备
            var settings = LoadCurrentSettings();
            if (!settings.SelectedAudioDeviceIndex.HasValue)
            {
                MessageBox.Show("请先在设置中选择音频输入设备", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                OpenSettings();
                return;
            }

            // 检查选择的设备是否仍然可用
            var availableDevices = WhisperService.GetAudioDevices();
            if (settings.SelectedAudioDeviceIndex.Value >= availableDevices.Count)
            {
                MessageBox.Show("已选择的音频设备不可用，请重新选择", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                OpenSettings();
                return;
            }

            if (_whisperService == null)
            {
                try
                {
                    VoiceInputButton.IsEnabled = false;
                    VoiceInputButton.ToolTip = "初始化中...";
                    await InitializeWhisperService();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"初始化语音服务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    _whisperService?.Dispose();
                    _whisperService = null;
                    return;
                }
                finally
                {
                    VoiceInputButton.IsEnabled = true;
                    VoiceInputButton.ToolTip = "语音输入";
                }
            }

            try
            {
                if (!_isRecording)
                {
                    // 开始录音
                    _whisperService.StartRecording();
                    _isRecording = true;
                    VoiceInputButton.ToolTip = "停止录音";
                    AudioWaveform.Visibility = Visibility.Visible;
                }
                else
                {
                    // 停止录音并识别
                    VoiceInputButton.IsEnabled = false;
                    VoiceInputButton.ToolTip = "转换中...";
                    processingAnimation?.Begin(VoiceInputButton);

                    await _whisperService.StopRecording();

                    processingAnimation?.Stop(VoiceInputButton);
                    VoiceInputButton.IsEnabled = true;
                    _isRecording = false;
                    VoiceInputButton.ToolTip = "语音输入";
                    AudioWaveform.Visibility = Visibility.Collapsed;
                    AudioWaveform.Reset();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"语音操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                _whisperService?.Dispose();
                _whisperService = null;
                _isRecording = false;
                processingAnimation?.Stop(VoiceInputButton);
                VoiceInputButton.IsEnabled = true;
                VoiceInputButton.ToolTip = "语音输入";
                AudioWaveform.Visibility = Visibility.Collapsed;
                AudioWaveform.Reset();
            }
        }
    }
}
