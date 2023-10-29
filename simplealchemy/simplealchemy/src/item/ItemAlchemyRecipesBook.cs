using simplealchemy.src.gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace simplealchemy.src.item
{
    public class ItemAlchemyRecipesBook: Item
    {

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity.Controls.Sneak)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }
            
            IPlayer player = (byEntity as EntityPlayer).Player;

            if (this.api.Side == EnumAppSide.Client)
            {
                GuiDialogAlchemyRecipesBook dlg = new GuiDialogAlchemyRecipesBook(slot.Itemstack, this.api as ICoreClientAPI);
                /*dlg.OnClosed += delegate ()
                {
                    if (dlg.DidSave)
                    {
                        this.bookModSys.EndEdit(player, dlg.AllPagesText, dlg.Title, dlg.DidSign);
                        return;
                    }
                    this.bookModSys.CancelEdit(player);
                };*/
                dlg.TryOpen();
            }
            handling = EnumHandHandling.PreventDefault;
            return;
        }



    }
}
