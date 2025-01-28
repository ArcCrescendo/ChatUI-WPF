using System.ComponentModel;

namespace WpfApp1.Models
{
    public class ChatMessage : INotifyPropertyChanged
    {
        private string _content;
        public string Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Content)));
                }
            }
        }
        public bool IsUser { get; set; }
        public DateTime Timestamp { get; set; }

        public ChatMessage(string content, bool isUser)
        {
            _content = content;
            IsUser = isUser;
            Timestamp = DateTime.Now;
        }

        public ChatMessage()
        {
            Content = string.Empty;
            IsUser = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
} 