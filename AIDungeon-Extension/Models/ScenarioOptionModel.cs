using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDungeon_Extension
{
    class ScenarioOptionModel
    {
        public ObservableCollection<Option> Options;

        public ScenarioOptionModel()
        {
            Options = new ObservableCollection<Option>();
        }

        public class Option
        {
            public Option() { }

            public string OrderText { get; set; }
            public string Text { get; set; }
        }
    }
}
