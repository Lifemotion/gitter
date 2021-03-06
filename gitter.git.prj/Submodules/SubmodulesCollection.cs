#region Copyright Notice
/*
 * gitter - VCS repository management tool
 * Copyright (C) 2013  Popovskiy Maxim Vladimirovitch <amgine.gitter@gmail.com>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

namespace gitter.Git
{
	using System;
	using System.IO;
	using System.Collections.Generic;

	using gitter.Framework;

	using gitter.Git.AccessLayer;

	using Resources = gitter.Git.Properties.Resources;

	/// <summary>Repository submodules collection.</summary>
	public sealed class SubmodulesCollection : GitObjectsCollection<Submodule, SubmoduleEventArgs>
	{
		#region .ctor

		internal SubmodulesCollection(Repository repository)
			: base(repository)
		{
		}

		#endregion

		protected override SubmoduleEventArgs CreateEventArgs(Submodule item)
		{
			return new SubmoduleEventArgs(item);
		}

		public void Update()
		{
			Repository.Accessor.UpdateSubmodule(
				new SubmoduleUpdateParameters()
				{
					Recursive = true,
					Init = true,
				});
		}

		public IAsyncAction UpdateAsync()
		{
			return AsyncAction.Create(
				new
				{
					Repository = Repository,
					Parameters =
						new SubmoduleUpdateParameters()
						{
							Recursive = true,
							Init = true,
						},
				},
				(data, monitor) =>
				{
					data.Repository.Accessor.UpdateSubmodule(data.Parameters);
				},
				Resources.StrUpdate,
				Resources.StrFetchingDataFromRemoteRepository);
		}

		public bool ExistsPath(string path)
		{
			Verify.Argument.IsNotNull(path, "path");

			lock(SyncRoot)
			{
				foreach(var s in ObjectStorage.Values)
				{
					if(s.Path == path) return true;
				}
			}
			return false;
		}

		public bool ExistsUrl(string url)
		{
			Verify.Argument.IsNotNull(url, "url");

			lock(SyncRoot)
			{
				foreach(var s in ObjectStorage.Values)
				{
					if(s.Url == url) return true;
				}
			}
			return false;
		}

		public bool TryGetSubmoduleByPath(string path, out Submodule submodule)
		{
			Verify.Argument.IsNotNull(path, "path");

			lock(SyncRoot)
			{
				foreach(var s in ObjectStorage.Values)
				{
					if(s.Path == path)
					{
						submodule = s;
						return true;
					}
				}
			}
			submodule = null;
			return false;
		}

		public Submodule TryGetSubmoduleByPath(string path)
		{
			Verify.Argument.IsNotNull(path, "path");

			lock(SyncRoot)
			{
				foreach(var s in ObjectStorage.Values)
				{
					if(s.Path == path)
					{
						return s;
					}
				}
			}
			return null;
		}

		public bool TryGetSubmoduleByUrl(string url, out Submodule submodule)
		{
			Verify.Argument.IsNotNull(url, "url");

			lock(SyncRoot)
			{
				foreach(var s in ObjectStorage.Values)
				{
					if(s.Url == url)
					{
						submodule = s;
						return true;
					}
				}
			}
			submodule = null;
			return false;
		}

		public Submodule TryGetSubmoduleByUrl(string url)
		{
			Verify.Argument.IsNotNull(url, "url");

			lock(SyncRoot)
			{
				foreach(var s in ObjectStorage.Values)
				{
					if(s.Url == url)
					{
						return s;
					}
				}
			}
			return null;
		}

		public Submodule Create(string path, string url)
		{
			return Create(path, url, null);
		}

		public Submodule Create(string path, string url, string branch)
		{
			Verify.Argument.IsNotNull(path, "path");
			Verify.Argument.IsNotNull(url, "url");

			using(Repository.Monitor.BlockNotifications(
				RepositoryNotifications.WorktreeUpdated,
				RepositoryNotifications.SubmodulesChanged))
			{
				lock(SyncRoot)
				{
					try
					{
						Repository.Accessor.AddSubmodule(
							new AddSubmoduleParameters()
							{
								Branch = branch,
								Path = "./" + path,
								Repository = url,
							});
					}
					finally
					{
						Repository.Status.Refresh();
					}
					var submodule = new Submodule(Repository, path, path, url);
					AddObject(submodule);
					return submodule;
				}
			}
		}

		public void Refresh()
		{
			var submodules = new Dictionary<string, SubmoduleData>();
			var cfg = Path.Combine(Repository.WorkingDirectory, GitConstants.SubmodulesConfigFile);
			bool skipUpdate = false;
			if(File.Exists(cfg))
			{
				ConfigurationFile cfgFile = null;
				try
				{
					cfgFile = new ConfigurationFile(Repository, GitConstants.SubmodulesConfigFile, true);
				}
				catch
				{
					skipUpdate = true;
				}
				if(cfgFile != null)
				{
					foreach(var param in cfgFile)
					{
						if(param.Name.StartsWith("submodule."))
						{
							int p = param.Name.LastIndexOf('.', param.Name.Length - 1, param.Name.Length - "submodule.".Length);
							if(p != -1 && p != param.Name.Length - 1)
							{
								var p3 = param.Name.Substring(p + 1);
								bool valueIsPath = false;
								bool valueIsUrl = false;
								switch(p3)
								{
									case "path":
										valueIsPath = true;
										break;
									case "url":
										valueIsUrl = true;
										break;
									default:
										continue;
								}
								var name = param.Name.Substring("submodule.".Length, p - "submodule.".Length);
								SubmoduleData info;
								if(!submodules.TryGetValue(name, out info))
								{
									info = new SubmoduleData(name);
									submodules.Add(name, info);
								}
								if(valueIsPath)
								{
									info.Path = param.Value;
								}
								if(valueIsUrl)
								{
									info.Url = param.Value;
								}
							}
						}
					}
				}
			}

			if(!skipUpdate)
			{
				lock(SyncRoot)
				{
					CacheUpdater.UpdateObjectDictionary<Submodule, SubmoduleData>(
						ObjectStorage,
						null,
						null,
						submodules,
						submoduleData => ObjectFactories.CreateSubmodue(Repository, submoduleData),
						ObjectFactories.UpdateSubmodule,
						InvokeObjectAdded,
						InvokeObjectRemoved,
						true);
				}
			}
		}
	}
}
