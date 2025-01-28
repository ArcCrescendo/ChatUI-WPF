using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WpfApp1.Models;

namespace WpfApp1.Controls
{
    /// <summary>
    /// chatHistory.xaml 的交互逻辑
    /// </summary>
    public partial class ChatHistory : UserControl
    {
        private ObservableCollection<Models.ChatHistory> _histories;
        public event Action<Models.ChatHistory> OnChatSelected;
        public event Action<Models.ChatHistory> OnChatDeleted;
        public event Action OnBackButtonClicked;

        public ChatHistory()
        {
            InitializeComponent();
            _histories = new ObservableCollection<Models.ChatHistory>();
            HistoryList.ItemsSource = _histories;
        }

        public void UpdateHistories(List<Models.ChatHistory> histories)
        {
            _histories.Clear();
            foreach (var history in histories.OrderByDescending(h => h.Timestamp))
            {
                _histories.Add(history);
            }
        }

        private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HistoryList.SelectedItem is Models.ChatHistory selectedChat)
            {
                OnChatSelected?.Invoke(selectedChat);
            }
        }

        private async void DeleteHistory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && 
                button.DataContext is Models.ChatHistory chatHistory)
            {
                var result = MessageBox.Show(
                    "确定要删除这个对话吗？",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    OnChatDeleted?.Invoke(chatHistory);
                }
            }
        }

        private void SelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var history in _histories)
            {
                history.IsSelected = true;
            }
        }

        private void SelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var history in _histories)
            {
                history.IsSelected = false;
            }
        }

        private async void BatchDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedHistories = _histories.Where(h => h.IsSelected).ToList();
            if (selectedHistories.Count == 0)
            {
                MessageBox.Show("请先选择要删除的对话", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"确定要删除选中的 {selectedHistories.Count} 个对话吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                foreach (var history in selectedHistories)
                {
                    OnChatDeleted?.Invoke(history);
                }
            }
        }

        private void OpenHistory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Models.ChatHistory chatHistory)
            {
                OnChatSelected?.Invoke(chatHistory);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            OnBackButtonClicked?.Invoke();
        }
    }
}
