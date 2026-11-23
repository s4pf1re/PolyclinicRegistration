using System.Drawing;

namespace PolyclinicRegistration.Views;

public class StaffLoginForm : Form
{
    private readonly TextBox _password = new();

    public StaffLoginForm()
    {
        Text = "Вход для сотрудников";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(470, 280);
        BackColor = UiTheme.Background;
        Font = new Font("Segoe UI", 10);

        Controls.Add(UiTheme.Label("Вход для сотрудников", 25, 24, 420, 16, true));
        Controls.Add(UiTheme.Label("Введите пароль для просмотра базы данных.",
            25, 68, 420, 9, false, UiTheme.Muted));
        Controls.Add(UiTheme.Label("Пароль", 25, 118, 420));
        _password.Location = new Point(25, 150);
        _password.Size = new Size(420, 34);
        _password.UseSystemPasswordChar = true;
        _password.MaxLength = 16;
        UiTheme.StyleInput(_password);
        Controls.Add(_password);

        var enter = UiTheme.Button("Войти", UiTheme.Blue);
        enter.Location = new Point(25, 213);
        enter.Size = new Size(200, 40);
        enter.Click += CheckPassword;
        Controls.Add(enter);
        var cancel = UiTheme.Button("Отмена", UiTheme.Navy);
        cancel.Location = new Point(245, 213);
        cancel.Size = new Size(200, 40);
        cancel.Click += (_, _) => Close();
        Controls.Add(cancel);
        AcceptButton = enter;
        CancelButton = cancel;
    }

    private void CheckPassword(object? sender, EventArgs e)
    {
        if (_password.Text == "111")
        {
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            MessageBox.Show(this, "Неверный пароль.", "Доступ закрыт",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _password.Clear();
            _password.Focus();
        }
    }
}
