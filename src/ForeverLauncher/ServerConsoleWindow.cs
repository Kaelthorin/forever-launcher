using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ForeverLauncher;

public sealed class ServerConsoleWindow : Window
{
	private static readonly Dictionary<string, ServerConsoleWindow> open = new Dictionary<string, ServerConsoleWindow>();

	public static Style PillStyle;

	private readonly Window owner;

	private readonly LocalConfig config;

	private readonly RealmServerConfig realm;

	private readonly Action saveConfig;

	private readonly string confPath;

	private readonly LogTail tail;

	private readonly List<string> lines = new List<string>();

	private readonly List<string> history = new List<string>();

	private int historyIndex;

	private string partial = "";

	private TextBox logBox;

	private TextBox filterBox;

	private TextBox input;

	private CheckBox pause;

	private TextBlock banner;

	private TextBlock status;

	private Button bannerButton;

	private RaClient ra;

	private readonly object raGate = new object();

	private bool sending;

	private Action bannerClick;

	private int RaPort
	{
		get
		{
			if (!int.TryParse(ConfFile.Get(confPath, "Ra.Port", "3443"), out var result))
			{
				return 3443;
			}
			return result;
		}
	}

	private bool RaEnabled => ConfFile.Get(confPath, "Ra.Enable", "0") == "1";

	public static void Show(Window owner, LocalConfig config, RealmServerConfig realm, Action saveConfig)
	{
		string key = (config.BinDir ?? "") + "|" + realm.Port;
		if (open.TryGetValue(key, out var w))
		{
			w.Activate();
			return;
		}
		w = new ServerConsoleWindow(owner, config, realm, saveConfig);
		open[key] = w;
		w.Closed += (object s, EventArgs e) =>
		{
			open.Remove(key);
			w.DisconnectRa();
		};
		w.Show();
	}

