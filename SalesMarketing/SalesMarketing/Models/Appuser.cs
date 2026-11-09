using System;
using System.Collections.Generic;

namespace SalesMarketing.Models;

public partial class Appuser
{
    public long Id { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public short Adminrole { get; set; }
}
