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
	using System.Collections.Generic;
	using System.Globalization;

	/// <summary>Represents historical change of <see cref="Reference"/>'s position.</summary>
	public sealed class ReflogRecord : GitLifeTimeDynamicNamedObject, IRevisionPointer
	{
		#region Data

		private readonly Reflog _reflog;
		private Revision _revision;
		private string _message;
		private int _index;

		#endregion

		#region Events

		/// <summary>Occurs when <see cref="M:Index"/> is changed.</summary>
		public event EventHandler IndexChanged;

		/// <summary>Occurs when <see cref="M:Revision"/> is changed.</summary>
		public event EventHandler RevisionChanged;

		/// <summary>Occurs when <see cref="M:Message"/> is changed.</summary>
		public event EventHandler MessageChanged;

		private void InvokeIndexChanged()
		{
			var handler = IndexChanged;
			if(handler != null) handler(this, EventArgs.Empty);
		}

		private void InvokeRevisionChanged()
		{
			var handler = RevisionChanged;
			if(handler != null) handler(this, EventArgs.Empty);
		}

		private void InvokeMessageChanged()
		{
			var handler = MessageChanged;
			if(handler != null) handler(this, EventArgs.Empty);
		}

		#endregion

		#region .ctor

		/// <summary>Initializes a new instance of the <see cref="ReflogRecord"/> class.</summary>
		/// <param name="repository">Host repository.</param>
		/// <param name="reflog">Owner reflog.</param>
		/// <param name="revision">Target revision.</param>
		/// <param name="message">Reflog record message.</param>
		/// <param name="index">Reflog record index.</param>
		internal ReflogRecord(Repository repository, Reflog reflog, Revision revision, string message, int index)
			: base(repository)
		{
			Verify.Argument.IsNotNull(reflog, "reflog");
			Verify.Argument.IsNotNull(revision, "revision");
			Verify.Argument.IsNotNull(message, "message");
			Verify.Argument.IsNotNegative(index, "index");

			_reflog = reflog;
			_revision = revision;
			_message = message;
			_index = index;
		}

		#endregion

		#region Properties

		/// <summary>Gets the reference, which owns this reflog record.</summary>
		/// <value>Reference, which owns this reflog record..</value>
		public Reference Reference
		{
			get { return _reflog.Reference; }
		}

		/// <summary>Gets reflog, containing this record.</summary>
		/// <value>Reflog, containing this record.</value>
		public Reflog Reflog
		{
			get { return _reflog; }
		}

		/// <summary>Gets revision, pointed by this <see cref="ReflogRecord"/>.</summary>
		/// <value>Revision, pointed by this <see cref="ReflogRecord"/>.</value>
		public Revision Revision
		{
			get { return _revision; }
			internal set
			{
				Verify.Argument.IsNotNull(value, "value");

				if(_revision != value)
				{
					_revision = value;
					InvokeRevisionChanged();
				}
			}
		}

		/// <summary>Gets reflog record message.</summary>
		/// <value>Reflog record message.</value>
		public string Message
		{
			get { return _message; }
			internal set
			{
				Verify.Argument.IsNotNull(value, "value");

				if(_message != value)
				{
					_message = value;
					InvokeMessageChanged();
				}
			}
		}

		/// <summary>Gets reflog record index.</summary>
		/// <value>Reflog record index.</value>
		public int Index
		{
			get { return _index; }
			internal set
			{
				Verify.Argument.IsNotNegative(value, "value");

				if(_index != value)
				{
					_index = value;
					InvokeIndexChanged();
				}
			}
		}

		/// <summary><see cref="ReferenceType"/>.</summary>
		/// <value><see cref="ReferenceType.ReflogRecord"/>.</value>
		public ReferenceType Type
		{
			get { return ReferenceType.ReflogRecord; }
		}

		/// <summary>Gets the full name of this <see cref="ReflogRecord"/>.</summary>
		/// <value>Full name of this <see cref="ReflogRecord"/>.</value>
		public string FullName
		{
			get { return _reflog.Reference.FullName + "@{" + _index.ToString(CultureInfo.InvariantCulture) + "}"; }
		}

		#endregion

		protected override string GetName()
		{
			return _reflog.Reference.Name + "@{" + _index.ToString(CultureInfo.InvariantCulture) + "}";
		}

		string IRevisionPointer.Pointer
		{
			get { return _reflog.Reference.Name + "@{" + _index.ToString(CultureInfo.InvariantCulture) + "}"; }
		}

		Revision IRevisionPointer.Dereference()
		{
			return _revision;
		}

		/// <summary>Returns a <see cref="System.String"/> that represents this <see cref="ReflogRecord"/>.</summary>
		/// <returns>A <see cref="System.String"/> that represents this <see cref="ReflogRecord"/>.</returns>
		public override string ToString()
		{
			return Name + ": " + _message;
		}
	}
}
