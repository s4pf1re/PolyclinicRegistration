using System.Drawing;

namespace PolyclinicRegistration.Views;

internal static class UiTheme
{
    public static readonly Color Navy = Color.FromArgb(27, 51, 79);
    public static readonly Color Blue = Color.FromArgb(36, 112, 185);
    public static readonly Color PaleBlue = Color.FromArgb(232, 243, 253);
    public static readonly Color Background = Color.FromArgb(244, 247, 250);
    public static readonly Color Text = Color.FromArgb(35, 48, 62);
    public static readonly Color Muted = Color.FromArgb(104, 119, 133);
    public static readonly Color Line = Color.FromArgb(220, 228, 235);

    public static Label Label(string text, int x, int y, int width,
        float size = 10, bool bold = false, Color? color = null)
    {
        return new Label
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, Math.Max(28, (int)Math.Ceiling(size * 2.2))),
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = color ?? Text,
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };
    }

    public static Button Button(string text, Color background)
    {
        var button = new Button
        {
            Text = text,
            BackColor = background,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    public static void StyleInput(Control control)
    {
        control.Font = new Font("Segoe UI", 11);
        control.ForeColor = Text;
        control.BackColor = Color.White;
    }
}
