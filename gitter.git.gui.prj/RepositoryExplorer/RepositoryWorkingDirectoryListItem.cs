﻿namespace gitter.Git.Gui
{
	using System;
	using System.Collections.Generic;
	using System.Drawing;
	using System.IO;
	using System.Windows.Forms;

	using gitter.Framework;
	using gitter.Framework.Controls;

	using gitter.Git.Gui.Controls;

	using Resources = gitter.Git.Gui.Properties.Resources;

	sealed class RepositoryWorkingDirectoryListItem : RepositoryExplorerItemBase
	{
		private TreeBinding _binding;
		private IAsyncFunc<Tree> _getTreeFunc;

		public RepositoryWorkingDirectoryListItem()
			: base(CachedResources.Bitmaps["ImgFolder"], Resources.StrWorkingDirectory)
		{
		}

		/// <summary>Called when item is attached to listbox.</summary>
		protected override void OnListBoxAttached()
		{
			base.OnListBoxAttached();
			if(Repository != null) Bind();
		}

		/// <summary>Called when item is detached from listbox.</summary>
		protected override void OnListBoxDetached()
		{
			Unbind();
			base.OnListBoxDetached();
		}

		protected override void DetachFromRepository()
		{
			Repository.Head.PositionChanged -= OnHeadPositionChanged;
			Unbind();
			Collapse();
		}

		protected override void AttachToRepository()
		{
			Refresh();
			Repository.Head.PositionChanged += OnHeadPositionChanged;
		}

		private void Unbind()
		{
			_getTreeFunc = null;
			if(_binding != null)
			{
				_binding.ItemActivated -= OnItemActivated;
				_binding.ItemContextMenuRequested -= OnItemContextMenuRequested;
				_binding.Dispose();
				_binding = null;
			}
		}

		private void Bind()
		{
			var f = _getTreeFunc = Repository.Head.GetTreeAsync();
			f.BeginInvoke(
				ListBox,
				new NullMonitor(),
				OnTreeLoaded,
				f);
		}

		private void OnTreeLoaded(IAsyncResult ar)
		{
			var f = (IAsyncFunc<Tree>)ar.AsyncState;
			if(_getTreeFunc == f)
			{
				var listBox = ListBox;
				if(listBox != null)
				{
					var tree = f.EndInvoke(ar);
					if(listBox.InvokeRequired)
					{
						listBox.BeginInvoke(new Action<Tree>(OnTreeLoadedSync), tree);
					}
					else
					{
						OnTreeLoadedSync(tree);
					}
				}
				_getTreeFunc = null;
			}
		}

		private void OnTreeLoadedSync(Tree tree)
		{
			_binding = new TreeBinding(Items, tree.Root, false);
			_binding.ItemActivated += OnItemActivated;
			_binding.ItemContextMenuRequested += OnItemContextMenuRequested;
		}

		private void Refresh()
		{
			Unbind();
			if(ListBox != null)
			{
				Bind();
			}
		}

		private void OnHeadPositionChanged(object sender, RevisionChangedEventArgs e)
		{
			var listBox = ListBox;
			if(listBox != null && listBox.InvokeRequired)
			{
				listBox.BeginInvoke(new Action(Refresh), null);
			}
			else
			{
				Refresh();
			}
		}

		private static void OnItemActivated(object sender, BoundItemActivatedEventArgs<TreeItem> e)
		{
			var item = e.Object;
			if(item.Type == TreeItemType.Blob)
			{
				Utility.OpenUrl(item.FullPath);
			}
		}

		private void OnItemContextMenuRequested(object sender, ItemContextMenuRequestEventArgs e)
		{
			var item = e.Item as ITreeItemListItem;
			if(item != null)
			{
				var file = item.TreeItem as TreeFile;
				if(file != null)
				{
					var menu = new ContextMenuStrip();
					menu.Items.AddRange(
						new ToolStripItem[]
						{
							GuiItemFactory.GetOpenUrlItem<ToolStripMenuItem>(Resources.StrOpen, null, file.FullPath),
							GuiItemFactory.GetOpenUrlWithItem<ToolStripMenuItem>(Resources.StrOpenWith.AddEllipsis(), null, file.FullPath),
							GuiItemFactory.GetOpenUrlItem<ToolStripMenuItem>(Resources.StrOpenContainingFolder, null, Path.GetDirectoryName(file.FullPath)),
							new ToolStripSeparator(),
							new ToolStripMenuItem(Resources.StrCopyToClipboard, null,
								new ToolStripItem[]
								{
									GuiItemFactory.GetCopyToClipboardItem<ToolStripMenuItem>(Resources.StrFileName, file.Name),
									GuiItemFactory.GetCopyToClipboardItem<ToolStripMenuItem>(Resources.StrRelativePath, file.RelativePath),
									GuiItemFactory.GetCopyToClipboardItem<ToolStripMenuItem>(Resources.StrFullPath, file.FullPath),
								}),
							new ToolStripSeparator(),
							GuiItemFactory.GetBlameItem<ToolStripMenuItem>(Repository.Head, file.RelativePath),
							GuiItemFactory.GetPathHistoryItem<ToolStripMenuItem>(Repository.Head, file.RelativePath),
						});
					Utility.MarkDropDownForAutoDispose(menu);
					e.ContextMenu = menu;
					return;
				}
				var directory = item.TreeItem as TreeDirectory;
				if(directory != null)
				{
					var menu = new ContextMenuStrip();
					menu.Items.AddRange(
						new ToolStripItem[]
						{
							GuiItemFactory.GetOpenUrlItem<ToolStripMenuItem>(Resources.StrOpenInWindowsExplorer, null, directory.FullPath),
							GuiItemFactory.GetOpenCmdAtItem<ToolStripMenuItem>(Resources.StrOpenCommandLine, null, directory.FullPath),
						});
					if(e.Item.Items.Count != 0)
					{
						menu.Items.AddRange(
							new ToolStripItem[]
							{
								new ToolStripSeparator(),
								GuiItemFactory.GetExpandAllItem<ToolStripMenuItem>(e.Item),
								GuiItemFactory.GetCollapseAllItem<ToolStripMenuItem>(e.Item),
							});
					}
					menu.Items.AddRange(
						new ToolStripItem[]
						{
							new ToolStripSeparator(),
							GuiItemFactory.GetPathHistoryItem<ToolStripMenuItem>(Repository.Head, directory.RelativePath + "/"),
						});
					Utility.MarkDropDownForAutoDispose(menu);
					e.ContextMenu = menu;
					return;
				}
				var commit = item.TreeItem as TreeCommit;
				if(commit != null)
				{
					var menu = new ContextMenuStrip();
					menu.Items.AddRange(
						new ToolStripItem[]
						{
							GuiItemFactory.GetOpenAppItem<ToolStripMenuItem>(
								Resources.StrOpenWithGitter, null, Application.ExecutablePath, commit.FullPath.SurroundWithDoubleQuotes()),
							GuiItemFactory.GetOpenUrlItem<ToolStripMenuItem>(Resources.StrOpenInWindowsExplorer, null, commit.FullPath),
							GuiItemFactory.GetOpenCmdAtItem<ToolStripMenuItem>(Resources.StrOpenCommandLine, null, commit.FullPath),
							new ToolStripSeparator(),
							GuiItemFactory.GetPathHistoryItem<ToolStripMenuItem>(Repository.Head, commit.RelativePath),
						});
					Utility.MarkDropDownForAutoDispose(menu);
					e.ContextMenu = menu;
					return;
				}
			}
		}

		/// <summary>Gets the context menu.</summary>
		/// <param name="requestEventArgs">Request parameters.</param>
		/// <returns>Context menu for specified location.</returns>
		public override ContextMenuStrip GetContextMenu(ItemContextMenuRequestEventArgs requestEventArgs)
		{
			if(Repository != null)
			{
				var menu = new ContextMenuStrip();
				menu.Items.AddRange(
					new ToolStripItem[]
					{
						GuiItemFactory.GetOpenUrlItem<ToolStripMenuItem>(Resources.StrOpenInWindowsExplorer, null, Repository.WorkingDirectory),
						GuiItemFactory.GetOpenCmdAtItem<ToolStripMenuItem>(Resources.StrOpenCommandLine, null, Repository.WorkingDirectory),
						new ToolStripSeparator(),
						GuiItemFactory.GetExpandAllItem<ToolStripMenuItem>(requestEventArgs.Item),
						GuiItemFactory.GetCollapseAllItem<ToolStripMenuItem>(requestEventArgs.Item),
					});
				Utility.MarkDropDownForAutoDispose(menu);
				return menu;
			}
			else
			{
				return null;
			}
		}
	}
}
