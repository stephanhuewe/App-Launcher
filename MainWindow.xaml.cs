﻿using AMS.Profile;
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
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		[DllImport("user32.dll")]
		private static extern bool ReleaseCapture();

		[DllImport("user32.dll")]
		private static extern int SendMessage(IntPtr hwnd, int msg, int wp, int lp);

		private Ini config = null;

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

		private ListBoxItem CreateItem(string Title, string Path)
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

			itemTitle.Text = Title;
			itemTitle.FontFamily = new FontFamily("Segoe UI");
			itemTitle.FontSize = 14;
			itemTitle.VerticalAlignment = VerticalAlignment.Center;

			itemPath.Text = Path;
			itemPath.SetValue(Grid.RowProperty, 1);
			itemPath.FontFamily = new FontFamily("Segoe UI");
			itemPath.FontSize = 11;
			itemPath.VerticalAlignment = VerticalAlignment.Center;

			listItem.BorderThickness = new Thickness(0);
			listItem.Cursor = Cursors.Hand;
			listItem.Focusable = false;

			itemLayout.Children.Add(itemTitle);
			itemLayout.Children.Add(itemPath);
			listItem.Content = itemLayout;

			GC.Collect();

			return listItem;
		}

		private ListBoxItem CreateCategory(string Title)
		{
			ListBoxItem listItem = new ListBoxItem();
			TextBlock itemTitle = new TextBlock();

			itemTitle.FontFamily = new FontFamily("Segoe UI");
			itemTitle.FontSize = 16;
			itemTitle.Margin = new Thickness(10);

			listItem.BorderThickness = new Thickness(0);
			listItem.Cursor = Cursors.Hand;
			listItem.Focusable = false;

			itemTitle.Text = Title;
			listItem.Content = itemTitle;

			GC.Collect();

			return listItem;
		}

		private Dictionary<ListBoxItem, List<ListBoxItem>> programs = new Dictionary<ListBoxItem, List<ListBoxItem>>();
		private ListBoxItem currentCategory = null;

		private void LoadPrograms()
		{
			Programs.Items.Clear();

			foreach (ListBoxItem program in programs[currentCategory])
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
			
			config = new Ini(Environment.CurrentDirectory + "\\config.ini");

			string[] categories = config.GetSectionNames();
			
			foreach (string category in categories)
			{
				ListBoxItem categoryItem = CreateCategory(category);
				string[] programs = config.GetEntryNames(category);
				List<ListBoxItem> programItems = new List<ListBoxItem>();

				categoryItem.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(SelectCategory);

				foreach (string program in programs)
				{
					ListBoxItem programItem = CreateItem(program, config.GetValue(category, program).ToString());
					programItem.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(SelectProgram);

					programItems.Add(programItem);
				}

				this.programs.Add(categoryItem, programItems);
				Categories.Items.Add(categoryItem);
			}

			currentCategory = programs.Keys.ElementAt(0);
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
				currentCategory = (ListBoxItem)sender;

				LoadPrograms();
			}
		}
	}
}
