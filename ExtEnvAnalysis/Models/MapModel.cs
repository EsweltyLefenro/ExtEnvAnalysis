using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace ExtEnvAnalysis.Models
{
    public sealed class CompanyPoint
    {
        public string Label { get; set; } = "";
        public double X { get; set; }      // 1..10
        public double Y { get; set; }      // 1..10
        public double Market { get; set; } // 0..100
        public Brush Brush { get; set; } = Brushes.Gray;
    }

    public sealed class MapModel : INotifyPropertyChanged
    {
        public int Index { get; set; }
        public int Count { get; set; }
        public string TitleX { get; set; } = "";
        public string TitleY { get; set; } = "";

        public CompanyPoint Me { get; set; } = new();
        public CompanyPoint A { get; set; } = new();
        public CompanyPoint B { get; set; } = new();
        public CompanyPoint C { get; set; } = new();

        private string _explanation = "";
        public string Explanation { get => _explanation; set { _explanation = value; OnPropertyChanged(); } }

        private string _direction = "";
        public string Direction { get => _direction; set { _direction = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? p = null)
           => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
