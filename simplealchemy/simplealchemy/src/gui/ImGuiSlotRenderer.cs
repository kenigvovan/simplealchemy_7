using System;
using System.Numerics;
using Cairo;
using ImGuiNET;
using Vintagestory.API.Client;

namespace simplealchemy.src.gui
{
    public class ImGuiSlotRenderer : IDisposable
    {
        private readonly ICoreClientAPI _capi;
        private int _slotBgTextureId;
        private int _slotHighlightTextureId;

        public int SlotSize { get; }

        public ImGuiSlotRenderer(ICoreClientAPI capi, int slotSize = 48)
        {
            _capi = capi;
            SlotSize = slotSize;
            GenerateSlotBackground();
            GenerateSlotHighlight();
        }

        private void GenerateSlotBackground()
        {
            int size = SlotSize;
            using var surface = new ImageSurface(Format.Argb32, size, size);
            using var ctx = new Context(surface);

            ctx.SetSourceRGBA(1.0, 0.886, 0.761, 1.0);
            RoundRect(ctx, 0, 0, size, size, 2);
            ctx.Fill();

            ctx.SetSourceRGBA(0.518, 0.361, 0.263, 1.0);
            RoundRect(ctx, 0, 0, size, size, 2);
            ctx.LineWidth = 3.0;
            ctx.Stroke();

            ctx.SetSourceRGBA(0, 0, 0, 0.8);
            RoundRect(ctx, 0, 0, size, size, 1);
            ctx.LineWidth = 2.0;
            ctx.Stroke();

            _slotBgTextureId = _capi.Gui.LoadCairoTexture(surface, true);
        }

        private void GenerateSlotHighlight()
        {
            int size = SlotSize + 4;
            using var surface = new ImageSurface(Format.Argb32, size, size);
            using var ctx = new Context(surface);

            ctx.SetSourceRGBA(0.384, 0.773, 0.859, 1.0);
            RoundRect(ctx, 0, 0, size, size, 2);
            ctx.LineWidth = 6.0;
            ctx.Stroke();

            ctx.SetSourceRGBA(0.384, 0.773, 0.859, 0.6);
            RoundRect(ctx, 2, 2, size - 4, size - 4, 1);
            ctx.LineWidth = 2.0;
            ctx.Stroke();

            _slotHighlightTextureId = _capi.Gui.LoadCairoTexture(surface, true);
        }

        private static void RoundRect(Context ctx, double x, double y, double w, double h, double r)
        {
            ctx.NewPath();
            ctx.Arc(x + r,     y + r,     r, Math.PI,      -Math.PI / 2);
            ctx.Arc(x + w - r, y + r,     r, -Math.PI / 2, 0);
            ctx.Arc(x + w - r, y + h - r, r, 0,             Math.PI / 2);
            ctx.Arc(x + r,     y + h - r, r, Math.PI / 2,   Math.PI);
            ctx.ClosePath();
        }

        public void DrawSlotBackground(Vector2 pos, ImDrawListPtr dl) =>
            dl.AddImage((IntPtr)_slotBgTextureId, pos, pos + new Vector2(SlotSize, SlotSize));

        public void DrawSlotHighlight(Vector2 pos, ImDrawListPtr dl) =>
            dl.AddImage((IntPtr)_slotHighlightTextureId,
                pos - new Vector2(2, 2), pos + new Vector2(SlotSize + 2, SlotSize + 2));

        public void Dispose()
        {
            if (_slotBgTextureId     > 0) _capi.Render.GLDeleteTexture(_slotBgTextureId);
            if (_slotHighlightTextureId > 0) _capi.Render.GLDeleteTexture(_slotHighlightTextureId);
        }
    }
}
