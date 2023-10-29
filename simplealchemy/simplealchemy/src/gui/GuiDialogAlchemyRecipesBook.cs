using HarmonyLib;
using simplealchemy.src.inventories;
using simplealchemy.src.recipies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using static simplealchemy.src.recipies.RecipeSystem;

namespace simplealchemy.src.gui
{
    public class GuiDialogAlchemyRecipesBook : GuiDialogGeneric
    {
        protected CairoFont font = CairoFont.TextInput().WithFontSize(18f);
        protected int maxWidth = 600;
        protected int maxLines = 20;
        public int selectedPage = -1;
        private InventoryGeneric inv;
        ElementBounds[] recipeIngredientsBounds;
        //private AlchemyBookInventory inventory;

        public GuiDialogAlchemyRecipesBook(ItemStack bookStack, ICoreClientAPI capi) : base("", capi)
        {
            this.inv = new InventoryGeneric(10, "GuiDialogAlchemyRecipesBook-1", capi, null);
            //capi.World.Player.InventoryManager.OpenInventory((IInventory)inventory);
            /* this.inventory = new AlchemyBookInventory((string)null, (ICoreAPI)null);
             this.inventory.LateInitialize("alchemybook-124232", capi);
             this.inventory.Pos = capi.World.Player.Entity.Pos.AsBlockPos;
             capi.World.Player.InventoryManager.OpenInventory((IInventory)inventory);*/
            this.Compose();
        }

