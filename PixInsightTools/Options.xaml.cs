﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PixInsightTools {
    [Export(typeof(ResourceDictionary))]
    partial class Options : ResourceDictionary {
        public Options() {
            InitializeComponent();
        }
    }
}
