using System;
using System.Numerics;
using ImGuiNET;
using simplealchemy.src.recipies;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using VSImGui.API;

namespace simplealchemy.src.gui
{
    public class ImGuiDialogPotionCauldron : ImGuiDialogBase
    {
        private readonly ICoreClientAPI _capi;
        private readonly BlockEntityPotionCauldron _be;

        private ItemIconAtlas _atlas;
        private ImGuiSlotRenderer _slotRenderer;
        private ImGuiInventoryGrid _grid;
        private bool _gpuReady;

        private float _dt;
        private float _animFuelFrac;
        private float _animTempFrac;
        private float _animPrepFrac;
        private float _animLiquidFrac;
        private float _pulsePhase;

        private static readonly Vector4 Col_Header = new(1f,    0.85f, 0.40f, 1f);
        private static readonly Vector4 Col_Gold   = new(0.82f, 0.70f, 0.35f, 1f);
        private static readonly Vector4 Col_Ok     = new(0.30f, 0.85f, 0.32f, 1f);
        private static readonly Vector4 Col_Bad    = new(0.92f, 0.32f, 0.30f, 1f);
        private static readonly Vector4 Col_Dim    = new(0.55f, 0.55f, 0.55f, 1f);

        private static readonly Vector4 Tint_Liquid     = new(0.20f, 0.55f, 0.95f, 0.30f);
        private static readonly Vector4 Tint_Fuel       = new(0.92f, 0.55f, 0.20f, 0.30f);
        private static readonly Vector4 Tint_Ingredient = new(0.45f, 0.85f, 0.45f, 0.18f);
        private static readonly Vector4 Tint_RecipeMatch  = new(0.30f, 0.85f, 0.32f, 0.45f);
        private static readonly Vector4 Tint_RecipeForeign = new(0.92f, 0.32f, 0.30f, 0.40f);

        public bool IsOpen => Opened;

        public ImGuiDialogPotionCauldron(ICoreClientAPI capi, BlockEntityPotionCauldron be) : base(capi)
        {
            _capi = capi;
            _be = be;
        }

        private void EnsureGpuReady()
        {
            if (_gpuReady) return;
            _atlas        = _capi.ModLoader.GetModSystem<simplealchemy>().GetSharedIconAtlas();
            _slotRenderer = new ImGuiSlotRenderer(_capi, slotSize: 52);
            _grid         = new ImGuiInventoryGrid(_capi, _slotRenderer, _atlas, SendInvPacket);
            _grid.SetInventory(_be.Inventory as InventoryBase);
            _gpuReady = true;
        }

        protected override bool OnOpen()
        {
            EnsureGpuReady();
            _capi.World.Player.InventoryManager.OpenInventory(_be.Inventory);
            return true;
        }

        protected override bool OnClose()
        {
            ImGuiInventoryGrid.SuppressMouseDrop = false;
            _capi.World.Player.InventoryManager.CloseInventory(_be.Inventory);
            _be?.OnDialogClosed();
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _slotRenderer?.Dispose(); }
            base.Dispose(disposing);
        }

        protected override CallbackGUIStatus Draw(float deltaSeconds)
        {
            if (!Opened) return CallbackGUIStatus.Closed;
            _dt = deltaSeconds;
            _pulsePhase = (_pulsePhase + deltaSeconds * 2.5f) % (MathF.PI * 2);
            if (!OnDraw()) Close();
            return Opened ? CallbackGUIStatus.GrabMouse : CallbackGUIStatus.Closed;
        }

        private float Lerp(float current, float target, float speed = 6f)
        {
            float a = Math.Clamp(_dt * speed, 0f, 1f);
            return current + (target - current) * a;
        }

        protected override bool OnDraw()
        {
            if (ImGui.IsKeyPressed(ImGuiKey.Escape, false)) return false;

            bool open = true;
            ImGui.SetNextWindowSize(new Vector2(640, 520), ImGuiCond.FirstUseEver);

            ImGui.PushStyleColor(ImGuiCol.Border, Col_Gold);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);

            string title = Lang.Get("simplealchemy:cauldron-title") + "##simplealchemycauldron";
            if (!ImGui.Begin(title, ref open,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.End();
                ImGui.PopStyleVar(2);
                ImGui.PopStyleColor();
                return open;
            }

            DrawContent();

            ImGui.End();
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();
            return open;
        }

        private static void H(string title)
        {
            ImGui.TextColored(Col_Header, title);
            ImGui.Separator();
            ImGui.Spacing();
        }

        private void DrawContent()
        {
            DrawTitleLine();
            ImGui.Spacing();

            ImGui.BeginGroup();
            DrawLiquidAndFuel();
            ImGui.EndGroup();

            ImGui.SameLine(0, 24f);

            ImGui.BeginGroup();
            DrawTemperatureBlock();
            ImGui.EndGroup();

            ImGui.Spacing();
            ImGui.Spacing();

            H(Lang.Get("simplealchemy:cauldron-ingredients"));
            DrawIngredientSlots();

            ImGui.Spacing();
            ImGui.Spacing();

            DrawRecipeBlock();
        }

