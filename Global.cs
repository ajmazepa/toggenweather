using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherGetter {
    internal class Global {
        private static int _napTime = 2000;

        public static int napTime
        {
            get { return _napTime; }
            set { _napTime = value; }
        }

        private static int _shortNap = 250;

        public static int shortNap
        {
            get { return _shortNap; }
            set { _shortNap = value; }
        }

        private static ChromeDriver? _driver = null;

        public static ChromeDriver? driver
        {
            get { return _driver; }
            set { _driver = value; }
        }

        private static FileSystemWatcher? _watcher = null;

        public static FileSystemWatcher? watcher
        {
            get { return _watcher; }
            set { _watcher = value; }
        }
    }
}
