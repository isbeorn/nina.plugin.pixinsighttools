using PixInsightTools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PixInsightTools.Dockables {
    public class TabTemplateSelector : DataTemplateSelector {
        public DataTemplate ColorTab { get; set; }
        public DataTemplate FilterTab { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            if (item is ColorTab) {
                return ColorTab;
            } else if (item is FilterTab) {
                return FilterTab;
            } 
            
            return FilterTab;
        }
    }
}
