using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteWindowsController
{
    internal interface IRemoteControllable
    {
        void onRemoteButtonPressed(WindowCommand cmd);
    }
}
