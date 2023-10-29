using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace simplealchemy.src.inventories
{
    public class AlchemyBookInventory: InventoryBase, ISlotProvider
    {
        private ItemSlot[] slots;
        public int[] stocks;
        public ItemSlot[] Slots => this.slots;
        public int slotsCount;
        public override int Count => slots.Length;

        public override ItemSlot this[int slotId]
        {
            get => slotId < 0 || slotId >= this.Count ? (ItemSlot)null : this.slots[slotId];
            set
            {
                if (slotId < 0 || slotId >= this.Count)
                    throw new ArgumentOutOfRangeException(nameof(slotId));
                this.slots[slotId] = value != null ? value : throw new ArgumentNullException(nameof(value));
            }
        }
        public override void FromTreeAttributes(ITreeAttribute tree) => this.slots = this.SlotsFromTreeAttributes(tree, this.slots);

        public override void ToTreeAttributes(ITreeAttribute tree) => this.SlotsToTreeAttributes(this.slots, tree);

        public AlchemyBookInventory(string inventoryID, ICoreAPI api, int slotsAmount = 6)
          : base(inventoryID, api)
        {
            this.slots = this.GenEmptySlotsInner(slotsAmount);
            stocks = new int[slotsAmount / 2];
        }

        public ItemSlot[] GenEmptySlotsInner(int quantity)
        {
            ItemSlot[] array = new ItemSlot[quantity];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = NewSlotInner(i);
            }

            return array;
        }

        protected ItemSlot NewSlotInner(int i)
        {
            return new AlchemyBookDummySlot(this);
        }
    }
}
