using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ExtEnvAnalysis.Core
{
    public partial class PestelState : ObservableObject, ISection
    {
        public ObservableCollection<PestelCategory> Categories { get; } = new();

        [ObservableProperty] private bool isValid;

        public PestelState()
        {
            InitializeFromSeeds();
        }

        public void InitializeFromSeeds()
        {
            Categories.Clear();
            foreach (var t in Enum.GetValues<PestelType>())
                Categories.Add(new PestelCategory(t, PestelSeeds.For(t)));
            Recalculate();
        }

        public void Recalculate()
        {
            IsValid = Categories.Count == 6 && Categories.All(c => c.CompletedCount >= 3);
        }

        public void Reset()
        {
            foreach (var c in Categories) c.Clear();
            IsValid = false;
        }
    }
}
