﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS
{
    public class Shift
    {
        public DateTime startTime { get; private set; }
        public DateTime endTime { get; private set; }

        public Shift(DateTime start, DateTime end)
        {
            startTime = start;
            endTime = end;
        }
    }
}
