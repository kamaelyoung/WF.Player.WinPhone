﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WF.Player.Core;
using System.IO.IsolatedStorage;
using Geowigo.Models;
using System.Collections.Generic;
using Microsoft.Phone.Controls;
using System.IO;
using System.Windows.Navigation;
using System.Linq;

namespace Geowigo.ViewModels
{	
	/// <summary>
	/// The application view model, which is responsible for application-wide flow and control of the app and game UI.
	/// </summary>
	public class AppViewModel
	{

		#region Fields

		private MessageBoxManager _MBManagerInstance;

		#endregion

		#region Properties

		#region Model
		/// <summary>
		/// Gets or sets the wherigo model used by this ViewModel.
		/// </summary>
		public WherigoModel Model 
		{
			get
			{
				return _Model;
			}
			set
			{
				if (_Model != value)
				{
					// Unregisters the current model.
					if (_Model != null)
					{
						UnregisterModel(_Model);
					}

					// Changes the model.
					_Model = value;

					// Registers the new model.
					if (_Model != null)
					{
						RegisterModel(_Model);
					}
				}
			}
		}

		private WherigoModel _Model;
		#endregion

		#region AppTitle

		/// <summary>
		/// Gets the title of the application.
		/// </summary>
		public string AppTitle
		{
			get
			{
				return "Geowigo Wherigo Player";
			}
		}

		#endregion

		#region MessageBoxManager

		/// <summary>
		/// Gets the message box manager for this view model.
		/// </summary>
		public MessageBoxManager MessageBoxManagerInstance
		{
			get
			{
				return _MBManagerInstance ?? (_MBManagerInstance = new MessageBoxManager());
			}
		}

		#endregion

		#endregion

		#region Constructors


		
		#endregion

		#region Public Methods

		/// <summary>
		/// Navigates the app to the main page of the app.
		/// </summary>
		public void NavigateToAppHome(bool stopCurrentGame = false)
		{
			// Stops the current game if needed.
			if (stopCurrentGame && Model.Core.Cartridge != null)
			{
				Model.Core.Stop();
			}

			// Removes all back entries until the app home is found.
			string prefix = "/Views/";
			foreach (JournalEntry entry in App.Current.RootFrame.BackStack.ToList())
			{
				if (entry.Source.ToString().StartsWith(prefix + "HomePage.xaml"))
				{
					break;
				}

				// Removes the current entry.
				App.Current.RootFrame.RemoveBackEntry();
			}

			// If there is a back entry, goes back: it is the game home.
			// Otherwise, navigates to the game home.
			if (App.Current.RootFrame.BackStack.Count() > 0)
			{
				App.Current.RootFrame.GoBack();
			}
			else
			{
				App.Current.RootFrame.Navigate(new Uri(prefix + "HomePage.xaml", UriKind.Relative));
			}
		}

