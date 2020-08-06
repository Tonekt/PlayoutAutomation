﻿using TAS.Database.Common;

namespace TAS.Server.Advantech.Model
{
    public class GpiPin
    {
        [Hibernate]
        public byte DeviceId { get; set; }
        [Hibernate]
        public int PortNumber { get; set; }
        [Hibernate]
        public byte PinNumber { get; set; }       
    }
}
