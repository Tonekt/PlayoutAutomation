﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface IXdcamMedia: IIngestMedia
    {
        int ClipNr { get; }
    }
}
