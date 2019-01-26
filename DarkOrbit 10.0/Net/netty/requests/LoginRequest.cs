﻿using Ow.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ow.Net.netty.requests
{
    class LoginRequest
    {
        public const short ID = 10996;

        public int instanceId = 0;      
        public int userID = 0;     
        public int factionID = 0;      
        public String sessionID = "";     
        public String version = "";

        public void readCommand(byte[] bytes)
        {
            var parser = new ByteParser(bytes);
            instanceId = parser.readInt();
            instanceId = instanceId << 8 | instanceId >> 24;
            userID = parser.readInt();
            userID = userID << 8 | userID >> 24;
            factionID = parser.readShort();
            sessionID = parser.readUTF();
            version = parser.readUTF();
        }
    }
}
