using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoltenMercury.DataModel
{
    public class ColorPreset
    {
        public String Name
        { get; set; }

        public String Preset
        { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
