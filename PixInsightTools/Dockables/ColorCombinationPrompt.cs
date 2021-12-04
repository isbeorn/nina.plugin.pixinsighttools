using NINA.Core.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace PixInsightTools.Dockables {
    internal class ColorCombinationPrompt : BaseINPC {
        public IList<FilterTab> FilterTabs { get; private set; }
        private IList<FilterTab> allTabs;
        public IList<string> Targets { get; private set; }

        public ColorCombinationPrompt(AsyncObservableCollection<FilterTab> filterTabs) {
            this.allTabs = filterTabs;
            this.ContinueCommand = new RelayCommand((object o) => { Continue = true; });

            Targets = allTabs.GroupBy(x => x.Target).Where(g => g.All(x => x.Filter != LiveStackVM.RGB)).Select(x => x.First()).Select(x => x.Target).ToList();

            Target = Targets.FirstOrDefault();
        }

        private void FilterByTarget(string target) {
            if(string.IsNullOrEmpty(target)) { return; }

            FilterTabs = allTabs.Where(x => x.Target == target).ToList();

            RedChannel = GetFilterForChannel("Red");
            BlueChannel = GetFilterForChannel("Blue");
            GreenChannel = GetFilterForChannel("Green");

            RaisePropertyChanged(nameof(FilterTabs));
            RaisePropertyChanged(nameof(RedChannel));
            RaisePropertyChanged(nameof(BlueChannel));
            RaisePropertyChanged(nameof(GreenChannel));
        }

        private FilterTab GetFilterForChannel(string target) {
            return FilterTabs.OrderBy(x => Fastenshtein.Levenshtein.Distance(target, x.Filter)).First();            
        }

        private string target;
        public string Target { 
            get => target;
            set {
                target = value;
                FilterByTarget(target);
                RaisePropertyChanged();
            }
        }
        public FilterTab RedChannel { get; set; }
        public FilterTab BlueChannel { get; set; }
        public FilterTab GreenChannel { get; set; }

        public bool Continue { get; set; } = false;

        public ICommand ContinueCommand { get; }
    }
}