using AircraftExplorer.Education;

namespace AircraftExplorer.Modes;

/// <summary>
/// A read-only dialog that displays an education topic's full content
/// in a screen-reader-accessible TextBox.
/// </summary>
public sealed class TopicReaderForm : Form
{
    public TopicReaderForm(EducationTopic topic)
        : this(topic.Title, $"{topic.Title}\r\n{topic.Category}\r\n\r\n{topic.Content}") { }

    public TopicReaderForm(string title, string content)
    {
        Text = title;
        Size = new Size(700, 500);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(30, 30, 30);
        KeyPreview = true;
        MinimizeBox = false;
        MaximizeBox = true;
        ShowIcon = false;
        ShowInTaskbar = false;

        var textBox = new TextBox
        {
            Text = content,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12f),
            WordWrap = true,
            AccessibleName = title,
            AccessibleRole = AccessibleRole.Text,
            TabStop = true
        };

        Controls.Add(textBox);

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                Close();
            }
        };

        Shown += (_, _) =>
        {
            textBox.Focus();
            textBox.SelectionStart = 0;
            textBox.SelectionLength = 0;
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (Control c in Controls)
                c.Dispose();
        }
        base.Dispose(disposing);
    }
}
