using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace ExtEnvAnalysis.Models
{
    public sealed class CompanyPoint
    {
        public string Label { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double Market { get; set; }
        public Brush Brush { get; set; } = Brushes.Gray;
    }

    public sealed class MapModel : INotifyPropertyChanged
    {
        public string[] Names { get; set; } = new[] { "Мы", "A", "B", "C" };
        public int Index { get; set; }
        public int Count { get; set; }
        public string TitleX { get; set; } = "";
        public string TitleY { get; set; } = "";

        public CompanyPoint Me { get; set; } = new();
        public CompanyPoint A { get; set; } = new();
        public CompanyPoint B { get; set; } = new();
        public CompanyPoint C { get; set; } = new();

        private string _direction = "";
        public string Direction { get => _direction; set { _direction = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? p = null)
           => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
