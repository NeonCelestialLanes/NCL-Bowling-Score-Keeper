using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Utility;

namespace FFXIVBowling;

/// <summary>
/// Palette from Neon Celestial Lanes (https://neoncelestial.carrd.co/).
/// Carrd logo on the left, neon wordmark in the middle, spinning ball on the right.
/// Luckiest Guy from the venue site; sparks are drawn so they always show.
/// </summary>
internal static class NeonTheme
{
    public static readonly Vector4 Cyan = new(0.10f, 1.00f, 0.92f, 1f);
    public static readonly Vector4 Ice = new(0.588f, 0.878f, 1.00f, 1f);
    public static readonly Vector4 White = new(1.00f, 1.00f, 1.00f, 1f);
    public static readonly Vector4 Magenta = new(1.00f, 0.16f, 0.74f, 1f);
    public static readonly Vector4 NeonPink = new(1.00f, 0.16f, 0.74f, 1f);
    public static readonly Vector4 Gold = new(1.00f, 0.97f, 0.18f, 1f);
    public static readonly Vector4 Purple = new(0.62f, 0.28f, 0.98f, 1f);
    public static readonly Vector4 Lavender = new(0.92f, 0.84f, 1.00f, 1f);
    public static readonly Vector4 Muted = new(0.78f, 0.70f, 0.90f, 1f);
    public static readonly Vector4 WindowBg = new(0.07f, 0.02f, 0.14f, 0.98f);
    public static readonly Vector4 ChildBg = new(0.12f, 0.04f, 0.22f, 0.55f);
    public static readonly Vector4 TitleBg = new(0.14f, 0.03f, 0.26f, 1f);
    public static readonly Vector4 TitleBgActive = new(0.26f, 0.06f, 0.42f, 1f);
    public static readonly Vector4 Button = new(0.12f, 0.05f, 0.24f, 0.98f);
    public static readonly Vector4 ButtonHovered = new(0.22f, 0.10f, 0.42f, 1f);
    public static readonly Vector4 ButtonActive = new(0.00f, 0.42f, 0.38f, 1f);
    public static readonly Vector4 Border = new(0.00f, 0.949f, 0.780f, 0.90f);
    public static readonly Vector4 Header = new(0.22f, 0.06f, 0.38f, 0.95f);
    public static readonly Vector4 TableRow = new(0.14f, 0.04f, 0.24f, 0.88f);
    public static readonly Vector4 CurrentFrame = new(0.85f, 0.18f, 0.62f, 0.48f);
    public static readonly Vector4 FrameBg = new(0.08f, 0.03f, 0.16f, 0.95f);

    private static readonly (float X, float Y, float R, Vector4 Color)[] Stars =
    {
        (0.05f, 0.10f, 3.6f, Gold),
        (0.14f, 0.06f, 2.0f, Magenta),
        (0.22f, 0.16f, 2.8f, Ice),
        (0.31f, 0.08f, 1.7f, Cyan),
        (0.41f, 0.13f, 3.4f, Magenta),
        (0.52f, 0.05f, 2.2f, Gold),
        (0.63f, 0.15f, 1.9f, Cyan),
        (0.74f, 0.07f, 3.0f, Magenta),
        (0.84f, 0.12f, 2.4f, Ice),
        (0.93f, 0.06f, 2.8f, Gold),
        (0.08f, 0.28f, 1.6f, Cyan),
        (0.19f, 0.34f, 2.5f, Magenta),
        (0.29f, 0.24f, 1.5f, Gold),
        (0.47f, 0.31f, 2.1f, Ice),
        (0.58f, 0.22f, 1.8f, Magenta),
        (0.69f, 0.36f, 2.6f, Cyan),
        (0.81f, 0.27f, 1.7f, Gold),
        (0.94f, 0.33f, 2.3f, Magenta),
        (0.04f, 0.48f, 2.0f, Ice),
        (0.16f, 0.54f, 1.5f, Cyan),
        (0.27f, 0.45f, 2.7f, Magenta),
        (0.38f, 0.58f, 1.8f, Gold),
        (0.51f, 0.49f, 2.2f, Cyan),
        (0.62f, 0.57f, 1.6f, Magenta),
        (0.76f, 0.46f, 2.9f, Ice),
        (0.88f, 0.53f, 1.9f, Gold),
        (0.07f, 0.68f, 2.4f, Magenta),
        (0.18f, 0.74f, 1.6f, Cyan),
        (0.33f, 0.66f, 2.1f, Gold),
        (0.44f, 0.78f, 1.7f, Ice),
        (0.55f, 0.69f, 2.8f, Magenta),
        (0.67f, 0.76f, 1.9f, Cyan),
        (0.79f, 0.64f, 2.3f, Gold),
        (0.91f, 0.72f, 1.5f, Magenta),
        (0.06f, 0.88f, 2.6f, Cyan),
        (0.21f, 0.92f, 1.8f, Magenta),
        (0.36f, 0.85f, 2.2f, Gold),
        (0.49f, 0.93f, 1.6f, Ice),
        (0.61f, 0.87f, 3.1f, Cyan),
        (0.73f, 0.94f, 2.0f, Magenta),
        (0.85f, 0.84f, 2.5f, Gold),
        (0.96f, 0.90f, 1.7f, Ice),
    };

