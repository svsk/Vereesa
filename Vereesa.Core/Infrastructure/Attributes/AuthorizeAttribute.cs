using System;

namespace Vereesa.Core.Infrastructure
{
	public class AuthorizeAttribute : Attribute
	{
		public AuthorizeAttribute(string roleName)
		{
			this.RoleName = roleName;
		}

		public AuthorizeAttribute(ulong roleId)
		{
			this.RoleId = roleId;
		}

		public ulong RoleId { get; set; }

		public string RoleName { get; set; }
	}
}