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

namespace gitter.Git.Gui.Controls
{
	using System;
	using System.Collections.Generic;
	using System.Windows.Forms;

	public sealed class DiffFileContextMenuRequestedEventArgs : EventArgs
	{
		private readonly DiffFile _file;
		private ContextMenuStrip _contextMenu;

		public DiffFileContextMenuRequestedEventArgs(DiffFile file)
		{
			_file = file;
		}

		public DiffFile File
		{
			get { return _file; }
		}

		public ContextMenuStrip ContextMenu
		{
			get { return _contextMenu; }
			set { _contextMenu = value; }
		}
	}
}