        protected virtual void Compose()
        {
            double lineHeight = this.font.GetFontExtents().Height * this.font.LineHeightMultiplier / (double)RuntimeEnv.GUIScale;
            ElementBounds recipeNameBounds = ElementBounds.Fixed(0.0, 30.0, (double)this.maxWidth, 25);
            ElementBounds recipeTierBounds = ElementBounds.FixedSize(250, 30.0).FixedUnder(recipeNameBounds);
            ElementBounds recipeTempBounds = ElementBounds.FixedSize(250, 30.0).FixedUnder(recipeTierBounds);
            ElementBounds ingredientText = ElementBounds.FixedSize(250, 30.0).FixedUnder(recipeTempBounds, 8);
            ElementBounds bigrecipeIngredientsBoundsFirst = ElementBounds.FixedSize(400, 48).FixedRightOf(ingredientText, 35);
            ElementBounds recipeIngredientsBoundsFirst = ElementBounds.FixedSize(48, 48).FixedUnder(recipeTempBounds, 35);
            recipeIngredientsBounds =
                new ElementBounds[6];
            recipeIngredientsBounds[0] = recipeIngredientsBoundsFirst;
            for (int i = 1; i < 6; i++)
            {
                recipeIngredientsBounds[i] = ElementBounds.FixedSize(48, 48).FixedRightOf(recipeIngredientsBounds[i - 1], 8);
                recipeIngredientsBounds[i].fixedY = recipeIngredientsBoundsFirst.fixedY;
            }
            ElementBounds outputList = ElementBounds.FixedSize(250, 30.0).FixedUnder(recipeIngredientsBoundsFirst);
            ElementBounds recipeOutput= ElementBounds.FixedSize(48, 48).FixedUnder(outputList, 8);

            ElementBounds prevButtonBounds = ElementBounds.FixedSize(60.0, 30.0).FixedUnder(recipeOutput, 23.0).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0);
            ElementBounds pageLabelBounds = ElementBounds.FixedSize(80.0, 30.0).FixedUnder(recipeOutput, 33.0).WithAlignment(EnumDialogArea.CenterFixed).WithFixedPadding(10.0, 2.0);
            ElementBounds nextButtonBounds = ElementBounds.FixedSize(60.0, 30.0).FixedUnder(recipeOutput, 23.0).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0);
            ElementBounds closeButton = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(prevButtonBounds, 25.0).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0);
            ElementBounds saveButtonBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(nextButtonBounds, 25.0).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(new ElementBounds[]
            {
                closeButton
            });
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);

            base.SingleComposer = this.capi.Gui
                .CreateCompo("recipebook", dialogBounds)
                .AddShadedDialogBG(bgBounds, true, 5.0, 0.75f)
                .AddDialogTitleBar("Potion recipes book", delegate
                    {
                        this.TryClose();
                    }, null, null)
                .BeginChildElements(bgBounds);
            
            var alchemyRecipesList = simplealchemy.potionCauldronRecipes;
            if (alchemyRecipesList.Count < 1)
            {
                SingleComposer.Compose(true);
                return;
            }
            if(selectedPage == -1)
            selectedPage = 0;
            var selectedRecipe = alchemyRecipesList.ElementAt(selectedPage);
            SingleComposer.AddDynamicText(Lang.Get("simplealchemy:" + selectedRecipe.Code), font, recipeNameBounds, "recipeName");
            SingleComposer.AddDynamicText(Lang.Get("simplealchemy:cauldron_level_gui", selectedRecipe.MinCauldronTier.ToString()), font, recipeTierBounds, "recipeTier");
            SingleComposer.AddDynamicText(Lang.Get("simplealchemy:min_max_temp_recipe", selectedRecipe.MinTemperature.ToString(), selectedRecipe.MaxTemperature.ToString()), font, recipeTempBounds, "recipeTemp");

            SingleComposer.AddStaticText(Lang.Get("simplealchemy:ingredient_list"), font, ingredientText);
            for (int i = 0; i < 6; i++)
            {
                List<ItemStack> listItemStacks = new List<ItemStack>();
                if (i >= selectedRecipe.Ingredients.Count())
                {
                    break;
                }
                var ing = selectedRecipe.Ingredients[i];
                if (ing.AllowedVariants != null)
                {
                    foreach (var it in ing.AllowedVariants)
                    {
                        var indexStar = ing.Code.Path.IndexOf("*");
                        if (ing.Type == EnumItemClass.Item)
                        {
                            Item itemHere = capi.World.GetItem(new AssetLocation(ing.Code.Domain + ":" + ing.Code.Path.Substring(0, ing.Code.Path.Length - 1) + it));
                            if (itemHere != null) {
                                ItemStack ist = new ItemStack(itemHere, selectedRecipe.Ingredients[i].Quantity);
                                listItemStacks.Add(ist);
                            }
                        }
                    }

                    var sho = new SlideshowItemstackTextComponent(capi, listItemStacks.ToArray(), 48, EnumFloat.Inline);
                    sho.ShowStackSize = true;
                    if (listItemStacks.Count > 0)
                    {
                        SingleComposer.AddRichtext(new RichTextComponentBase[] { sho }, recipeIngredientsBounds[i], i.ToString());
                    }
                }
                else if(ing.Litres != -1)
                {
                    var bucket = new ItemStack(capi.World.GetBlock(new AssetLocation("game:woodbucket")), 1);
                    ITreeAttribute tree = new TreeAttribute();
                    ItemStack liqStack = selectedRecipe.Ingredients[i].ResolvedItemstack;
                    liqStack.StackSize = selectedRecipe.Ingredients[i].Quantity;
                    tree.SetItemstack("0", liqStack);
                    bucket.Attributes["contents"] = tree;
                    var sho = new SlideshowItemstackTextComponent(capi, new ItemStack[] { bucket }, 48, EnumFloat.Inline);
                    sho.ShowStackSize = true;
                    if (bucket != null)
                    {
                        SingleComposer.AddRichtext(new RichTextComponentBase[] { sho }, recipeIngredientsBounds[i], "ing" + i.ToString());
                    }
                }

                else
                {
                    ItemStack itemStackToShow = null;

                    if ((selectedRecipe.Ingredients[i].Type == EnumItemClass.Item))
                    {
                        itemStackToShow = new ItemStack(capi.World.GetItem(selectedRecipe.Ingredients[i].Code), selectedRecipe.Ingredients[i].Quantity);
                    }
                    else
                    {
                        itemStackToShow = new ItemStack(capi.World.GetBlock(selectedRecipe.Ingredients[i].Code), selectedRecipe.Ingredients[i].Quantity);
                    }
                    var slideShow = new SlideshowItemstackTextComponent(capi, new ItemStack[] { itemStackToShow }, 48, EnumFloat.Inline);
                    slideShow.ShowStackSize = true;
                    if (itemStackToShow != null)
                    {
                        SingleComposer.AddRichtext(new RichTextComponentBase[] { slideShow }, recipeIngredientsBounds[i], "ing" + i.ToString());
                    }
                }
                
            }
            
            SingleComposer.AddStaticText(Lang.Get("simplealchemy:output_list"), font, outputList);
            var bucketSatck = new ItemStack(capi.World.GetBlock(new AssetLocation("game:woodbucket")), 1);
            ITreeAttribute treeh = new TreeAttribute();
            ItemStack outStack = selectedRecipe.Output.ResolvedItemstack;
            outStack.StackSize = selectedRecipe.Output.Quantity;
            treeh.SetItemstack("0", outStack);
            bucketSatck.Attributes["contents"] = treeh;
            var sli = new SlideshowItemstackTextComponent(capi, new ItemStack[] { bucketSatck }, 48, EnumFloat.Inline);
            sli.ShowStackSize = true;
            if (bucketSatck != null)
            {
                SingleComposer.AddRichtext(new RichTextComponentBase[] { sli }, recipeOutput, "outputstack");
            }
            SingleComposer.AddSmallButton(Lang.Get("<", Array.Empty<object>()), new ActionConsumable(this.prevPage), prevButtonBounds, EnumButtonStyle.Normal, null)
                .AddDynamicText("1/1", CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), pageLabelBounds, "pageNum")
                .AddSmallButton(Lang.Get(">", Array.Empty<object>()), new ActionConsumable(this.nextPage), nextButtonBounds, EnumButtonStyle.Normal, null)
                .AddSmallButton(Lang.Get("Close", Array.Empty<object>()), () => this.TryClose(), closeButton, EnumButtonStyle.Normal, null)
                //.AddIf(this.onTranscribedPressed != null)
                //.AddSmallButton(Lang.Get("Transcribe", Array.Empty<object>()), new ActionConsumable(this.onButtonTranscribe), saveButtonBounds, EnumButtonStyle.Normal, null)
                //.EndIf()
                .EndChildElements();

            SingleComposer.Compose(true);
        }
        protected void DoSendPacket(object p)
        {
            var c = 3;
        }
        private bool nextPage()
        {
            var alchemyRecipesList = simplealchemy.potionCauldronRecipes;
            if (alchemyRecipesList.Count < 1 || (alchemyRecipesList.Count - 1) <= this.selectedPage)
            {
                return false;
            }
            this.selectedPage++;
            Compose();
            return true;
            updatePage(alchemyRecipesList.ElementAt(this.selectedPage));
            //Compose();
            //SingleComposer.ReCompose();
            return true;
        }

        private bool prevPage()
        {
            
            var alchemyRecipesList = simplealchemy.potionCauldronRecipes;
            if (alchemyRecipesList.Count < 1 ||  0 > (this.selectedPage - 1))
            {
                return false;
            }
            this.selectedPage--;
            Compose();
            return true;
            updatePage(alchemyRecipesList.ElementAt(this.selectedPage));
            return true;
        }

        protected void updatePage(PotionCauldronRecipe recipe)
        {
            base.SingleComposer
                .GetDynamicText("recipeName")
                .SetNewText(Lang.Get("simplealchemy:" + recipe.Code), false, false, false);

            //Lang.Get("simplelachemy:", selectedRecipe.MinCauldronTier.ToString())
            base.SingleComposer
                .GetDynamicText("recipeTier")
                .SetNewText(Lang.Get("simplealchemy:cauldron_level_gui", recipe.MinCauldronTier), false, false, false);
            //SingleComposer.AddDynamicText(Lang.Get("simplelachemy:min_max_temp_recipe", selectedRecipe.MinTemperature.ToString(), selectedRecipe.MaxTemperature.ToString()), font, recipeTempBounds, "recipeTemp");
            base.SingleComposer
                .GetDynamicText("recipeTemp")
                .SetNewText(Lang.Get("simplealchemy:min_max_temp_recipe", recipe.MinTemperature.ToString(), recipe.MaxTemperature.ToString()), false, false, false);
            updateRecipeIngredient(recipe);
        }
        protected void updateRecipeIngredient(PotionCauldronRecipe recipe)
        {
            for (int i = 0; i < 6; i++) {

                List<ItemStack> listItemStacks = new List<ItemStack>();
                if (i >= recipe.Ingredients.Count())
                {
                    break;
                }
                var ing = recipe.Ingredients[i];
                SlideshowItemstackTextComponent slideShow = null;
                if (ing.AllowedVariants != null)
                {
                    foreach (var it in ing.AllowedVariants)
                    {
                        var indexStar = ing.Code.Path.IndexOf("*");
                        if (ing.Type == EnumItemClass.Item)
                        {
                            Item itemHere = capi.World.GetItem(new AssetLocation(ing.Code.Domain + ":" + ing.Code.Path.Substring(0, ing.Code.Path.Length - 1) + it));
                            if (itemHere != null)
                            {
                                ItemStack ist = new ItemStack(itemHere, ing.ConsumeQuantity ?? 1);
                                listItemStacks.Add(ist);
                            }
                        }
                    }

                    var sho = new SlideshowItemstackTextComponent(capi, listItemStacks.ToArray(), 48, EnumFloat.Inline);
                    if (listItemStacks.Count > 0)
                    {
                        SingleComposer.AddRichtext(new RichTextComponentBase[] { sho }, recipeIngredientsBounds[i], i.ToString());
                    }
                }
                else if (ing.Litres != -1)
                {
                    var bucket = new ItemStack(capi.World.GetBlock(new AssetLocation("game:woodbucket")), 1);
                    ITreeAttribute tree = new TreeAttribute();
                    ItemStack liqStack = recipe.Ingredients[i].ResolvedItemstack;
                    liqStack.StackSize = recipe.Ingredients[i].Quantity;
                    tree.SetItemstack("0", liqStack);
                    bucket.Attributes["contents"] = tree;
                    slideShow = new SlideshowItemstackTextComponent(capi, new ItemStack[] { bucket }, 48, EnumFloat.Inline);

                }

                else
                {
                    var bucket = new ItemStack(capi.World.GetItem(recipe.Ingredients[i].Code), 12);
                    slideShow = new SlideshowItemstackTextComponent(capi, new ItemStack[] { bucket }, 48, EnumFloat.Inline);
                }
                var richText = SingleComposer.GetRichtext("ing" + i.ToString());
                if (slideShow != null && slideShow.Itemstacks.Count() > 0)
                {
                    SingleComposer.AddRichtext(new RichTextComponentBase[] { slideShow }, recipeIngredientsBounds[i], "ing" + i.ToString());
                }

                if (richText == null)
                {
                    //no such richText existed need to create
                    if(slideShow != null && slideShow.Itemstacks.Count() > 0)
                    {
                        SingleComposer.AddRichtext(new RichTextComponentBase[] { slideShow }, recipeIngredientsBounds[i], "ing" + i.ToString());
                    }
                    else
                    {
                        //didn't exist and no need
                        continue;
                    }
                }
                else
                {
                    //existed but we don't need it now
                    if (slideShow == null || slideShow.Itemstacks.Count() < 1)
                    {
                        //SingleComposer.
                        richText.Dispose();
                        Compose();
                        //SingleComposer.ReCompose();
                    }
                    else
                    {
                        richText.SetNewText(new RichTextComponentBase[] { slideShow });
                    }
                }



            }
        }
    }
}
