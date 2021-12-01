using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Sequencer.SequenceItem;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PixInsightTools.Instructions {
    [ExportMetadata("Name", "Start live stacking")]
    [ExportMetadata("Description", "This instruction will start the live stack")]
    [ExportMetadata("Icon", "")]
    [ExportMetadata("Category", "PixInsight Tools")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class StartLiveStacking : SequenceItem {
        [ImportingConstructor]
        public StartLiveStacking() {

        }

        private StartLiveStacking(StartLiveStacking copyMe) : this() {
            CopyMetaData(copyMe);
        }

        public override object Clone() {
            var clone = new StartLiveStacking(this) {
            };

            return clone;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Info("Starting up live stack");
            _ = PixInsightToolsMediator.Instance.StartLiveStack();
        }
    }
}
