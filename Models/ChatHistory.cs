namespace WpfApp1.Models
{
    using System.ComponentModel;

    public class ChatHistory : INotifyPropertyChanged
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime Timestamp { get; set; }
        public List<ChatMessage> Messages { get; set; }

        public ChatHistory()
        {
            Id = Guid.NewGuid().ToString();
            Timestamp = DateTime.Now;
            Messages = new List<ChatMessage>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 