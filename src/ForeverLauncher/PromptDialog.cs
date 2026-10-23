using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ForeverLauncher;

internal sealed class PromptDialog : Window
{
	private readonly List<TextBox> boxes = new List<TextBox>();

	public string[] Values { get; private set; }

	private PromptDialog(Window owner, string title, string[] labels, string[] defaults)
	{
		Owner = owner;
		Title = title;
		Width = 380.0;
		SizeToContent = SizeToContent.Height;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		ResizeMode = ResizeMode.NoResize;
		Background = new SolidColorBrush(Color.FromRgb(20, 20, 28));
		Foreground = new SolidColorBrush(Color.FromRgb(237, 230, 214));
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(18.0)
		};
		for (int i = 0; i < labels.Length; i++)
		{
			stackPanel.Children.Add(new TextBlock
			{
				Text = labels[i],
				FontSize = 12.0,
				Foreground = new SolidColorBrush(Color.FromRgb(156, 148, 128)),
				Margin = new Thickness(0.0, (i != 0) ? 10 : 0, 0.0, 4.0)
			});
			TextBox textBox = ServerConsoleWindow.Box();
			textBox.Text = ((defaults != null && i < defaults.Length) ? defaults[i] : "");
			boxes.Add(textBox);
			stackPanel.Children.Add(textBox);
		}
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 16.0, 0.0, 0.0)
		};
		Button button = new Button
		{
			Content = L.Get("con.ok"),
			Style = ServerConsoleWindow.PillStyle,
			IsDefault = true,
			Margin = new Thickness(0.0, 0.0, 8.0, 0.0)
		};
		Button element = new Button
		{
			Content = L.Get("con.cancel"),
			Style = ServerConsoleWindow.PillStyle,
			IsCancel = true
		};
		button.Click += (object s, RoutedEventArgs e) =>
		{
			Values = boxes.Select((TextBox b) => b.Text.Trim()).ToArray();
			if (!Values.Any((string v) => v.Length == 0))
			{
				DialogResult = true;
			}
		};
		stackPanel2.Children.Add(button);
		stackPanel2.Children.Add(element);
		stackPanel.Children.Add(stackPanel2);
		Content = stackPanel;
		Loaded += (object s, RoutedEventArgs e) =>
		{
			if (boxes.Count > 0)
			{
				boxes[0].Focus();
				boxes[0].SelectAll();
			}
		};
	}

	public static string[] Ask(Window owner, string title, string[] labels, string[] defaults = null)
	{
		PromptDialog promptDialog = new PromptDialog(owner, title, labels, defaults);
		if (promptDialog.ShowDialog() != true)
		{
			return null;
		}
		return promptDialog.Values;
	}
}
