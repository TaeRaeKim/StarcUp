using StarcUp.Infrastructure.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StarcUp.Src.Business.UnitManager
{
    public interface IUnitManager
    {
        public void SetUnitArrayBaseAddress(nint baseAddress);
        public bool LoadAllUnits();
        extern private Unit[] ByteArrayToUnitArray(byte[] bytes);
        extern private int CountActiveUnits();
        public IEnumerable<Unit> GetAllUnits();
        public IEnumerable<Unit> GetPlayerUnits(byte playerId);
        public IEnumerable<Unit> GetUnitsByType(ushort unitType);
        public IEnumerable<Unit> GetUnitsNearPosition(ushort x, ushort y, int radius);

        public bool RefreshUnits();
    }
}