		/// <summary>
		/// Navigates the app to the game main page of a cartridge.
		/// </summary>
		public void NavigateToGameHome(string filename)
		{
			App.Current.RootFrame.Navigate(new Uri(String.Format("/Views/GameHomePage.xaml?{0}={1}", GameHomeViewModel.CartridgeFilenameKey, filename), UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the game main page of a cartridge at a specific section.
		/// </summary>
		public void NavigateToGameHome(string filename, string section)
		{
			App.Current.RootFrame.Navigate(new Uri(String.Format("/Views/GameHomePage.xaml?{0}={1}&{2}={3}", GameHomeViewModel.CartridgeFilenameKey, filename, GameHomeViewModel.SectionKey, section), UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the view that best fits an Input object.
		/// </summary>
		/// <param name="wherigoObj"></param>
		public void NavigateToView(Input wherigoObj)
		{
			App.Current.RootFrame.Navigate(new Uri("/Views/InputPage.xaml?wid=" + wherigoObj.ObjIndex, UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the view that best fits a Thing object.
		/// </summary>
		/// <param name="wherigoObj"></param>
		public void NavigateToView(Thing wherigoObj)
		{
			App.Current.RootFrame.Navigate(new Uri("/Views/ThingPage.xaml?wid=" + wherigoObj.ObjIndex, UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the view that best fits a Task object.
		/// </summary>
		/// <param name="wherigoObj"></param>
		public void NavigateToView(Task wherigoObj)
		{
			App.Current.RootFrame.Navigate(new Uri("/Views/TaskPage.xaml?wid=" + wherigoObj.ObjIndex, UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the view that best fits a UIObject object.
		/// </summary>
		/// <param name="wherigoObj"></param>
		public void NavigateToView(UIObject wherigoObj)
		{
			if (wherigoObj is Thing)
			{
				NavigateToView((Thing)wherigoObj);
			}
			else if (wherigoObj is Task)
			{
				NavigateToView((Task)wherigoObj);
			}
		}

		/// <summary>
		/// Displays a message box. If a message box is currently on-screen, it will be cancelled.
		/// </summary>
		/// <param name="mbox"></param>
		public void ShowMessageBox(WF.Player.Core.MessageBox mbox)
		{
			// Delegates this to the message box manager.
			MessageBoxManagerInstance.Show(mbox);
		}

		/// <summary>
		/// Navigates one step back in the game activity.
		/// </summary>
		/// <remarks>
		/// This method has no effect if the previous view in the stack is not a game view.
		/// </remarks>
		public void NavigateBack()
		{
			// Returns if the previous view is not a game view.
			JournalEntry previousPage = App.Current.RootFrame.BackStack.FirstOrDefault();
			if (previousPage == null)
			{
				System.Diagnostics.Debug.WriteLine("Warning: NavigateBack() cancelled because no page in the stack.");
				
				return;
			}
			string previousPageName = previousPage.Source.ToString();
			string prefix = "/Views/";
			if (!(previousPageName.StartsWith(prefix + "GameHomePage.xaml") ||
				previousPageName.StartsWith(prefix + "InputPage.xaml") ||
				previousPageName.StartsWith(prefix + "TaskPage.xaml") ||
				previousPageName.StartsWith(prefix + "ThingPage.xaml")))
			{
				System.Diagnostics.Debug.WriteLine("Warning: NavigateBack() cancelled because previous page is no game!");
				
				return;
			}

			// Goes back.
			App.Current.RootFrame.GoBack();
		}

		/// <summary>
		/// Clears the navigation back stack, making the current view the first one.
		/// </summary>
		public void ClearBackStack()
		{
			int entriesToRemove = App.Current.RootFrame.BackStack.Count();
			for (int i = 0; i < entriesToRemove; i++)
			{
				App.Current.RootFrame.RemoveBackEntry();
			}
		}

		#endregion

		#region Private Methods

		private void RegisterModel(WherigoModel model)
		{
			model.Core.InputRequested += new EventHandler<ObjectEventArgs<Input>>(Core_InputRequested);
			model.Core.ShowMessageBoxRequested += new EventHandler<MessageBoxEventArgs>(Core_MessageBoxRequested);
			model.Core.ShowScreenRequested += new EventHandler<ScreenEventArgs>(Core_ScreenRequested);
			model.Core.PlayMediaRequested += new EventHandler<ObjectEventArgs<Media>>(Core_PlaySoundRequested);

			// Temp debug
			model.Core.SaveRequested += new EventHandler<CartridgeEventArgs>(Core_SaveRequested);
			model.Core.AttributeChanged += new EventHandler<AttributeChangedEventArgs>(Core_AttributeChanged);
			model.Core.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Core_PropertyChanged);
		}

		void Core_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "IsBusy")
			{
				Microsoft.Phone.Shell.SystemTray.ProgressIndicator = new Microsoft.Phone.Shell.ProgressIndicator()
				{
					IsIndeterminate = true,
					IsVisible = Model.Core.IsBusy,
					Text = "Loading..."
				};
			}
		}

		void Core_AttributeChanged(object sender, AttributeChangedEventArgs e)
		{
			string name = e.Object is Thing ? ((Thing)e.Object).Name : e.Object.ToString();
			System.Diagnostics.Debug.WriteLine("AttributeChanged: " + name + "." + e.PropertyName);
		}

		void Core_SaveRequested(object sender, CartridgeEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine("WARNING!!! Core_SaveRequested NOT IMPLEMENTED !!!!!");
		}

		private void UnregisterModel(WherigoModel model)
		{
			model.Core.InputRequested -= new EventHandler<ObjectEventArgs<Input>>(Core_InputRequested);
			model.Core.ShowMessageBoxRequested -= new EventHandler<MessageBoxEventArgs>(Core_MessageBoxRequested);
			model.Core.ShowScreenRequested -= new EventHandler<ScreenEventArgs>(Core_ScreenRequested);
			model.Core.PlayMediaRequested -= new EventHandler<ObjectEventArgs<Media>>(Core_PlaySoundRequested);
		}

		private void PlayMediaSound(Media media)
		{
			System.Diagnostics.Debug.WriteLine("WARNING!!! PlayMediaSound NOT IMPLEMENTED !!!!!");
			
			//using (var stream = new MemoryStream(media.Data))
			//{
			//    var effect = SoundEffect.FromStream(stream);
			//    FrameworkDispatcher.Update();
			//    effect.Play();
			//}
		}

		#region Core Event Handlers

		private void Core_InputRequested(object sender, ObjectEventArgs<Input> e)
		{
			// Navigates to the input view.
			NavigateToView(e.Object);
		}

		private void Core_MessageBoxRequested(object sender, MessageBoxEventArgs e)
		{
			// Displays the message box.
			ShowMessageBox(e.Descriptor);
		}

		private void Core_ScreenRequested(object sender, ScreenEventArgs e)
		{
			// Shows the right screen depending on the event.
			switch (e.Screen)
			{
				case ScreenType.Main:
					NavigateToGameHome(Model.Core.Cartridge.Filename, GameHomeViewModel.SectionValue_Overview);
					break;

				case ScreenType.Locations:
				case ScreenType.Items:
					NavigateToGameHome(Model.Core.Cartridge.Filename, GameHomeViewModel.SectionValue_World);
					break;

				case ScreenType.Inventory:
					NavigateToGameHome(Model.Core.Cartridge.Filename, GameHomeViewModel.SectionValue_Inventory);
					break;

				case ScreenType.Tasks:
					NavigateToGameHome(Model.Core.Cartridge.Filename, GameHomeViewModel.SectionValue_Tasks);
					break;

				case ScreenType.Details:
					NavigateToView(e.Object);
					break;

				default:
					throw new InvalidOperationException(String.Format("Unknown WherigoScreenKind cannot be processed: {0}", e.Screen.ToString()));
			}
		}

		private void Core_PlaySoundRequested(object sender, ObjectEventArgs<Media> e)
		{
			// TODO: Pass to SoundManager for uncompressing to isolated storage and playing using a MediaElement?
			//PlayMediaSound(e.Object);
		}


		#endregion

		#endregion
	}
}
