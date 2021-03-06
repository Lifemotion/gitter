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

namespace gitter.Git.AccessLayer.CLI
{
	using System.Collections.Generic;

	/// <summary>Register file contents in the working tree to the index.</summary>
	public sealed class UpdateIndexCommand : Command
	{
		public static CommandArgument Add()
		{
			return new CommandArgument("--add");
		}

		public static CommandArgument Remove()
		{
			return new CommandArgument("--remove");
		}

		public static CommandArgument Refresh()
		{
			return new CommandArgument("--refresh");
		}

		public static CommandArgument Quiet()
		{
			return new CommandArgument("-q");
		}

		public static CommandArgument IgnoreSubmodules()
		{
			return new CommandArgument("--ignore-submodules");
		}

		public static CommandArgument Unmerged()
		{
			return new CommandArgument("--unmerged");
		}

		public static CommandArgument IgnoreMissing()
		{
			return new CommandArgument("--ignore-missing");
		}

		public static CommandArgument IndexInfo()
		{
			return new CommandArgument("--index-info");
		}

		public static CommandArgument AssumeUnchanged()
		{
			return new CommandArgument("--assume-unchanged");
		}

		public static CommandArgument NoAssumeUnchanged()
		{
			return new CommandArgument("--no-assume-unchanged");
		}

		public static CommandArgument Again()
		{
			return new CommandArgument("--again");
		}

		public static CommandArgument Unresolve()
		{
			return new CommandArgument("--unresolve");
		}

		public static CommandArgument InfoOnly()
		{
			return new CommandArgument("--info-only");
		}

		public static CommandArgument ForceRemove()
		{
			return new CommandArgument("--force-remove");
		}

		public static CommandArgument Replace()
		{
			return new CommandArgument("--replace");
		}

		public static CommandArgument Stdin()
		{
			return new CommandArgument("--stdin");
		}

		public static CommandArgument NullTerminate()
		{
			return new CommandArgument("-z");
		}

		public static CommandArgument Verbose()
		{
			return CommandArgument.Verbose();
		}

		public static CommandArgument NoMoreOptions()
		{
			return CommandArgument.NoMoreOptions();
		}

		public UpdateIndexCommand()
			: base("update-index")
		{
		}

		public UpdateIndexCommand(params CommandArgument[] args)
			: base("update-index", args)
		{
		}

		public UpdateIndexCommand(IList<CommandArgument> args)
			: base("update-index", args)
		{
		}
	}
}