    public static void Push()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 16f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 14f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 16f);
        ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, 12f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1.6f);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(12f, 10f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(18f, 16f));
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12f, 5f));
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(6f, 7f));

        ImGui.PushStyleColor(ImGuiCol.WindowBg, WindowBg);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ChildBg);
        ImGui.PushStyleColor(ImGuiCol.Border, Border);
        ImGui.PushStyleColor(ImGuiCol.TitleBg, TitleBg);
        ImGui.PushStyleColor(ImGuiCol.TitleBgActive, TitleBgActive);
        ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, TitleBg);
        ImGui.PushStyleColor(ImGuiCol.Text, Ice);
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, Muted);
        ImGui.PushStyleColor(ImGuiCol.Button, Button);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ButtonHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ButtonActive);
        ImGui.PushStyleColor(ImGuiCol.Header, Header);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ButtonHovered);
        ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, new Vector4(0.18f, 0.05f, 0.34f, 1f));
        ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, new Vector4(Cyan.X, Cyan.Y, Cyan.Z, 0.45f));
        ImGui.PushStyleColor(ImGuiCol.TableBorderLight, new Vector4(Magenta.X, Magenta.Y, Magenta.Z, 0.28f));
        ImGui.PushStyleColor(ImGuiCol.TableRowBg, TableRow);
        ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, new Vector4(0.20f, 0.07f, 0.34f, 0.72f));
        ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(Magenta.X, Magenta.Y, Magenta.Z, 0.45f));
        ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, new Vector4(0.06f, 0.01f, 0.12f, 0.85f));
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, Cyan);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, FrameBg);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.16f, 0.06f, 0.28f, 0.95f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.18f, 0.08f, 0.32f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(0.14f, 0.04f, 0.26f, 0.96f));
        ImGui.PushStyleColor(ImGuiCol.TabHovered, Mix(Cyan, Ice, 0.25f));
        ImGui.PushStyleColor(ImGuiCol.TabActive, Mix(Cyan, new Vector4(0.05f, 0.12f, 0.20f, 1f), 0.35f));
        ImGui.PushStyleColor(ImGuiCol.TabUnfocused, TitleBg);
        ImGui.PushStyleColor(ImGuiCol.TabUnfocusedActive, Header);
    }

    public static void Pop()
    {
        ImGui.PopStyleColor(29);
        ImGui.PopStyleVar(11);
    }

    public static void DrawBackdrop()
    {
        var dl = ImGui.GetWindowDrawList();
        var min = ImGui.GetWindowPos();
        var max = min + ImGui.GetWindowSize();
        var time = (float)ImGui.GetTime();

        dl.AddRectFilledMultiColor(
            min,
            max,
            Col(new Vector4(0.42f, 0.08f, 0.58f, 0.98f)),
            Col(new Vector4(0.02f, 0.42f, 0.52f, 0.96f)),
            Col(new Vector4(0.05f, 0.02f, 0.16f, 0.98f)),
            Col(new Vector4(0.22f, 0.05f, 0.46f, 0.97f)));
        dl.AddRectFilledMultiColor(
            min,
            max,
            Col(WithAlpha(Purple, 0.16f)),
            Col(WithAlpha(Cyan, 0.24f)),
            Col(WithAlpha(Gold, 0.08f)),
            Col(WithAlpha(Purple, 0.12f)));

        dl.AddRectFilled(
            min,
            min + new Vector2(max.X - min.X, 46f),
            Col(new Vector4(0.12f, 0.03f, 0.24f, 0.92f)),
            16f,
            ImDrawFlags.RoundCornersTop);

        for (var i = 6; i >= 1; i--)
        {
            var t = i / 6f;
            dl.AddRect(
                min + new Vector2(i * 0.7f),
                max - new Vector2(i * 0.7f),
                Col(WithAlpha(i % 2 == 0 ? Magenta : Cyan, 0.06f + (0.10f * (1f - t)))),
                16f,
                ImDrawFlags.None,
                1.8f);
        }

        dl.AddRect(min + new Vector2(1.5f), max - new Vector2(1.5f), Col(WithAlpha(Magenta, 0.35f)), 15f, ImDrawFlags.None, 2.4f);
        dl.AddRect(min + new Vector2(3.2f), max - new Vector2(3.2f), Col(Cyan), 14f, ImDrawFlags.None, 1.6f);

        var size = max - min;
        foreach (var star in Stars)
        {
            var pulse = 0.55f + (0.45f * MathF.Sin(time * 2.4f + star.X * 14f + star.Y * 9f));
            var pos = min + new Vector2(star.X * size.X, star.Y * size.Y);
            DrawSpark(dl, pos, star.R * pulse, star.Color);
        }

        for (var i = 0; i < 28; i++)
        {
            var fx = Fract((i * 0.6180339f) + 0.11f);
            var fy = Fract((i * 0.3819660f) + 0.19f);
            var twinkle = 0.25f + (0.75f * MathF.Sin(time * (1.6f + (i % 5) * 0.35f) + i));
            if (twinkle < 0.35f)
                continue;
            var dust = min + new Vector2(fx * size.X, fy * size.Y);
            var hue = (i % 4) switch
            {
                0 => Cyan,
                1 => Magenta,
                2 => Gold,
                _ => White,
            };
            dl.AddCircleFilled(dust, 1.15f * twinkle, Col(WithAlpha(hue, 0.55f * twinkle)));
        }
    }

    public static void BrandHeader(IDalamudTextureWrap? logo, IFontHandle? labelFont = null, string? hours = null)
    {
        var start = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        const float height = 118f;
        var end = start + new Vector2(width, height);
        var dl = ImGui.GetWindowDrawList();
        var time = (float)ImGui.GetTime();

        dl.AddRectFilled(start, end, Col(new Vector4(0.08f, 0.02f, 0.16f, 0.94f)), 14f);
        dl.AddRectFilledMultiColor(
            start,
            end,
            Col(WithAlpha(Magenta, 0.42f)),
            Col(WithAlpha(Cyan, 0.28f)),
            Col(WithAlpha(Gold, 0.10f)),
            Col(WithAlpha(new Vector4(0.18f, 0.04f, 0.32f, 1f), 0.55f)));

        dl.AddRectFilled(
            start + new Vector2(10f, height - 18f),
            end - new Vector2(10f, 8f),
            Col(new Vector4(0.05f, 0.02f, 0.10f, 0.55f)),
            8f);
        dl.AddLine(
            start + new Vector2(18f, height - 13f),
            end - new Vector2(18f, 13f),
            Col(WithAlpha(Gold, 0.55f + (0.25f * MathF.Sin(time * 2f)))),
            2.2f);
        dl.AddLine(
            start + new Vector2(28f, height - 9f),
            end - new Vector2(28f, 9f),
            Col(WithAlpha(Cyan, 0.35f)),
            1.2f);

        dl.AddRect(start, end, Col(Cyan), 14f, ImDrawFlags.None, 1.9f);
        dl.AddRect(start + new Vector2(2.6f, 2.6f), end - new Vector2(2.6f, 2.6f), Col(WithAlpha(Magenta, 0.45f)), 12f, ImDrawFlags.None, 1.3f);

        var logoH = 92f;
        var logoW = 142f;
        if (logo is not null && logo.Handle != nint.Zero && logo.Width > 0 && logo.Height > 0)
            logoW = logoH * logo.Width / logo.Height;

        var logoX = start.X + ((width - logoW) * 0.5f);
        var logoY = start.Y + ((height - logoH) * 0.5f) - 6f;

        const string name = "NEON CELESTIAL LANES";
        const string sub = "GOBLIN  ·  LB W17 P21  ·  OPEN BOWL";
        var hoursLine = string.IsNullOrWhiteSpace(hours) ? string.Empty : hours;
        Vector2 hoursMin = default;
        Vector2 hoursMax = default;
        void DrawHeaderWords()
        {
            var nameSize = ImGui.CalcTextSize(name);
            var subSize = ImGui.CalcTextSize(sub);
            var hoursSize = hoursLine.Length == 0 ? Vector2.Zero : ImGui.CalcTextSize(hoursLine);
            var gap = 2f;
            var blockH = nameSize.Y + gap + subSize.Y + (hoursSize.Y > 0 ? gap + hoursSize.Y : 0f);
            var textX = start.X + 16f;
            var textY = start.Y + ((height - blockH) * 0.5f) - 4f;
            dl.AddText(new Vector2(textX, textY), Col(Cyan), name);
            dl.AddText(new Vector2(textX, textY + nameSize.Y + gap), Col(Magenta), sub);
            if (hoursSize.Y <= 0)
                return;
            var hoursPos = new Vector2(textX, textY + nameSize.Y + gap + subSize.Y + gap);
            hoursMin = hoursPos;
            hoursMax = hoursPos + hoursSize;
            dl.AddText(hoursPos, Col(Gold), hoursLine);
        }

        if (labelFont is not null)
        {
            using (labelFont.Push())
                DrawHeaderWords();
        }
        else
        {
            DrawHeaderWords();
        }

        if (hoursLine.Length > 0 && hoursMax.X > hoursMin.X)
        {
            ImGui.SetCursorScreenPos(hoursMin);
            if (ImGui.InvisibleButton("##ncl_hours", hoursMax - hoursMin))
                Util.OpenLink(NclHours.PageUrl);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Hours from FFXIV Venues");
        }

        if (logo is not null && logo.Handle != nint.Zero)
        {
            ImGui.SetCursorScreenPos(new Vector2(logoX, logoY));
            ImGui.Image(logo.Handle, new Vector2(logoW, logoH));
        }

        DrawBowlingBall(dl, new Vector2(end.X - 48f, start.Y + 42f), 16f, time);
        DrawSpark(dl, new Vector2(logoX + logoW - 8f, logoY + 10f), 5.5f, Gold);
        DrawSpark(dl, new Vector2(logoX + logoW + 8f, logoY + 4f), 3.2f, Ice);

        ImGui.SetCursorScreenPos(start);
        ImGui.Dummy(new Vector2(width, height + 8f));
    }

    public static int GlowTabBar(ReadOnlySpan<string> labels, int selected)
    {
        var n = labels.Length;
        if (n <= 0)
            return 0;

        selected = Math.Clamp(selected, 0, n - 1);
        var avail = ImGui.GetContentRegionAvail().X;
        const float height = 34f;
        const float inset = 4f;
        var row = ImGui.GetCursorScreenPos();
        var dl = ImGui.GetWindowDrawList();
        var trackMax = row + new Vector2(avail, height);
        var radius = height * 0.5f;

        dl.AddRectFilled(row + new Vector2(0f, 3f), trackMax + new Vector2(0f, 4f), Col(new Vector4(0f, 0f, 0f, 0.42f)), radius);
        dl.AddRectFilled(row, trackMax, Col(new Vector4(0.05f, 0.02f, 0.10f, 0.96f)), radius);
        dl.AddRect(row, trackMax, Col(new Vector4(1f, 1f, 1f, 0.06f)), radius, ImDrawFlags.None, 1f);

        var inner = avail - (inset * 2f);
        var tabW = inner / n;
        ImGui.SetCursorScreenPos(row + new Vector2(inset, inset));

        for (var i = 0; i < n; i++)
        {
            if (i > 0)
                ImGui.SameLine(0, 0);

            var text = labels[i].Trim();
            ImGui.PushID(i);
            var clicked = ImGui.InvisibleButton("##ncl_tab", new Vector2(tabW, height - (inset * 2f)));
            var hovered = ImGui.IsItemHovered();
            var held = ImGui.IsItemActive();
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            ImGui.PopID();

            var on = i == selected;
            var accent = TabAccent(i);
            if (on)
                DrawRaisedPill(dl, min, max, accent, filled: true, hovered, held, punch: 0.62f);
            else if (hovered)
                DrawRaisedPill(dl, min, max, accent, filled: true, hovered, held, punch: 0.22f);

            var textSize = ImGui.CalcTextSize(text);
            var textPos = new Vector2(min.X + ((tabW - textSize.X) * 0.5f), min.Y + (((height - (inset * 2f)) - textSize.Y) * 0.5f) - 1f);
            var ink = on || hovered ? White : WithAlpha(White, 0.78f);
            dl.AddText(textPos + new Vector2(0f, 1f), Col(new Vector4(0f, 0f, 0f, on ? 0.45f : 0.25f)), text);
            dl.AddText(textPos, Col(ink), text);

            if (clicked && !on)
                selected = i;
        }

        ImGui.SetCursorScreenPos(row);
        ImGui.Dummy(new Vector2(avail, height + 10f));
        return selected;
    }

    private static Vector4 TabAccent(int index) => index switch
    {
        0 => Cyan,
        1 => new Vector4(0.32f, 0.82f, 1.00f, 1f),
        2 => Gold,
        3 => Magenta,
        4 => new Vector4(0.78f, 0.48f, 1.00f, 1f),
        _ => Mix(Cyan, Magenta, 0.45f),
    };

    private static Vector4 Mix(Vector4 a, Vector4 b, float t) =>
        new(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t), a.Z + ((b.Z - a.Z) * t), a.W + ((b.W - a.W) * t));

    public static bool GlowButton(string label, Vector4 glow)
    {
        var text = label;
        var cut = label.IndexOf("##", StringComparison.Ordinal);
        if (cut >= 0)
            text = label[..cut];

        var size = ImGui.CalcTextSize(text) + new Vector2(28f, 16f);
        ImGui.PushID(label);
        var clicked = ImGui.InvisibleButton("##glow", size);
        var hovered = ImGui.IsItemHovered();
        var held = ImGui.IsItemActive();
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var dl = ImGui.GetWindowDrawList();
        DrawRaisedPill(dl, min, max, glow, filled: true, hovered, held, punch: 0.62f);

        var textSize = ImGui.CalcTextSize(text);
        var textPos = min + ((size - textSize) * 0.5f) + new Vector2(0f, held ? 1f : -0.5f);
        var dim = ImGui.GetStyle().Alpha;
        dl.AddText(textPos + new Vector2(0f, 1.2f), Col(new Vector4(0f, 0f, 0f, 0.45f * dim)), text);
        dl.AddText(textPos, Col(WithAlpha(White, dim)), text);
        ImGui.PopID();
        return clicked;
    }

    public static void DrawBallAndPin(IDalamudTextureWrap? art, float extraLift = 0f)
    {
        ImGui.SameLine(0, 12f);
        var remain = ImGui.GetContentRegionAvail().X;
        var lineH = ImGui.GetItemRectSize().Y;
        ImGui.Dummy(new Vector2(MathF.Max(1f, remain), lineH));
        if (remain < 48f || art is null || art.Handle == nint.Zero || art.Width <= 0 || art.Height <= 0)
            return;

        var min = ImGui.GetItemRectMin();
        const float pad = 6f;
        const float lift = 34f;
        const float boxH = 152f;
        var fitW = remain - (pad * 2f);
        var fitH = boxH - (pad * 2f);
        var scale = MathF.Min(fitW / art.Width, fitH / art.Height);
        var w = art.Width * scale;
        var h = art.Height * scale;
        var pos = new Vector2(min.X + ((remain - w) * 0.5f), min.Y - lift - extraLift);
        ImGui.GetWindowDrawList().AddImage(art.Handle, pos, pos + new Vector2(w, h));
    }

    public static void DrawOverlayArt(IDalamudTextureWrap? art, Vector2 topLeft, float width)
    {
        if (art is null || art.Handle == nint.Zero || art.Width <= 0 || art.Height <= 0 || width < 8f)
            return;

        var h = width * art.Height / art.Width;
        ImGui.GetWindowDrawList().AddImage(art.Handle, topLeft, topLeft + new Vector2(width, h));
    }

    public static bool Chip(string label, Vector4 glow)
    {
        var text = label;
        var cut = label.IndexOf("##", StringComparison.Ordinal);
        if (cut >= 0)
            text = label[..cut];

        var size = ImGui.CalcTextSize(text) + new Vector2(18f, 12f);
        ImGui.PushID(label);
        var clicked = ImGui.InvisibleButton("##chip", size);
        var hovered = ImGui.IsItemHovered();
        var held = ImGui.IsItemActive();
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        DrawRaisedPill(ImGui.GetWindowDrawList(), min, max, glow, filled: true, hovered, held, punch: 0.62f);
        var textSize = ImGui.CalcTextSize(text);
        var textPos = min + ((size - textSize) * 0.5f) + new Vector2(0f, held ? 1f : -0.5f);
        var chipDl = ImGui.GetWindowDrawList();
        chipDl.AddText(textPos + new Vector2(0f, 1.2f), Col(new Vector4(0f, 0f, 0f, 0.45f)), text);
        chipDl.AddText(textPos, Col(White), text);
        ImGui.PopID();
        return clicked;
    }

    public static bool NamePlate(string id, string label, Vector4 accent, float width)
    {
        const float height = 32f;
        ImGui.PushID(id);
        var clicked = ImGui.InvisibleButton("##name", new Vector2(MathF.Max(80f, width), height));
        var hovered = ImGui.IsItemHovered();
        var held = ImGui.IsItemActive();
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var dl = ImGui.GetWindowDrawList();
        DrawRaisedPill(dl, min, max, accent, filled: true, hovered, held, punch: 0.58f);
        var textSize = ImGui.CalcTextSize(label);
        var textPos = new Vector2(min.X + 14f, min.Y + ((height - textSize.Y) * 0.5f) + (held ? 1f : -0.5f));
        dl.AddText(textPos + new Vector2(0f, 1.2f), Col(new Vector4(0f, 0f, 0f, 0.4f)), label);
        dl.AddText(textPos, Col(Ice), label);
        ImGui.PopID();
        return clicked;
    }

    public static void BowlingSheet(System.Collections.Generic.IReadOnlyList<FrameView> frames, int gameTotal, bool complete)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        const float height = 74f;
        ImGui.Dummy(new Vector2(avail, height));
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var dl = ImGui.GetWindowDrawList();
        const float radius = 10f;

        dl.AddRectFilled(min + new Vector2(0f, 3f), max + new Vector2(0.5f, 4f), Col(new Vector4(0f, 0f, 0f, 0.40f)), radius);
        dl.AddRectFilled(min, max, Col(new Vector4(0.08f, 0.03f, 0.14f, 0.96f)), radius);
        dl.AddRect(min, max, Col(WithAlpha(Cyan, 0.55f)), radius, ImDrawFlags.None, 1.4f);
        dl.AddRect(min + new Vector2(1.5f), max - new Vector2(1.5f), Col(new Vector4(1f, 1f, 1f, 0.08f)), radius - 1f, ImDrawFlags.None, 1f);

        const float tenthMul = 1.42f;
        var unit = (max.X - min.X) / (9f + tenthMul + 1f);
        var x = min.X;
        for (var i = 0; i < frames.Count; i++)
        {
            var w = i == 9 ? unit * tenthMul : unit;
            DrawSheetFrame(dl, new Vector2(x, min.Y), new Vector2(x + w, max.Y), frames[i], complete);
            x += w;
            dl.AddLine(new Vector2(x, min.Y + 6f), new Vector2(x, max.Y - 6f), Col(WithAlpha(Cyan, 0.22f)), 1f);
        }

        DrawSheetTotal(dl, new Vector2(x, min.Y), max, gameTotal);
    }

    public static void MiniSheet(System.Collections.Generic.IReadOnlyList<FrameView> frames, int gameTotal, bool complete)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        const float height = 28f;
        ImGui.Dummy(new Vector2(avail, height));
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var dl = ImGui.GetWindowDrawList();
        const float radius = 8f;
        dl.AddRectFilled(min, max, Col(new Vector4(0.07f, 0.02f, 0.12f, 0.94f)), radius);
        dl.AddRect(min, max, Col(WithAlpha(Cyan, 0.45f)), radius, ImDrawFlags.None, 1.1f);

        const float tenthMul = 1.35f;
        var unit = (max.X - min.X) / (9f + tenthMul + 0.9f);
        var x = min.X;
        for (var i = 0; i < frames.Count; i++)
        {
            var w = i == 9 ? unit * tenthMul : unit;
            DrawMiniFrame(dl, new Vector2(x, min.Y), new Vector2(x + w, max.Y), frames[i], complete);
            x += w;
            dl.AddLine(new Vector2(x, min.Y + 4f), new Vector2(x, max.Y - 4f), Col(WithAlpha(Cyan, 0.18f)), 1f);
        }

        var score = gameTotal.ToString();
        var ss = ImGui.CalcTextSize(score);
        dl.AddText(new Vector2(x + (((max.X - x) - ss.X) * 0.5f), min.Y + ((height - ss.Y) * 0.5f)), Col(Gold), score);
    }

    private static void DrawMiniFrame(ImDrawListPtr dl, Vector2 min, Vector2 max, FrameView frame, bool complete)
    {
        if (frame.IsCurrent && !complete)
            dl.AddRectFilled(min, max, Col(WithAlpha(Magenta, 0.22f)), 0f);

        var marks = frame.Number == 10
            ? JoinMini(frame.Roll1, frame.Roll2, frame.Roll3)
            : frame.IsStrike ? "X" : JoinMini(frame.Roll1, frame.Roll2);
        if (marks.Length == 0)
            return;
        var ms = ImGui.CalcTextSize(marks);
        var ink = frame.IsStrike || frame.IsSpare ? Gold : Ice;
        dl.AddText(new Vector2(min.X + (((max.X - min.X) - ms.X) * 0.5f), min.Y + (((max.Y - min.Y) - ms.Y) * 0.5f) - 1f), Col(ink), marks);
    }

    private static string JoinMini(params string[] parts)
    {
        var filled = Array.FindAll(parts, static p => !string.IsNullOrEmpty(p));
        return filled.Length == 0 ? string.Empty : string.Join("", filled);
    }

    private static void DrawSheetFrame(ImDrawListPtr dl, Vector2 min, Vector2 max, FrameView frame, bool complete)
    {
        var current = frame.IsCurrent && !complete;
        if (current)
        {
            var pulse = 0.16f + (0.10f * MathF.Sin((float)ImGui.GetTime() * 3.1f));
            dl.AddRectFilled(min, max, Col(WithAlpha(Magenta, pulse)), 0f);
        }

        var header = frame.Number.ToString();
        dl.AddText(new Vector2(min.X + 6f, min.Y + 4f), Col(current ? Ice : Mix(Cyan, Muted, 0.25f)), header);

        var boxes = frame.Number == 10 ? 3 : 2;
        var box = 16f;
        var boxTop = min.Y + 4f;
        var labels = frame.Number == 10
            ? new[] { frame.Roll1, frame.Roll2, frame.Roll3 }
            : new[] { frame.Roll1, frame.Roll2 };
        if (frame.Number != 10 && frame.IsStrike)
            labels = new[] { string.Empty, "X" };

        for (var b = 0; b < boxes; b++)
        {
            var bx = max.X - 5f - ((boxes - b) * box);
            var bMin = new Vector2(bx, boxTop);
            var bMax = new Vector2(bx + box, boxTop + box);
            dl.AddRectFilled(bMin, bMax, Col(new Vector4(0.04f, 0.01f, 0.08f, 0.85f)), 2f);
            dl.AddRect(bMin, bMax, Col(WithAlpha(Ice, 0.35f)), 2f, ImDrawFlags.None, 1f);
            var mark = b < labels.Length ? labels[b] : string.Empty;
            if (string.IsNullOrEmpty(mark))
                continue;
            var ms = ImGui.CalcTextSize(mark);
            var ink = mark is "X" or "/" ? Gold : Ice;
            dl.AddText(new Vector2(bx + ((box - ms.X) * 0.5f), boxTop + ((box - ms.Y) * 0.5f) - 1f), Col(ink), mark);
        }

        dl.AddLine(new Vector2(min.X + 5f, min.Y + 24f), new Vector2(max.X - 5f, min.Y + 24f), Col(WithAlpha(Cyan, 0.18f)), 1f);

        var score = frame.CumulativeScore?.ToString() ?? string.Empty;
        if (score.Length == 0)
            return;
        var ss = ImGui.CalcTextSize(score);
        dl.AddText(
            new Vector2(min.X + (((max.X - min.X) - ss.X) * 0.5f), min.Y + 40f),
            Col(current ? Ice : Gold),
            score);
    }

    private static void DrawSheetTotal(ImDrawListPtr dl, Vector2 min, Vector2 max, int gameTotal)
    {
        var header = "TOT";
        var hs = ImGui.CalcTextSize(header);
        dl.AddText(new Vector2(min.X + (((max.X - min.X) - hs.X) * 0.5f), min.Y + 6f), Col(Gold), header);
        var score = gameTotal.ToString();
        var ss = ImGui.CalcTextSize(score);
        dl.AddText(new Vector2(min.X + (((max.X - min.X) - ss.X) * 0.5f), min.Y + 38f), Col(Gold), score);
    }

    public static void ClockPip(string label, bool done, bool current)
    {
        var textSize = ImGui.CalcTextSize(label);
        var size = new Vector2(MathF.Max(28f, textSize.X + 14f), 24f);
        ImGui.Dummy(size);
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var dl = ImGui.GetWindowDrawList();
        var accent = done ? Gold : current ? Magenta : Mix(Muted, Ice, 0.15f);
        var punch = current ? 0.55f : done ? 0.32f : 0.08f;
        DrawRaisedPill(dl, min, max, accent, filled: done || current, hovered: false, held: false, punch, radius: 8f);
        var pos = min + ((size - textSize) * 0.5f);
        dl.AddText(pos, Col(done || current ? Ice : Muted), label);
    }

    private static void DrawRaisedPill(ImDrawListPtr dl, Vector2 min, Vector2 max, Vector4 glow, bool filled, bool hovered, bool held, float punch = 0.18f, float radius = -1f)
    {
        var dim = ImGui.GetStyle().Alpha;
        var h = max.Y - min.Y;
        var r = radius < 0f ? h * 0.5f : radius;
        var press = held ? 1.5f : 0f;
        min += new Vector2(0f, press);
        max += new Vector2(0f, press);

        var shade = held ? 0.18f : 0.48f;
        if (!held)
            dl.AddRectFilled(min + new Vector2(0f, 3.5f), max + new Vector2(0.5f, 5f), Col(new Vector4(0f, 0f, 0f, shade * dim)), r);

        var lift = hovered && !held ? 0.34f : held ? 0.16f : 0.24f;
        var top = Mix(new Vector4(0.22f, 0.10f, 0.32f, 1f), glow, lift + 0.12f);
        var bot = Mix(new Vector4(0.07f, 0.02f, 0.12f, 1f), glow, lift * 0.45f);
        if (filled)
        {
            top = Mix(top, glow, hovered ? punch + 0.12f : punch);
            bot = Mix(bot, glow, punch * 0.72f);
        }

        dl.AddRectFilled(min, max, Col(WithAlpha(bot, dim)), r);
        dl.AddRectFilled(min, new Vector2(max.X, min.Y + (h * 0.55f)), Col(WithAlpha(top, dim)), r, ImDrawFlags.RoundCornersTop);
        dl.AddLine(
            new Vector2(min.X + r, min.Y + 2.2f),
            new Vector2(max.X - r, min.Y + 2.2f),
            Col(new Vector4(1f, 1f, 1f, (hovered ? 0.28f : 0.16f) * dim)),
            1.6f);
        dl.AddRect(min, max, Col(WithAlpha(Mix(glow, Ice, 0.2f), (hovered ? 0.95f : 0.72f) * dim)), r, ImDrawFlags.None, 1.25f);
        dl.AddRect(min + new Vector2(1.4f), max - new Vector2(1.4f), Col(new Vector4(1f, 1f, 1f, 0.10f * dim)), MathF.Max(2f, r - 1.4f), ImDrawFlags.None, 1f);
    }

    public static void GlowSeparator()
    {
        var start = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var dl = ImGui.GetWindowDrawList();
        dl.AddLine(start, start + new Vector2(width, 0), Col(WithAlpha(Magenta, 0.22f)), 4f);
        dl.AddLine(start, start + new Vector2(width, 0), Col(WithAlpha(Cyan, 0.75f)), 1.3f);
        ImGui.Dummy(new Vector2(width, 10f));
    }

    public static void StatusBar(string text)
    {
        var start = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        const float height = 32f;
        var dl = ImGui.GetWindowDrawList();
        var max = start + new Vector2(width, height);
        DrawRaisedPill(dl, start, max, NeonPink, filled: true, hovered: false, held: false, punch: 0.78f);
        var labelSize = ImGui.CalcTextSize(text);
        dl.AddText(start + new Vector2(16f, (height - labelSize.Y) * 0.5f), Col(new Vector4(1f, 0.92f, 0.98f, 1f)), text);
        ImGui.Dummy(new Vector2(width, height));
    }

    [ThreadStatic] private static Vector2 trayOrigin;
    [ThreadStatic] private static float trayWidth;
    [ThreadStatic] private static float laneCardRight;

    public static void BeginInsetBar()
    {
        ImGui.Dummy(new Vector2(0f, 4f));
        trayOrigin = ImGui.GetCursorScreenPos();
        trayWidth = ImGui.GetContentRegionAvail().X;
        var dl = ImGui.GetWindowDrawList();
        dl.ChannelsSplit(2);
        dl.ChannelsSetCurrent(1);
        ImGui.SetCursorScreenPos(trayOrigin + new Vector2(10f, 8f));
        ImGui.BeginGroup();
    }

    public static void EndInsetBar()
    {
        ImGui.EndGroup();
        var contentMin = ImGui.GetItemRectMin();
        var contentMax = ImGui.GetItemRectMax();
        var min = contentMin - new Vector2(10f, 8f);
        var max = contentMax + new Vector2(10f, 8f);
        var dl = ImGui.GetWindowDrawList();
        dl.ChannelsSetCurrent(0);
        var r = 16f;
        dl.AddRectFilled(min + new Vector2(0f, 3f), max + new Vector2(0f, 4f), Col(new Vector4(0f, 0f, 0f, 0.40f)), r);
        dl.AddRectFilled(min, max, Col(new Vector4(0.04f, 0.01f, 0.09f, 0.92f)), r);
        dl.AddRect(min, max, Col(new Vector4(1f, 1f, 1f, 0.07f)), r, ImDrawFlags.None, 1f);
        dl.ChannelsMerge();
        ImGui.SetCursorScreenPos(new Vector2(trayOrigin.X, max.Y + 6f));
        ImGui.Dummy(new Vector2(trayWidth, 1f));
    }



    public static void BeginLaneCard(ulong id, bool isTurn)
    {
        ImGui.PushID((int)id);
        var width = ImGui.GetContentRegionAvail().X;
        ImGui.BeginGroup();
        ImGui.Dummy(new Vector2(width, 1f));
        laneCardRight = ImGui.GetItemRectMax().X;
        ImGui.Dummy(new Vector2(0f, 4f));
    }

    public static void EndLaneCard(bool isTurn)
    {
        ImGui.Dummy(new Vector2(0f, 6f));
        ImGui.EndGroup();
        var min = ImGui.GetItemRectMin() - new Vector2(10f, 6f);
        var max = new Vector2(Math.Max(ImGui.GetItemRectMax().X, laneCardRight) + 10f, ImGui.GetItemRectMax().Y + 8f);
        var dl = ImGui.GetWindowDrawList();
        var accent = isTurn ? Magenta : Cyan;
        dl.AddRect(min - new Vector2(3f), max + new Vector2(3f), Col(WithAlpha(accent, 0.16f)), 16f, ImDrawFlags.None, 5.5f);
        dl.AddRect(min, max, Col(WithAlpha(accent, 0.95f)), 13f, ImDrawFlags.None, 1.9f);
        dl.AddRect(min + new Vector2(2.4f), max - new Vector2(2.4f), Col(WithAlpha(isTurn ? Gold : Magenta, 0.32f)), 11f, ImDrawFlags.None, 1.1f);
        dl.AddRectFilled(min, new Vector2(min.X + 5f, max.Y), Col(accent), 13f);
        if (isTurn)
            DrawSpark(dl, new Vector2(max.X - 18f, min.Y + 16f), 4.5f, Gold);
        ImGui.PopID();
        ImGui.Dummy(new Vector2(0f, 10f));
    }

    private static float DrawGlowText(ImDrawListPtr dl, Vector2 pos, string text, Vector4 fill, Vector4 glow)
    {
        var font = ImGui.GetFont();
        var size = ImGui.GetFontSize();
        var width = ImGui.CalcTextSize(text).X;
        foreach (var o in new[] { new Vector2(-1.5f, 0), new Vector2(1.5f, 0), new Vector2(0, -1.3f), new Vector2(0, 1.3f) })
            dl.AddText(font, size, pos + o, Col(WithAlpha(glow, 0.55f)), text);
        dl.AddText(font, size, pos, Col(fill), text);
        return width;
    }

    private static void DrawBowlingBall(ImDrawListPtr dl, Vector2 center, float radius, float time)
    {
        dl.AddCircleFilled(center, radius + 5f, Col(WithAlpha(Magenta, 0.18f)));
        dl.AddCircleFilled(center, radius, Col(new Vector4(0.16f, 0.05f, 0.28f, 1f)));
        dl.AddCircle(center, radius, Col(Cyan), 0, 1.6f);
        dl.AddCircleFilled(center + new Vector2(-5f, -4f), 2.1f, Col(WithAlpha(Ice, 0.85f)));
        dl.AddCircleFilled(center + new Vector2(-1f, -7f), 1.7f, Col(WithAlpha(Ice, 0.70f)));
        dl.AddCircleFilled(center + new Vector2(3f, -5f), 1.5f, Col(WithAlpha(Ice, 0.55f)));
        var spin = time * 1.4f;
        dl.AddCircleFilled(center + new Vector2(MathF.Cos(spin) * 5f, MathF.Sin(spin) * 5f), 1.4f, Col(Gold));
    }

    private static void DrawSpark(ImDrawListPtr dl, Vector2 pos, float radius, Vector4 color)
    {
        dl.AddCircleFilled(pos, radius * 0.95f, Col(WithAlpha(color, 0.20f)));
        dl.AddCircleFilled(pos, radius * 0.28f, Col(color));
        dl.AddLine(pos - new Vector2(radius, 0), pos + new Vector2(radius, 0), Col(color), 1.25f);
        dl.AddLine(pos - new Vector2(0, radius), pos + new Vector2(0, radius), Col(color), 1.25f);
        var d = radius * 0.64f;
        dl.AddLine(pos - new Vector2(d, d), pos + new Vector2(d, d), Col(WithAlpha(color, 0.75f)), 0.95f);
        dl.AddLine(pos - new Vector2(d, -d), pos + new Vector2(d, -d), Col(WithAlpha(color, 0.75f)), 0.95f);
    }

    private static float Fract(float value) => value - MathF.Floor(value);

    private static Vector4 WithAlpha(Vector4 color, float alpha) => new(color.X, color.Y, color.Z, alpha);

    private static uint Col(Vector4 color) => ImGui.GetColorU32(color);
}
