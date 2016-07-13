using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WearFPSForms.Net {

    static class LocalInterfaces {
        private struct Interfaz {
            public IPAddress ip;
            public IPAddress mask;
            public string name;
            public IPAddress netAddr;

            public Interfaz(string i, string m, string n) {
                this.ip = IPAddress.Parse(i);
                this.mask = IPAddress.Parse(m);
                this.name = n;
                byte[] ipAddr = this.ip.GetAddressBytes();
                byte[] maskAddr = this.mask.GetAddressBytes();
                byte[] netBytes = new byte[ipAddr.Length];
                for (int j= 0; j < ipAddr.Length; j++) {
                    netBytes[j] = (byte)(ipAddr[j] & maskAddr[j]);
                }
                this.netAddr = new IPAddress(netBytes);
                Log.Info("Network address for " + i + " is " + this.netAddr.ToString() + ". Adapter is " + n);
            }
        }

        private static List<Interfaz> interfaces = new List<Interfaz>();

        public static void Add(string ip, string mask, string name) {
            interfaces.Add(new Interfaz(ip, mask, name));
        }

        public static int Count {
            get { return interfaces.Count; }
        }

        public static IPAddress GetAssociatedIp(IPAddress addr) {
            foreach(Interfaz loc in interfaces) {
                byte[] ipBytes = addr.GetAddressBytes();
                byte[] netBytes = loc.netAddr.GetAddressBytes();
                if (ipBytes.Length != netBytes.Length) continue;
                byte[] maskBytes = loc.mask.GetAddressBytes();
                bool matches = true;
                for (int i = 0; i < ipBytes.Length; i++) {
                    matches &= (ipBytes[i] & maskBytes[i]) == netBytes[i];
                }
                if (matches) return loc.ip;
            }
            return null;
        }

        
    }
}
