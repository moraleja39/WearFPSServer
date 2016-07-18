﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WearFPSForms.Net {
    static class Clients {
        private static List<Client> clients = new List<Client>();

        public static void Add(TcpClient client) {
            clients.Add(new Client(client));
            notifyHardwareThread();
        }

        public static void Remove(Client client) {
            clients.Remove(client);
            notifyHardwareThread();
        }

        private static void notifyHardwareThread() {
            if (clients.Count > 0) HardwareMonitor.ShouldUpdate(true);
            else HardwareMonitor.ShouldUpdate(false);
        }

        public static void StopAll() {
            lock (clients) {
                for (int i = clients.Count-1; i >= 0; i--) {
                    clients[i].stop();
                }
            }
            notifyHardwareThread();
        }

        public static void NotifyDataChanged() {
            lock (clients) {
                foreach (var client in clients) {
                    client.NotifyDataChanged();
                }
            }
        }

    }
}