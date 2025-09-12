using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;

namespace ExtEnvAnalysis.Core
{
    public partial class PestelCategory : ObservableObject
    {
        public PestelType Type { get; }
        public string Title => Type.Display();
        public string[] Hints { get; }

        [ObservableProperty] private string? field1;
        [ObservableProperty] private string? field2;
        [ObservableProperty] private string? field3;
        [ObservableProperty] private string? field4;
        [ObservableProperty] private string? field5;

        public PestelCategory(PestelType type, string[] hints)
        {
            Type = type;
            Hints = hints;
        }

        public int CompletedCount =>
            new[] { Field1, Field2, Field3, Field4, Field5 }.Count(s => !string.IsNullOrWhiteSpace(s));

        public void Clear()
        {
            Field1 = Field2 = Field3 = Field4 = Field5 = string.Empty;
            OnPropertyChanged(nameof(CompletedCount));
        }

        // чтобы обновлялся CompletedCount при любом изменении поля
        partial void OnField1Changed(string? value) => OnPropertyChanged(nameof(CompletedCount));
        partial void OnField2Changed(string? value) => OnPropertyChanged(nameof(CompletedCount));
        partial void OnField3Changed(string? value) => OnPropertyChanged(nameof(CompletedCount));
        partial void OnField4Changed(string? value) => OnPropertyChanged(nameof(CompletedCount));
        partial void OnField5Changed(string? value) => OnPropertyChanged(nameof(CompletedCount));
    }
}
