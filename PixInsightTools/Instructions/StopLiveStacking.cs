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

namespace NINA.Plugins.PixInsightTools.Instructions {
    [ExportMetadata("Name", "Stop live stacking")]
    [ExportMetadata("Description", "This instruction will stop the live stack")]
    [ExportMetadata("Icon", "")]
    [ExportMetadata("Category", "PixInsight Tools")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class StopLiveStacking : SequenceItem {
        [ImportingConstructor]
        public StopLiveStacking() {

        }

        private StopLiveStacking(StopLiveStacking copyMe) : this() {
            CopyMetaData(copyMe);
        }

        public override object Clone() {
            var clone = new StopLiveStacking(this) {
            };

            return clone;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Info("Stopping up live stack");
            PixInsightToolsMediator.Instance.StopLiveStack();
        }
    }
}