	private ServerConsoleWindow(Window owner, LocalConfig config, RealmServerConfig realm, Action saveConfig)
	{
		this.owner = owner;
		PillStyle = owner.TryFindResource("PillButton") as Style;
		this.config = config;
		this.realm = realm;
		this.saveConfig = saveConfig;
		confPath = Path.Combine(config.BinDir, string.IsNullOrEmpty(realm.Config) ? "worldserver.conf" : realm.Config);
		string text = ConfFile.Get(confPath, "LogsDir", "");
		tail = new LogTail(Path.Combine(Path.IsPathRooted(text) ? text : Path.Combine(config.BinDir, text), "Server.log"));
		Title = L.Get("con.title", realm.Name);
		Width = 1180.0;
		Height = 720.0;
		MinWidth = 760.0;
		MinHeight = 420.0;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		Background = new SolidColorBrush(Color.FromRgb(16, 16, 23));
		Foreground = new SolidColorBrush(Color.FromRgb(237, 230, 214));
		Icon = owner.Icon;
		Build();
		CheckSetup();
		DispatcherTimer timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(500.0)
		};
		timer.Tick += (object s, EventArgs e) =>
		{
			PollLog();
		};
		timer.Start();
		Closed += (object s, EventArgs e) =>
		{
			timer.Stop();
		};
		PollLog();
	}

	public static TextBox Box()
	{
		TextBox textBox = new TextBox();
		textBox.FontSize = 12.5;
		textBox.Padding = new Thickness(6.0, 4.0, 6.0, 4.0);
		textBox.Background = new SolidColorBrush(Color.FromArgb(85, 0, 0, 0));
		textBox.Foreground = new SolidColorBrush(Color.FromRgb(237, 230, 214));
		textBox.CaretBrush = Brushes.White;
		textBox.BorderBrush = new SolidColorBrush(Color.FromArgb(68, 232, 196, 106));
		return textBox;
	}

	private Button Pill(string text)
	{
		Button button = new Button();
		button.Content = text;
		button.Style = (Style)owner.FindResource("PillButton");
		button.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
		return button;
	}

	private Button ActionButton(string text)
	{
		Button button = new Button();
		button.Content = text;
		button.Style = (Style)owner.FindResource("PillButton");
		button.Margin = new Thickness(0.0, 0.0, 0.0, 6.0);
		button.HorizontalAlignment = HorizontalAlignment.Stretch;
		button.HorizontalContentAlignment = HorizontalAlignment.Left;
		return button;
	}

	private void Build()
	{
		Grid grid = new Grid();
		grid.Margin = new Thickness(14.0);
		Grid grid2 = grid;
		grid2.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid2.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid2.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		grid2.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid2.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(230.0)
		});
		DockPanel dockPanel = new DockPanel();
		dockPanel.Margin = new Thickness(0.0, 0.0, 0.0, 8.0);
		DockPanel dockPanel2 = dockPanel;
		TextBlock textBlock = new TextBlock();
		textBlock.Text = L.Get("con.title", realm.Name);
		textBlock.FontSize = 18.0;
		textBlock.FontWeight = FontWeights.SemiBold;
		textBlock.Foreground = (Brush)owner.FindResource("Gold");
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		textBlock.Margin = new Thickness(0.0, 0.0, 16.0, 0.0);
		TextBlock element = textBlock;
		DockPanel.SetDock(element, Dock.Left);
		dockPanel2.Children.Add(element);
		StackPanel stackPanel = new StackPanel();
		stackPanel.Orientation = Orientation.Horizontal;
		StackPanel stackPanel2 = stackPanel;
		DockPanel.SetDock(stackPanel2, Dock.Right);
		pause = new CheckBox
		{
			Content = L.Get("con.pause"),
			Foreground = Foreground,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 12.0, 0.0)
		};
		pause.Click += (object s, RoutedEventArgs e) =>
		{
			if (pause.IsChecked != true)
			{
				Render();
			}
		};
		stackPanel2.Children.Add(pause);
		Button button = Pill(L.Get("con.clearview"));
		button.Click += (object s, RoutedEventArgs e) =>
		{
			lines.Clear();
			Render();
		};
		stackPanel2.Children.Add(button);
		Button button2 = Pill(L.Get("con.openlogs"));
		button2.Click += (object s, RoutedEventArgs e) =>
		{
			try
			{
				Process.Start(new ProcessStartInfo(Path.GetDirectoryName(tail.Path))
				{
					UseShellExecute = true
				});
			}
			catch
			{
			}
		};
		stackPanel2.Children.Add(button2);
		dockPanel2.Children.Add(stackPanel2);
		DockPanel dockPanel3 = new DockPanel();
		dockPanel3.VerticalAlignment = VerticalAlignment.Center;
		DockPanel dockPanel4 = dockPanel3;
		TextBlock textBlock2 = new TextBlock();
		textBlock2.Text = L.Get("con.filter");
		textBlock2.Foreground = (Brush)owner.FindResource("Dim");
		textBlock2.VerticalAlignment = VerticalAlignment.Center;
		textBlock2.Margin = new Thickness(0.0, 0.0, 6.0, 0.0);
		TextBlock element2 = textBlock2;
		DockPanel.SetDock(element2, Dock.Left);
		dockPanel4.Children.Add(element2);
		filterBox = Box();
		filterBox.Margin = new Thickness(0.0, 0.0, 14.0, 0.0);
		filterBox.TextChanged += (object s, TextChangedEventArgs e) =>
		{
			Render();
		};
		dockPanel4.Children.Add(filterBox);
		dockPanel2.Children.Add(dockPanel4);
		Grid.SetColumnSpan(dockPanel2, 2);
		grid2.Children.Add(dockPanel2);
		DockPanel dockPanel5 = new DockPanel();
		dockPanel5.Margin = new Thickness(0.0, 0.0, 0.0, 8.0);
		dockPanel5.Visibility = Visibility.Collapsed;
		DockPanel dockPanel6 = dockPanel5;
		bannerButton = Pill("");
		DockPanel.SetDock(bannerButton, Dock.Right);
		dockPanel6.Children.Add(bannerButton);
		banner = new TextBlock
		{
			TextWrapping = TextWrapping.Wrap,
			Foreground = new SolidColorBrush(Color.FromRgb(240, 178, 62)),
			VerticalAlignment = VerticalAlignment.Center
		};
		dockPanel6.Children.Add(banner);
		Grid.SetRow(dockPanel6, 1);
		Grid.SetColumnSpan(dockPanel6, 2);
		grid2.Children.Add(dockPanel6);
		logBox = new TextBox
		{
			IsReadOnly = true,
			FontFamily = new FontFamily("Consolas"),
			FontSize = 12.0,
			TextWrapping = TextWrapping.NoWrap,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			Background = new SolidColorBrush(Color.FromRgb(8, 8, 12)),
			Foreground = new SolidColorBrush(Color.FromRgb(200, 208, 200)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(68, 232, 196, 106)),
			Padding = new Thickness(6.0)
		};
		Grid.SetRow(logBox, 2);
		grid2.Children.Add(logBox);
		StackPanel stackPanel3 = new StackPanel();
		stackPanel3.Margin = new Thickness(12.0, 0.0, 0.0, 0.0);
		StackPanel stackPanel4 = stackPanel3;
		stackPanel4.Children.Add(new TextBlock
		{
			Text = L.Get("con.actions"),
			FontSize = 13.0,
			FontWeight = FontWeights.SemiBold,
			Foreground = (Brush)owner.FindResource("Gold"),
			Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
		});
		foreach (QuickAction item in Actions())
		{
			QuickAction action = item;
			Button button3 = ActionButton(L.Get(action.Key));
			button3.Click += (object s, RoutedEventArgs e) =>
			{
				RunAction(action);
			};
			stackPanel4.Children.Add(button3);
		}
		Button button4 = ActionButton(L.Get("con.a.account"));
		button4.Click += (object s, RoutedEventArgs e) =>
		{
			CreateAccountDialog();
		};
		stackPanel4.Children.Add(button4);
		ScrollViewer scrollViewer = new ScrollViewer();
		scrollViewer.Content = stackPanel4;
		scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
		ScrollViewer element3 = scrollViewer;
		Grid.SetRow(element3, 2);
		Grid.SetColumn(element3, 1);
		Grid.SetRowSpan(element3, 2);
		grid2.Children.Add(element3);
		DockPanel dockPanel7 = new DockPanel();
		dockPanel7.Margin = new Thickness(0.0, 8.0, 0.0, 0.0);
		DockPanel dockPanel8 = dockPanel7;
		Button button5 = Pill(L.Get("con.send"));
		button5.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
		button5.Click += (object s, RoutedEventArgs e) =>
		{
			SendInput();
		};
		DockPanel.SetDock(button5, Dock.Right);
		dockPanel8.Children.Add(button5);
		input = Box();
		input.FontFamily = new FontFamily("Consolas");
		input.KeyDown += InputKey;
		dockPanel8.Children.Add(input);
		Grid.SetRow(dockPanel8, 3);
		grid2.Children.Add(dockPanel8);
		status = new TextBlock
		{
			FontSize = 12.0,
			Foreground = (Brush)owner.FindResource("Dim"),
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0),
			TextWrapping = TextWrapping.Wrap
		};
		Grid.SetRow(status, 4);
		Grid.SetColumnSpan(status, 2);
		grid2.Children.Add(status);
		status.Text = L.Get("con.hint", tail.Path);
		Content = grid2;
	}

	private static IEnumerable<QuickAction> Actions()
	{
		Func<string, string> money = (string g) => (!long.TryParse(g, out var result)) ? "0" : (result * 10000).ToString();
		return new QuickAction[15]
		{
			new QuickAction
			{
				Key = "con.a.online",
				Fields = new string[0],
				Command = (string[] v) => "account onlinelist"
			},
			new QuickAction
			{
				Key = "con.a.info",
				Fields = new string[0],
				Command = (string[] v) => "server info"
			},
			new QuickAction
			{
				Key = "con.a.announce",
				Fields = new string[1] { "con.f.message" },
				Command = (string[] v) => "announce " + v[0]
			},
			new QuickAction
			{
				Key = "con.a.money",
				Fields = new string[2] { "con.f.player", "con.f.gold" },
				Defaults = new string[2] { "", "100" },
				Command = (string[] v) => "send money " + v[0] + " \"Gift\" \"From the server\" " + money(v[1])
			},
			new QuickAction
			{
				Key = "con.a.item",
				Fields = new string[3] { "con.f.player", "con.f.itemid", "con.f.count" },
				Defaults = new string[3] { "", "", "1" },
				Command = (string[] v) => "send items " + v[0] + " \"Gift\" \"From the server\" " + v[1] + ":" + v[2]
			},
			new QuickAction
			{
				Key = "con.a.level",
				Fields = new string[2] { "con.f.player", "con.f.level" },
				Defaults = new string[2] { "", "60" },
				Command = (string[] v) => "character level " + v[0] + " " + v[1]
			},
			new QuickAction
			{
				Key = "con.a.tele",
				Fields = new string[2] { "con.f.player", "con.f.location" },
				Defaults = new string[2] { "", "goldshire" },
				Command = (string[] v) => "tele name " + v[0] + " " + v[1]
			},
			new QuickAction
			{
				Key = "con.a.revive",
				Fields = new string[1] { "con.f.player" },
				Command = (string[] v) => "revive " + v[0]
			},
			new QuickAction
			{
				Key = "con.a.kick",
				Fields = new string[1] { "con.f.player" },
				Command = (string[] v) => "kick " + v[0]
			},
			new QuickAction
			{
				Key = "con.a.ban",
				Fields = new string[3] { "con.f.player", "con.f.duration", "con.f.reason" },
				Defaults = new string[3] { "", "1d", "" },
				Command = (string[] v) => "ban character " + v[0] + " " + v[1] + " " + v[2]
			},
			new QuickAction
			{
				Key = "con.a.unban",
				Fields = new string[1] { "con.f.player" },
				Command = (string[] v) => "unban character " + v[0]
			},
			new QuickAction
			{
				Key = "con.a.save",
				Fields = new string[0],
				Command = (string[] v) => "saveall"
			},
			new QuickAction
			{
				Key = "con.a.restart",
				Fields = new string[1] { "con.f.seconds" },
				Defaults = new string[1] { "60" },
				Command = (string[] v) => "server restart " + v[0],
				Confirm = true
			},
			new QuickAction
			{
				Key = "con.a.shutdown",
				Fields = new string[1] { "con.f.seconds" },
				Defaults = new string[1] { "60" },
				Command = (string[] v) => "server shutdown " + v[0],
				Confirm = true
			},
			new QuickAction
			{
				Key = "con.a.cancel",
				Fields = new string[0],
				Command = (string[] v) => "server shutdown cancel"
			}
		};
	}

	private void RunAction(QuickAction action)
	{
		try
		{
			RunActionUnsafe(action);
		}
		catch (Exception ex)
		{
			status.Text = ex.Message;
		}
	}

	private void RunActionUnsafe(QuickAction action)
	{
		string[] array = new string[0];
		if (action.Fields.Length > 0)
		{
			array = PromptDialog.Ask(this, L.Get(action.Key), action.Fields.Select((string f) => L.Get(f)).ToArray(), action.Defaults);
			if (array == null)
			{
				return;
			}
		}
		string text = action.Command(array);
		if (!action.Confirm || MessageBox.Show(this, L.Get("con.confirm", text), Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
		{
			Send(text);
		}
	}

	private void PollLog()
	{
		string text = tail.ReadNew(262144);
		if (text.Length != 0)
		{
			text = partial + text.Replace("\r", "");
			int num = text.LastIndexOf('\n');
			partial = ((num >= 0) ? text.Substring(num + 1) : text);
			if (num >= 0)
			{
				AddLines(text.Substring(0, num).Split('\n'));
			}
		}
	}

	private void AddLines(IEnumerable<string> newLines)
	{
		List<string> list = newLines.ToList();
		lines.AddRange(list);
		if (lines.Count > 20000)
		{
			lines.RemoveRange(0, lines.Count - 20000);
		}
		if (pause.IsChecked == true)
		{
			return;
		}
		string filter = filterBox.Text.Trim();
		List<string> list2 = ((filter.Length == 0) ? list : list.Where((string l) => l.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList());
		if (list2.Count != 0)
		{
			if (logBox.Text.Length > 2000000)
			{
				Render();
				return;
			}
			logBox.AppendText(((logBox.Text.Length > 0) ? "\n" : "") + string.Join("\n", list2));
			logBox.ScrollToEnd();
		}
	}

	private void Render()
	{
		if (pause.IsChecked != true)
		{
			string filter = filterBox.Text.Trim();
			List<string> list = ((filter.Length == 0) ? lines : lines.Where((string l) => l.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList());
			if (list.Count > 8000)
			{
				list = list.Skip(list.Count - 8000).ToList();
			}
			logBox.Text = string.Join("\n", list);
			logBox.ScrollToEnd();
		}
	}

	private void InputKey(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			SendInput();
			e.Handled = true;
		}
		else if (e.Key == Key.Up && history.Count > 0)
		{
			historyIndex = Math.Max(0, historyIndex - 1);
			input.Text = history[historyIndex];
			input.CaretIndex = input.Text.Length;
			e.Handled = true;
		}
		else if (e.Key == Key.Down && history.Count > 0)
		{
			historyIndex = Math.Min(history.Count, historyIndex + 1);
			input.Text = ((historyIndex < history.Count) ? history[historyIndex] : "");
			input.CaretIndex = input.Text.Length;
			e.Handled = true;
		}
	}

	private void SendInput()
	{
		string text = input.Text.Trim();
		if (text.Length != 0)
		{
			history.Remove(text);
			history.Add(text);
			historyIndex = history.Count;
			input.Clear();
			Send(text);
		}
	}

	private async void Send(string command)
	{
		if (sending)
		{
			status.Text = L.Get("con.busy");
			return;
		}
		command = command.TrimStart('.', '!');
		if (!RaEnabled || string.IsNullOrEmpty(config.ConsoleUser))
		{
			CheckSetup();
			status.Text = L.Get("con.notready");
			return;
		}
		sending = true;
		AddLines(new string[1] { "> " + command });
		string user = config.ConsoleUser;
		string password = config.ConsolePassword;
		int port = RaPort;
		try
		{
			List<string> replyLines = (from l in (await Task.Run(() =>
				{
					lock (raGate)
					{
						int num = 0;
						while (true)
						{
							try
							{
								if (ra == null || !ra.Alive)
								{
									DisconnectRa();
									ra = RaClient.Connect(port, user, password);
								}
								return ra.Run(command);
							}
							catch
							{
								DisconnectRa();
								if (num > 0)
								{
									throw;
								}
							}
							num++;
						}
					}
				})).Replace("\r", "").Split('\n')
				where l.Trim().Length > 0
				select "  " + l).ToList();
			AddLines((replyLines.Count > 0) ? replyLines : new List<string> { "  " + L.Get("con.noreply") });
			status.Text = L.Get("con.sent", command);
		}
		catch (Exception ex)
		{
			AddLines(new string[1] { "  ! " + (ex.InnerException ?? ex).Message });
			status.Text = (ex.InnerException ?? ex).Message;
		}
		sending = false;
	}

	private void DisconnectRa()
	{
		if (ra != null)
		{
			ra.Dispose();
			ra = null;
		}
	}

	private void CheckSetup()
	{
		FrameworkElement frameworkElement = (FrameworkElement)banner.Parent;
		if (!File.Exists(confPath))
		{
			ShowBanner(frameworkElement, L.Get("con.noconf", confPath), null, null);
		}
		else if (!RaEnabled)
		{
			ShowBanner(frameworkElement, L.Get("con.raoff"), L.Get("con.raon"), EnableRa);
		}
		else if (string.IsNullOrEmpty(config.ConsoleUser))
		{
			ShowBanner(frameworkElement, L.Get("con.noaccount"), L.Get("con.makeaccount"), CreateConsoleAccount);
		}
		else
		{
			frameworkElement.Visibility = Visibility.Collapsed;
		}
	}

	private void ShowBanner(FrameworkElement row, string text, string button, Action click)
	{
		row.Visibility = Visibility.Visible;
		banner.Text = text;
		bannerButton.Visibility = ((button == null) ? Visibility.Collapsed : Visibility.Visible);
		bannerButton.Content = button ?? "";
		bannerButton.Click -= BannerClick;
		bannerClick = click;
		bannerButton.Click += BannerClick;
	}

	private void BannerClick(object sender, RoutedEventArgs e)
	{
		if (bannerClick != null)
		{
			bannerClick();
		}
	}

	private void EnableRa()
	{
		try
		{
			int num = 0;
			foreach (RealmServerConfig realm in config.Realms)
			{
				string path = Path.Combine(config.BinDir, string.IsNullOrEmpty(realm.Config) ? "worldserver.conf" : realm.Config);
				if (File.Exists(path))
				{
					ConfFile.Set(path, new Dictionary<string, string>
					{
						{ "Ra.Enable", "1" },
						{ "Ra.IP", "\"127.0.0.1\"" },
						{
							"Ra.Port",
							(3443 + num).ToString()
						},
						{ "Ra.MinLevel", "3" }
					});
				}
				num++;
			}
			status.Text = L.Get("con.raondone");
			CheckSetup();
		}
		catch (Exception ex)
		{
			status.Text = ex.Message;
		}
	}

	private async void CreateConsoleAccount()
	{
		string password = RandomPassword();
		string email = "console" + new Random().Next(1000, 9999) + "@launcher";
		status.Text = L.Get("con.creating");
		try
		{
			string gameAccount = await Task.Run(() => CreateAccount(email, password, 3));
			config.ConsoleUser = gameAccount;
			config.ConsolePassword = password;
			saveConfig();
			status.Text = L.Get("con.accountmade", gameAccount);
			CheckSetup();
		}
		catch (Exception ex)
		{
			status.Text = (ex.InnerException ?? ex).Message;
		}
	}

	private async void CreateAccountDialog()
	{
		string[] v;
		try
		{
			v = PromptDialog.Ask(this, L.Get("con.a.account"), new string[3]
			{
				L.Get("con.f.email"),
				L.Get("con.f.password"),
				L.Get("con.f.gmlevel")
			}, new string[3] { "", "", "0" });
		}
		catch (Exception ex)
		{
			status.Text = ex.Message;
			return;
		}
		if (v == null)
		{
			return;
		}
		if (!int.TryParse(v[2], out var gm))
		{
			gm = 0;
		}
		status.Text = L.Get("con.creating");
		try
		{
			string gameAccount = await Task.Run(() => CreateAccount(v[0], v[1], gm));
			AddLines(new string[1] { "  " + L.Get("con.accountdone", v[0], gameAccount, gm) });
			status.Text = L.Get("con.accountdone", v[0], gameAccount, gm);
		}
		catch (Exception ex2)
		{
			status.Text = (ex2.InnerException ?? ex2).Message;
		}
	}

	private string CreateAccount(string email, string password, int gmLevel)
	{
		string fileName = Path.Combine(config.BinDir, "worldserver.exe");
		string text = Path.Combine(config.BinDir, string.IsNullOrEmpty(config.Realms[0].Config) ? "worldserver.conf" : config.Realms[0].Config);
		ProcessStartInfo processStartInfo = new ProcessStartInfo(fileName, "-c \"" + text + "\" --create-account \"" + email + "\" --account-password \"" + password + "\" --account-gm-level " + gmLevel);
		processStartInfo.WorkingDirectory = config.BinDir;
		processStartInfo.UseShellExecute = false;
		processStartInfo.CreateNoWindow = true;
		processStartInfo.RedirectStandardOutput = true;
		processStartInfo.RedirectStandardError = true;
		ProcessStartInfo startInfo = processStartInfo;
		using Process process = Process.Start(startInfo);
		string text2 = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
		process.WaitForExit(60000);
		if (process.ExitCode == 3)
		{
			throw new Exception(L.Get("con.exists", email));
		}
		if (process.ExitCode != 0)
		{
			throw new Exception(L.Get("con.createfailed", process.ExitCode));
		}
		string text3 = ConfFile.Get(text, "LogsDir", "");
		string path = Path.Combine(Path.IsPathRooted(text3) ? text3 : Path.Combine(config.BinDir, text3), "Server.log");
		try
		{
			text2 += File.ReadAllText(path);
		}
		catch
		{
		}
		Match match = Regex.Match(text2, "Account " + Regex.Escape(email) + " created \\(game account (\\S+?)\\)", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			throw new Exception(L.Get("con.createfailed", "?"));
		}
		return match.Groups[1].Value;
	}

	private static string RandomPassword()
	{
		byte[] array = new byte[16];
		using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
		{
			randomNumberGenerator.GetBytes(array);
		}
		return new string(array.Select((byte b) => "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789"[b % "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789".Length]).ToArray());
	}
}
