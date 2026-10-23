using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ForeverLauncher;

public sealed class LauncherWindow
{
	private enum UpdateState
	{
		None,
		Available,
		Downloading,
		Restarting,
		Failed,
		Done
	}

	private const string CommitsTag = "commits";

	public readonly Window Window;

	private readonly System.Windows.Application app;

	private readonly Dispatcher ui;

	private System.Windows.Controls.CheckBox noFogBox;

	private System.Windows.Controls.Button playButton;

	private System.Windows.Controls.Button folderButton;

	private System.Windows.Controls.Button gameLangButton;

	private System.Windows.Controls.Button logButton;

	private System.Windows.Controls.Button updateLink;

	private System.Windows.Controls.Button langEs;

	private System.Windows.Controls.Button langEn;

	private System.Windows.Controls.Button langRu;

	private System.Windows.Controls.Button serverButton;

	private System.Windows.Controls.Button editServersLink;

	private System.Windows.Controls.Button reloadServersLink;

	private System.Windows.Controls.Button websiteLink;

	private System.Windows.Controls.Button playTab;

	private System.Windows.Controls.Button localTab;

	private LocalServerView localView;

	private bool localTabActive;

	private TextBlock statusText;

	private TextBlock detailText;

	private TextBlock pingText;

	private TextBlock messageText;

	private TextBlock stepText;

	private TextBlock clientText;

	private TextBlock folderText;

	private TextBlock versionText;

	private TextBlock updateText;

	private TextBlock subtitleText;

	private TextBlock newsHeader;

	private TextBlock loginLabel;

	private TextBlock newsPlaceholder;

	private TextBlock titleText;

	private TextBlock serverLabel;

	private Ellipse statusDot;

	private Ellipse statusHalo;

	private Ellipse loginDot;

	private UniformGrid realmGrid;

	private StackPanel newsList;

	private WrapPanel linksPanel;

	private Border updateBanner;

	private Canvas stars;

	private string gameDir;

	private readonly List<Patcher> patchers = new List<Patcher>();

	private bool gameRunning;

	private NotifyIcon tray;

	private string updateUrl = "https://github.com/Kittnz/forever-launcher/releases/latest";

	private UpdateState updateState;

	private LauncherRelease launcherUpdate;

	private int updatePercent;

	private string updateError;

	private ServerStatus lastStatus;

	private ServerEntry server;

	private Msg lastStep;

	private string clientVersion;

	private bool clientOk;

	private static readonly System.Windows.Media.Color Green = System.Windows.Media.Color.FromRgb(92, 214, 122);

	private static readonly System.Windows.Media.Color Red = System.Windows.Media.Color.FromRgb(224, 90, 78);

	private static readonly System.Windows.Media.Color Amber = System.Windows.Media.Color.FromRgb(240, 178, 62);

	private static readonly System.Windows.Media.Color Grey = System.Windows.Media.Color.FromRgb(138, 138, 138);

