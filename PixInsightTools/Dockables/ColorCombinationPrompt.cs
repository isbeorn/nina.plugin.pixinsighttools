using NINA.Core.Utility;
using PixInsightTools.Model;
using System;
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
            this.ContinueCommand = new GalaSoft.MvvmLight.Command.RelayCommand(() => { Continue = true; });

            Targets = allTabs.GroupBy(x => x.Target).Where(g => g.All(x => x.Filter != ColorTab.RGB)).Select(x => x.First()).Select(x => x.Target).ToList();

            Target = Targets.FirstOrDefault();
        }

        private void FilterByTarget(string target) {
            if(string.IsNullOrEmpty(target)) { return; }

            FilterTabs = allTabs.Where(x => x.Target == target).ToList();

            var distanceRed = int.MaxValue;
            var distanceHa = int.MaxValue;            
            for(int i = 0; i < FilterTabs.Count; i++) {
                distanceRed = Math.Min(distanceRed, Fastenshtein.Levenshtein.Distance("Red", FilterTabs[i].Filter));
                distanceHa = Math.Min(distanceRed, Fastenshtein.Levenshtein.Distance("HA", FilterTabs[i].Filter));
            }

            if(distanceRed <= distanceHa) { 
                RedChannel = GetFilterForChannel("Red");
                GreenChannel = GetFilterForChannel("Green");
                BlueChannel = GetFilterForChannel("Blue");
            } else {
                if(FilterTabs.Count < 3) {
                    RedChannel = GetFilterForChannel("HA");
                    GreenChannel = GetFilterForChannel("OIII");
                    BlueChannel = GetFilterForChannel("OIII");
                } else {
                    RedChannel = GetFilterForChannel("SII");
                    GreenChannel = GetFilterForChannel("HA");
                    BlueChannel = GetFilterForChannel("OIII");
                }                
            }
             
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