﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace foxGopherClient
{
    class FoxSettings
    {

        public string textFileViewer { get; set; }
        public bool ChangeLineEncodings { get; set; }
        public bool useAltDowloadForTextFiles { get; set; }
        public List<String> Bookmarks { get; set; }
    }
}