        private void DrawTitleLine()
        {
            string tier = TierName(_be.cauldronTier);
            ImGui.TextColored(TierColor(_be.cauldronTier),
                Lang.Get("simplealchemy:cauldron-tier", _be.cauldronTier, tier));
        }

        private static Vector4 TierColor(int tier) => tier switch
        {
            0 => new Vector4(0.78f, 0.55f, 0.35f, 1f),  // clay - terracotta
            1 => new Vector4(0.95f, 0.55f, 0.20f, 1f),  // copper - orange
            2 => new Vector4(0.86f, 0.69f, 0.36f, 1f),  // bronze - tan
            3 => new Vector4(0.78f, 0.80f, 0.84f, 1f),  // iron - silver-grey
            4 => new Vector4(0.55f, 0.78f, 0.95f, 1f),  // steel - cool blue
            _ => Col_Gold
        };

        private void DrawLiquidAndFuel()
        {
            int sz = _slotRenderer.SlotSize;

            ImGui.TextColored(Col_Header, Lang.Get("simplealchemy:cauldron-liquid"));
            DrawLiquidLevelText();

            ImGui.Spacing();

            ImGui.TextColored(Col_Header, Lang.Get("simplealchemy:cauldron-fuel"));
            _grid.DrawSingleSlot(1, Tint_Fuel);
            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4f);
            DrawFuelInfo();
            ImGui.EndGroup();
        }

        private void DrawLiquidLevelText()
        {
            var stack = _be.GetContent();
            float capacity = Math.Max(_be.CapacityLitres, 1);
            float litres = 0f;
            string name = null;

            if (stack != null)
            {
                var props = BlockLiquidContainerBase.GetContainableProps(stack);
                litres = props != null && props.ItemsPerLitre > 0
                    ? stack.StackSize / props.ItemsPerLitre
                    : stack.StackSize;
                name = stack.GetName();
            }

            float target = Math.Clamp(litres / capacity, 0f, 1f);
            _animLiquidFrac = Lerp(_animLiquidFrac, target);

            if (name == null) ImGui.TextColored(Col_Dim, Lang.Get("simplealchemy:cauldron-liquid-empty"));
            else ImGui.TextColored(Col_Gold, name);

            Vector4 barColor = stack != null
                ? new Vector4(0.20f, 0.55f, 0.95f, 1f)
                : new Vector4(0.35f, 0.35f, 0.40f, 1f);

            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, barColor);
            ImGui.ProgressBar(_animLiquidFrac, new Vector2(280, 18), $"{litres:0.##} / {(int)capacity} L");
            ImGui.PopStyleColor();
        }

        private void DrawFuelInfo()
        {
            var fuel = _be.fuelStack;
            if (fuel == null)
            {
                ImGui.TextColored(Col_Dim, Lang.Get("simplealchemy:cauldron-fuel-empty"));
                return;
            }
            ImGui.TextColored(Col_Gold, $"{fuel.StackSize}× {fuel.GetName()}");

            if (_be.IsBurning && _be.maxFuelBurnTime > 0)
            {
                float target = Math.Clamp(_be.fuelBurnTime / _be.maxFuelBurnTime, 0f, 1f);
                _animFuelFrac = Lerp(_animFuelFrac, target);
                Vector2 size = new(160, 14);
                ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.92f, 0.55f, 0.20f, 1f));
                ImGui.ProgressBar(_animFuelFrac, size, "");
                ImGui.PopStyleColor();
            }
            else
            {
                _animFuelFrac = Lerp(_animFuelFrac, 0f);
                if (!_be.fuelSlot.Empty)
                {
                    DrawIgniteButton();
                }
            }
        }

        private void DrawTemperatureBlock()
        {
            ImGui.TextColored(Col_Header, Lang.Get("simplealchemy:cauldron-temperature"));

            float t = _be.furnaceTemperature;
            int tmax = Math.Max(_be.maxTemperature, 1100);
            float target = Math.Clamp(t / tmax, 0f, 1f);
            _animTempFrac = Lerp(_animTempFrac, target);

            var recipe = _be.ResolveCurrentRecipe();
            bool inWindow = recipe != null
                ? t >= recipe.MinTemperature && t <= recipe.MaxTemperature
                : false;

            Vector4 barColor = recipe == null
                ? TempColor(t)
                : (inWindow ? Col_Ok : Col_Bad);

            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, barColor);
            ImGui.ProgressBar(_animTempFrac, new Vector2(280, 18), $"{(int)t}°C / {_be.maxTemperature}°C");
            ImGui.PopStyleColor();

            if (recipe != null)
            {
                ImGui.TextColored(Col_Dim,
                    Lang.Get("simplealchemy:cauldron-temp-window", recipe.MinTemperature, recipe.MaxTemperature));
            }
            else
            {
                ImGui.TextColored(Col_Dim, Lang.Get("simplealchemy:cauldron-no-recipe-hint"));
            }

            if (!_be.IsBurning)
                ImGui.TextColored(Col_Dim, Lang.Get("simplealchemy:cauldron-not-burning"));
        }

        private void DrawIngredientSlots()
        {
            var recipe = _be.ResolveCurrentRecipe();
            for (int i = _be.firstIngredientSlot; i < _be.Inventory.Count; i++)
            {
                _grid.DrawSingleSlot(i, SlotTintForRecipe(i, recipe, Tint_Ingredient));
                if (i < _be.Inventory.Count - 1) ImGui.SameLine(0, 8f);
            }
        }

        private Vector4 SlotTintForRecipe(int slotId, PotionCauldronRecipe recipe, Vector4 fallback)
        {
            if (recipe == null) return fallback;
            var stack = _be.Inventory[slotId].Itemstack;
            if (stack == null)
            {
                float pulse = 0.20f + 0.20f * (MathF.Sin(_pulsePhase) * 0.5f + 0.5f);
                return new Vector4(1f, 0.85f, 0.30f, pulse);
            }
            foreach (var ing in recipe.Ingredients)
            {
                if (ing.SatisfiesAsIngredient(stack, true)) return Tint_RecipeMatch;
            }
            return Tint_RecipeForeign;
        }

        private void DrawRecipeBlock()
        {
            var recipe = _be.ResolveCurrentRecipe();
            H(Lang.Get("simplealchemy:cauldron-recipe"));

            if (recipe == null)
            {
                ImGui.TextColored(Col_Dim, Lang.Get("simplealchemy:cauldron-no-recipe"));
                return;
            }

            string outName = recipe.Output?.ResolvedItemstack?.GetName() ?? recipe.Code;
            ImGui.TextColored(Col_Gold, outName);

            ImGui.Spacing();

            foreach (var ing in recipe.Ingredients)
            {
                bool ok = IsIngredientSatisfied(ing);
                ImGui.TextColored(ok ? Col_Ok : Col_Bad, ok ? "  v  " : "  x  ");
                ImGui.SameLine();
                string display = ing.ResolvedItemStack?.GetName() ?? ing.Code?.ToShortString() ?? "?";
                int qty = ing.Quantity;
                ImGui.Text(qty > 1 ? $"{display}  x{qty}" : display);
            }

            ImGui.Spacing();

            int totalTicks = Math.Max(recipe.PreparationTicks, 1);
            int passed = Math.Clamp(_be.ticksPassedForRecipe, 0, totalTicks);
            float ptarget = passed / (float)totalTicks;
            _animPrepFrac = Lerp(_animPrepFrac, ptarget, 4f);

            bool tempOk = _be.furnaceTemperature >= recipe.MinTemperature
                          && _be.furnaceTemperature <= recipe.MaxTemperature;

            ImGui.PushStyleColor(ImGuiCol.PlotHistogram,
                tempOk ? Col_Ok : new Vector4(0.55f, 0.55f, 0.55f, 1f));
            ImGui.ProgressBar(_animPrepFrac, new Vector2(380, 16), $"{passed} / {totalTicks}");
            ImGui.PopStyleColor();
        }

        private bool IsIngredientSatisfied(PotionCauldronRecipeIngredient ing)
        {
            int need = ing.Quantity;
            int[] slotIds = { 0, 2, 3, 4, 5 };
            foreach (int idx in slotIds)
            {
                if (idx >= _be.Inventory.Count) continue;
                var stack = _be.Inventory[idx].Itemstack;
                if (stack == null) continue;
                if (!ing.SatisfiesAsIngredient(stack, true)) continue;
                if (stack.StackSize >= need) return true;
            }
            return false;
        }

        private void DrawIgniteButton()
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.55f, 0.32f, 0.10f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.78f, 0.50f, 0.18f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.55f, 0.32f, 0.10f, 1f));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
            if (ImGui.Button(Lang.Get("simplealchemy:cauldron-ignite") + "##ignite", new Vector2(160, 22)))
            {
                _capi.Network.SendBlockEntityPacket(_be.Pos.X, _be.Pos.Y, _be.Pos.Z,
                    BlockEntityPotionCauldron.PacketIdIgnite);
            }
            ImGui.PopStyleVar();
            ImGui.PopStyleColor(3);
        }

        private static Vector4 TempColor(float t)
        {
            if (t < 200) return new Vector4(0.50f, 0.55f, 0.62f, 1f);
            if (t < 500) return new Vector4(0.95f, 0.78f, 0.22f, 1f);
            if (t < 900) return new Vector4(0.95f, 0.45f, 0.18f, 1f);
            return new Vector4(0.95f, 0.20f, 0.18f, 1f);
        }

        private static string TierName(int tier) => tier switch
        {
            0 => "clay",
            1 => "copper",
            2 => "bronze",
            3 => "iron",
            4 => "steel",
            _ => "?"
        };

        private void SendInvPacket(object packet) =>
            _capi.Network.SendBlockEntityPacket(_be.Pos.X, _be.Pos.Y, _be.Pos.Z, packet);
    }
}