	public LauncherWindow(System.Windows.Application app)
	{
		this.app = app;
		using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ForeverLauncher.MainWindow.xaml"))
		{
			Window = (Window)XamlReader.Load(stream);
		}
		ui = Window.Dispatcher;
		try
		{
			Window.Icon = Imaging.CreateBitmapSourceFromHIcon(AppIcon().Handle, Int32Rect.Empty, null);
		}
		catch
		{
		}
		playButton = Find<System.Windows.Controls.Button>("PlayButton");
		folderButton = Find<System.Windows.Controls.Button>("FolderButton");
		logButton = Find<System.Windows.Controls.Button>("LogButton");
		updateLink = Find<System.Windows.Controls.Button>("UpdateLink");
		statusText = Find<TextBlock>("StatusText");
		detailText = Find<TextBlock>("DetailText");
		pingText = Find<TextBlock>("PingText");
		messageText = Find<TextBlock>("MessageText");
		stepText = Find<TextBlock>("StepText");
		clientText = Find<TextBlock>("ClientText");
		folderText = Find<TextBlock>("FolderText");
		versionText = Find<TextBlock>("VersionText");
		updateText = Find<TextBlock>("UpdateText");
		statusDot = Find<Ellipse>("StatusDot");
		statusHalo = Find<Ellipse>("StatusHalo");
		loginDot = Find<Ellipse>("LoginDot");
		realmGrid = Find<UniformGrid>("RealmGrid");
		newsList = Find<StackPanel>("NewsList");
		linksPanel = Find<WrapPanel>("LinksPanel");
		updateBanner = Find<Border>("UpdateBanner");
		stars = Find<Canvas>("Stars");
		subtitleText = Find<TextBlock>("SubtitleText");
		newsHeader = Find<TextBlock>("NewsHeader");
		loginLabel = Find<TextBlock>("LoginLabel");
		newsPlaceholder = Find<TextBlock>("NewsPlaceholder");
		langEs = Find<System.Windows.Controls.Button>("LangEs");
		langEn = Find<System.Windows.Controls.Button>("LangEn");
		langRu = Find<System.Windows.Controls.Button>("LangRu");
		titleText = Find<TextBlock>("TitleText");
		serverLabel = Find<TextBlock>("ServerLabel");
		serverButton = Find<System.Windows.Controls.Button>("ServerButton");
		editServersLink = Find<System.Windows.Controls.Button>("EditServersLink");
		reloadServersLink = Find<System.Windows.Controls.Button>("ReloadServersLink");
		websiteLink = Find<System.Windows.Controls.Button>("WebsiteLink");
		serverButton.Click += (object s, RoutedEventArgs e) =>
		{
			ShowServerMenu();
		};
		editServersLink.Click += (object s, RoutedEventArgs e) =>
		{
			EditServers();
		};
		reloadServersLink.Click += (object s, RoutedEventArgs e) =>
		{
			LoadServers();
		};
		websiteLink.Click += (object s, RoutedEventArgs e) =>
		{
			if (server != null)
			{
				OpenUrl(server.Website);
			}
		};
		System.Windows.Controls.Button button = Find<System.Windows.Controls.Button>("DiscordButton");
		button.Click += (object s, RoutedEventArgs e) =>
		{
			OpenUrl("https://discord.gg/4tqq7EQjs9");
		};
		button.ToolTip = "https://discord.gg/4tqq7EQjs9";
		playTab = Find<System.Windows.Controls.Button>("PlayTab");
		localTab = Find<System.Windows.Controls.Button>("LocalTab");
		playTab.Click += (object s, RoutedEventArgs e) =>
		{
			ShowTab(local: false);
		};
		localTab.Click += (object s, RoutedEventArgs e) =>
		{
			ShowTab(local: true);
		};
		localView = new LocalServerView(Window, Find<Grid>("LocalPanel"));
		langEs.Click += (object s, RoutedEventArgs e) =>
		{
			SetLanguage("es");
		};
		langEn.Click += (object s, RoutedEventArgs e) =>
		{
			SetLanguage("en");
		};
		langRu.Click += (object s, RoutedEventArgs e) =>
		{
			SetLanguage("ru");
		};
		versionText.Text = "v2.4.1";
		Find<Grid>("TitleBar").MouseLeftButtonDown += (object s, MouseButtonEventArgs e) =>
		{
			if (e.ButtonState == MouseButtonState.Pressed)
			{
				Window.DragMove();
			}
		};
		Find<System.Windows.Controls.Button>("MinButton").Click += (object s, RoutedEventArgs e) =>
		{
			Window.WindowState = WindowState.Minimized;
		};
		Find<System.Windows.Controls.Button>("CloseButton").Click += (object s, RoutedEventArgs e) =>
		{
			OnCloseClicked();
		};
		playButton.Click += (object s, RoutedEventArgs e) =>
		{
			Play();
		};
		folderButton.Click += (object s, RoutedEventArgs e) =>
		{
			PickFolder();
		};
		gameLangButton = Find<System.Windows.Controls.Button>("GameLangButton");
		gameLangButton.Click += (object s, RoutedEventArgs e) =>
		{
			ShowGameLangMenu();
		};
		noFogBox = Find<System.Windows.Controls.CheckBox>("NoFogBox");
		Patcher.RemoveFog = Settings.LoadRemoveFog();
		noFogBox.IsChecked = Patcher.RemoveFog;
		noFogBox.Click += (object s, RoutedEventArgs e) =>
		{
			Patcher.RemoveFog = noFogBox.IsChecked == true;
			Settings.SaveRemoveFog(Patcher.RemoveFog);
		};
		logButton.Click += (object s, RoutedEventArgs e) =>
		{
			OpenLog();
		};
		updateLink.Click += (object s, RoutedEventArgs e) =>
		{
			if (updateState == UpdateState.Available && Updater.CanWriteHere())
			{
				RunLauncherUpdate();
			}
			else
			{
				OpenUrl((launcherUpdate != null && launcherUpdate.PageUrl != null) ? launcherUpdate.PageUrl : updateUrl);
			}
		};
		Window.Closing += (object s, CancelEventArgs e) =>
		{
			if (gameRunning)
			{
				e.Cancel = true;
				HideToTray();
			}
		};
		Window.Closed += (object s, EventArgs e) =>
		{
			Shutdown();
		};
		DrawStars();
		PulseHalo();
		LoadServers();
		ApplyLanguage();
		SetGameDir(Settings.LoadGameDir() ?? GameLocator.Find(), save: false);
		DispatcherTimer timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(60.0)
		};
		timer.Tick += (object s, EventArgs e) =>
		{
			RefreshStatus();
		};
		Window.Loaded += (object s, RoutedEventArgs e) =>
		{
			timer.Start();
			RefreshStatus();
			CheckLauncherUpdate();
		};
	}

	private async void CheckLauncherUpdate()
	{
		Updater.Cleanup();
		if (App.UpdatedTo != null)
		{
			updateState = UpdateState.Done;
			RenderLauncherUpdate();
		}
		LauncherRelease r = await Task.Run(() => Updater.Latest());
		if (Updater.IsNewer(r))
		{
			launcherUpdate = r;
			updateState = UpdateState.Available;
			RenderLauncherUpdate();
			if (Updater.AutoUpdate && Updater.CanWriteHere())
			{
				RunLauncherUpdate();
			}
		}
	}

	private async void RunLauncherUpdate()
	{
		if (updateState == UpdateState.Downloading || updateState == UpdateState.Restarting)
		{
			return;
		}
		updateState = UpdateState.Downloading;
		updatePercent = 0;
		RenderLauncherUpdate();
		try
		{
			await Updater.Download(launcherUpdate, (int p) =>
			{
				ui.BeginInvoke((Action)(() =>
				{
					if (updateState == UpdateState.Downloading)
					{
						updatePercent = p;
						RenderLauncherUpdate();
					}
				}));
			});
			updateState = UpdateState.Restarting;
			RenderLauncherUpdate();
			Updater.InstallAndRestart(launcherUpdate.Version);
			Shutdown();
		}
		catch (Exception ex)
		{
			updateState = UpdateState.Failed;
			updateError = ((ex is WebException) ? L.Get("upd.nonet") : ex.Message);
			RenderLauncherUpdate();
		}
	}

	private void RenderLauncherUpdate()
	{
		string text = ((launcherUpdate != null) ? launcherUpdate.Version : App.UpdatedTo);
		switch (updateState)
		{
		case UpdateState.None:
			return;
		case UpdateState.Available:
			updateText.Text = L.Get("ui.update", text);
			updateLink.Content = L.Get("upd.now");
			break;
		case UpdateState.Downloading:
			updateText.Text = L.Get("upd.downloading", text, updatePercent);
			break;
		case UpdateState.Restarting:
			updateText.Text = L.Get("upd.restarting");
			break;
		case UpdateState.Failed:
			updateText.Text = L.Get("upd.failed", text, updateError);
			updateLink.Content = L.Get("ui.download");
			break;
		case UpdateState.Done:
			updateText.Text = L.Get("upd.done", text);
			break;
		}
		updateLink.Visibility = ((updateState != UpdateState.Available && updateState != UpdateState.Failed) ? Visibility.Collapsed : Visibility.Visible);
		updateBanner.Visibility = Visibility.Visible;
	}

	private T Find<T>(string name) where T : class
	{
		return (T)Window.FindName(name);
	}

	private void SetLanguage(string lang)
	{
		if (!(L.Lang == lang))
		{
			L.Lang = lang;
			L.Save();
			ApplyLanguage();
		}
	}

	private void ApplyLanguage()
	{
		System.Windows.Media.Brush brush = (System.Windows.Media.Brush)Window.FindResource("Gold");
		SolidColorBrush solidColorBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 232, 196, 106));
		langEs.Foreground = ((L.Lang == "es") ? brush : new SolidColorBrush(System.Windows.Media.Color.FromRgb(143, 135, 115)));
		langEs.Background = ((L.Lang == "es") ? solidColorBrush : System.Windows.Media.Brushes.Transparent);
		langEn.Foreground = ((L.Lang == "en") ? brush : new SolidColorBrush(System.Windows.Media.Color.FromRgb(143, 135, 115)));
		langEn.Background = ((L.Lang == "en") ? solidColorBrush : System.Windows.Media.Brushes.Transparent);
		langRu.Foreground = ((L.Lang == "ru") ? brush : new SolidColorBrush(System.Windows.Media.Color.FromRgb(143, 135, 115)));
		langRu.Background = ((L.Lang == "ru") ? solidColorBrush : System.Windows.Media.Brushes.Transparent);
		subtitleText.Text = L.Get("ui.subtitle");
		newsHeader.Text = L.Get("ui.news");
		loginLabel.Text = L.Get("ui.login");
		logButton.Content = L.Get("ui.viewlog");
		serverLabel.Text = L.Get("ui.server");
		editServersLink.Content = L.Get("ui.editservers");
		reloadServersLink.Content = L.Get("ui.reloadservers");
		websiteLink.Content = L.Get("ui.website");
		playTab.Content = L.Get("tab.play");
		localTab.Content = L.Get("tab.local");
		UpdateTabs();
		localView.ApplyLanguage();
		updateLink.Content = L.Get("ui.download");
		RenderLauncherUpdate();
		playButton.Content = L.Get(gameRunning ? "ui.playanother" : "ui.play");
		folderButton.Content = L.Get((gameDir == null) ? "ui.pickfolder" : "ui.changefolder");
		UpdateGameLangButton();
		clientText.Text = ((clientVersion == null) ? "" : (L.Get("ui.client", clientVersion) + (clientOk ? "  ✓" : "")));
		if (newsPlaceholder.Parent != null)
		{
			newsPlaceholder.Text = L.Get((lastStatus == null) ? "ui.loading" : "ui.newsfail");
		}
		if (server == null)
		{
			RenderNoServer();
		}
		else if (lastStatus == null)
		{
			statusText.Text = L.Get("status.checking");
		}
		else
		{
			RenderStatus(lastStatus);
		}
		if (lastStep != null)
		{
			Step(lastStep);
		}
		if (tray != null)
		{
			BuildTrayMenu();
		}
	}

	private static Icon AppIcon()
	{
		return Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
	}

	private void DrawStars()
	{
		Random random = new Random(1601);
		for (int i = 0; i < 140; i++)
		{
			double num = ((random.NextDouble() < 0.12) ? 2.2 : (1.2 + random.NextDouble() * 0.6));
			Ellipse ellipse = new Ellipse();
			ellipse.Width = num;
			ellipse.Height = num;
			ellipse.Fill = System.Windows.Media.Brushes.White;
			ellipse.Opacity = 0.25 + random.NextDouble() * 0.6;
			Ellipse ellipse2 = ellipse;
			Canvas.SetLeft(ellipse2, random.NextDouble() * 978.0);
			Canvas.SetTop(ellipse2, Math.Pow(random.NextDouble(), 1.6) * 330.0);
			if (i % 5 == 0)
			{
				DoubleAnimation doubleAnimation = new DoubleAnimation(ellipse2.Opacity, 0.1, TimeSpan.FromSeconds(1.5 + random.NextDouble() * 3.0));
				doubleAnimation.AutoReverse = true;
				doubleAnimation.RepeatBehavior = RepeatBehavior.Forever;
				doubleAnimation.BeginTime = TimeSpan.FromSeconds(random.NextDouble() * 4.0);
				DoubleAnimation animation = doubleAnimation;
				ellipse2.BeginAnimation(UIElement.OpacityProperty, animation);
			}
			stars.Children.Add(ellipse2);
		}
	}

	private void PulseHalo()
	{
		ScaleTransform scaleTransform = new ScaleTransform(1.0, 1.0);
		statusHalo.RenderTransform = scaleTransform;
		DoubleAnimation doubleAnimation = new DoubleAnimation(1.0, 2.1, TimeSpan.FromSeconds(1.8));
		doubleAnimation.RepeatBehavior = RepeatBehavior.Forever;
		doubleAnimation.EasingFunction = new QuadraticEase
		{
			EasingMode = EasingMode.EaseOut
		};
		DoubleAnimation animation = doubleAnimation;
		scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
		scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
		DoubleAnimation doubleAnimation2 = new DoubleAnimation(0.5, 0.0, TimeSpan.FromSeconds(1.8));
		doubleAnimation2.RepeatBehavior = RepeatBehavior.Forever;
		DoubleAnimation animation2 = doubleAnimation2;
		statusHalo.BeginAnimation(UIElement.OpacityProperty, animation2);
	}

	private async void RefreshStatus()
	{
		ServerEntry target = server;
		if (target == null)
		{
			RenderNoServer();
			return;
		}
		ServerStatus s;
		try
		{
			s = await StatusClient.FetchAsync(target);
		}
		catch
		{
			return;
		}
		if (target == server)
		{
			if (s.FeedOk || lastStatus == null || !lastStatus.FeedOk)
			{
				lastStatus = s;
			}
			else
			{
				s.FeedOk = true;
				s.Root = lastStatus.Root;
				s.News = lastStatus.News;
				s.Links = lastStatus.Links;
				s.LatestLauncher = lastStatus.LatestLauncher;
				s.LauncherUrl = lastStatus.LauncherUrl;
				lastStatus = s;
			}
			RenderStatus(s);
		}
	}

	private void RenderStatus(ServerStatus s)
	{
		System.Windows.Media.Color c;
		string key;
		if (s.LoginUp && s.WorldUp)
		{
			c = Green;
			key = "status.online";
		}
		else if (s.LoginUp)
		{
			c = Amber;
			key = "status.worlddown";
		}
		else if (s.FeedFresh && s.FeedLoginUp)
		{
			c = Amber;
			key = "status.unreachable";
		}
		else if (s.Maintenance)
		{
			c = Amber;
			key = "status.maintenance";
		}
		else
		{
			c = Red;
			key = "status.offline";
		}
		SetDot(statusDot, c);
		SetDot(statusHalo, c);
		statusText.Text = L.Get(key);
		SetDot(loginDot, s.LoginUp ? Green : Red);
		FillRealms(s);
		pingText.Text = ((s.LoginMs >= 0) ? (s.LoginMs + " ms") : "");
		if (!s.LoginUp && !s.WorldUp)
		{
			detailText.Text = L.Get("status.noresponse");
		}
		else
		{
			detailText.Text = " ";
		}
		string text = s.Root.Get("message") ?? "";
		messageText.Text = text;
		messageText.Visibility = (string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible);
		if (s.FeedOk)
		{
			FillNews(s);
			FillLinks(s);
		}
		else if (string.IsNullOrEmpty(server.StatusUrl))
		{
			ShowNewsText(L.Get("ui.nofeed"));
		}
		else
		{
			ShowNewsText(L.Get("ui.newsfail"));
		}
		ShowCommits();
		if (updateState == UpdateState.None && StatusClient.IsNewer(s.LatestLauncher, "2.4.1"))
		{
			updateText.Text = L.Get("ui.update", s.LatestLauncher);
			if (!string.IsNullOrEmpty(s.LauncherUrl))
			{
				updateUrl = s.LauncherUrl;
			}
			updateBanner.Visibility = Visibility.Visible;
		}
	}

	private void FillRealms(ServerStatus s)
	{
		realmGrid.Children.Clear();
		System.Windows.Media.Brush brush = (System.Windows.Media.Brush)Window.FindResource("Dim");
		foreach (RealmStatus realm in s.Realms)
		{
			Border border = new Border();
			border.Width = 3.0;
			border.CornerRadius = new CornerRadius(2.0);
			border.Margin = new Thickness(0.0, 0.0, 10.0, 0.0);
			border.Background = new SolidColorBrush(RealmColor(realm.Realm.Kind));
			Border element = border;
			Ellipse ellipse = new Ellipse();
			ellipse.Width = 8.0;
			ellipse.Height = 8.0;
			ellipse.VerticalAlignment = VerticalAlignment.Center;
			ellipse.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
			Ellipse ellipse2 = ellipse;
			SetDot(ellipse2, realm.Up ? Green : Red);
			StackPanel stackPanel = new StackPanel();
			stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
			StackPanel stackPanel2 = stackPanel;
			stackPanel2.Children.Add(ellipse2);
			stackPanel2.Children.Add(new TextBlock
			{
				Text = realm.Realm.Name,
				FontSize = 14.0,
				FontWeight = FontWeights.SemiBold,
				TextTrimming = TextTrimming.CharacterEllipsis
			});
			StackPanel stackPanel3 = new StackPanel();
			stackPanel3.VerticalAlignment = VerticalAlignment.Center;
			StackPanel stackPanel4 = stackPanel3;
			stackPanel4.Children.Add(stackPanel2);
			stackPanel4.Children.Add(new TextBlock
			{
				Text = L.Get(realm.Up ? "realm.online" : "realm.offline") + (realm.Up ? ("  ·  " + realm.Ms + " ms") : ""),
				FontSize = 11.5,
				Foreground = (realm.Up ? new SolidColorBrush(Green) : brush),
				Margin = new Thickness(16.0, 2.0, 0.0, 0.0)
			});
			DockPanel dockPanel = new DockPanel();
			DockPanel.SetDock(element, Dock.Left);
			dockPanel.Children.Add(element);
			dockPanel.Children.Add(stackPanel4);
			realmGrid.Children.Add(new Border
			{
				Child = dockPanel,
				CornerRadius = new CornerRadius(6.0),
				Padding = new Thickness(8.0, 7.0, 10.0, 7.0),
				Margin = new Thickness(0.0, 6.0, 6.0, 0.0),
				Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(64, 0, 0, 0)),
				BorderThickness = new Thickness(1.0),
				BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)(realm.Up ? 85 : 34), 232, 196, 106)),
				ToolTip = server.Portal + ":" + realm.Realm.Port
			});
		}
	}

	private static System.Windows.Media.Color RealmColor(string kind)
	{
		return kind switch
		{
			"pvp" => System.Windows.Media.Color.FromRgb(208, 74, 60), 
			"rp" => System.Windows.Media.Color.FromRgb(92, 184, 138), 
			"hardcore" => System.Windows.Media.Color.FromRgb(154, 106, 224), 
			_ => System.Windows.Media.Color.FromRgb(232, 196, 106), 
		};
	}

	private void RenderNoServer()
	{
		lastStatus = null;
		SetDot(statusDot, Grey);
		SetDot(statusHalo, Grey);
		statusText.Text = L.Get("status.noserver");
		SetDot(loginDot, Grey);
		realmGrid.Children.Clear();
		pingText.Text = "";
		detailText.Text = ((Servers.Errors.Count > 0) ? L.Get("srv.errors", string.Join("; ", Servers.Errors.ToArray())) : L.Get("srv.none"));
		messageText.Visibility = Visibility.Collapsed;
		ShowNewsText("");
		linksPanel.Children.Clear();
		ShowCommits();
	}

	private void ShowNewsText(string text)
	{
		newsList.Children.Clear();
		if (!string.IsNullOrEmpty(text))
		{
			newsList.Children.Add(new TextBlock
			{
				Text = text,
				Foreground = (System.Windows.Media.Brush)Window.FindResource("Dim"),
				FontSize = 13.0,
				TextWrapping = TextWrapping.Wrap
			});
		}
	}

	private async void ShowCommits()
	{
		int num = default;
		_ = num;
		_ = 0;
		List<CommitInfo> commits;
		try
		{
			commits = await CommitFeed.GetAsync();
		}
		catch
		{
			return;
		}
		if (commits == null || commits.Count == 0)
		{
			return;
		}
		for (int num2 = newsList.Children.Count - 1; num2 >= 0; num2--)
		{
			if (newsList.Children[num2] is FrameworkElement frameworkElement && frameworkElement.Tag as string == "commits")
			{
				newsList.Children.RemoveAt(num2);
			}
		}
		System.Windows.Media.Brush gold = (System.Windows.Media.Brush)Window.FindResource("Gold");
		System.Windows.Media.Brush dim = (System.Windows.Media.Brush)Window.FindResource("Dim");
		TextBlock header = new TextBlock
		{
			Text = L.Get("ui.commits"),
			Tag = "commits",
			FontSize = 11.5,
			FontWeight = FontWeights.SemiBold,
			Foreground = gold,
			Cursor = System.Windows.Input.Cursors.Hand,
			Margin = new Thickness(0.0, (newsList.Children.Count > 0) ? 6 : 0, 0.0, 10.0),
			ToolTip = CommitFeed.BranchUrl
		};
		header.MouseLeftButtonUp += (object o, MouseButtonEventArgs e) =>
		{
			OpenUrl(CommitFeed.BranchUrl);
		};
		newsList.Children.Add(header);
		foreach (CommitInfo item in commits)
		{
			StackPanel stackPanel = new StackPanel();
			stackPanel.Tag = "commits";
			stackPanel.Margin = new Thickness(0.0, 0.0, 0.0, 14.0);
			StackPanel stackPanel2 = stackPanel;
			if (item.Date != DateTime.MinValue)
			{
				stackPanel2.Children.Add(new TextBlock
				{
					Text = item.Date.ToString("yyyy-MM-dd HH:mm"),
					FontSize = 11.0,
					Foreground = dim
				});
			}
			TextBlock textBlock = new TextBlock();
			textBlock.Text = item.Title;
			textBlock.FontSize = 13.5;
			textBlock.FontWeight = FontWeights.SemiBold;
			textBlock.TextWrapping = TextWrapping.Wrap;
			textBlock.Margin = new Thickness(0.0, 2.0, 0.0, 0.0);
			TextBlock textBlock2 = textBlock;
			if (!string.IsNullOrEmpty(item.Url))
			{
				string url = item.Url;
				textBlock2.Cursor = System.Windows.Input.Cursors.Hand;
				textBlock2.ToolTip = url;
				textBlock2.MouseLeftButtonUp += (object o, MouseButtonEventArgs e) =>
				{
					OpenUrl(url);
				};
			}
			stackPanel2.Children.Add(textBlock2);
			if (!string.IsNullOrEmpty(item.Text))
			{
				stackPanel2.Children.Add(new TextBlock
				{
					Text = item.Text,
					FontSize = 12.0,
					Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(196, 187, 165)),
					TextWrapping = TextWrapping.Wrap,
					Margin = new Thickness(0.0, 3.0, 0.0, 0.0),
					LineHeight = 17.0
				});
			}
			newsList.Children.Add(stackPanel2);
		}
	}

	private void LoadServers()
	{
		Servers.Load();
		SelectServer(Servers.Find((server != null) ? server.Name : Settings.LoadServer()), save: false);
		if (Servers.Errors.Count > 0)
		{
			Step(new Msg(MsgKind.Warn, "srv.errors", string.Join("; ", Servers.Errors.ToArray())));
		}
		else if (Servers.List.Count == 0)
		{
			Step(new Msg(MsgKind.Warn, "srv.none"));
		}
		else
		{
			Step(new Msg(MsgKind.Dim, "srv.loaded", Servers.List.Count, Servers.FilePath));
		}
	}

	private void SelectServer(ServerEntry s, bool save)
	{
		server = s;
		lastStatus = null;
		titleText.Text = ((s != null) ? s.Name : "Forever Launcher");
		serverButton.Content = ((s != null) ? (s.Name + "  (" + s.Portal + ")") : "-") + "  ▾";
		websiteLink.Visibility = ((s == null || string.IsNullOrEmpty(s.Website)) ? Visibility.Collapsed : Visibility.Visible);
		if (updateState == UpdateState.None)
		{
			updateBanner.Visibility = Visibility.Collapsed;
		}
		if (save && s != null)
		{
			Settings.SaveServer(s.Name);
		}
		if (localView != null)
		{
			localView.SetServer(s?.Name);
		}
		if (s == null)
		{
			RenderNoServer();
			return;
		}
		statusText.Text = L.Get("status.checking");
		ShowNewsText(L.Get("ui.loading"));
		linksPanel.Children.Clear();
		if (Window.IsLoaded)
		{
			RefreshStatus();
		}
	}

	private void ShowTab(bool local)
	{
		localTabActive = local;
		Find<Grid>("PlayPanel").Visibility = (local ? Visibility.Collapsed : Visibility.Visible);
		Find<Border>("BottomBar").Visibility = (local ? Visibility.Collapsed : Visibility.Visible);
		Find<Grid>("LocalPanel").Visibility = ((!local) ? Visibility.Collapsed : Visibility.Visible);
		UpdateTabs();
	}

	private void UpdateTabs()
	{
		System.Windows.Media.Brush brush = (System.Windows.Media.Brush)Window.FindResource("Gold");
		SolidColorBrush solidColorBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 232, 196, 106));
		SolidColorBrush solidColorBrush2 = new SolidColorBrush(System.Windows.Media.Color.FromRgb(143, 135, 115));
		playTab.Foreground = (localTabActive ? solidColorBrush2 : brush);
		playTab.Background = (localTabActive ? System.Windows.Media.Brushes.Transparent : solidColorBrush);
		localTab.Foreground = (localTabActive ? brush : solidColorBrush2);
		localTab.Background = (localTabActive ? solidColorBrush : System.Windows.Media.Brushes.Transparent);
	}

	private void ShowServerMenu()
	{
		System.Windows.Controls.ContextMenu contextMenu = new System.Windows.Controls.ContextMenu();
		foreach (ServerEntry item in Servers.List)
		{
			ServerEntry entry = item;
			System.Windows.Controls.MenuItem menuItem = new System.Windows.Controls.MenuItem();
			menuItem.Header = item.Name + "   " + item.Portal;
			menuItem.IsCheckable = true;
			menuItem.IsChecked = item == server;
			System.Windows.Controls.MenuItem menuItem2 = menuItem;
			menuItem2.Click += (object o, RoutedEventArgs e) =>
			{
				SelectServer(entry, save: true);
			};
			contextMenu.Items.Add(menuItem2);
		}
		if (contextMenu.Items.Count > 0)
		{
			contextMenu.Items.Add(new Separator());
		}
		System.Windows.Controls.MenuItem menuItem3 = new System.Windows.Controls.MenuItem();
		menuItem3.Header = L.Get("ui.editservers");
		System.Windows.Controls.MenuItem menuItem4 = menuItem3;
		menuItem4.Click += (object o, RoutedEventArgs e) =>
		{
			EditServers();
		};
		contextMenu.Items.Add(menuItem4);
		contextMenu.PlacementTarget = serverButton;
		contextMenu.Placement = PlacementMode.Bottom;
		contextMenu.IsOpen = true;
	}

	private void UpdateGameLangButton()
	{
		if (gameLangButton != null)
		{
			string text = Settings.LoadGameLang();
			gameLangButton.Content = L.Get("ui.gamelang", (text.Length > 0) ? Patcher.LocaleName(text) : L.Get("ui.gamelang.auto"));
			gameLangButton.Visibility = ((gameDir == null) ? Visibility.Collapsed : Visibility.Visible);
			if (noFogBox != null)
			{
				noFogBox.Content = L.Get("ui.nofog");
				noFogBox.ToolTip = L.Get("ui.nofogtip");
				noFogBox.Visibility = gameLangButton.Visibility;
			}
		}
	}

	private void ShowGameLangMenu()
	{
		if (gameDir == null)
		{
			return;
		}
		string text = Settings.LoadGameLang();
		System.Windows.Controls.ContextMenu contextMenu = new System.Windows.Controls.ContextMenu();
		System.Windows.Controls.MenuItem menuItem = new System.Windows.Controls.MenuItem();
		menuItem.Header = L.Get("ui.gamelang.auto");
		menuItem.IsCheckable = true;
		menuItem.IsChecked = text.Length == 0;
		System.Windows.Controls.MenuItem menuItem2 = menuItem;
		menuItem2.Click += (object o, RoutedEventArgs e) =>
		{
			Settings.SaveGameLang("");
			UpdateGameLangButton();
		};
		contextMenu.Items.Add(menuItem2);
		List<string> item = Patcher.InstalledLocales(gameDir).Item1;
		if (item.Count > 0)
		{
			contextMenu.Items.Add(new Separator());
		}
		foreach (string item2 in item.Distinct())
		{
			string c = item2;
			System.Windows.Controls.MenuItem menuItem3 = new System.Windows.Controls.MenuItem();
			menuItem3.Header = Patcher.LocaleName(c) + "   " + c;
			menuItem3.IsCheckable = true;
			menuItem3.IsChecked = c == text;
			System.Windows.Controls.MenuItem menuItem4 = menuItem3;
			menuItem4.Click += (object o, RoutedEventArgs e) =>
			{
				Settings.SaveGameLang(c);
				UpdateGameLangButton();
			};
			contextMenu.Items.Add(menuItem4);
		}
		contextMenu.PlacementTarget = gameLangButton;
		contextMenu.Placement = PlacementMode.Top;
		contextMenu.IsOpen = true;
	}

	private void EditServers()
	{
		if (Servers.FilePath == null)
		{
			Servers.Load();
		}
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo("notepad.exe", "\"" + Servers.FilePath + "\"");
			processStartInfo.UseShellExecute = true;
			Process.Start(processStartInfo);
		}
		catch
		{
		}
	}

	private static void SetDot(Shape e, System.Windows.Media.Color c)
	{
		e.Fill = new SolidColorBrush(c);
	}

	private void FillNews(ServerStatus s)
	{
		newsList.Children.Clear();
		if (s.News.Count == 0)
		{
			newsList.Children.Add(new TextBlock
			{
				Text = L.Get("ui.nonews"),
				Foreground = (System.Windows.Media.Brush)Window.FindResource("Dim"),
				FontSize = 13.0
			});
			return;
		}
		foreach (FeedEntry item in s.News)
		{
			string text = item.Get("date");
			string text2 = item.Get("text");
			string text3 = item.Get("url");
			StackPanel stackPanel = new StackPanel();
			stackPanel.Margin = new Thickness(0.0, 0.0, 0.0, 16.0);
			StackPanel stackPanel2 = stackPanel;
			if (!string.IsNullOrEmpty(text))
			{
				stackPanel2.Children.Add(new TextBlock
				{
					Text = text,
					FontSize = 11.0,
					Foreground = (System.Windows.Media.Brush)Window.FindResource("Dim")
				});
			}
			TextBlock textBlock = new TextBlock();
			textBlock.Text = item.Get("title") ?? "";
			textBlock.FontSize = 14.0;
			textBlock.FontWeight = FontWeights.SemiBold;
			textBlock.TextWrapping = TextWrapping.Wrap;
			textBlock.Margin = new Thickness(0.0, 2.0, 0.0, 0.0);
			TextBlock textBlock2 = textBlock;
			if (!string.IsNullOrEmpty(text3))
			{
				string url = text3;
				textBlock2.Cursor = System.Windows.Input.Cursors.Hand;
				textBlock2.Foreground = (System.Windows.Media.Brush)Window.FindResource("Gold");
				textBlock2.MouseLeftButtonUp += (object o, MouseButtonEventArgs e) =>
				{
					OpenUrl(url);
				};
			}
			stackPanel2.Children.Add(textBlock2);
			if (!string.IsNullOrEmpty(text2))
			{
				stackPanel2.Children.Add(new TextBlock
				{
					Text = text2,
					FontSize = 12.5,
					Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(196, 187, 165)),
					TextWrapping = TextWrapping.Wrap,
					Margin = new Thickness(0.0, 3.0, 0.0, 0.0),
					LineHeight = 18.0
				});
			}
			newsList.Children.Add(stackPanel2);
		}
	}

	private void FillLinks(ServerStatus s)
	{
		linksPanel.Children.Clear();
		foreach (FeedEntry link in s.Links)
		{
			string url = link.Get("url");
			if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				System.Windows.Controls.Button button = new System.Windows.Controls.Button();
				button.Content = link.Get("label");
				button.Style = (Style)Window.FindResource("PillButton");
				System.Windows.Controls.Button button2 = button;
				button2.Click += (object o, RoutedEventArgs e) =>
				{
					OpenUrl(url);
				};
				linksPanel.Children.Add(button2);
			}
		}
	}

	private static void OpenUrl(string url)
	{
		if (string.IsNullOrEmpty(url) || !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo(url);
			processStartInfo.UseShellExecute = true;
			Process.Start(processStartInfo);
		}
		catch
		{
		}
	}

	private void SetGameDir(string dir, bool save)
	{
		gameDir = dir;
		Msg msg = Patcher.CheckGameDir(dir, out var version);
		folderText.Text = dir ?? "";
		folderText.ToolTip = dir;
		clientVersion = version;
		clientOk = msg == null;
		clientText.Text = ((version == null) ? "" : (L.Get("ui.client", version) + (clientOk ? "  ✓" : "")));
		folderButton.Content = L.Get((dir == null) ? "ui.pickfolder" : "ui.changefolder");
		UpdateGameLangButton();
		if (msg != null)
		{
			Step(msg);
			return;
		}
		Msg msg2 = Patcher.UntestedNotice(version);
		Step(msg2 ?? new Msg(MsgKind.Info, "ui.ready"));
		if (save)
		{
			Settings.SaveGameDir(dir);
		}
	}

	private void PickFolder()
	{
		using FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
		folderBrowserDialog.Description = L.Get("ui.folderdlg");
		folderBrowserDialog.ShowNewFolderButton = false;
		if (gameDir != null && Directory.Exists(gameDir))
		{
			folderBrowserDialog.SelectedPath = gameDir;
		}
		if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
		{
			string text = folderBrowserDialog.SelectedPath;
			string text2 = System.IO.Path.Combine(text, "_classic_beta_");
			if (!File.Exists(System.IO.Path.Combine(text, "WowB.exe")) && File.Exists(System.IO.Path.Combine(text2, "WowB.exe")))
			{
				text = text2;
			}
			SetGameDir(text, save: true);
		}
	}

	private void OpenLog()
	{
		if (gameDir == null)
		{
			return;
		}
		string text = System.IO.Path.Combine(gameDir, "Logs", "launcher.log");
		if (File.Exists(text))
		{
			try
			{
				ProcessStartInfo processStartInfo = new ProcessStartInfo(text);
				processStartInfo.UseShellExecute = true;
				Process.Start(processStartInfo);
				return;
			}
			catch
			{
				return;
			}
		}
		Step(new Msg(MsgKind.Dim, "ui.nolog"));
	}

	private void Play()
	{
		Msg msg = Patcher.CheckGameDir(gameDir, out var _);
		if (msg != null)
		{
			Step(new Msg(MsgKind.Error, msg.Key, msg.Args));
			if (!File.Exists(System.IO.Path.Combine(gameDir ?? "", "WowB.exe")))
			{
				PickFolder();
			}
			return;
		}
		Settings.SaveGameDir(gameDir);
		if (server == null)
		{
			Step(new Msg(MsgKind.Error, "srv.none"));
			return;
		}
		if (server.Portal.EndsWith("example.com", StringComparison.OrdinalIgnoreCase))
		{
			Step(new Msg(MsgKind.Error, "srv.example", server.Name));
			return;
		}
		Patcher patcher = new Patcher(gameDir, server)
		{
			StartNew = gameRunning
		};
		gameRunning = true;
		playButton.Content = L.Get("ui.playanother");
		folderButton.IsEnabled = false;
		serverButton.IsEnabled = false;
		patchers.Add(patcher);
		patcher.Message += (Msg m) =>
		{
			ui.BeginInvoke((Action)(() =>
			{
				Step(m);
			}));
		};
		patcher.PhaseChanged += (PatchPhase p) =>
		{
			ui.BeginInvoke((Action)(() =>
			{
				OnPhase(patcher, p);
			}));
		};
		Thread thread = new Thread(patcher.Run);
		thread.IsBackground = true;
		thread.Name = "patcher";
		Thread thread2 = thread;
		thread2.Start();
	}

	private void OnPhase(Patcher patcher, PatchPhase p)
	{
		switch (p)
		{
		case PatchPhase.WaitingLogin:
			ShowTray();
			Window.WindowState = WindowState.Minimized;
			break;
		case PatchPhase.Ready:
			SystemSounds.Asterisk.Play();
			if (tray != null)
			{
				tray.ShowBalloonTip(6000, L.Get("tray.readytitle"), L.Get("tray.readytext"), ToolTipIcon.Info);
			}
			break;
		case PatchPhase.Closed:
		case PatchPhase.Failed:
			patchers.Remove(patcher);
			if (patchers.Count <= 0)
			{
				gameRunning = false;
				playButton.IsEnabled = true;
				playButton.Content = L.Get("ui.play");
				folderButton.IsEnabled = true;
				serverButton.IsEnabled = true;
				HideTray();
				RestoreWindow();
			}
			break;
		}
	}

	private void Step(Msg m)
	{
		lastStep = m;
		string text = m.ToString();
		stepText.Text = text;
		System.Windows.Media.Color color = m.Kind switch
		{
			MsgKind.Good => Green, 
			MsgKind.Warn => Amber, 
			MsgKind.Error => Red, 
			MsgKind.Dim => System.Windows.Media.Color.FromRgb(176, 168, 148), 
			_ => System.Windows.Media.Color.FromRgb(237, 230, 214), 
		};
		stepText.Foreground = new SolidColorBrush(color);
		if (tray != null)
		{
			tray.Text = Truncate("Forever Launcher - " + text, 63);
		}
	}

	private static string Truncate(string s, int n)
	{
		if (s.Length > n)
		{
			return s.Substring(0, n - 1) + "…";
		}
		return s;
	}

	private void ShowTray()
	{
		if (tray == null)
		{
			tray = new NotifyIcon
			{
				Icon = AppIcon(),
				Text = "Forever Launcher",
				Visible = true
			};
			BuildTrayMenu();
			tray.DoubleClick += (object s, EventArgs e) =>
			{
				RestoreWindow();
			};
		}
	}

	private void BuildTrayMenu()
	{
		ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
		contextMenuStrip.Items.Add(L.Get("tray.open"), null, (object s, EventArgs e) =>
		{
			RestoreWindow();
		});
		contextMenuStrip.Items.Add(L.Get("tray.exit"), null, (object s, EventArgs e) =>
		{
			ConfirmExit();
		});
		ContextMenuStrip contextMenuStrip2 = tray.ContextMenuStrip;
		tray.ContextMenuStrip = contextMenuStrip;
		contextMenuStrip2?.Dispose();
	}

	private void HideTray()
	{
		if (tray != null)
		{
			tray.Visible = false;
			tray.Dispose();
			tray = null;
		}
	}

	private void HideToTray()
	{
		ShowTray();
		Window.Hide();
		tray.ShowBalloonTip(4000, "Forever Launcher", L.Get("tray.hidden"), ToolTipIcon.None);
	}

	private void RestoreWindow()
	{
		Window.Show();
		if (Window.WindowState == WindowState.Minimized)
		{
			Window.WindowState = WindowState.Normal;
		}
		Window.Activate();
	}

	private void OnCloseClicked()
	{
		if (gameRunning)
		{
			HideToTray();
		}
		else
		{
			Window.Close();
		}
	}

	private void ConfirmExit()
	{
		if (gameRunning)
		{
			MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(L.Get("exit.confirm"), "Forever Launcher", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
			if (messageBoxResult != MessageBoxResult.Yes)
			{
				return;
			}
		}
		Shutdown();
	}

	private void Shutdown()
	{
		foreach (Patcher patcher in patchers)
		{
			patcher.Stop();
		}
		HideTray();
		app.Shutdown();
	}
}
