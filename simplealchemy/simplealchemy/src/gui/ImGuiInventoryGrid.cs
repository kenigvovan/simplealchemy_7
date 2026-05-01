using System;
using System.Numerics;
using System.Text;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace simplealchemy.src.gui
{
    public class ImGuiInventoryGrid
    {
        private readonly ICoreClientAPI _capi;
        private readonly ImGuiSlotRenderer _slotRenderer;
        private readonly ItemIconAtlas _iconAtlas;
        private readonly Action<object> _sendPacket;
        private readonly int _cols;
        private readonly float _padding;
        private InventoryBase _inventory;
        private int _hoveredSlotId = -1;

        public static bool SuppressMouseDrop { get; set; }

        public ImGuiInventoryGrid(
            ICoreClientAPI capi,
            ImGuiSlotRenderer slotRenderer,
            ItemIconAtlas iconAtlas,
            Action<object> sendPacket = null,
            int cols = 4,
            float padding = 3f)
        {
            _capi = capi;
            _slotRenderer = slotRenderer;
            _iconAtlas = iconAtlas;
            _sendPacket = sendPacket;
            _cols = cols;
            _padding = padding;
        }

        public void SetInventory(InventoryBase inventory)
        {
            _inventory = inventory;
        }

        public void Draw()
        {
            if (_inventory == null) return;
            SuppressMouseDrop = true;

            int slotCount = _inventory.Count;
            int slotSize = _slotRenderer.SlotSize;
            float step = slotSize + _padding;
            int rows = (int)Math.Ceiling((float)slotCount / _cols);

            Vector2 gridSize = new(_cols * step - _padding, rows * step - _padding);
            Vector2 origin = ImGui.GetCursorScreenPos();
            ImGui.Dummy(gridSize);

            _hoveredSlotId = -1;
            for (int i = 0; i < slotCount; i++)
            {
                int col = i % _cols;
                int row = i / _cols;
                DrawSlotAt(i, origin + new Vector2(col * step, row * step), slotSize);
            }
        }

        public void DrawSingleSlot(int slotId, Vector4 tint = default)
        {
            if (_inventory == null) return;
            SuppressMouseDrop = true;
            int size = _slotRenderer.SlotSize;
            Vector2 pos = ImGui.GetCursorScreenPos();
            DrawSlotAt(slotId, pos, size, tint);
        }

        private void DrawSlotAt(int slotId, Vector2 pos, int size, Vector4 tint = default)
        {
            var dl = ImGui.GetWindowDrawList();

            _slotRenderer.DrawSlotBackground(pos, dl);

            if (tint.W > 0f)
                dl.AddRectFilled(pos, pos + new Vector2(size, size), ImGui.GetColorU32(tint), 2f);

            ImGui.SetCursorScreenPos(pos);
            ImGui.InvisibleButton($"slot{slotId}_{(int)pos.X}_{(int)pos.Y}", new Vector2(size, size));

            bool hovered = ImGui.IsItemHovered();
            bool leftClick  = hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
            bool rightClick = hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Right);

            if (hovered)
            {
                _hoveredSlotId = slotId;
                _slotRenderer.DrawSlotHighlight(pos, dl);
            }

            ItemSlot slot = _inventory[slotId];
            if (slot?.Itemstack != null)
            {
                float iconSize   = size * 0.75f;
                float iconOffset = (size - iconSize) * 0.5f;
                Vector2 iconPos  = pos + new Vector2(iconOffset, iconOffset);

                _iconAtlas.DrawToList(slot.Itemstack, iconPos, new Vector2(iconSize, iconSize), dl);

                if (slot.Itemstack.StackSize > 1 && !(slot is ItemSlotLiquidOnly))
                {
                    string countText = slot.Itemstack.StackSize.ToString();
                    Vector2 textSize = ImGui.CalcTextSize(countText);
                    Vector2 textPos  = pos + new Vector2(size - textSize.X - 2, size - textSize.Y - 1);
                    dl.AddText(textPos + new Vector2(1, 1), 0xFF000000, countText);
                    dl.AddText(textPos, 0xFFFFFFFF, countText);
                }

                if (hovered)
                {
                    DrawItemTooltip(slot);
                }
            }

            if (leftClick)       OnSlotClick(slotId, EnumMouseButton.Left);
            else if (rightClick) OnSlotClick(slotId, EnumMouseButton.Right);
        }

        private void OnSlotClick(int slotId, EnumMouseButton button)
        {
            var player = _capi.World.Player;
            var mouseSlot = player.InventoryManager.MouseItemSlot;
            if (mouseSlot == null) return;

            EnumModifierKey modifiers = 0;
            if (_capi.Input.KeyboardKeyState[(int)GlKeys.ShiftLeft] ||
                _capi.Input.KeyboardKeyState[(int)GlKeys.ShiftRight])
                modifiers |= EnumModifierKey.SHIFT;
            if (_capi.Input.KeyboardKeyState[(int)GlKeys.ControlLeft] ||
                _capi.Input.KeyboardKeyState[(int)GlKeys.ControlRight])
                modifiers |= EnumModifierKey.CTRL;

            bool shiftDown = (modifiers & EnumModifierKey.SHIFT) != 0;

            var op = new ItemStackMoveOperation(
                _capi.World, button, modifiers,
                shiftDown ? EnumMergePriority.AutoMerge : EnumMergePriority.DirectMerge, 0);
            op.ActingPlayer = player;

            if (shiftDown) op.RequestedQuantity = _inventory[slotId].StackSize;

            ItemSlot sourceSlot = shiftDown ? _inventory[slotId] : mouseSlot;

            _inventory.InvNetworkUtil.PauseInventoryUpdates = true;
            mouseSlot.Inventory.InvNetworkUtil.PauseInventoryUpdates = true;

            object packet = _inventory.ActivateSlot(slotId, sourceSlot, ref op);

            _inventory.InvNetworkUtil.PauseInventoryUpdates = false;
            mouseSlot.Inventory.InvNetworkUtil.PauseInventoryUpdates = false;

            if (packet != null && _sendPacket != null)
            {
                if (packet is object[] packets)
                    foreach (var p in packets) _sendPacket(p);
                else
                    _sendPacket(packet);
            }

            _capi.Input.TriggerOnMouseClickSlot(_inventory[slotId]);
        }

        private void DrawItemTooltip(ItemSlot slot)
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 28f);

            ImGui.TextColored(new Vector4(0.82f, 0.70f, 0.35f, 1f), slot.Itemstack.GetName());

            var sb = new StringBuilder();
            try
            {
                slot.Itemstack.Collectible.GetHeldItemInfo(slot, sb, _capi.World, false);
            }
            catch { }

            string desc = sb.ToString().TrimEnd();
            if (desc.Length > 0)
            {
                ImGui.Separator();
                ImGui.TextUnformatted(desc);
            }

            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }
}
