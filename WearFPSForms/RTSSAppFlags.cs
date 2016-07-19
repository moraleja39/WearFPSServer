using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WearFPSForms.GameStarted.Types;

namespace WearFPSForms {
    enum AppFlags : uint {
        APPFLAG_DD = 0x00000010,
        APPFLAG_D3D8 = 0x00000100,
        APPFLAG_D3D9 = 0x00001000,
        APPFLAG_D3D9EX = 0x00002000,
        APPFLAG_OGL = 0x00010000,
        APPFLAG_D3D10 = 0x00100000,
        APPFLAG_D3D11 = 0x01000000,
        APPFLAG_API_USAGE_MASK = (APPFLAG_DD | APPFLAG_D3D8 | APPFLAG_D3D9 | APPFLAG_D3D9EX | APPFLAG_OGL | APPFLAG_D3D10  | APPFLAG_D3D11)
    }
}
