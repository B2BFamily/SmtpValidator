using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace B2BFamily.SmtpValidation.Lib.SmtpConnector {
    internal class SmtpConnectorWithSsl : SmtpConnectorBase {
        private SslStream _sslStream = null;
        private TcpClient _client = null;
        /// <summary>
        /// Таймаут подклчюения в секундах
        /// </summary>
        private const byte CONNECT_TIMEOUT = 2;
        private IAsyncResult _connectionResult = null;

        public SmtpConnectorWithSsl(string smtpServerAddress, int port) : base(smtpServerAddress, port) {
            try {
                _client = new TcpClient();
                _connectionResult = _client.BeginConnect(smtpServerAddress, port, null, null);

                var success = _connectionResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(CONNECT_TIMEOUT));

                if (!success) {
                    _client = null;
                    _sslStream = null;
                    return;
                }

                _sslStream = new SslStream(
                    _client.GetStream(),
                    false,
                    new RemoteCertificateValidationCallback(ValidateServerCertificate),
                    null
                    );
                // The server name must match the name on the server certificate.
            } catch {
                //Timeout excepted
                _client = null;
                _sslStream = null;
                return;
            }
            try {
                _sslStream.AuthenticateAsClient(smtpServerAddress);
            } catch (AuthenticationException e) {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null) {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");
                _client.Close();

                _client = null;
                _sslStream = null;
            }
        }

        ~SmtpConnectorWithSsl() {
            try {
                if (_sslStream != null) {
                    _sslStream.Close();
                    _sslStream.Dispose();
                    _sslStream = null;
                }
            } catch (Exception) {
                ;
            }

            try {
                if (_client != null) {
                    // we have connected
                    _client.EndConnect(_connectionResult);
                    _client.Close();
                    _client = null;
                }
            } catch (Exception) {
                ;
            }
        }

        // The following method is invoked by the RemoteCertificateValidationDelegate.
        private static bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors) {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        public override bool CheckResponse(int expectedCode) {
            if (_sslStream == null) {
                return false;
            }
            var message = ReadMessageFromStream(_sslStream);
            int responseCode = Convert.ToInt32(message.Substring(0, 3));
            if (responseCode == expectedCode) {
                return true;
            }
            return false;
        }

        public override void SendData(string data) {
            if (_client == null || _sslStream == null) {
                return;
            }
            byte[] messsage = Encoding.UTF8.GetBytes(data);
            // Send hello message to the server. 
            _sslStream.Write(messsage);
            _sslStream.Flush();
        }

        private string ReadMessageFromStream(SslStream stream) {
            byte[] buffer = new byte[2048];
            StringBuilder messageData = new StringBuilder();
            int bytes = -1;
            do {
                bytes = stream.Read(buffer, 0, buffer.Length);

                // Use Decoder class to convert from bytes to UTF8
                // in case a character spans two buffers.
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);
                // Check for EOF.
                if (messageData.ToString().IndexOf(EOF) != -1) {
                    break;
                }
            } while (bytes != 0);

            return messageData.ToString();
        }
    }
}
