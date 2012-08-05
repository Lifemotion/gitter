﻿namespace gitter.Git
{
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.IO;
	using System.Windows.Forms;

	using gitter.Framework;
	using gitter.Framework.Options;
	using gitter.Framework.Configuration;

	using gitter.Git.Gui;
	using gitter.Git.Gui.Dialogs;
	using gitter.Git.Integration;
	using gitter.Git.AccessLayer;

	using Resources = gitter.Git.Gui.Properties.Resources;

	/// <summary>git <see cref="Repository"/> provider.</summary>
	public sealed class RepositoryProvider : IGitRepositoryProvider
	{
		#region Static Data

		private static readonly IGitAccessorProvider[] _gitAccessorProviders = new[]
			{
				new gitter.Git.AccessLayer.CLI.MSysGitAccessorProvider(),
			};

		private static readonly Version _minVersion = new Version(1,7,0,2);
		private static readonly IntegrationFeatures _integrationFeatures;
		private static IGitAccessorProvider _gitAccessorProvider;
		private static IGitAccessor _gitAccessor;

		#endregion

		#region Data

		private IWorkingEnvironment _environment;
		private GuiProvider _guiProvider;
		private Section _configSection;

		#endregion

		#region .ctor

		/// <summary>Initializes the <see cref="RepositoryProvider"/> class.</summary>
		static RepositoryProvider()
		{
			_integrationFeatures = new IntegrationFeatures();
		}

		/// <summary>Create <see cref="RepositoryProvider"/>.</summary>
		public RepositoryProvider()
		{
		}

		#endregion

		#region Properties

		public string Name
		{
			get { return "git"; }
		}

		public bool IsLoaded
		{
			get { return _environment != null; }
		}

		#endregion

		public IEnumerable<IGitAccessorProvider> GitAccessorProviders
		{
			get { return _gitAccessorProviders; }
		}

		public IGitAccessorProvider ActiveGitAccessorProvider
		{
			get { return _gitAccessorProvider; }
			set
			{
				if(value == null) throw new ArgumentNullException("value");

				if(_gitAccessorProvider != value)
				{
					if(_gitAccessorProvider != null && _gitAccessor != null && _configSection != null)
					{
						var gitAccessorSection = _configSection.GetCreateSection(_gitAccessorProvider.Name);
						_gitAccessor.SaveTo(gitAccessorSection);
					}

					_gitAccessorProvider = value;
					_gitAccessor = value.CreateAccessor();

					if(_gitAccessor != null && _configSection != null)
					{
						var gitAccessorSection = _configSection.TryGetSection(value.Name);
						_gitAccessor.LoadFrom(gitAccessorSection);
					}
				}
			}
		}

		public IGitAccessor GitAccessor
		{
			get { return _gitAccessor; }
			set
			{
				if(value == null) throw new ArgumentNullException("value");

				if(_gitAccessor != value)
				{
					if(_gitAccessorProvider != null && _gitAccessor != null && _configSection != null)
					{
						var gitAccessorSection = _configSection.GetCreateSection(_gitAccessorProvider.Name);
						_gitAccessor.SaveTo(gitAccessorSection);
					}

					_gitAccessorProvider = _gitAccessor.Provider;
					_gitAccessor = value;
				}
			}
		}

		internal static IGitAccessor Git
		{
			get { return _gitAccessor; }
		}

		public static IntegrationFeatures Integration
		{
			get { return _integrationFeatures; }
		}

		public static Version MinimumRequiredGitVersion
		{
			get { return _minVersion; }
		}

		public bool LoadFor(IWorkingEnvironment environment, Section section)
		{
			if(environment == null) throw new ArgumentNullException("environment");

			if(section != null)
			{
				var providerName = section.GetValue<string>("AccessorProvider", string.Empty);
				if(!string.IsNullOrWhiteSpace(providerName))
				{
					ActiveGitAccessorProvider = GitAccessorProviders.FirstOrDefault(
						prov => prov.Name == providerName);
				}
				if(ActiveGitAccessorProvider == null)
				{
					ActiveGitAccessorProvider = GitAccessorProviders.First();
				}
				var gitAccessorSection = section.TryGetSection(ActiveGitAccessorProvider.Name);
				if(gitAccessorSection != null)
				{
					GitAccessor.LoadFrom(gitAccessorSection);
				}
			}
			else
			{
				ActiveGitAccessorProvider = GitAccessorProviders.First();
			}
			Version gitVersion;
			try
			{
				gitVersion = _gitAccessor.GitVersion;
			}
			catch
			{
				gitVersion = null;
			}
			if(gitVersion == null || gitVersion < MinimumRequiredGitVersion)
			{
				using(var dlg = new VersionCheckDialog(environment, this, MinimumRequiredGitVersion, gitVersion))
				{
					dlg.Run(environment.MainForm);
					gitVersion = dlg.InstalledVersion;
					if(gitVersion == null || gitVersion < _minVersion)
					{
						return false;
					}
				}
			}
			if(section != null)
			{
				var integrationNode = section.TryGetSection("Integration");
				if(integrationNode != null)
				{
					_integrationFeatures.LoadFrom(integrationNode);
				}
			}
			GlobalOptions.RegisterPropertyPageFactory(
				new PropertyPageFactory(
					GitOptionsPage.Guid,
					Resources.StrGit,
					null,
					PropertyPageFactory.RootGroupGuid,
					env => new GitOptionsPage(env)));
			GlobalOptions.RegisterPropertyPageFactory(
				new PropertyPageFactory(
					IntegrationOptionsPage.Guid,
					Resources.StrIntegration,
					null,
					GitOptionsPage.Guid,
					env => new IntegrationOptionsPage()));
			GlobalOptions.RegisterPropertyPageFactory(
				new PropertyPageFactory(
					ConfigurationPage.Guid,
					Resources.StrConfig,
					null,
					GitOptionsPage.Guid,
					env => new ConfigurationPage(env)));
			_environment = environment;
			_configSection = section;
			return true;
		}

		/// <summary>Save configuration to <paramref name="section"/>.</summary>
		/// <param name="section"><see cref="Section"/> for storing configuration.</param>
		public void SaveTo(Section section)
		{
			if(section == null) throw new ArgumentNullException("section");

			if(ActiveGitAccessorProvider != null)
			{
				section.SetValue<string>("AccessorProvider", ActiveGitAccessorProvider.Name);
				if(GitAccessor != null)
				{
					var gitAccessorSection = section.GetCreateSection(ActiveGitAccessorProvider.Name);
					GitAccessor.SaveTo(gitAccessorSection);
				}
			}

			var integrationNode = section.GetCreateSection("Integration");
			_integrationFeatures.SaveTo(integrationNode);
			_configSection = section;
		}

		public bool IsValidFor(string workingDirectory)
		{
			if(GitAccessor != null)
			{
				return GitAccessor.IsValidRepository(workingDirectory);
			}
			else
			{
				return false;
			}
		}

		public IRepository OpenRepository(string workingDirectory)
		{
			return Repository.Load(GitAccessor, workingDirectory);
		}

		public IAsyncFunc<IRepository> OpenRepositoryAsync(string workingDirectory)
		{
			return Repository.LoadAsync(GitAccessor, workingDirectory);
		}

		public void OnRepositoryLoaded(IRepository repository)
		{
			if(repository == null) throw new ArgumentNullException("repository");
			var gitRepository = repository as Repository;
			if(gitRepository == null) throw new ArgumentException("repository");

			if(gitRepository.UserIdentity == null)
			{
				using(var dlg = new UserIdentificationDialog(_environment, gitRepository))
				{
					dlg.Run(_environment.MainForm);
				}
			}
		}

		public void CloseRepository(IRepository repository)
		{
			var gitRepository = (Repository)repository;
			try
			{
				var cfgFileName = Path.Combine(gitRepository.GitDirectory, "gitter-config");
				using(var fs = new FileStream(cfgFileName, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					gitRepository.ConfigurationManager.Save(new XmlAdapter(fs));
				}
			}
			catch
			{
			}
			finally
			{
				gitRepository.Dispose();
			}
		}

		public IRepositoryGuiProvider GuiProvider
		{
			get
			{
				if(_guiProvider == null)
				{
					_guiProvider = new GuiProvider(this);
				}
				return _guiProvider;
			}
		}

		public DialogResult RunInitDialog()
		{
			if(_environment == null)
			{
				throw new InvalidOperationException(string.Format("{0} is not loaded.", GetType().FullName));
			}

			DialogResult res;
			string path = "";
			using(var dlg = new InitDialog(this))
			{
				dlg.RepositoryPath = _environment.RecentRepositoryPath;
				res = dlg.Run(_environment.MainForm);
				if(res == DialogResult.OK)
				{
					path = Path.GetFullPath(dlg.RepositoryPath.Trim());
				}
			}
			if(res == DialogResult.OK)
			{
				_environment.OpenRepository(path);
			}
			return res;
		}

		bool IGitRepositoryProvider.RunInitDialog()
		{
			return RunInitDialog() == DialogResult.OK;
		}

		public DialogResult RunCloneDialog()
		{
			if(_environment == null)
			{
				throw new InvalidOperationException(string.Format("{0} is not loaded.", GetType().Name));
			}

			DialogResult res;
			string path = "";
			using(var dlg = new CloneDialog(this))
			{
				dlg.RepositoryPath = _environment.RecentRepositoryPath;
				res = dlg.Run(_environment.MainForm);
				if(res == DialogResult.OK)
				{
					path = Path.GetFullPath(dlg.TargetPath.Trim());
				}
			}
			if(res == DialogResult.OK)
			{
				_environment.OpenRepository(path);
			}
			return res;
		}

		bool IGitRepositoryProvider.RunCloneDialog()
		{
			return RunCloneDialog() == DialogResult.OK;
		}

		public IEnumerable<StaticRepositoryAction> GetStaticActions()
		{
			yield return new StaticRepositoryAction(
				"init",
				Resources.StrInit.AddEllipsis(),
				CachedResources.Bitmaps["ImgInit"],
				env => RunInitDialog());
			yield return new StaticRepositoryAction(
				"clone",
				Resources.StrClone.AddEllipsis(),
				CachedResources.Bitmaps["ImgClone"],
				env => RunCloneDialog());
		}
	}
}