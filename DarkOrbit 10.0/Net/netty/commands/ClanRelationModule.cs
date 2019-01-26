﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ow.Utils;

namespace Ow.Net.netty.commands
{
    class ClanRelationModule
    {
        public const short ID = 27378;

        public static short NONE = 0;
        public static short ALLIED = 1;
        public static short NON_AGGRESSION_PACT = 2;
        public static short AT_WAR = 3;

        public short type = 0;
        public ClanRelationModule(short type)
        {
            this.type = type;
        }

        public byte[] write()
        {
            var param1 = new ByteArray(ID);
            param1.writeShort(type);
            return param1.Message.ToArray();
        }
    }
}
