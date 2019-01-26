﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ow.Utils;

namespace Ow.Net.netty.commands
{
    class PetGearSelectCommand
    {
        public const short ID = 19459;

        public static byte[] write(PetGearTypeModule gearType, List<int> optionalParams)
        {
            var param1 = new ByteArray(ID);
            param1.write(gearType.write());
            param1.writeInt(optionalParams.Count);
            foreach(var i in optionalParams)
            {
                param1.writeInt(i);
            }
            return param1.ToByteArray();
        }
    }
}
