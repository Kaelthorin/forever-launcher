using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ForeverLauncher;

public sealed class LocalServerView
{
	private readonly Window window;

	private readonly System.Windows.Controls.Panel host;

	private readonly LocalServer local = new LocalServer();

	private readonly Dictionary<ServerProgram, Ellipse> dots = new Dictionary<ServerProgram, Ellipse>();

	private readonly Dictionary<ServerProgram, TextBlock> states = new Dictionary<ServerProgram, TextBlock>();

	private readonly List<System.Windows.Controls.Button> actionButtons = new List<System.Windows.Controls.Button>();

	private TextBlock header;

	private TextBlock binText;

	private TextBlock message;

	private TextBlock setupState;

	private System.Windows.Controls.TextBox mysqlExeBox;

	private System.Windows.Controls.TextBox mysqlArgsBox;

	private string serverName;

	private bool busy;

	private static readonly Color Green = Color.FromRgb(92, 214, 122);

	private static readonly Color Red = Color.FromRgb(224, 90, 78);

	private static readonly Color Amber = Color.FromRgb(240, 178, 62);

	private static readonly Color Grey = Color.FromRgb(138, 138, 138);

	public LocalServerView(Window window, System.Windows.Controls.Panel host)
	{
		LocalServerView localServerView = this;
		this.window = window;
		this.host = host;
		Build();
		DispatcherTimer dispatcherTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(2.0)
		};
		EventHandler value = (object s, EventArgs e) =>
		{
			if (host.IsVisible)
			{
				localServerView.RefreshStates();
			}
		};
		dispatcherTimer.Tick += value;
		dispatcherTimer.Start();
	}

	public void SetServer(string name)
	{
		serverName = name ?? "";
		local.Config = LocalConfigStore.Load(serverName);
		host.Children.Clear();
		dots.Clear();
		states.Clear();
		actionButtons.Clear();
		Build();
		header.Text = L.Get("local.title", (serverName.Length > 0) ? serverName : "-");
		ShowConfig();
		RefreshStates();
	}

	public void ApplyLanguage()
	{
		SetServer(serverName);
	}

	private void Build()
	{
		Grid grid = new Grid();
		grid.Margin = new Thickness(0.0, 4.0, 0.0, 0.0);
		Grid grid2 = grid;
		for (int i = 0; i < 3; i++)
		{
			grid2.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
		}
		grid2.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		header = new TextBlock
		{
			FontSize = 22.0,
			FontWeight = FontWeights.SemiBold,
			Foreground = (Brush)window.FindResource("Gold")
		};
		grid2.Children.Add(header);
		TextBlock textBlock = new TextBlock();
		textBlock.Text = L.Get("local.help");
		textBlock.FontSize = 12.5;
		textBlock.Foreground = (Brush)window.FindResource("Dim");
		textBlock.TextWrapping = TextWrapping.Wrap;
		textBlock.Margin = new Thickness(0.0, 4.0, 0.0, 14.0);
		textBlock.MaxWidth = 860.0;
		textBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
		TextBlock element = textBlock;
		Grid.SetRow(element, 1);
		grid2.Children.Add(element);
		setupState = null;
		if (LocalConfigStore.RepackDir != null)
		{
			Border border = Card();
			border.Margin = new Thickness(0.0, 0.0, 0.0, 14.0);
			DockPanel dockPanel = new DockPanel();
			StackPanel stackPanel = new StackPanel();
			stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
			stackPanel.VerticalAlignment = VerticalAlignment.Center;
			StackPanel stackPanel2 = stackPanel;
			DockPanel.SetDock(stackPanel2, Dock.Right);
			System.Windows.Controls.Button button = Pill(L.Get("local.runsetup"));
			button.Click += (object s, RoutedEventArgs e) =>
			{
				RunRepackScript("Setup.bat");
			};
			System.Windows.Controls.Button button2 = Pill(L.Get("local.newaccount"));
			button2.Click += (object s, RoutedEventArgs e) =>
			{
				RunRepackScript("setup\\create-account.bat");
			};
			System.Windows.Controls.Button button3 = Pill(L.Get("local.clientupdate"));
			button3.Click += (object s, RoutedEventArgs e) =>
			{
				RunRepackScript("setup\\add-client-build.bat");
			};
			button3.ToolTip = L.Get("local.clientupdatetip");
			stackPanel2.Children.Add(button);
			stackPanel2.Children.Add(button2);
			stackPanel2.Children.Add(button3);
			dockPanel.Children.Add(stackPanel2);
			StackPanel stackPanel3 = new StackPanel();
			stackPanel3.Margin = new Thickness(0.0, 0.0, 14.0, 0.0);
			StackPanel stackPanel4 = stackPanel3;
			stackPanel4.Children.Add(new TextBlock
			{
				Text = L.Get("local.setuptitle"),
				FontSize = 15.0,
				FontWeight = FontWeights.SemiBold
			});
			stackPanel4.Children.Add(new TextBlock
			{
				Text = L.Get("local.setuphelp"),
				FontSize = 12.0,
				Foreground = (Brush)window.FindResource("Dim"),
				TextWrapping = TextWrapping.Wrap
			});
			setupState = new TextBlock
			{
				FontSize = 12.5,
				FontWeight = FontWeights.SemiBold,
				Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
			};
			stackPanel4.Children.Add(setupState);
			dockPanel.Children.Add(stackPanel4);
			border.Child = dockPanel;
			Grid.SetRow(border, 2);
			grid2.Children.Add(border);
		}
		Grid grid3 = new Grid();
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(400.0)
		});
		Border border2 = Card();
		StackPanel stackPanel5 = new StackPanel();
		stackPanel5.Children.Add(Label(L.Get("local.bindir")));
		DockPanel dockPanel2 = new DockPanel();
		dockPanel2.Margin = new Thickness(0.0, 4.0, 0.0, 12.0);
		DockPanel dockPanel3 = dockPanel2;
		System.Windows.Controls.Button button4 = Link(L.Get("local.browse"));
		button4.Click += (object s, RoutedEventArgs e) =>
		{
			PickBinDir();
		};
		DockPanel.SetDock(button4, Dock.Right);
		dockPanel3.Children.Add(button4);
		binText = new TextBlock
		{
			FontSize = 12.5,
			TextTrimming = TextTrimming.CharacterEllipsis,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 10.0, 0.0)
		};
		dockPanel3.Children.Add(binText);
		stackPanel5.Children.Add(dockPanel3);
		stackPanel5.Children.Add(Label(L.Get("local.mysqlexe")));
		DockPanel dockPanel4 = new DockPanel();
		dockPanel4.Margin = new Thickness(0.0, 4.0, 0.0, 10.0);
		DockPanel dockPanel5 = dockPanel4;
		System.Windows.Controls.Button button5 = Link(L.Get("local.browse"));
		button5.Click += (object s, RoutedEventArgs e) =>
		{
			PickMysql();
		};
		DockPanel.SetDock(button5, Dock.Right);
		dockPanel5.Children.Add(button5);
		mysqlExeBox = Box();
		mysqlExeBox.Margin = new Thickness(0.0, 0.0, 10.0, 0.0);
		dockPanel5.Children.Add(mysqlExeBox);
		stackPanel5.Children.Add(dockPanel5);
		stackPanel5.Children.Add(Label(L.Get("local.mysqlargs")));
		mysqlArgsBox = Box();
		mysqlArgsBox.Margin = new Thickness(0.0, 4.0, 0.0, 12.0);
		stackPanel5.Children.Add(mysqlArgsBox);
		StackPanel stackPanel6 = new StackPanel();
		stackPanel6.Orientation = System.Windows.Controls.Orientation.Horizontal;
		StackPanel stackPanel7 = stackPanel6;
		System.Windows.Controls.Button button6 = Pill(L.Get("local.save"));
		button6.Click += (object s, RoutedEventArgs e) =>
		{
			SaveConfig();
		};
		stackPanel7.Children.Add(button6);
		System.Windows.Controls.Button button7 = Pill(L.Get("local.openfolder"));
		button7.Click += (object s, RoutedEventArgs e) =>
		{
			OpenBinDir();
		};
		stackPanel7.Children.Add(button7);
		System.Windows.Controls.Button button8 = Pill(L.Get("local.clearlogs"));
		button8.Click += (object s, RoutedEventArgs e) =>
		{
			ClearLogs();
		};
		button8.ToolTip = L.Get("local.clearlogstip");
		stackPanel7.Children.Add(button8);
		stackPanel5.Children.Add(stackPanel7);
		border2.Child = new ScrollViewer
		{
			Content = stackPanel5,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};
		border2.VerticalAlignment = VerticalAlignment.Top;
		grid3.Children.Add(border2);
		Border border3 = Card();
		Grid.SetColumn(border3, 1);
		border3.Margin = new Thickness(18.0, 0.0, 0.0, 0.0);
		border3.VerticalAlignment = VerticalAlignment.Top;
		Grid grid4 = new Grid();
		grid4.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		grid4.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid4.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		StackPanel stackPanel8 = new StackPanel();
		foreach (ServerProgram item in local.All)
		{
			stackPanel8.Children.Add(ProgramRow(item));
		}
		grid4.Children.Add(new ScrollViewer
		{
			Content = stackPanel8,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		});
		StackPanel stackPanel9 = new StackPanel();
		stackPanel9.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel9.Margin = new Thickness(0.0, 12.0, 0.0, 0.0);
		StackPanel stackPanel10 = stackPanel9;
		System.Windows.Controls.Button button9 = Pill(L.Get("local.startall"));
		button9.Click += (object s, RoutedEventArgs e) =>
		{
			StartAll();
		};
		System.Windows.Controls.Button button10 = Pill(L.Get("local.stopall"));
		button10.Click += (object s, RoutedEventArgs e) =>
		{
			StopAll();
		};
		actionButtons.Add(button9);
		actionButtons.Add(button10);
		stackPanel10.Children.Add(button9);
		stackPanel10.Children.Add(button10);
		Grid.SetRow(stackPanel10, 1);
		grid4.Children.Add(stackPanel10);
		message = new TextBlock
		{
			FontSize = 12.5,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0),
			Foreground = (Brush)window.FindResource("Dim")
		};
		Grid.SetRow(message, 2);
		grid4.Children.Add(message);
		border3.Child = grid4;
		grid3.Children.Add(border3);
		Grid.SetRow(grid3, 3);
		grid2.Children.Add(grid3);
		host.Children.Add(grid2);
	}

	private UIElement ProgramRow(ServerProgram prog)
	{
		DockPanel dockPanel = new DockPanel();
		dockPanel.Margin = new Thickness(0.0, 0.0, 0.0, 10.0);
		DockPanel dockPanel2 = dockPanel;
		Ellipse ellipse = new Ellipse();
		ellipse.Width = 10.0;
		ellipse.Height = 10.0;
		ellipse.Fill = new SolidColorBrush(Grey);
		ellipse.VerticalAlignment = VerticalAlignment.Center;
		ellipse.Margin = new Thickness(0.0, 0.0, 10.0, 0.0);
		Ellipse ellipse2 = ellipse;
		dots[prog] = ellipse2;
		dockPanel2.Children.Add(ellipse2);
		StackPanel stackPanel = new StackPanel();
		stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
		StackPanel stackPanel2 = stackPanel;
		DockPanel.SetDock(stackPanel2, Dock.Right);
		if (local.WorldRealms.TryGetValue(prog, out var consoleRealm))
		{
			System.Windows.Controls.Button button = Link(L.Get("local.console"));
			button.Margin = new Thickness(0.0, 0.0, 14.0, 0.0);
			button.Click += (object s, RoutedEventArgs e) =>
			{
				if (string.IsNullOrEmpty(local.Config.BinDir))
				{
					Say(MsgKind.Warn, L.Get("local.notconfigured"));
				}
				else
				{
					ServerConsoleWindow.Show(window, local.Config, consoleRealm, () =>
					{
						LocalConfigStore.Save(serverName, local.Config);
					});
				}
			};
			stackPanel2.Children.Add(button);
		}
		System.Windows.Controls.Button button2 = Link(L.Get("local.start"));
		button2.Click += (object s, RoutedEventArgs e) =>
		{
			Run(() =>
			{
				StartOne(prog);
			});
		};
		System.Windows.Controls.Button button3 = Link(L.Get("local.stop"));
		button3.Click += (object s, RoutedEventArgs e) =>
		{
			Run(() =>
			{
				StopOne(prog);
			});
		};
		button2.Margin = new Thickness(0.0, 0.0, 14.0, 0.0);
		actionButtons.Add(button2);
		actionButtons.Add(button3);
		stackPanel2.Children.Add(button2);
		stackPanel2.Children.Add(button3);
		dockPanel2.Children.Add(stackPanel2);
		if (local.WorldRealms.TryGetValue(prog, out var realm))
		{
			System.Windows.Controls.CheckBox include = new System.Windows.Controls.CheckBox
			{
				IsChecked = realm.Enabled,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0.0, 0.0, 8.0, 0.0),
				ToolTip = L.Get("local.realmenabled")
			};
			include.Click += (object s, RoutedEventArgs e) =>
			{
				realm.Enabled = include.IsChecked == true;
				try
				{
					LocalConfigStore.Save(serverName, local.Config);
				}
				catch (Exception ex)
				{
					Say(MsgKind.Error, ex.Message);
				}
			};
			DockPanel.SetDock(include, Dock.Left);
			dockPanel2.Children.Add(include);
		}
		StackPanel stackPanel3 = new StackPanel();
		stackPanel3.Children.Add(new TextBlock
		{
			Text = prog.Name,
			FontSize = 15.0,
			FontWeight = FontWeights.SemiBold
		});
		TextBlock textBlock = new TextBlock();
		textBlock.FontSize = 12.0;
		textBlock.Foreground = (Brush)window.FindResource("Dim");
		TextBlock textBlock2 = textBlock;
		states[prog] = textBlock2;
		stackPanel3.Children.Add(textBlock2);
		dockPanel2.Children.Add(stackPanel3);
		return dockPanel2;
	}

	private Border Card()
	{
		Border border = new Border();
		border.CornerRadius = new CornerRadius(8.0);
		border.Padding = new Thickness(20.0, 16.0, 20.0, 16.0);
		border.Background = (Brush)window.FindResource("Card");
		border.BorderBrush = (Brush)window.FindResource("CardLine");
		border.BorderThickness = new Thickness(1.0);
		return border;
	}

	private TextBlock Label(string text)
	{
		TextBlock textBlock = new TextBlock();
		textBlock.Text = text;
		textBlock.FontSize = 12.0;
		textBlock.Foreground = (Brush)window.FindResource("Dim");
		return textBlock;
	}

	private System.Windows.Controls.Button Link(string text)
	{
		System.Windows.Controls.Button button = new System.Windows.Controls.Button();
		button.Content = text;
		button.Style = (Style)window.FindResource("LinkButton");
		button.VerticalAlignment = VerticalAlignment.Center;
		return button;
	}

	private System.Windows.Controls.Button Pill(string text)
	{
		System.Windows.Controls.Button button = new System.Windows.Controls.Button();
		button.Content = text;
		button.Style = (Style)window.FindResource("PillButton");
		return button;
	}

	private static System.Windows.Controls.TextBox Box()
	{
		System.Windows.Controls.TextBox textBox = new System.Windows.Controls.TextBox();
		textBox.FontSize = 12.5;
		textBox.Padding = new Thickness(6.0, 4.0, 6.0, 4.0);
		textBox.Background = new SolidColorBrush(Color.FromArgb(85, 0, 0, 0));
		textBox.Foreground = new SolidColorBrush(Color.FromRgb(237, 230, 214));
		textBox.CaretBrush = Brushes.White;
		textBox.BorderBrush = new SolidColorBrush(Color.FromArgb(68, 232, 196, 106));
		return textBox;
	}

	private void ShowConfig()
	{
		binText.Text = (string.IsNullOrEmpty(local.Config.BinDir) ? L.Get("local.notset") : local.Config.BinDir);
		binText.ToolTip = local.Config.BinDir;
		mysqlExeBox.Text = local.Config.MysqlExe;
		mysqlArgsBox.Text = local.Config.MysqlArgs;
	}

	private void SaveConfig()
	{
		local.Config.MysqlExe = mysqlExeBox.Text.Trim().Trim('"');
		local.Config.MysqlArgs = mysqlArgsBox.Text.Trim();
		try
		{
			LocalConfigStore.Save(serverName, local.Config);
			Say(MsgKind.Good, L.Get("local.saved"));
		}
		catch (Exception ex)
		{
			Say(MsgKind.Error, ex.Message);
		}
		RefreshStates();
	}

	private void PickBinDir()
	{
		using FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
		folderBrowserDialog.Description = L.Get("local.bindlg");
		folderBrowserDialog.ShowNewFolderButton = false;
		if (Directory.Exists(local.Config.BinDir))
		{
			folderBrowserDialog.SelectedPath = local.Config.BinDir;
		}
		if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
		{
			string selectedPath = folderBrowserDialog.SelectedPath;
			if (!File.Exists(System.IO.Path.Combine(selectedPath, "worldserver.exe")) || !File.Exists(System.IO.Path.Combine(selectedPath, "bnetserver.exe")))
			{
				Say(MsgKind.Warn, L.Get("local.nobin"));
				return;
			}
			local.Config.BinDir = selectedPath;
			SaveConfig();
			ShowConfig();
		}
	}

	private void PickMysql()
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "mysqld.exe|mysqld.exe|*.exe|*.exe";
		openFileDialog.Title = "mysqld.exe";
		using OpenFileDialog openFileDialog2 = openFileDialog;
		if (File.Exists(mysqlExeBox.Text))
		{
			openFileDialog2.InitialDirectory = System.IO.Path.GetDirectoryName(mysqlExeBox.Text);
		}
		if (openFileDialog2.ShowDialog() == DialogResult.OK)
		{
			mysqlExeBox.Text = openFileDialog2.FileName;
		}
	}

	private void RunRepackScript(string relative)
	{
		string text = System.IO.Path.Combine(LocalConfigStore.RepackDir ?? "", relative);
		if (!File.Exists(text))
		{
			Say(MsgKind.Error, text + " not found");
			return;
		}
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo(text);
			processStartInfo.UseShellExecute = true;
			processStartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(text);
			Process.Start(processStartInfo);
		}
		catch (Exception ex)
		{
			Say(MsgKind.Error, ex.Message);
		}
	}

	private void ClearLogs()
	{
		if (string.IsNullOrEmpty(local.Config.BinDir))
		{
			return;
		}
		try
		{
			Say(MsgKind.Good, LogCleaner.Run(local));
		}
		catch (Exception ex)
		{
			Say(MsgKind.Error, ex.Message);
		}
	}

	private void OpenBinDir()
	{
		if (Directory.Exists(local.Config.BinDir))
		{
			try
			{
				ProcessStartInfo processStartInfo = new ProcessStartInfo(local.Config.BinDir);
				processStartInfo.UseShellExecute = true;
				Process.Start(processStartInfo);
			}
			catch
			{
			}
		}
	}

	private void RefreshStates()
	{
		foreach (ServerProgram key2 in dots.Keys)
		{
			Color color;
			string key;
			if (!key2.Configured)
			{
				color = Grey;
				key = ((key2 == local.Mysql) ? "local.mysqlnone" : "local.notconfigured");
			}
			else if (key2.IsRunning)
			{
				color = Green;
				key = "local.running";
			}
			else
			{
				color = Red;
				key = "local.stopped";
			}
			dots[key2].Fill = new SolidColorBrush(color);
			states[key2].Text = L.Get(key);
		}
		foreach (System.Windows.Controls.Button actionButton in actionButtons)
		{
			actionButton.IsEnabled = !busy && serverName != null;
		}
		if (setupState != null)
		{
			bool repackSetupDone = LocalConfigStore.RepackSetupDone;
			setupState.Text = L.Get(repackSetupDone ? "local.setupdone" : "local.setupneeded");
			setupState.Foreground = new SolidColorBrush(repackSetupDone ? Green : Amber);
		}
	}

	private void StartOne(ServerProgram prog)
	{
		if (prog != local.Mysql && !LocalConfigStore.RepackSetupDone)
		{
			throw new Exception(L.Get("local.setupfirst"));
		}
		prog.Start();
		if (prog == local.Mysql)
		{
			WaitForMysql();
		}
	}

	private void StopOne(ServerProgram prog)
	{
		List<string> list = prog.Stop();
		if (list.Count > 0)
		{
			throw new Exception(string.Join(" ", list.ToArray()));
		}
	}

	private void StartAll()
	{
		Run(() =>
		{
			if (!LocalConfigStore.RepackSetupDone)
			{
				throw new Exception(L.Get("local.setupfirst"));
			}
			bool flag = true;
			foreach (ServerProgram item in local.StartOrder)
			{
				Post(L.Get("local.starting", item.Name));
				StartOne(item);
				if (item == local.Bnet)
				{
					Thread.Sleep(2000);
				}
				else if (local.WorldRealms.ContainsKey(item))
				{
					Thread.Sleep(flag ? 8000 : 2000);
					flag = false;
				}
			}
		});
	}

	private void StopAll()
	{
		Run(() =>
		{
			List<string> list = new List<string>();
			foreach (ServerProgram item in local.StopOrder)
			{
				if (item.Configured && item.IsRunning)
				{
					Post(L.Get("local.stopping", item.Name));
					list.AddRange(item.Stop());
				}
			}
			if (list.Count > 0)
			{
				throw new Exception(string.Join(" ", list.ToArray()));
			}
		});
	}

	private void WaitForMysql()
	{
		int mysqlPort = local.Config.MysqlPort;
		Post(L.Get("local.waitmysql", mysqlPort));
		for (int i = 0; i < 60; i++)
		{
			try
			{
				using TcpClient tcpClient = new TcpClient();
				if (tcpClient.ConnectAsync("127.0.0.1", mysqlPort).Wait(1000) && tcpClient.Connected)
				{
					return;
				}
			}
			catch
			{
			}
			Thread.Sleep(1000);
		}
		throw new Exception(L.Get("local.mysqltimeout", mysqlPort));
	}

	private async void Run(Action work)
	{
		if (!busy)
		{
			busy = true;
			RefreshStates();
			try
			{
				await Task.Run(work);
				Say(MsgKind.Good, L.Get("local.done"));
			}
			catch (Exception ex)
			{
				Say(MsgKind.Error, (ex.InnerException ?? ex).Message);
			}
			busy = false;
			RefreshStates();
		}
	}

	private void Post(string text)
	{
		window.Dispatcher.BeginInvoke((Action)(() =>
		{
			Say(MsgKind.Info, text);
		}));
	}

	private void Say(MsgKind kind, string text)
	{
		message.Text = text;
		message.Foreground = new SolidColorBrush(kind switch
		{
			MsgKind.Warn => Amber, 
			MsgKind.Good => Green, 
			MsgKind.Error => Red, 
			_ => Color.FromRgb(196, 187, 165), 
		});
	}
}
