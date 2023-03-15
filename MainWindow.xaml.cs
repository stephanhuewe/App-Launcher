using AMS.Profile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace App_Launcher
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
    {
		public MainWindow()
		{
			InitializeComponent();
		}

		[DllImport("user32.dll")]
		private static extern bool ReleaseCapture();

		[DllImport("user32.dll")]
		private static extern int SendMessage(IntPtr hwnd, int msg, int wp, int lp);

		private Ini _config;

		private void DragWindow(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				ReleaseCapture();
				SendMessage(new WindowInteropHelper(this).Handle, 161, 2, 0);
			}
		}

		private void CloseWindow(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
				Application.Current.Shutdown();
		}

		private void MaximizeWindow(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				if (WindowState == WindowState.Maximized)
				{
					WindowState = WindowState.Normal;
					Maximize.Content = "1";
				}
				else if (WindowState == WindowState.Normal)
				{
					WindowState = WindowState.Maximized;
					Maximize.Content = "2";
				}
			}
		}

		private void MinimizeWindow(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
				WindowState = WindowState.Minimized;
		}

		private void ViewInExplorer(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				Process.Start("explorer.exe", Path.GetDirectoryName(_config.Name) ?? string.Empty);
			}
		}

		private void AddCategory(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				CategoryPrompt categoryPrompt = new CategoryPrompt
				{
					Owner = this,
				};
				categoryPrompt.Closed += (_, _) =>
				{
					if (categoryPrompt.PromptSucceeded() && categoryPrompt.ConfirmClicked)
					{
						string categoryName = categoryPrompt.GetCategoryName();
						ListBoxItem categoryItem = CreateCategory(categoryName);

						Categories.Items.Add(categoryItem);
						_programs.Add(categoryItem, new List<ListBoxItem>());

						string configInfo;
						using (StreamReader reader = new StreamReader(_config.Name))
						{
							configInfo = reader.ReadToEnd();
							reader.Close();
						}
						using (StreamWriter writer = new StreamWriter(_config.Name))
						{
							writer.WriteLine($"{configInfo}\n[{categoryName}]");
							writer.Close();
						}

						categoryItem.PreviewMouseLeftButtonDown += SelectCategory;

						_currentCategory = categoryItem;
						LoadPrograms();
					}
					else
					{
						MessageBox.Show("Could not add category because no information was specified.", "No Category Information Given",
							MessageBoxButton.OK, MessageBoxImage.Error);
					}
				};
				categoryPrompt.ShowDialog();
			}
		}

		private void AddProgram(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				ProgramPrompt programPrompt = new ProgramPrompt
				{
					Owner = this,
				};
				programPrompt.Closed += (_, _) =>
				{
					if (programPrompt.PromptSucceeded() && programPrompt.ConfirmClicked)
					{
						(string, string) program = programPrompt.GetProgram();
						ListBoxItem programItem = CreateItem(program.Item1, program.Item2);

						Programs.Items.Add(programItem);
						List<ListBoxItem> programs = this._programs[_currentCategory];
						programs.Add(programItem);
						this._programs[_currentCategory] = programs;

						_config.SetValue(((TextBlock)_currentCategory.Content).Text, program.Item1, program.Item2);

						programItem.PreviewMouseLeftButtonDown += SelectProgram;
					}
					else
					{
						MessageBox.Show("Could not add program because no information was specified.", "No Program Information Given",
							MessageBoxButton.OK, MessageBoxImage.Error);
					}
				};
				programPrompt.ShowDialog();
			}
		}

		protected override void OnStateChanged(EventArgs e)
		{
			base.OnStateChanged(e);

			if (WindowState == WindowState.Maximized)
			{
				Maximize.Content = "2";
				Maximize.ToolTip = "Restore";
				ContentWindow.Padding = new Thickness(8);
			}
			else if (WindowState == WindowState.Normal)
			{
				Maximize.Content = "1";
				Maximize.ToolTip = "Maximize";
				ContentWindow.Padding = new Thickness(0);
			}
		}

		private ListBoxItem CreateItem(string title, string path)
		{
			ListBoxItem listItem = new ListBoxItem();
			Grid itemLayout = new Grid();
			TextBlock itemTitle = new TextBlock();
			TextBlock itemPath = new TextBlock();
			RowDefinition row1 = new RowDefinition();
			RowDefinition row2 = new RowDefinition();

			row1.Height = new GridLength(20);
			row2.Height = new GridLength(15);

			itemLayout.Margin = new Thickness(10);
			itemLayout.RowDefinitions.Add(row1);
			itemLayout.RowDefinitions.Add(row2);

			itemTitle.Text = title;
			itemTitle.FontFamily = new FontFamily("Segoe UI");
			itemTitle.FontSize = 14;
			itemTitle.VerticalAlignment = VerticalAlignment.Center;

			itemPath.Text = path;
			itemPath.SetValue(Grid.RowProperty, 1);
			itemPath.FontFamily = new FontFamily("Segoe UI");
			itemPath.FontSize = 11;
			itemPath.VerticalAlignment = VerticalAlignment.Center;

			listItem.BorderThickness = new Thickness(0);
			listItem.Cursor = Cursors.Hand;
			listItem.Focusable = false;

			ContextMenu contextMenu = new ContextMenu();
			MenuItem modifyItem = new MenuItem();
			MenuItem removeItem = new MenuItem();

			modifyItem.PreviewMouseLeftButtonDown += (_, _) =>
			{
				ModifyProgram(listItem);
			};
			modifyItem.Loaded += (_, _) =>
			{
				modifyItem.Header = $"Modify '{itemTitle.Text}'";
			};
			modifyItem.FontFamily = new FontFamily("Segoe UI");
			modifyItem.FontSize = 12;
			modifyItem.Cursor = Cursors.Hand;

			removeItem.PreviewMouseLeftButtonDown += (_, _) =>
			{
				List<ListBoxItem> programs = this._programs[_currentCategory];
				programs.Remove(listItem);

				this._programs[_currentCategory] = programs;
				Programs.Items.Remove(listItem);

				_config.RemoveEntry(((TextBlock)_currentCategory.Content).Text, itemTitle.Text);
			};
			removeItem.Loaded += (_, _) =>
			{
				removeItem.Header = $"Remove '{itemTitle.Text}'";
			};
			removeItem.FontFamily = new FontFamily("Segoe UI");
			removeItem.FontSize = 12;
			removeItem.Cursor = Cursors.Hand;

			contextMenu.Items.Add(modifyItem);
			contextMenu.Items.Add(removeItem);

			itemLayout.Children.Add(itemTitle);
			itemLayout.Children.Add(itemPath);
			//itemLayout.Children.Add(itemPath);

            //BitmapImage myBitmapImage = new BitmapImage();
            //InlineUIContainer c = new InlineUIContainer(myBitmapImage);
            //itemLayout.Children.Add(c);

            listItem.Content = itemLayout;
			listItem.ContextMenu = contextMenu;

			GC.Collect();

			return listItem;
		}

		private ListBoxItem CreateCategory(string title)
		{
			ListBoxItem listItem = new ListBoxItem();
			TextBlock itemTitle = new TextBlock();

			itemTitle.FontFamily = new FontFamily("Segoe UI");
			itemTitle.FontSize = 16;
			itemTitle.Margin = new Thickness(10);

			listItem.BorderThickness = new Thickness(0);
			listItem.Cursor = Cursors.Hand;
			listItem.Focusable = false;

			ContextMenu contextMenu = new ContextMenu();
			MenuItem modifyItem = new MenuItem();
			MenuItem removeItem = new MenuItem();

			modifyItem.PreviewMouseLeftButtonDown += (_, _) =>
			{
				ModifyCategory(listItem);
			};
			modifyItem.Loaded += (_, _) =>
			{
				modifyItem.Header = $"Modify '{itemTitle.Text}'";
			};
			modifyItem.FontFamily = new FontFamily("Segoe UI");
			modifyItem.FontSize = 12;
			modifyItem.Cursor = Cursors.Hand;

			removeItem.PreviewMouseLeftButtonDown += (_, _) =>
			{
				_programs.Remove(listItem);
				Categories.Items.Remove(listItem);

				_currentCategory = (ListBoxItem)Categories.Items[0];
				LoadPrograms();

				_config.RemoveSection(itemTitle.Text);

				File.Delete(_config.Name);
				File.Create(_config.Name).Close();

				foreach (ListBoxItem category in _programs.Keys)
				{
					string section = ((TextBlock)category.Content).Text;

					foreach (ListBoxItem program in _programs[category])
					{
						string name = ((TextBlock)((Grid)program.Content).Children[0]).Text;
						string path = ((TextBlock)((Grid)program.Content).Children[1]).Text;

						_config.SetValue(section, name, path);
					}

					string currentIni = File.ReadAllText(_config.Name);
					currentIni += category == _programs.Keys.ElementAt(_programs.Keys.Count - 1) ? "" : "\n";

					File.WriteAllText(_config.Name, currentIni);
				}
			};
			removeItem.Loaded += (_, _) =>
			{
				removeItem.Header = $"Remove '{itemTitle.Text}'";
			};
			removeItem.FontFamily = new FontFamily("Segoe UI");
			removeItem.FontSize = 12;
			removeItem.Cursor = Cursors.Hand;

			contextMenu.Items.Add(modifyItem);
			contextMenu.Items.Add(removeItem);

			itemTitle.Text = title;
			listItem.Content = itemTitle;
			listItem.ContextMenu = contextMenu;

			GC.Collect();

			return listItem;
		}

		private readonly Dictionary<ListBoxItem, List<ListBoxItem>> _programs = new Dictionary<ListBoxItem, List<ListBoxItem>>();
		private ListBoxItem _currentCategory;

		private void LoadPrograms()
		{
			Programs.Items.Clear();

			foreach (ListBoxItem program in _programs[_currentCategory])
			{
				Programs.Items.Add(program);
			}
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			if (!File.Exists(Environment.CurrentDirectory + "\\config.ini"))
			{
				using (StreamWriter writer = new StreamWriter(File.Create(Environment.CurrentDirectory + "\\config.ini")))
				{
					writer.WriteLine("[Basic Utilities]");
					writer.WriteLine("Notepad=notepad.exe");
					writer.WriteLine("WordPad=wordpad.exe");
					writer.WriteLine("Calculator=calc.exe");
					writer.WriteLine("");
					writer.WriteLine("[Text Editing]");
					writer.WriteLine("Notepad=notepad.exe");
					writer.WriteLine("WordPad=wordpad.exe");
					writer.WriteLine("");
					writer.WriteLine("[Mathematics]");
					writer.WriteLine("Calculator=calc.exe");
				}
			}

			_config = new Ini(Environment.CurrentDirectory + "\\config.ini");

			string[] categories = _config.GetSectionNames();

			foreach (string category in categories)
			{
				ListBoxItem categoryItem = CreateCategory(category);
				string[] programs = _config.GetEntryNames(category);
				List<ListBoxItem> programItems = new List<ListBoxItem>();

				categoryItem.PreviewMouseLeftButtonDown += SelectCategory;

				foreach (string program in programs)
				{
					ListBoxItem programItem = CreateItem(program, _config.GetValue(category, program).ToString());
					programItem.PreviewMouseLeftButtonDown += SelectProgram;

					programItems.Add(programItem);
				}

				this._programs.Add(categoryItem, programItems);
				Categories.Items.Add(categoryItem);
			}

			_currentCategory = _programs.Keys.ElementAt(0);
			LoadPrograms();
		}

		private void SelectProgram(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				ListBoxItem listItem = (ListBoxItem)sender;
				Grid itemLayout = (Grid)listItem.Content;
				TextBlock itemPath = (TextBlock)itemLayout.Children[1];
				string path = itemPath.Text;

				Process.Start(path);

				GC.Collect();
			}
		}

		private void SelectCategory(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				_currentCategory = (ListBoxItem)sender;

				LoadPrograms();
			}
		}

		private void ModifyProgram(object sender)
		{
			Grid grid = (Grid)((ListBoxItem)sender).Content;
			string name = ((TextBlock)grid.Children[0]).Text;
			string path = ((TextBlock)grid.Children[1]).Text;

			ProgramPrompt programPrompt = new ProgramPrompt
			{
				Owner = this,
			};
			programPrompt.SetDefault(name, path);
			programPrompt.Closed += (_, _) =>
			{
				if (programPrompt.PromptSucceeded() && programPrompt.ConfirmClicked)
				{
					(string, string) program = programPrompt.GetProgram();
					ListBoxItem programItem = CreateItem(program.Item1, program.Item2);

					List<ListBoxItem> programs = this._programs[_currentCategory];

					int programIndex = 0;
					foreach (ListBoxItem tempItem in programs)
					{
						string nameFromItem = ((TextBlock)((Grid)tempItem.Content).Children[0]).Text;
						string pathFromItem = ((TextBlock)((Grid)tempItem.Content).Children[1]).Text;

						if (nameFromItem == name && pathFromItem == path)
						{
							programIndex = programs.IndexOf(tempItem);
						}
					}

					ChangeEntryName(_config, ((TextBlock)_currentCategory.Content).Text, name, program.Item1);
					_config.SetValue(((TextBlock)_currentCategory.Content).Text, program.Item1, program.Item2);

					programs[programIndex] = programItem;
					this._programs[_currentCategory] = programs;
					LoadPrograms();

					programItem.PreviewMouseLeftButtonDown += SelectProgram;
				}
				else
				{
					MessageBox.Show("Could not add program because no information was specified.", "No Program Information Given",
						MessageBoxButton.OK, MessageBoxImage.Error);
				}
			};
			programPrompt.ShowDialog();
		}

		private void ModifyCategory(object sender)
		{
			string title = ((TextBlock)((ListBoxItem)sender).Content).Text;

			CategoryPrompt categoryPrompt = new CategoryPrompt
			{
				Owner = this,
			};
			categoryPrompt.SetDefault(title);
			categoryPrompt.Closed += (_, _) =>
			{
				if (categoryPrompt.PromptSucceeded() && categoryPrompt.ConfirmClicked)
				{
					string categoryName = categoryPrompt.GetCategoryName();

					string iniConfig;
					using (StreamReader reader = new StreamReader(_config.Name))
					{
						iniConfig = reader.ReadToEnd();
						iniConfig = iniConfig.Replace($"[{title}]", $"[{categoryName}]");

						reader.Close();
					}

					File.Delete(_config.Name);
					File.Create(_config.Name).Close();

					using (StreamWriter writer = new StreamWriter(_config.Name))
					{
						writer.Write(iniConfig);
					}

					List<(ListBoxItem, List<ListBoxItem>)> tempPrograms = new List<(ListBoxItem, List<ListBoxItem>)>();

					foreach (ListBoxItem category in _programs.Keys.ToArray())
					{
						tempPrograms.Add((category, _programs[category]));
						_programs.Remove(category);
					}

					foreach ((ListBoxItem, List<ListBoxItem>) programSet in tempPrograms)
					{
						int programIndex = tempPrograms.IndexOf(programSet);
						(ListBoxItem, List<ListBoxItem>) tempSet = programSet;
						string tempCategory = ((TextBlock)tempSet.Item1.Content).Text;

						if (tempCategory == categoryName)
						{
							((TextBlock)tempSet.Item1.Content).Text = categoryName;
							tempPrograms[programIndex] = tempSet;

						}

						_programs.Add(tempSet.Item1, tempSet.Item2);
					}

					((TextBlock)((ListBoxItem)sender).Content).Text = categoryName;

					//MessageBox.Show("The application needs to restart to apply changes to the category list.", "Application Restart Required",
					//	MessageBoxButton.OK, MessageBoxImage.Information);

					//Process.Start($"{Environment.CurrentDirectory}\\{Assembly.GetExecutingAssembly().GetName().Name}.exe");
					//Application.Current.Shutdown();
				}
				else
				{
					MessageBox.Show("Could not add category because no information was specified.", "No Category Information Given",
						MessageBoxButton.OK, MessageBoxImage.Error);
				}
			};
			categoryPrompt.ShowDialog();
		}

		private void ChangeEntryName(Ini config, string section, string entry, string newEntry)
		{
			foreach (string s in config.GetEntryNames(section))
			{
				bool comparison = s == entry;
				string value = config.GetValue(section, s).ToString();

				config.RemoveEntry(section, s);
				config.SetValue(section, comparison ? newEntry : s, value);
			}
		}
	}
}
