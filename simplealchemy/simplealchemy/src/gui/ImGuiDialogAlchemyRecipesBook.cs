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
                .ThenBy(r => RecipeBaseName(r), StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => StrengthOrder(r))
                .ToList();
        }

        private static string RecipeBaseName(PotionCauldronRecipe rec)
        {
            string code = rec.Code ?? "";
            foreach (string suffix in new[] { "-weak", "-medium", "-strong" })
                if (code.EndsWith(suffix)) return code.Substring(0, code.Length - suffix.Length);
            return code;
        }

        private static int StrengthOrder(PotionCauldronRecipe rec)
        {
            string code = (rec.Code ?? "").ToLowerInvariant();
            if (code.EndsWith("-weak"))   return 0;
            if (code.EndsWith("-medium")) return 1;
            if (code.EndsWith("-strong")) return 2;
            return 0;
        }

        private static readonly System.Collections.Generic.HashSet<string> CoatingPrefixes = new()
        {
            "poison", "walkslow", "weakmelee"
        };

        private static bool IsCoating(string code) =>
            CoatingPrefixes.Any(p => code.StartsWith(p));

        private static int CategoryOrder(PotionCauldronRecipe rec)
        {
            string code = (rec.Code ?? "").ToLowerInvariant();
            if (code.Contains("base") || code.Contains("based")) return 0;
            if (IsCoating(code)) return 2;
            if (code.StartsWith("transmutation")) return 3;
            return 1;
        }

        private void DrawContent()
        {
            if (!ImGui.BeginTabBar("##alchemybook_tabs")) return;

            if (ImGui.BeginTabItem(Lang.Get("simplealchemy:recipebook-tab-recipes")))
            {
                float listW = 280f;
                float availH = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing();

                ImGui.BeginGroup();
                DrawList(listW, availH);
                ImGui.EndGroup();

                ImGui.SameLine();

                ImGui.BeginGroup();
                DrawDetails(availH);
                ImGui.EndGroup();

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Lang.Get("simplealchemy:recipebook-tab-guide")))
            {
                DrawGuide();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        private void DrawGuide()
        {
            float availH = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing();
            ImGui.BeginChild("##guidescroll", new Vector2(0, availH), false);

            DrawGuideSection("simplealchemy:guide-ingredients-title", new[]
            {
                "simplealchemy:guide-ingredients-1",
                "simplealchemy:guide-ingredients-2",
                "simplealchemy:guide-ingredients-3",
            });

            DrawGuideSection("simplealchemy:guide-bases-title", new[]
            {
                "simplealchemy:guide-bases-1",
                "simplealchemy:guide-bases-2",
                "simplealchemy:guide-bases-3",
            });

            DrawGuideSection("simplealchemy:guide-cauldron-title", new[]
            {
                "simplealchemy:guide-cauldron-1",
                "simplealchemy:guide-cauldron-2",
                "simplealchemy:guide-cauldron-3",
                "simplealchemy:guide-cauldron-4",
            });

            DrawGuideSection("simplealchemy:guide-flasks-title", new[]
            {
                "simplealchemy:guide-flasks-1",
                "simplealchemy:guide-flasks-2",
            });

            DrawGuideSection("simplealchemy:guide-coating-title", new[]
            {
                "simplealchemy:guide-coating-1",
                "simplealchemy:guide-coating-2",
                "simplealchemy:guide-coating-3",
            });

            DrawGuideSection("simplealchemy:guide-firedmgimmune-title", new[]
            {
                "simplealchemy:guide-firedmgimmune-1",
                "simplealchemy:guide-firedmgimmune-2",
            });

            DrawGuideSection("simplealchemy:guide-antidote-title", new[]
            {
                "simplealchemy:guide-antidote-1",
                "simplealchemy:guide-antidote-2",
                "simplealchemy:guide-antidote-3",
            });

            ImGui.EndChild();
        }

        private void DrawGuideSection(string titleKey, string[] lineKeys)
        {
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.TextColored(Col_Header, Lang.Get(titleKey));
            ImGui.PushStyleColor(ImGuiCol.Separator, Col_Gold);
            ImGui.Separator();
            ImGui.PopStyleColor();
            ImGui.Spacing();

            ImGui.Indent(8f);
            foreach (var key in lineKeys)
            {
                ImGui.Bullet();
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, Col_Gold);
                ImGui.PushTextWrapPos(0f);
                ImGui.TextWrapped(Lang.Get(key));
                ImGui.PopTextWrapPos();
                ImGui.PopStyleColor();
                ImGui.Spacing();
            }
            ImGui.Unindent(8f);
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

            DrawCauldronTierLine(rec.MinCauldronTier);
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

            DrawEffectInfo(rec);

            ImGui.EndChild();
        }

        private void DrawCauldronTierLine(int tier)
        {
            float iconSz = ImGui.GetTextLineHeight() + 4f;
            Vector2 pos = ImGui.GetCursorScreenPos();
            var dl = ImGui.GetWindowDrawList();

            var stack = GetCauldronStack(tier);
            if (stack != null)
            {
                _atlas.DrawToList(stack, pos, new Vector2(iconSz, iconSz), dl);
                ImGui.SetCursorScreenPos(pos);
                ImGui.InvisibleButton($"##cauldronicon{tier}", new Vector2(iconSz, iconSz));
                ImGui.SameLine(0, 6f);
            }
            ImGui.TextColored(Col_Gold, Lang.Get("simplealchemy:cauldron_level_gui", tier));
        }

        private ItemStack GetCauldronStack(int tier)
        {
            string mat = tier switch
            {
                1 => "copper",
                2 => "tinbronze",
                3 => "iron",
                4 => "steel",
                _ => null
            };
            if (mat == null) return null;
            var block = _capi.World.GetBlock(new AssetLocation("simplealchemy", $"cauldron-{mat}"));
            return block != null ? new ItemStack(block) : null;
        }

        private void DrawEffectInfo(PotionCauldronRecipe rec)
        {
            var output = rec.Output?.ResolvedItemstack;
            if (output?.Collectible == null) return;

            var potionInfo = output.Collectible.Attributes?["potioninfo"];
            if (potionInfo == null || !potionInfo.Exists) return;

            string potionId = potionInfo["potionId"].AsString("");
            string strength = output.Collectible.Variant?["strength"] ?? "weak";
            int duration = potionInfo["duration"][strength].AsInt(0);
            float statChange = output.Collectible.Attributes["statChangeValue"].AsFloat(0);
            int tier = strength switch { "strong" => 3, "medium" => 2, _ => 1 };

            string line = potionId switch
            {
                "walkspeedtypepotion" => Lang.Get("simplealchemy:stat_change_by_percents",
                    FormatPercent(statChange * tier), Lang.Get("simplealchemy:walkspeed")),
                "miningspeedtypepotion" => Lang.Get("simplealchemy:stat_change_by_percents",
                    FormatPercent(statChange * tier), Lang.Get("simplealchemy:mining_speed")),
                "meleetypepotion" => Lang.Get("simplealchemy:stat_change_by_percents",
                    FormatPercent(statChange * tier), Lang.Get("simplealchemy:melee_attack")),
                "regenerationtypepotion" => Lang.Get("simplealchemy:regen_hp_per_tick", statChange.ToString("0.##")),
                "temporalstabilityrestore" => Lang.Get("simplealchemy:restore_stability", tier * 30),
                "forgetting"        => Lang.Get("simplealchemy:forgetting_potion_desc"),
                "firedamageimmune"  => Lang.Get("simplealchemy:firedmgimmune_potion_desc"),
                "antidote"          => Lang.Get("simplealchemy:antidote_potion_desc_tier" + tier),
                "poison"            => Lang.Get("simplealchemy:poison_potion_desc"),
                "walkslow"          => Lang.Get("simplealchemy:walkslow_potion_desc"),
                "weakmelee"         => Lang.Get("simplealchemy:weakmelee_potion_desc"),
                _ => null
            };

            if (line == null && duration <= 0) return;

            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.TextColored(Col_Header, Lang.Get("simplealchemy:recipebook-effect"));
            ImGui.Spacing();

            if (line != null) ImGui.TextColored(Col_Gold, line.Replace("%", "%%"));
            if (duration > 0) ImGui.TextColored(Col_Dim, Lang.Get("simplealchemy:duration_sec", duration));

            if (potionId == "poison" || potionId == "walkslow" || potionId == "weakmelee")
            {
                float chance = Math.Min(Config.Current.weaponCoatingChancePerTier.Val * tier, 1f) * 100f;
                int charges = 3 + tier * 2;
                ImGui.TextColored(Col_Dim, Lang.Get("simplealchemy:recipebook-coating-chance", (int)chance));
                ImGui.TextColored(Col_Dim, Lang.Get("simplealchemy:recipebook-coating-charges", charges));
            }
        }

        private static string FormatPercent(float v)
        {
            float pct = v * 100;
            return (pct >= 0 ? "+" : "") + pct.ToString("0.#");
        }

        private void DrawIngredientRow(PotionCauldronRecipeIngredient ing, int sz)
        {
            ItemStack stack = BuildIngredientStack(ing);
            string label = stack?.GetName() ?? ing.Code?.ToShortString() ?? "?";
            string qty = LitresQty(ing.Litres, ing.Quantity, stack);
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
            string qty = output != null ? LitresQty(output.Litres, output.Quantity, stack) : "";

            DrawRow(stack, label, qty, sz);
        }

        private static string LitresQty(float litres, int quantity, ItemStack stack)
        {
            if (litres > 0f)
                return $"{litres:0.##} L";

            if (stack != null)
            {
                var props = BlockLiquidContainerBase.GetContainableProps(stack);
                if (props != null && props.ItemsPerLitre > 0)
                    return $"{quantity / props.ItemsPerLitre:0.##} L";
            }

            return quantity > 1 ? $"×{quantity}" : "";
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
            if (code.Contains("base") || code.Contains("based")) return Lang.Get("simplealchemy:recipebook-cat-base");
            if (IsCoating(code)) return Lang.Get("simplealchemy:recipebook-cat-coating");
            if (code.StartsWith("transmutation")) return Lang.Get("simplealchemy:recipebook-cat-transmutation");
            return Lang.Get("simplealchemy:recipebook-cat-potion");
        }
    }
}
