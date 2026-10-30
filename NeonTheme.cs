using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Textures.TextureWraps;

namespace FFXIVBowling;

/// <summary>
/// Palette from Neon Celestial Lanes (https://neoncelestial.carrd.co/).
/// Carrd logo on the left, neon wordmark in the middle, spinning ball on the right.
/// Luckiest Guy from the venue site; sparks are drawn so they always show.
/// </summary>
internal static class NeonTheme
{
    public static readonly Vector4 Cyan = new(0.00f, 0.949f, 0.780f, 1f);
    public static readonly Vector4 Ice = new(0.588f, 0.878f, 1.00f, 1f);
    public static readonly Vector4 Magenta = new(1.00f, 0.43f, 0.82f, 1f);
    public static readonly Vector4 Gold = new(1.00f, 0.90f, 0.22f, 1f);
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
        (0.06f, 0.12f, 3.4f, Gold),
        (0.18f, 0.07f, 2.2f, Magenta),
        (0.33f, 0.14f, 3.8f, Ice),
        (0.52f, 0.06f, 2.0f, Cyan),
        (0.71f, 0.11f, 3.1f, Magenta),
        (0.89f, 0.08f, 2.6f, Gold),
        (0.11f, 0.38f, 1.8f, Cyan),
        (0.91f, 0.44f, 2.1f, Magenta),
        (0.08f, 0.78f, 2.4f, Cyan),
        (0.41f, 0.90f, 1.8f, Magenta),
        (0.68f, 0.86f, 2.8f, Gold),
        (0.93f, 0.80f, 2.0f, Ice),
    };

    public static void Push()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 16f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 14f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 18f);
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
    }

    public static void Pop()
    {
        ImGui.PopStyleColor(24);
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
            Col(new Vector4(0.30f, 0.06f, 0.40f, 0.96f)),
            Col(new Vector4(0.04f, 0.14f, 0.26f, 0.94f)),
            Col(new Vector4(0.08f, 0.02f, 0.16f, 0.98f)),
            Col(new Vector4(0.20f, 0.03f, 0.26f, 0.96f)));

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

        foreach (var star in Stars)
        {
            var pulse = 0.72f + (0.28f * MathF.Sin(time * 2.4f + star.X * 12f));
            var pos = min + new Vector2(star.X * (max.X - min.X), star.Y * (max.Y - min.Y));
            DrawSpark(dl, pos, star.R * pulse, star.Color);
        }
    }

    public static void BrandHeader(IDalamudTextureWrap? logo, IFontHandle? labelFont = null)
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

        var logoH = 88f;
        var logoW = 157f;
        if (logo is not null && logo.Handle != nint.Zero && logo.Width > 0 && logo.Height > 0)
            logoW = logoH * logo.Width / logo.Height;

        var logoX = start.X + MathF.Max(12f, (width - logoW - 56f) * 0.5f);
        var logoY = start.Y + 6f;
        if (logo is not null && logo.Handle != nint.Zero)
        {
            ImGui.SetCursorScreenPos(new Vector2(logoX, logoY));
            ImGui.Image(logo.Handle, new Vector2(logoW, logoH));
        }

        DrawBowlingBall(dl, new Vector2(end.X - 48f, start.Y + 42f), 16f, time);
        DrawSpark(dl, new Vector2(logoX - 8f, logoY + 18f), 5.5f, Gold);
        DrawSpark(dl, new Vector2(logoX + logoW + 6f, logoY + 12f), 4.2f, Ice);

        const string sub = "GOBLIN  ·  W23 P49  ·  OPEN BOWL";
        var subSize = ImGui.CalcTextSize(sub);
        var subPos = new Vector2(start.X + ((width - subSize.X) * 0.5f), start.Y + height - 28f);
        if (labelFont is not null)
        {
            using (labelFont.Push())
                dl.AddText(subPos, Col(Magenta), sub);
        }
        else
        {
            dl.AddText(subPos, Col(Magenta), sub);
        }

        ImGui.SetCursorScreenPos(start);
        ImGui.Dummy(new Vector2(width, height + 8f));
    }

    public static bool GlowButton(string label, Vector4 glow)
    {
        var start = ImGui.GetCursorScreenPos();
        var text = label;
        var cut = label.IndexOf("##", StringComparison.Ordinal);
        if (cut >= 0)
            text = label[..cut];

        var size = ImGui.CalcTextSize(text) + (ImGui.GetStyle().FramePadding * 2f);
        var dl = ImGui.GetWindowDrawList();
        var max = start + size;
        dl.AddRectFilled(start - new Vector2(5f), max + new Vector2(5f), Col(WithAlpha(glow, 0.16f)), 22f);
        dl.AddRectFilled(start - new Vector2(2f), max + new Vector2(2f), Col(WithAlpha(glow, 0.22f)), 20f);
        dl.AddRect(start - new Vector2(1.2f), max + new Vector2(1.2f), Col(WithAlpha(glow, 0.95f)), 18f, ImDrawFlags.None, 1.7f);
        return ImGui.Button(label);
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
        const float height = 28f;
        const float bottomPad = 10f;
        var dl = ImGui.GetWindowDrawList();
        dl.AddRectFilled(start, start + new Vector2(width, height), Col(new Vector4(0.10f, 0.03f, 0.18f, 0.88f)), 10f);
        dl.AddRect(start, start + new Vector2(width, height), Col(WithAlpha(Magenta, 0.55f)), 10f, ImDrawFlags.None, 1.2f);
        dl.AddText(start + new Vector2(12f, 6f), Col(Magenta), text);
        ImGui.Dummy(new Vector2(width, height + bottomPad));
    }

    [ThreadStatic] private static float laneCardRight;

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

    private static Vector4 WithAlpha(Vector4 color, float alpha) => new(color.X, color.Y, color.Z, alpha);

    private static uint Col(Vector4 color) => ImGui.GetColorU32(color);
}
