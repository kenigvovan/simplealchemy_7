using ProtoBuf;
using simplealchemy.src.recipies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simplealchemy.src.gui
{
    [ProtoContract]
    public class SyncPotionRecipesPacket
    {
        [ProtoMember(1)]
        public string  potionList;
    }
}
