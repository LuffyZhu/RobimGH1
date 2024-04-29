using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robim.Cloud
{
    public static class Settings_Global
    {
        public static bool DisplayDynamic { get; set; }

        public static bool DisplayPositions { get; set; }

        public static int DisplayRadius { get; set; }

        static Settings_Global()
        {
            Settings_Global.DisplayDynamic = false;
            Settings_Global.DisplayRadius = 2;
            Settings_Global.DisplayPositions = false;
        }
    }
}
