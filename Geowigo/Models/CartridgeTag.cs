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
using System.IO;
using System.Windows.Media.Imaging;
using Geowigo.Utils;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace Geowigo.Models
{
	/// <summary>
	/// Provides a static metadata description and cache of a Cartridge.
	/// </summary>
	public class CartridgeTag : INotifyPropertyChanged
	{
		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
		
		#region Constants

		public const string GlobalCachePath = "/Cache";

		public const int SmallThumbnailMinWidth = 173;
		public const int BigThumbnailMinWidth = 432;

		private const string ThumbCacheFilename = "thumb.jpg";
		private const string PosterCacheFilename = "poster.jpg";

		#endregion

		#region Fields

		private ImageSource _thumbnail;
		private ImageSource _poster;
		private Dictionary<int, string> _soundFiles;
        private List<CartridgeSavegame> _savegames;

		#endregion
		
		#region Properties

		/// <summary>
		/// Gets the path to the cache folder for the Cartridge.
		/// </summary>
		public string PathToCache { get; private set; }

		/// <summary>
		/// Gets the unique identifier for the Cartridge.
		/// </summary>
		public string Guid { get; private set; }

		/// <summary>
		/// Gets the title of the Cartridge.
		/// </summary>
		public string Title { get; private set; }

		/// <summary>
		/// Gets the Cartridge object.
		/// </summary>
		public Cartridge Cartridge { get; private set; }

		/// <summary>
		/// Gets the cached thumbnail icon for the Cartridge.
		/// </summary>
		public ImageSource Thumbnail
		{
			get
			{
				return _thumbnail;
			}

			private set
			{
				if (_thumbnail != value)
				{
					_thumbnail = value;

					RaisePropertyChanged("Thumbnail");
				}
			}
		} 

		/// <summary>
		/// Gets the cached poster image for the Cartridge.
		/// </summary>
		public ImageSource Poster
		{
			get
			{
				return _poster;
			}

			private set
			{
				if (_poster != value)
				{
					_poster = value;

					RaisePropertyChanged("Poster");
				}
			}
		}

		/// <summary>
		/// Gets the cached filenames of sounds of this Cartridge.
		/// </summary>
		public IDictionary<int, string> Sounds
		{
			get
			{
				return _soundFiles;
			}
		}

        /// <summary>
        /// Gets the available savegames for the cartridge.
        /// </summary>
        public IEnumerable<CartridgeSavegame> Savegames
        {
            get
            {
                return _savegames;
            }
        }

		#endregion

		#region Constructors
		/// <summary>
		/// Constructs an uncached CartridgeTag from the basic metadata of a Cartridge.
		/// </summary>
		/// <param name="cart"></param>
		public CartridgeTag(Cartridge cart)
		{
			if (cart == null)
			{
				throw new ArgumentNullException("cart");
			}

			// Basic metadata.
			Cartridge = cart;
			Guid = cart.Guid;
			Title = cart.Name;
			PathToCache = GlobalCachePath + "/" + Guid;
		} 
		#endregion

		#region Public Methods
		/// <summary>
		/// Imports or create the cache for this CartridgeTag.
		/// </summary>
		public void ImportOrMakeCache()
		{
			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{
				// Ensures the cache folder exists.
				isf.CreateDirectory(PathToCache);

				// Thumbnail
				string thumbCachePath = GetCachePathCore(ThumbCacheFilename);
				if (isf.FileExists(thumbCachePath))
				{
					Thumbnail = ImageUtils.GetBitmapSource(thumbCachePath, isf);
				}
				else
				{
					Thumbnail = ImageUtils.SaveThumbnail(isf, thumbCachePath, Cartridge.Icon, Cartridge.Poster, SmallThumbnailMinWidth);
				}

				// Poster
				string posterCachePath = GetCachePathCore(PosterCacheFilename);
				if (isf.FileExists(posterCachePath))
				{
					Poster = ImageUtils.GetBitmapSource(posterCachePath, isf);
				}
				else
				{
					Poster = ImageUtils.SaveThumbnail(isf, posterCachePath, Cartridge.Poster, null, BigThumbnailMinWidth);
				}

				// Sounds
				ImportOrMakeSoundsCache(isf);

                // Savegames
                ImportSavegamesCache(isf);
			}
		}

		/// <summary>
		/// Gets the path to the cached version of a media.
		/// </summary>
		/// <param name="media"></param>
		/// <returns>The isostore path of the media if it is cached, null otherwise.</returns>
		public string GetCachePath(Media media)
		{
			string filename = GetCachePathCore(media);

			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{
				return isf.FileExists(filename) ? filename : null;
			}
		}

        /// <summary>
        /// Adds a savegame to this tag.
        /// </summary>
        /// <param name="cs">The savegame to add.</param>
        public void AddSavegame(CartridgeSavegame cs)
        {
            // Makes sure the savegame is exported to the cache.
            cs.ExportToCache();
            
            // Adds the savegame.
            _savegames.Add(cs);

            // Notifies of a change.
            RaisePropertyChanged("Savegames");
        }

		#endregion

		#region Private Methods

		private string GetCachePathCore(string filename)
		{
			return PathToCache + "/" + filename;
		}

		private string GetCachePathCore(Media media)
		{
			return GetCachePathCore(String.Format("{0}.{1}", media.MediaId, media.Type.ToString()));
		}

		private void ImportOrMakeSoundsCache(IsolatedStorageFile isf)
		{
			_soundFiles = new Dictionary<int, string>();

			foreach (var sound in Cartridge.Resources.Where(m => m.Type == MediaType.MP3 || m.Type == MediaType.WAV))
			{
				// Copies the sound file to the cache if it doesn't exist already.
				string cacheFilename = GetCachePathCore(sound);
				if (!isf.FileExists(cacheFilename))
				{
					using (IsolatedStorageFileStream fs = isf.CreateFile(cacheFilename))
					{
						fs.Write(sound.Data, 0, sound.Data.Length);
					}
				}

				// Adds the sound filename to the dictionary.
				_soundFiles.Add(sound.MediaId, cacheFilename);
			}

			RaisePropertyChanged("Sounds");
		}

        private void ImportSavegamesCache(IsolatedStorageFile isf)
        {
            string[] gwsFiles = isf.GetFileNames(GetCachePathCore("*.gws"));
            if (gwsFiles == null)
            {
                return;
            }

            // For each file, imports its metadata.
            List<CartridgeSavegame> cSavegames = new List<CartridgeSavegame>();
            foreach (string file in gwsFiles)
            {
                try
                {
                    cSavegames.Add(CartridgeSavegame.FromCache(GetCachePathCore(file), isf));
                }
                catch (Exception ex)
                {
                    // Outputs the exception.
                    System.Diagnostics.Debug.WriteLine("CartridgeTag: WARNING: Exception during savegame import.");
                    DebugUtils.DumpException(ex);
                }
            }

            // Sets the savegame list.
            _savegames = cSavegames;
            RaisePropertyChanged("Savegames");
        }

		private void RaisePropertyChanged(string propName)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				if (PropertyChanged != null)
				{
					PropertyChanged(this, new PropertyChangedEventArgs(propName));
				}
			});
		} 

		#endregion



    }
}
