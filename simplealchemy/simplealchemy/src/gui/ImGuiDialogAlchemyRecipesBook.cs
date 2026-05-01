using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ImGuiDialogAlchemyRecipesBook : ImGuiDialogBase
    {
        private readonly ICoreClientAPI _capi;

        private ItemIconAtlas _atlas;
        private ImGuiSlotRenderer _slotRenderer;
        private bool _gpuReady;

        private string _filter = "";
        private int _selectedIndex = -1;

        private static readonly Vector4 Col_Header = new(1f,    0.85f, 0.40f, 1f);
        private static readonly Vector4 Col_Gold   = new(0.82f, 0.70f, 0.35f, 1f);
        private static readonly Vector4 Col_Dim    = new(0.55f, 0.55f, 0.55f, 1f);
        private static readonly Vector4 Col_Cat    = new(0.50f, 0.78f, 1.00f, 1f);

        public bool IsOpen => Opened;

        public ImGuiDialogAlchemyRecipesBook(ICoreClientAPI capi) : base(capi)
        {
            _capi = capi;
        }

        private void EnsureGpuReady()
        {
            if (_gpuReady) return;
            _atlas        = _capi.ModLoader.GetModSystem<simplealchemy>().GetSharedIconAtlas();
            _slotRenderer = new ImGuiSlotRenderer(_capi, slotSize: 48);
            _gpuReady = true;
        }

        protected override bool OnOpen()
        {
            EnsureGpuReady();
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
            if (!OnDraw()) Close();
            return Opened ? CallbackGUIStatus.GrabMouse : CallbackGUIStatus.Closed;
        }

        protected override bool OnDraw()
        {
            if (ImGui.IsKeyPressed(ImGuiKey.Escape, false)) return false;

            bool open = true;
            ImGui.SetNextWindowSize(new Vector2(820, 560), ImGuiCond.FirstUseEver);

            ImGui.PushStyleColor(ImGuiCol.Border, Col_Gold);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);

            string title = Lang.Get("simplealchemy:potion_book_tab_name_gui") + "##simplealchemyrecipes";
            if (!ImGui.Begin(title, ref open, ImGuiWindowFlags.NoScrollbar))
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

        private List<PotionCauldronRecipe> GetFiltered()
        {
            var all = simplealchemy.potionCauldronRecipes ?? new List<PotionCauldronRecipe>();
            IEnumerable<PotionCauldronRecipe> q = all;
            if (!string.IsNullOrWhiteSpace(_filter))
            {
                string f = _filter.Trim().ToLowerInvariant();
                q = q.Where(r =>
                    (r.Code ?? "").ToLowerInvariant().Contains(f) ||
                    Lang.Get("simplealchemy:" + (r.Code ?? "")).ToLowerInvariant().Contains(f));
            }
            return q
                .OrderBy(r => CategoryOrder(r))
                .ThenBy(r => RecipeDisplayName(r), StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static int CategoryOrder(PotionCauldronRecipe rec)
        {
            string code = (rec.Code ?? "").ToLowerInvariant();
            if (code.Contains("base") || code.Contains("based")) return 0;
            if (code.StartsWith("transmutation")) return 2;
            return 1;
        }

        private void DrawContent()
        {
            float listW = 280f;
            float availH = ImGui.GetContentRegionAvail().Y;

            ImGui.BeginGroup();
            DrawList(listW, availH);
            ImGui.EndGroup();

            ImGui.SameLine();

            ImGui.BeginGroup();
            DrawDetails(availH);
            ImGui.EndGroup();
        }

        private void DrawList(float w, float h)
        {
            ImGui.SetNextItemWidth(w - 4f);
            ImGui.InputTextWithHint("##recipefilter", Lang.Get("simplealchemy:recipebook-search-hint"), ref _filter, 64);

            ImGui.BeginChild("##recipelist", new Vector2(w, h - ImGui.GetFrameHeightWithSpacing()), true);

            var recipes = GetFiltered();
            if (recipes.Count == 0)
            {
                ImGui.TextColored(Col_Dim, Lang.Get("simplealchemy:recipebook-no-results"));
            }

            string lastCategory = null;
            for (int i = 0; i < recipes.Count; i++)
            {
                var rec = recipes[i];
                string cat = CategoryOf(rec);
                if (cat != lastCategory)
                {
                    if (lastCategory != null) ImGui.Spacing();
                    ImGui.TextColored(Col_Cat, cat);
                    ImGui.Separator();
                    lastCategory = cat;
                }

                string label = RecipeDisplayName(rec);
                bool isSelected = _selectedIndex >= 0
                    && _selectedIndex < simplealchemy.potionCauldronRecipes.Count
                    && simplealchemy.potionCauldronRecipes[_selectedIndex] == rec;

                if (ImGui.Selectable("  " + label + "##rec" + i, isSelected))
                {
                    _selectedIndex = simplealchemy.potionCauldronRecipes.IndexOf(rec);
                }
            }

            ImGui.EndChild();
        }

        private void DrawDetails(float h)
        {
            ImGui.BeginChild("##recipedetails", new Vector2(0, h), true);

            var all = simplealchemy.potionCauldronRecipes;
            if (all == null || _selectedIndex < 0 || _selectedIndex >= all.Count)
            {
                ImGui.TextColored(Col_Dim, Lang.Get("simplealchemy:recipebook-pick-recipe"));
                ImGui.EndChild();
                return;
            }

            var rec = all[_selectedIndex];

            ImGui.TextColored(Col_Header, RecipeDisplayName(rec));
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextColored(Col_Gold, Lang.Get("simplealchemy:cauldron_level_gui", rec.MinCauldronTier));
            ImGui.TextColored(Col_Gold, Lang.Get("simplealchemy:min_max_temp_recipe", rec.MinTemperature, rec.MaxTemperature));
            ImGui.TextColored(Col_Dim, Lang.Get("simplealchemy:recipebook-prep-ticks", rec.PreparationTicks));

            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.TextColored(Col_Header, Lang.Get("simplealchemy:ingredient_list"));
            ImGui.Spacing();

            int sz = _slotRenderer.SlotSize;
            foreach (var ing in rec.Ingredients)
            {
                DrawIngredientRow(ing, sz);
            }

            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.TextColored(Col_Header, Lang.Get("simplealchemy:output_list"));
            ImGui.Spacing();

            DrawOutputRow(rec, sz);

            ImGui.EndChild();
        }

        private void DrawIngredientRow(PotionCauldronRecipeIngredient ing, int sz)
        {
            ItemStack stack = BuildIngredientStack(ing);
            string label = stack?.GetName() ?? ing.Code?.ToShortString() ?? "?";
            string qty = ing.Litres > 0
                ? $"{ing.Litres:0.##} L"
                : (ing.Quantity > 1 ? $"×{ing.Quantity}" : "");
            if (ing.ConsumeQuantity == 0)
                qty += "  " + Lang.Get("simplealchemy:recipebook-catalyst");

            DrawRow(stack, label, qty, sz);
        }

        private void DrawOutputRow(PotionCauldronRecipe rec, int sz)
        {
            var output = rec.Output;
            ItemStack stack = output?.ResolvedItemstack?.Clone();
            if (stack != null) stack.StackSize = Math.Max(output.Quantity, 1);

            string label = stack?.GetName() ?? rec.Code ?? "?";
            string qty = output != null && output.Litres > 0
                ? $"{output.Litres:0.##} L"
                : (output?.Quantity > 1 ? $"×{output.Quantity}" : "");

            DrawRow(stack, label, qty, sz);
        }

        private void DrawRow(ItemStack stack, string label, string qty, int sz)
        {
            Vector2 pos = ImGui.GetCursorScreenPos();
            var dl = ImGui.GetWindowDrawList();
            _slotRenderer.DrawSlotBackground(pos, dl);

            if (stack?.Collectible != null)
            {
                float iconSize = sz * 0.78f;
                float offset = (sz - iconSize) * 0.5f;
                _atlas.DrawToList(stack, pos + new Vector2(offset, offset), new Vector2(iconSize, iconSize), dl);
            }

            ImGui.SetCursorScreenPos(pos);
            ImGui.InvisibleButton($"row_{label}_{(int)pos.X}_{(int)pos.Y}", new Vector2(sz, sz));
            if (stack?.Collectible != null && ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.TextColored(Col_Gold, label);
                ImGui.EndTooltip();
            }

            ImGui.SameLine(0, 12f);
            ImGui.BeginGroup();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (sz - ImGui.GetTextLineHeightWithSpacing() * 2) * 0.5f);
            ImGui.TextColored(Col_Gold, label);
            if (!string.IsNullOrEmpty(qty)) ImGui.TextColored(Col_Dim, qty);
            ImGui.EndGroup();

            ImGui.Spacing();
        }

        private ItemStack BuildIngredientStack(PotionCauldronRecipeIngredient ing)
        {
            if (ing == null) return null;

            // Resolved (no wildcard) — use directly.
            if (ing.ResolvedItemStack != null && !(ing.Code?.Path?.Contains('*') ?? false))
            {
                var s = ing.ResolvedItemStack.Clone();
                s.StackSize = Math.Max(ing.Quantity, 1);
                return s;
            }

            // Wildcard with variants — pick the first concrete variant we can resolve.
            if (ing.AllowedVariants != null && ing.AllowedVariants.Length > 0 && ing.Code != null)
            {
                int starIdx = ing.Code.Path.IndexOf('*');
                if (starIdx >= 0)
                {
                    string prefix = ing.Code.Path.Substring(0, starIdx);
                    string suffix = ing.Code.Path.Substring(starIdx + 1);
                    foreach (var variant in ing.AllowedVariants)
                    {
                        var loc = new AssetLocation(ing.Code.Domain, prefix + variant + suffix);
                        ItemStack s = ing.Type == EnumItemClass.Item
                            ? (_capi.World.GetItem(loc) is { } it ? new ItemStack(it, Math.Max(ing.Quantity, 1)) : null)
                            : (_capi.World.GetBlock(loc) is { } bl ? new ItemStack(bl, Math.Max(ing.Quantity, 1)) : null);
                        if (s != null) return s;
                    }
                }
            }

            return ing.ResolvedItemStack;
        }

        private static string RecipeDisplayName(PotionCauldronRecipe rec)
        {
            if (rec == null) return "";
            string fromCode = Lang.Get("simplealchemy:" + rec.Code);
            if (!string.IsNullOrEmpty(fromCode) && fromCode != "simplealchemy:" + rec.Code) return fromCode;

            string fromOutput = rec.Output?.ResolvedItemstack?.GetName();
            if (!string.IsNullOrEmpty(fromOutput)) return fromOutput;

            return rec.Code ?? "?";
        }

        private static string CategoryOf(PotionCauldronRecipe rec)
        {
            string code = (rec.Code ?? "").ToLowerInvariant();
            if (code.StartsWith("transmutation")) return Lang.Get("simplealchemy:recipebook-cat-transmutation");
            if (code.Contains("base") || code.Contains("based")) return Lang.Get("simplealchemy:recipebook-cat-base");
            return Lang.Get("simplealchemy:recipebook-cat-potion");
        }
    }
}
