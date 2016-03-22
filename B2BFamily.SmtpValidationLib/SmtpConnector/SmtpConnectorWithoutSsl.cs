using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace B2BFamily.SmtpValidation.Lib.SmtpConnector {
    internal class SmtpConnectorWithoutSsl : SmtpConnectorBase {
        private Socket _socket = null;
        private const int MAX_ATTEMPTS_COUNT = 100;

        public SmtpConnectorWithoutSsl(string smtpServerAddress, int port) : base(smtpServerAddress, port) {
            try {
                IPHostEntry hostEntry = Dns.GetHostEntry(smtpServerAddress);
                IPEndPoint endPoint = new IPEndPoint(hostEntry.AddressList[0], port);
                _socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                //try to connect and test the rsponse for code 220 = success
                _socket.Connect(endPoint);
            } catch (Exception) {
                _socket = null;
            }
        }

        ~SmtpConnectorWithoutSsl() {
            try {
                if (_socket != null) {
                    _socket.Close();
                    _socket.Dispose();
                    _socket = null;
                }
            } catch (Exception) {
                ;
            }
        }

        public override bool CheckResponse(int expectedCode) {
            if (_socket == null) {
                return false;
            }
            var currentAttemptIndex = 1;
            while (_socket.Available == 0) {
                System.Threading.Thread.Sleep(100);
                if (currentAttemptIndex++ > MAX_ATTEMPTS_COUNT) {
                    return false;
                }
            }
            byte[] responseArray = new byte[1024];
            _socket.Receive(responseArray, 0, _socket.Available, SocketFlags.None);
            string responseData = Encoding.UTF8.GetString(responseArray);
            int responseCode = Convert.ToInt32(responseData.Substring(0, 3));
            if (responseCode == expectedCode) {
                return true;
            }
            return false;
        }

        public override void SendData(string data) {
            if (_socket == null) {
                return;
            }
            byte[] dataArray = Encoding.UTF8.GetBytes(data);
            _socket.Send(dataArray, 0, dataArray.Length, SocketFlags.None);
        }
    }
}
